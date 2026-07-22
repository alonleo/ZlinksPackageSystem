using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ZlinksPackageSystem.Desktop.Services
{
    public class LocalCacheService : ILocalCacheService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _cacheDirectory;
        private readonly string _indexFile;
        private readonly SemaphoreSlim _ioLock = new(1, 1);

        private Dictionary<string, DateTime> _index = new(StringComparer.OrdinalIgnoreCase);
        private bool _indexLoaded;

        public LocalCacheService(string? directoryOverride = null)
        {
            _cacheDirectory = directoryOverride
                ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ZlinksPackageSystem",
                    "cache");
            _indexFile = Path.Combine(_cacheDirectory, "_index.json");
        }

        public string CacheDirectory => _cacheDirectory;

        public async Task<T?> LoadAsync<T>(string key) where T : class
        {
            if (string.IsNullOrWhiteSpace(key)) return null;

            var path = GetFilePath(key);
            if (!File.Exists(path)) return null;

            await _ioLock.WaitAsync();
            try
            {
                await using var fs = File.OpenRead(path);
                var entry = await JsonSerializer.DeserializeAsync<CacheEntry<T>>(fs, JsonOptions);
                return entry?.Data;
            }
            catch
            {
                return null;
            }
            finally
            {
                _ioLock.Release();
            }
        }

        public async Task SaveAsync<T>(string key, T data) where T : class
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            await _ioLock.WaitAsync();
            try
            {
                Directory.CreateDirectory(_cacheDirectory);

                var entry = new CacheEntry<T>
                {
                    Data = data,
                    LastUpdated = DateTime.Now
                };

                var path = GetFilePath(key);
                var tmp = path + ".tmp";
                await using (var fs = File.Create(tmp))
                {
                    await JsonSerializer.SerializeAsync(fs, entry, JsonOptions);
                }

                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);

                await UpdateIndexAsync(key, entry.LastUpdated);
            }
            catch
            {
            }
            finally
            {
                _ioLock.Release();
            }
        }

        public async Task<DateTime?> GetLastUpdatedAsync(string key)
        {
            await EnsureIndexLoadedAsync();
            if (_index.TryGetValue(key, out var dt)) return dt;
            return null;
        }

        public Task<bool> ExistsAsync(string key)
        {
            var path = GetFilePath(key);
            return Task.FromResult(File.Exists(path));
        }

        public async Task ClearAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            await _ioLock.WaitAsync();
            try
            {
                var path = GetFilePath(key);
                if (File.Exists(path)) File.Delete(path);

                await EnsureIndexLoadedAsync();
                _index.Remove(key);
                await PersistIndexAsync();
            }
            catch
            {
            }
            finally
            {
                _ioLock.Release();
            }
        }

        public async Task ClearAllAsync()
        {
            await _ioLock.WaitAsync();
            try
            {
                if (Directory.Exists(_cacheDirectory))
                {
                    foreach (var file in Directory.EnumerateFiles(_cacheDirectory))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
                _index.Clear();
                await PersistIndexAsync();
            }
            catch
            {
            }
            finally
            {
                _ioLock.Release();
            }
        }

        public IReadOnlyDictionary<string, DateTime> SnapshotIndex()
        {
            return _index;
        }

        private string GetFilePath(string key)
        {
            var safe = SanitizeKey(key);
            return Path.Combine(_cacheDirectory, safe + ".json");
        }

        private static string SanitizeKey(string key)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var arr = key.ToCharArray();
            for (int i = 0; i < arr.Length; i++)
            {
                if (Array.IndexOf(invalid, arr[i]) >= 0) arr[i] = '_';
            }
            return new string(arr);
        }

        private async Task EnsureIndexLoadedAsync()
        {
            if (_indexLoaded) return;
            await _ioLock.WaitAsync();
            try
            {
                if (_indexLoaded) return;
                if (File.Exists(_indexFile))
                {
                    try
                    {
                        await using var fs = File.OpenRead(_indexFile);
                        var dict = await JsonSerializer.DeserializeAsync<Dictionary<string, DateTime>>(fs, JsonOptions);
                        if (dict != null) _index = dict;
                    }
                    catch
                    {
                        _index = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
                    }
                }
                _indexLoaded = true;
            }
            finally
            {
                _ioLock.Release();
            }
        }

        private async Task UpdateIndexAsync(string key, DateTime when)
        {
            await EnsureIndexLoadedAsync();
            _index[key] = when;
            await PersistIndexAsync();
        }

        private async Task PersistIndexAsync()
        {
            try
            {
                Directory.CreateDirectory(_cacheDirectory);
                var tmp = _indexFile + ".tmp";
                await using (var fs = File.Create(tmp))
                {
                    await JsonSerializer.SerializeAsync(fs, _index, JsonOptions);
                }
                if (File.Exists(_indexFile)) File.Delete(_indexFile);
                File.Move(tmp, _indexFile);
            }
            catch
            {
            }
        }
    }
}
