using System.Threading.Tasks;

namespace ZlinksPackageSystem.Desktop.Services
{
    public interface IApiService
    {
        Task<T?> GetAsync<T>(string endpoint);
        Task<T?> PostAsync<T>(string endpoint, object? data = null);
        Task<T?> PutAsync<T>(string endpoint, object? data = null);
        Task<bool> DeleteAsync(string endpoint);
        void SetAuthToken(string token);
    }
}