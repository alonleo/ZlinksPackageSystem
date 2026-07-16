using System.Collections.Generic;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public class AuthService : IAuthService
    {
        private readonly IApiService _apiService;
        private string? _token;
        private User? _currentUser;
        private bool _useLocalAccount;

        public AuthService(IApiService apiService)
        {
            _apiService = apiService;
        }

        public bool IsAuthenticated => !string.IsNullOrEmpty(_token);
        public string? Token => _token;

        public async Task<bool> LoginAsync(string username, string password)
        {
            // Local test account fallback
            if (username == "admin" && password == "admin")
            {
                _token = "local-token";
                _useLocalAccount = true;
                _currentUser = new User
                {
                    Id = 1,
                    Username = "admin",
                    RealName = "管理员",
                    Status = "active",
                    GroupId = 1
                };
                return true;
            }

            var loginRequest = new { username, password };
            var token = await _apiService.PostAsync<string>("/auth/login", loginRequest);

            if (!string.IsNullOrEmpty(token))
            {
                _token = token;
                _useLocalAccount = false;
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
            _useLocalAccount = false;
            _apiService.SetAuthToken(string.Empty);
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            if (_useLocalAccount)
            {
                return _currentUser;
            }
            return await _apiService.GetAsync<User>("/auth/info");
        }
    }
}