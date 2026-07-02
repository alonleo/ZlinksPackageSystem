using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public class AuthService : IAuthService
    {
        private readonly IApiService _apiService;
        private string? _token;
        private User? _currentUser;

        public AuthService(IApiService apiService)
        {
            _apiService = apiService;
        }

        public bool IsAuthenticated => !string.IsNullOrEmpty(_token);
        public string? Token => _token;

        public async Task<bool> LoginAsync(string username, string password)
        {
            var loginRequest = new { username, password };
            var token = await _apiService.PostAsync<string>("/auth/login", loginRequest);

            if (!string.IsNullOrEmpty(token))
            {
                _token = token;
                _apiService.SetAuthToken(token);
                _currentUser = await GetCurrentUserAsync();
                return true;
            }

            return false;
        }

        public async Task LogoutAsync()
        {
            _token = null;
            _currentUser = null;
            _apiService.SetAuthToken(string.Empty);
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            return await _apiService.GetAsync<User>("/auth/info");
        }
    }
}