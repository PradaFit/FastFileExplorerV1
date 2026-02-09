using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastest_FileExplorer.Core
{
    public class FileSystemCache : IDisposable
    {
        private readonly ConcurrentDictionary<string, CachedDirectory> _directoryCache;
        private readonly ConcurrentDictionary<string, DateTime> _cacheTimestamps;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
        private readonly SemaphoreSlim _refreshLock;
        private readonly int _maxCachedDirectories = 10000;
        private FileSystemWatcher _watcher;
        private string _currentWatchPath;

        public event EventHandler<DirectoryChangedEventArgs> DirectoryChanged;

        public FileSystemCache()
        {
            _directoryCache = new ConcurrentDictionary<string, CachedDirectory>(StringComparer.OrdinalIgnoreCase);
            _cacheTimestamps = new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            _refreshLock = new SemaphoreSlim(1, 1);
        }

        public async Task<DirectoryContents> GetDirectoryContentsAsync(string path, bool forceRefresh = false)
        {
            if (string.IsNullOrEmpty(path))
                return DirectoryContents.Empty;

            var normalizedPath = NormalizePath(path);

            if (!forceRefresh && TryGetFromCache(normalizedPath, out var cached))
            {
                if (IsCacheStale(normalizedPath))
                {
                    _ = RefreshCacheAsync(normalizedPath);
                }
                return ToDirectoryContents(cached);
            }

            return await LoadDirectoryAsync(normalizedPath).ConfigureAwait(false);
        }

        public DirectoryContents GetDirectoryContents(string path, bool forceRefresh = false)
        {
            if (string.IsNullOrEmpty(path))
                return DirectoryContents.Empty;

            var normalizedPath = NormalizePath(path);

            if (!forceRefresh && TryGetFromCache(normalizedPath, out var cached))
            {
                return ToDirectoryContents(cached);
            }

            return LoadDirectory(normalizedPath);
        }

        private bool TryGetFromCache(string path, out CachedDirectory cached)
        {
            return _directoryCache.TryGetValue(path, out cached) && cached != null;
        }

        private bool IsCacheStale(string path)
        {
            if (_cacheTimestamps.TryGetValue(path, out var timestamp))
            {
                return DateTime.UtcNow - timestamp > _cacheExpiration;
            }
            return true;
        }

        private async Task RefreshCacheAsync(string path)
        {
            if (!await _refreshLock.WaitAsync(0).ConfigureAwait(false))
                return;

            try
            {
                await Task.Run(() => LoadDirectory(path)).ConfigureAwait(false);
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private async Task<DirectoryContents> LoadDirectoryAsync(string path)
        {
            return await Task.Run(() => LoadDirectory(path)).ConfigureAwait(false);
        }

        private DirectoryContents LoadDirectory(string path)
        {
            var contents = new DirectoryContents { Path = path };

            try
            {
                var dirInfo = new DirectoryInfo(path);
                if (!dirInfo.Exists)
                    return contents;

                contents.Exists = true;

                var dirs = new List<FileSystemItem>();
                try
                {
                    foreach (var dir in dirInfo.EnumerateDirectories())
                    {
                        try
                        {
                            dirs.Add(new FileSystemItem
                            {
                                Name = dir.Name,
                                FullPath = dir.FullName,
                                IsDirectory = true,
                                LastModified = dir.LastWriteTime,
                                Created = dir.CreationTime,
                                Attributes = dir.Attributes
                            });
                        }
                        catch { }
                    }
                }
                catch { }

                var files = new List<FileSystemItem>();
                try
                {
                    foreach (var file in dirInfo.EnumerateFiles())
                    {
                        try
                        {
                            files.Add(new FileSystemItem
                            {
                                Name = file.Name,
                                FullPath = file.FullName,
                                IsDirectory = false,
                                Size = file.Length,
                                Extension = file.Extension,
                                LastModified = file.LastWriteTime,
                                Created = file.CreationTime,
                                Attributes = file.Attributes
                            });
                        }
                        catch { }
                    }
                }
                catch { }

                contents.Directories = dirs;
                contents.Files = files;

                UpdateCache(path, contents);

                return contents;
            }
            catch (UnauthorizedAccessException)
            {
                contents.AccessDenied = true;
                return contents;
            }
            catch (Exception)
            {
                return contents;
            }
        }

        private void UpdateCache(string path, DirectoryContents contents)
        {
            if (_directoryCache.Count > _maxCachedDirectories)
            {
                var oldest = _cacheTimestamps
                    .OrderBy(kvp => kvp.Value)
                    .Take(_maxCachedDirectories / 4)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in oldest)
                {
                    _directoryCache.TryRemove(key, out _);
                    _cacheTimestamps.TryRemove(key, out _);
                }
            }

            var cached = new CachedDirectory
            {
                Path = path,
                Directories = contents.Directories,
                Files = contents.Files,
                Exists = contents.Exists,
                AccessDenied = contents.AccessDenied
            };

            _directoryCache[path] = cached;
            _cacheTimestamps[path] = DateTime.UtcNow;
        }

        private DirectoryContents ToDirectoryContents(CachedDirectory cached)
        {
            return new DirectoryContents
            {
                Path = cached.Path,
                Directories = cached.Directories,
                Files = cached.Files,
                Exists = cached.Exists,
                AccessDenied = cached.AccessDenied
            };
        }

        public void WatchDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return;

            if (_currentWatchPath == path)
                return;

            StopWatching();

            try
            {
                _watcher = new FileSystemWatcher(path)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | 
                                   NotifyFilters.LastWrite | NotifyFilters.Size,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };

                _watcher.Created += OnFileSystemChanged;
                _watcher.Deleted += OnFileSystemChanged;
                _watcher.Renamed += OnFileSystemRenamed;
                _watcher.Changed += OnFileSystemChanged;

                _currentWatchPath = path;
            }
            catch { }
        }

        private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
        {
            InvalidateCache(Path.GetDirectoryName(e.FullPath));
            DirectoryChanged?.Invoke(this, new DirectoryChangedEventArgs(e.FullPath, e.ChangeType));
        }

        private void OnFileSystemRenamed(object sender, RenamedEventArgs e)
        {
            InvalidateCache(Path.GetDirectoryName(e.FullPath));
            DirectoryChanged?.Invoke(this, new DirectoryChangedEventArgs(e.FullPath, e.ChangeType));
        }

        public void InvalidateCache(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var normalizedPath = NormalizePath(path);
            _directoryCache.TryRemove(normalizedPath, out _);
            _cacheTimestamps.TryRemove(normalizedPath, out _);
        }

        public void ClearCache()
        {
            _directoryCache.Clear();
            _cacheTimestamps.Clear();
        }

        public void StopWatching()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
                _currentWatchPath = null;
            }
        }

        private string NormalizePath(string path)
        {
            return path.TrimEnd('\\', '/');
        }

        public List<DriveInfoItem> GetDrives()
        {
            var drives = new List<DriveInfoItem>();

            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    drives.Add(new DriveInfoItem
                    {
                        Name = drive.Name,
                        VolumeLabel = drive.IsReady ? drive.VolumeLabel : "",
                        DriveType = drive.DriveType,
                        IsReady = drive.IsReady,
                        TotalSize = drive.IsReady ? drive.TotalSize : 0,
                        FreeSpace = drive.IsReady ? drive.AvailableFreeSpace : 0,
                        DriveFormat = drive.IsReady ? drive.DriveFormat : ""
                    });
                }
                catch
                {
                    drives.Add(new DriveInfoItem
                    {
                        Name = drive.Name,
                        IsReady = false
                    });
                }
            }

            return drives;
        }

        public void Dispose()
        {
            StopWatching();
            _refreshLock?.Dispose();
        }
    }

    public class CachedDirectory
    {
        public string Path { get; set; }
        public List<FileSystemItem> Directories { get; set; } = new List<FileSystemItem>();
        public List<FileSystemItem> Files { get; set; } = new List<FileSystemItem>();
        public bool Exists { get; set; }
        public bool AccessDenied { get; set; }
    }

    public class DirectoryContents
    {
        public string Path { get; set; }
        public List<FileSystemItem> Directories { get; set; } = new List<FileSystemItem>();
        public List<FileSystemItem> Files { get; set; } = new List<FileSystemItem>();
        public bool Exists { get; set; }
        public bool AccessDenied { get; set; }

        public static DirectoryContents Empty => new DirectoryContents();

        public IEnumerable<FileSystemItem> AllItems => Directories.Concat(Files);
        public int TotalCount => Directories.Count + Files.Count;
    }

    public class FileSystemItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public string Extension { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime Created { get; set; }
        public FileAttributes Attributes { get; set; }

        public bool IsHidden => (Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
        public bool IsSystem => (Attributes & FileAttributes.System) == FileAttributes.System;
    }

    public class DriveInfoItem
    {
        public string Name { get; set; }
        public string VolumeLabel { get; set; }
        public DriveType DriveType { get; set; }
        public bool IsReady { get; set; }
        public long TotalSize { get; set; }
        public long FreeSpace { get; set; }
        public string DriveFormat { get; set; }

        public double UsedPercentage => TotalSize > 0 ? (double)(TotalSize - FreeSpace) / TotalSize * 100 : 0;

        public string DisplayName => string.IsNullOrEmpty(VolumeLabel) 
            ? $"Local Disk ({Name.TrimEnd('\\')})" 
            : $"{VolumeLabel} ({Name.TrimEnd('\\')})";
    }

    public class DirectoryChangedEventArgs : EventArgs
    {
        public string Path { get; }
        public WatcherChangeTypes ChangeType { get; }

        public DirectoryChangedEventArgs(string path, WatcherChangeTypes changeType)
        {
            Path = path;
            ChangeType = changeType;
        }
    }
}
