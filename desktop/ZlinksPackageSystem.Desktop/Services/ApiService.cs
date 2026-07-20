using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly int _timeout;

        public ApiService(IConfiguration configuration)
        {
            _baseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:8080/api";
            _timeout = int.TryParse(configuration["Timeout"], out var t) ? t : 10000;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromMilliseconds(_timeout)
            };
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public string BaseUrl => _baseUrl;

        public void SetAuthToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<T?> GetAsync<T>(string endpoint) where T : class
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse<T>>(content);
                return result?.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GET {endpoint} failed: {ex.Message}");
                return default;
            }
        }

        public async Task<T?> PostAsync<T>(string endpoint, object? data = null) where T : class
        {
            try
            {
                var json = data != null ? JsonConvert.SerializeObject(data) : "{}";
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse<T>>(responseContent);
                return result?.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"POST {endpoint} failed: {ex.Message}");
                return default;
            }
        }

        public async Task<T?> PutAsync<T>(string endpoint, object? data = null) where T : class
        {
            try
            {
                var json = data != null ? JsonConvert.SerializeObject(data) : "{}";
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(endpoint, content);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse<T>>(responseContent);
                return result?.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PUT {endpoint} failed: {ex.Message}");
                return default;
            }
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(endpoint);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DELETE {endpoint} failed: {ex.Message}");
                return false;
            }
        }
    }
}
