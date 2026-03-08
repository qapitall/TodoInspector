using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TodoInspector
{
    internal class TodoScanner
    {
        internal event Action OnScanCompleted;
        internal bool IsScanning => _isScanning;

        private readonly string _assetsPath;
        private readonly string _excludePath;

        private readonly ConcurrentDictionary<string, List<TodoEntry>> _cache = new();
        private static readonly List<TodoEntry> EmptyEntryList = new(0);

        private CancellationTokenSource _cts;
        private volatile bool _isScanning;

        internal TodoScanner(string assetsPath, string excludePath)
        {
            _assetsPath = assetsPath;
            _excludePath = excludePath;
        }

        internal List<TodoEntry> GetAllEntries()
        {
            List<TodoEntry> result = new List<TodoEntry>(GetEntryCount());
            foreach (var kvp in _cache)
            {
                result.AddRange(kvp.Value);
            }

            return result;
        }

        internal int GetEntryCount()
        {
            int count = 0;
            foreach (var kvp in _cache)
            {
                count += kvp.Value.Count;
            }

            return count;
        }

        internal void RemoveFile(string filePath)
        {
            string normalized = NormalizePath(filePath);
            _cache.TryRemove(normalized, out _);
        }

        internal void ScanFileImmediate(string filePath)
        {
            string normalized = NormalizePath(filePath);

            if (!File.Exists(normalized))
            {
                _cache.TryRemove(normalized, out _);
                return;
            }

            List<TodoEntry> entries = ParseFile(normalized);

            if (entries.Count > 0)
                _cache[normalized] = entries;
            else
                _cache.TryRemove(normalized, out _);
        }

        internal void NotifyScanCompleted()
        {
            OnScanCompleted?.Invoke();
        }

        internal void StartFullScan()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;
            _isScanning = true;

            Task.Run(() =>
            {
                try
                {
                    ExecuteFullScan(token);
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    _isScanning = false;
                    OnScanCompleted?.Invoke();
                }
            }, token);
        }

        internal void CancelScan()
        {
            _cts?.Cancel();
        }

        private void ExecuteFullScan(CancellationToken token)
        {
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(_assetsPath, "*.cs", SearchOption.AllDirectories);
            }
            catch (Exception)
            {
                return;
            }

            ConcurrentDictionary<string, List<TodoEntry>> results = new ConcurrentDictionary<string, List<TodoEntry>>();

            Parallel.ForEach(files, new ParallelOptions
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            }, file =>
            {
                token.ThrowIfCancellationRequested();

                string normalized = NormalizePath(file);

                if (ShouldExclude(normalized))
                    return;

                List<TodoEntry> entries = ParseFile(normalized);
                if (entries.Count > 0)
                    results[normalized] = entries;
            });

            token.ThrowIfCancellationRequested();

            _cache.Clear();
            foreach (var kvp in results)
            {
                _cache[kvp.Key] = kvp.Value;
            }
        }

        private bool ShouldExclude(string normalizedPath)
        {
            if (string.IsNullOrEmpty(_excludePath))
                return false;

            return normalizedPath.StartsWith(_excludePath, StringComparison.OrdinalIgnoreCase);
        }

        private static List<TodoEntry> ParseFile(string filePath)
        {
            List<TodoEntry> entries = null;

            try
            {
                int lineNumber = 0;
                foreach (string line in File.ReadLines(filePath))
                {
                    lineNumber++;
                    if (TodoParser.TryParse(line, lineNumber, filePath, out TodoEntry entry))
                    {
                        entries ??= new List<TodoEntry>(4);
                        entries.Add(entry);
                    }
                }
            }
            catch (Exception)
            {
            }

            return entries ?? EmptyEntryList;
        }

        private static string NormalizePath(string path)
        {
            if (path.IndexOf('\\') < 0)
                return path;

            return path.Replace('\\', '/');
        }
    }
}