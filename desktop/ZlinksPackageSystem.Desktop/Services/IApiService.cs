using System.Threading.Tasks;

namespace ZlinksPackageSystem.Desktop.Services
{
    public interface IApiService
    {
        Task<T?> GetAsync<T>(string endpoint) where T : class;
        Task<T?> PostAsync<T>(string endpoint, object? data = null) where T : class;
        Task<T?> PutAsync<T>(string endpoint, object? data = null) where T : class;
        Task<bool> DeleteAsync(string endpoint);
        void SetAuthToken(string token);
    }
}