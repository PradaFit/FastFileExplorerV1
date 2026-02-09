using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Fastest_FileExplorer.Core
{
    public class SearchEngine
    {
        private readonly FileIndexer _indexer;
        private readonly int _maxDegreeOfParallelism;
        private CancellationTokenSource _searchCts;

        public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

        public SearchEngine(FileIndexer indexer)
        {
            _indexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
            _maxDegreeOfParallelism = Environment.ProcessorCount;
        }

        public async Task<List<SearchResult>> SearchAsync(SearchQuery query, CancellationToken externalToken = default)
        {
            CancelCurrentSearch();
            _searchCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var token = _searchCts.Token;

            var results = new ConcurrentBag<SearchResult>();
            var startTime = DateTime.UtcNow;

            await Task.Run(() =>
            {
                try
                {
                    if (_indexer.TotalFilesIndexed > 0)
                    {
                        var indexedResults = SearchIndex(query, token);
                        foreach (var result in indexedResults)
                        {
                            if (token.IsCancellationRequested) break;
                            results.Add(result);
                        }
                    }

                    if (results.Count < query.MaxResults && !string.IsNullOrEmpty(query.SearchPath))
                    {
                        var fsResults = SearchFileSystem(query, token);
                        foreach (var result in fsResults)
                        {
                            if (token.IsCancellationRequested) break;
                            if (results.Count >= query.MaxResults) break;
                            results.Add(result);
                        }
                    }
                }
                catch (OperationCanceledException) { }
            }, token).ConfigureAwait(false);

            var elapsed = DateTime.UtcNow - startTime;
            var resultList = results.Take(query.MaxResults).ToList();

            SearchCompleted?.Invoke(this, new SearchCompletedEventArgs(resultList.Count, elapsed));
            return resultList;
        }

        public async Task SearchRealtimeAsync(SearchQuery query, Action<SearchResult> onResultFound, CancellationToken externalToken = default)
        {
            CancelCurrentSearch();
            _searchCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var token = _searchCts.Token;
            var resultCount = 0;

            await Task.Run(() =>
            {
                if (_indexer.TotalFilesIndexed > 0)
                {
                    foreach (var result in SearchIndex(query, token))
                    {
                        if (token.IsCancellationRequested) return;
                        if (resultCount >= query.MaxResults) return;
                        
                        onResultFound(result);
                        resultCount++;
                    }
                }
            }, token).ConfigureAwait(false);
        }

        private IEnumerable<SearchResult> SearchIndex(SearchQuery query, CancellationToken token)
        {
            var queryLower = query.SearchText.ToLowerInvariant();
            var terms = queryLower.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var useRegex = query.UseRegex && IsValidRegex(query.SearchText);
            Regex regex = null;

            if (useRegex)
            {
                try
                {
                    regex = new Regex(query.SearchText, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                }
                catch
                {
                    useRegex = false;
                }
            }

            var indexedFiles = _indexer.Search(query.SearchText, query.MaxResults * 2);

            foreach (var file in indexedFiles)
            {
                if (token.IsCancellationRequested) yield break;

                if (!PassesFilters(file, query)) continue;

                var score = CalculateRelevanceScore(file, terms, queryLower);

                yield return new SearchResult
                {
                    FullPath = file.FullPath,
                    Name = file.Name,
                    Extension = file.Extension,
                    Size = file.Size,
                    LastModified = file.LastModified,
                    RelevanceScore = score,
                    IsDirectory = false
                };
            }
        }

        private IEnumerable<SearchResult> SearchFileSystem(SearchQuery query, CancellationToken token)
        {
            var searchPath = query.SearchPath;
            if (string.IsNullOrEmpty(searchPath) || !Directory.Exists(searchPath))
                yield break;

            var searchPattern = "*" + query.SearchText + "*";
            var searchOption = query.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(searchPath, searchPattern, searchOption);
            }
            catch
            {
                yield break;
            }

            foreach (var filePath in files)
            {
                if (token.IsCancellationRequested) yield break;

                FileInfo fileInfo;
                try
                {
                    fileInfo = new FileInfo(filePath);
                    if (!fileInfo.Exists) continue;
                }
                catch
                {
                    continue;
                }

                var indexed = new IndexedFile
                {
                    FullPath = filePath,
                    Name = fileInfo.Name,
                    NameLower = fileInfo.Name.ToLowerInvariant(),
                    Extension = fileInfo.Extension.ToLowerInvariant(),
                    Size = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTimeUtc
                };

                if (!PassesFilters(indexed, query)) continue;

                yield return new SearchResult
                {
                    FullPath = filePath,
                    Name = fileInfo.Name,
                    Extension = fileInfo.Extension,
                    Size = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTimeUtc,
                    RelevanceScore = 50,
                    IsDirectory = false
                };
            }
        }

        private bool PassesFilters(IndexedFile file, SearchQuery query)
        {
            if (query.MinSize.HasValue && file.Size < query.MinSize.Value)
                return false;
            if (query.MaxSize.HasValue && file.Size > query.MaxSize.Value)
                return false;

            if (query.ModifiedAfter.HasValue && file.LastModified < query.ModifiedAfter.Value)
                return false;
            if (query.ModifiedBefore.HasValue && file.LastModified > query.ModifiedBefore.Value)
                return false;

            if (query.Extensions != null && query.Extensions.Length > 0)
            {
                if (!query.Extensions.Any(ext => 
                    file.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase) ||
                    file.Extension.Equals("." + ext, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            return true;
        }

        private int CalculateRelevanceScore(IndexedFile file, string[] terms, string query)
        {
            var score = 0;

            if (file.NameLower.Equals(query))
                score += 100;
            else if (file.NameLower.StartsWith(query))
                score += 80;
            else if (file.NameLower.Contains(query))
                score += 60;

            if (terms.All(t => file.NameLower.Contains(t)))
                score += 40;

            if (file.Name.Length < 20)
                score += 10;

            if (file.LastModified > DateTime.UtcNow.AddDays(-30))
                score += 5;

            return score;
        }

        private bool IsValidRegex(string pattern)
        {
            try
            {
                var _ = new Regex(pattern);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void CancelCurrentSearch()
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = null;
        }
    }

    public class SearchQuery
    {
        public string SearchText { get; set; }
        public string SearchPath { get; set; }
        public bool IncludeSubdirectories { get; set; } = true;
        public bool UseRegex { get; set; } = false;
        public bool CaseSensitive { get; set; } = false;
        public int MaxResults { get; set; } = 10000;
        public long? MinSize { get; set; }
        public long? MaxSize { get; set; }
        public DateTime? ModifiedAfter { get; set; }
        public DateTime? ModifiedBefore { get; set; }
        public string[] Extensions { get; set; }
    }

    public class SearchResult
    {
        public string FullPath { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public int RelevanceScore { get; set; }
        public bool IsDirectory { get; set; }
    }

    public class SearchCompletedEventArgs : EventArgs
    {
        public int TotalResults { get; }
        public TimeSpan ElapsedTime { get; }
        public SearchCompletedEventArgs(int totalResults, TimeSpan elapsedTime)
        {
            TotalResults = totalResults;
            ElapsedTime = elapsedTime;
        }
    }
}
