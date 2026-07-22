using System;
using System.Threading.Tasks;

namespace ZlinksPackageSystem.Desktop.Services
{
    public class CacheEntry<T>
    {
        public T? Data { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public interface ILocalCacheService
    {
        string CacheDirectory { get; }

        Task<T?> LoadAsync<T>(string key) where T : class;

        Task SaveAsync<T>(string key, T data) where T : class;

        Task<DateTime?> GetLastUpdatedAsync(string key);

        Task<bool> ExistsAsync(string key);

        Task ClearAsync(string key);

        Task ClearAllAsync();
    }
}
