using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZlinksPackageSystem.Desktop.Services;

namespace ZlinksPackageSystem.Desktop.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private ViewModelBase? _currentViewModel;

        [ObservableProperty]
        private bool _isLoggedIn;

        [ObservableProperty]
        private string _username = string.Empty;

        public MainViewModel(IAuthService authService)
        {
            _authService = authService;
            Title = "Zlinks Package System";
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (CurrentViewModel is LoginViewModel loginViewModel)
            {
                var success = await _authService.LoginAsync(loginViewModel.Username, loginViewModel.Password);
                if (success)
                {
                    IsLoggedIn = true;
                    var user = await _authService.GetCurrentUserAsync();
                    Username = user?.RealName ?? "User";
                    CurrentViewModel = new HomeViewModel();
                }
            }
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            await _authService.LogoutAsync();
            IsLoggedIn = false;
            Username = string.Empty;
            CurrentViewModel = new LoginViewModel();
        }

        [RelayCommand]
        private void NavigateToHome()
        {
            CurrentViewModel = new HomeViewModel();
        }

        [RelayCommand]
        private void NavigateToGames()
        {
            CurrentViewModel = new GameListViewModel();
        }
    }
}