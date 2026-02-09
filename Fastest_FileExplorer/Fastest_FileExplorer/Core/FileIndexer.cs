using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Fastest_FileExplorer.Core
{
    public class FileIndexer : IDisposable
    {
        private readonly ConcurrentDictionary<string, IndexedFile>[] _indexPartitions;
        private readonly ConcurrentDictionary<string, byte> _processedDirs;
        private readonly ConcurrentQueue<string> _directoryQueue;
        private CancellationTokenSource _cts;
        private readonly int _workerCount;
        private long _totalFilesIndexed;
        private long _directoriesProcessed;
        private long _activeWorkers;
        private volatile bool _isCompleted;

        private readonly ConcurrentBag<List<IndexedFile>> _batchPool = new ConcurrentBag<List<IndexedFile>>();
        private const int BatchSize = 5000;
        private const int PartitionCount = 16;

        private readonly ConcurrentDictionary<string, string> _extensionCache = 
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public event EventHandler<IndexProgressEventArgs> ProgressChanged;
        public event EventHandler IndexingCompleted;

        public long TotalFilesIndexed => Interlocked.Read(ref _totalFilesIndexed);
        public long DirectoriesProcessed => Interlocked.Read(ref _directoriesProcessed);
        public bool IsIndexing { get; private set; }

        public FileIndexer()
        {
            _indexPartitions = new ConcurrentDictionary<string, IndexedFile>[PartitionCount];
            for (int i = 0; i < PartitionCount; i++)
            {
                _indexPartitions[i] = new ConcurrentDictionary<string, IndexedFile>(
                    Environment.ProcessorCount * 4,
                    1500000,
                    StringComparer.OrdinalIgnoreCase);
            }

            _processedDirs = new ConcurrentDictionary<string, byte>(
                Environment.ProcessorCount * 8,
                1000000,
                StringComparer.OrdinalIgnoreCase);
            _directoryQueue = new ConcurrentQueue<string>();
            
            _workerCount = Math.Max(Environment.ProcessorCount * 12, 64);
            
            for (int i = 0; i < _workerCount; i++)
            {
                _batchPool.Add(new List<IndexedFile>(BatchSize));
            }

            // Pre-cache common extensions
            var commonExt = new[] { ".txt", ".dll", ".exe", ".log", ".xml", ".json", ".cs", ".js", 
                ".html", ".css", ".png", ".jpg", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx",
                ".zip", ".rar", ".mp3", ".mp4", ".wav", ".avi", ".mkv", ".ini", ".cfg", ".dat",
                ".sys", ".tmp", ".bak", ".old", ".pdb", ".cache", ".db", ".sqlite", ".md", "" };
            foreach (var ext in commonExt)
            {
                _extensionCache[ext] = ext;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetPartitionIndex(string path)
        {
            unchecked
            {
                int hash = 17;
                var len = Math.Min(path.Length, 64);
                for (int i = 0; i < len; i++)
                {
                    hash = hash * 31 + char.ToLowerInvariant(path[i]);
                }
                return (hash & 0x7FFFFFFF) % PartitionCount;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string InternExtension(string ext)
        {
            if (string.IsNullOrEmpty(ext)) return "";
            return _extensionCache.GetOrAdd(ext, e => e.ToLowerInvariant());
        }

        public async Task StartFullIndexAsync(CancellationToken externalToken = default)
        {
            if (IsIndexing) return;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            IsIndexing = true;
            _isCompleted = false;

            // clean slate
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true, true);

            try
            {
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                    .Select(d => d.RootDirectory.FullName)
                    .ToArray();

                foreach (var drive in drives)
                {
                    _directoryQueue.Enqueue(drive);
                }

                await Task.Run(() => SeedDirectoriesDeep(drives, 2), _cts.Token);

                var workers = new Task[_workerCount];
                for (int i = 0; i < _workerCount; i++)
                {
                    var workerId = i;
                    workers[i] = Task.Factory.StartNew(
                        () => IndexWorkerOptimized(_cts.Token, workerId),
                        _cts.Token,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default);
                }

                await Task.WhenAll(workers).ConfigureAwait(false);
                IndexingCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException) { }
            catch { }
            finally
            {
                IsIndexing = false;
                _isCompleted = true;
            }
        }

        private void SeedDirectoriesDeep(string[] drives, int depth)
        {
            var currentLevel = new List<string>(drives);
            
            for (int level = 0; level < depth && currentLevel.Count > 0; level++)
            {
                var nextLevel = new ConcurrentBag<string>();
                
                Parallel.ForEach(currentLevel, new ParallelOptions { MaxDegreeOfParallelism = 16 }, dir =>
                {
                    try
                    {
                        if (!_processedDirs.TryAdd(dir, 0)) return;

                        NativeFileEnumerator.EnumerateDirectory(
                            dir,
                            null,
                            (dirPath, dirName) =>
                            {
                                if (!ShouldSkipDirectoryFast(dirName))
                                {
                                    nextLevel.Add(dirPath);
                                    _directoryQueue.Enqueue(dirPath);
                                }
                            },
                            () => false);
                    }
                    catch { }
                });

                currentLevel = nextLevel.ToList();
            }
        }

        private void IndexWorkerOptimized(CancellationToken token, int workerId)
        {
            Interlocked.Increment(ref _activeWorkers);

            if (!_batchPool.TryTake(out var localBatch))
                localBatch = new List<IndexedFile>(BatchSize);

            try
            {
                var spinWait = new SpinWait();
                var idleCount = 0;
                var localFileCount = 0L;
                var flushThreshold = BatchSize;

                while (!token.IsCancellationRequested && !_isCompleted)
                {
                    if (_directoryQueue.TryDequeue(out var path))
                    {
                        idleCount = 0;
                        ProcessDirectoryFast(path, token, localBatch);
                        
                        if (localBatch.Count >= flushThreshold)
                        {
                            FlushBatchFast(localBatch, ref localFileCount);
                        }
                    }
                    else
                    {
                        if (localBatch.Count > 0)
                        {
                            FlushBatchFast(localBatch, ref localFileCount);
                        }

                        idleCount++;
                        
                        if (idleCount > 300 && Interlocked.Read(ref _activeWorkers) > 4)
                        {
                            break;
                        }

                        if (idleCount < 50)
                        {
                            spinWait.SpinOnce();
                        }
                        else if (idleCount < 150)
                        {
                            Thread.SpinWait(20);
                        }
                        else
                        {
                            Thread.Sleep(0);
                        }
                    }
                }

                if (localBatch.Count > 0)
                {
                    FlushBatchFast(localBatch, ref localFileCount);
                }
            }
            finally
            {
                localBatch.Clear();
                _batchPool.Add(localBatch);
                Interlocked.Decrement(ref _activeWorkers);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FlushBatchFast(List<IndexedFile> batch, ref long localCount)
        {
            var added = 0;
            
            foreach (var file in batch)
            {
                var partition = GetPartitionIndex(file.FullPath);
                if (_indexPartitions[partition].TryAdd(file.FullPath, file))
                {
                    added++;
                }
            }
            
            localCount += added;
            var total = Interlocked.Add(ref _totalFilesIndexed, added);
            batch.Clear();

            if (total % 500000 == 0 && total > 0)
            {
                ProgressChanged?.Invoke(this, new IndexProgressEventArgs(total, "Indexing..."));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessDirectoryFast(string path, CancellationToken token, List<IndexedFile> batch)
        {
            if (token.IsCancellationRequested) return;
            if (!_processedDirs.TryAdd(path, 0)) return;

            Interlocked.Increment(ref _directoriesProcessed);

            NativeFileEnumerator.EnumerateDirectory(
                path,
                (fullPath, name, size, lastWrite, created, isDir) =>
                {
                    string ext = "";
                    var lastDot = name.LastIndexOf('.');
                    if (lastDot > 0 && lastDot < name.Length - 1)
                    {
                        ext = InternExtension(name.Substring(lastDot));
                    }

                    batch.Add(new IndexedFile
                    {
                        FullPath = fullPath,
                        Name = name,
                        NameLower = name.ToLowerInvariant(),
                        Extension = ext,
                        Size = size,
                        LastModified = lastWrite,
                        Created = created
                    });
                },
                (dirPath, dirName) =>
                {
                    if (!ShouldSkipDirectoryFast(dirName))
                    {
                        _directoryQueue.Enqueue(dirPath);
                    }
                },
                () => token.IsCancellationRequested
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldSkipDirectoryFast(string name)
        {
            if (string.IsNullOrEmpty(name)) return true;

            var firstChar = name[0];
            if (firstChar == '$' || firstChar == '.') return true;
            
            var len = name.Length;
            
            switch (len)
            {
                case 7:
                    return string.Equals(name, "Windows", StringComparison.OrdinalIgnoreCase);
                case 8:
                    return string.Equals(name, "Recovery", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(name, "PerfLogs", StringComparison.OrdinalIgnoreCase);
                case 10:
                    return string.Equals(name, "Config.Msi", StringComparison.OrdinalIgnoreCase);
                case 11:
                    return string.Equals(name, "ProgramData", StringComparison.OrdinalIgnoreCase);
                case 12:
                    return string.Equals(name, "$RECYCLE.BIN", StringComparison.OrdinalIgnoreCase);
                case 25:
                    return string.Equals(name, "System Volume Information", StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }

        public async Task IndexDirectoryAsync(string path, bool recursive = true, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return;

            await Task.Run(() =>
            {
                if (recursive)
                {
                    NativeFileEnumerator.EnumerateFilesRecursive(
                        path,
                        (fullPath, name, size, lastWrite, created) =>
                        {
                            var ext = "";
                            var lastDot = name.LastIndexOf('.');
                            if (lastDot > 0) ext = InternExtension(name.Substring(lastDot));
                            
                            var file = new IndexedFile
                            {
                                FullPath = fullPath,
                                Name = name,
                                NameLower = name.ToLowerInvariant(),
                                Extension = ext,
                                Size = size,
                                LastModified = lastWrite,
                                Created = created
                            };

                            var partition = GetPartitionIndex(fullPath);
                            if (_indexPartitions[partition].TryAdd(fullPath, file))
                            {
                                Interlocked.Increment(ref _totalFilesIndexed);
                            }
                        },
                        ShouldSkipDirectoryFast,
                        () => token.IsCancellationRequested
                    );
                }
                else
                {
                    NativeFileEnumerator.EnumerateDirectory(
                        path,
                        (fullPath, name, size, lastWrite, created, isDir) =>
                        {
                            var ext = "";
                            var lastDot = name.LastIndexOf('.');
                            if (lastDot > 0) ext = InternExtension(name.Substring(lastDot));
                            
                            var file = new IndexedFile
                            {
                                FullPath = fullPath,
                                Name = name,
                                NameLower = name.ToLowerInvariant(),
                                Extension = ext,
                                Size = size,
                                LastModified = lastWrite,
                                Created = created
                            };

                            var partition = GetPartitionIndex(fullPath);
                            if (_indexPartitions[partition].TryAdd(fullPath, file))
                            {
                                Interlocked.Increment(ref _totalFilesIndexed);
                            }
                        },
                        null,
                        () => token.IsCancellationRequested
                    );
                }
            }, token).ConfigureAwait(false);
        }

        public IEnumerable<IndexedFile> Search(string query, int maxResults = 1000)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Enumerable.Empty<IndexedFile>();

            var queryLower = query.ToLowerInvariant();
            var terms = queryLower.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return _indexPartitions
                .AsParallel()
                .WithDegreeOfParallelism(PartitionCount)
                .SelectMany(p => p.Values)
                .Where(f => MatchesTerms(f, terms))
                .Take(maxResults);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchesTerms(IndexedFile file, string[] terms)
        {
            for (int i = 0; i < terms.Length; i++)
            {
                var term = terms[i];
                if (file.NameLower.IndexOf(term, StringComparison.Ordinal) < 0 &&
                    file.FullPath.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
            }
            return true;
        }

        public IEnumerable<IndexedFile> GetFilesInDirectory(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath)) return Enumerable.Empty<IndexedFile>();

            var normalizedPath = directoryPath.TrimEnd('\\');
            
            return _indexPartitions
                .AsParallel()
                .SelectMany(p => p.Values)
                .Where(f =>
                {
                    var dir = Path.GetDirectoryName(f.FullPath);
                    return dir != null && dir.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase);
                });
        }

        public void StopIndexing()
        {
            _isCompleted = true;
            _cts?.Cancel();
        }

        public void ClearIndex()
        {
            foreach (var partition in _indexPartitions)
            {
                partition.Clear();
            }
            _processedDirs.Clear();
            Interlocked.Exchange(ref _totalFilesIndexed, 0);
            Interlocked.Exchange(ref _directoriesProcessed, 0);
        }

        public void Dispose()
        {
            StopIndexing();
            _cts?.Dispose();
        }
    }

    public class IndexedFile
    {
        public string FullPath { get; set; }
        public string Name { get; set; }
        public string NameLower { get; set; }
        public string Extension { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime Created { get; set; }
    }

    public class IndexProgressEventArgs : EventArgs
    {
        public long FilesIndexed { get; }
        public string CurrentPath { get; }

        public IndexProgressEventArgs(long filesIndexed, string currentPath)
        {
            FilesIndexed = filesIndexed;
            CurrentPath = currentPath;
        }
    }
}
