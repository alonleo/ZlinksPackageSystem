using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ZlinksPackageSystem.Desktop.Models;
using ZlinksPackageSystem.Desktop.Services;

namespace ZlinksPackageSystem.Desktop.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private ViewModelBase? _currentViewModel;

        [ObservableProperty]
        private bool _isLoggedIn;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private IBrush _backgroundBrush = new SolidColorBrush(Color.FromRgb(30, 30, 46));

        [ObservableProperty]
        private double _backgroundOpacity = 1.0;

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZlinksPackageSystem", "settings.json");

        public MainViewModel(IAuthService authService, IServiceProvider serviceProvider, IDialogService dialogService)
        {
            _authService = authService;
            _serviceProvider = serviceProvider;
            _dialogService = dialogService;
            Title = "Zlinks Package System";
            CurrentViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
            LoadSettings();
        }

        public void LoadSettings()
        {
            try
            {
                AppSettings? settings = null;
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    settings = JsonSerializer.Deserialize<AppSettings>(json);
                }
                settings ??= new AppSettings();

                ApplyBackground(settings.BackgroundType, settings.BackgroundValue, settings.BackgroundOpacity);
            }
            catch
            {
                BackgroundBrush = new SolidColorBrush(Color.FromRgb(30, 30, 46));
                BackgroundOpacity = 1.0;
            }
        }

        public void ApplyBackground(int type, string value, double opacity)
        {
            BackgroundOpacity = opacity;

            if (type == 3 && !string.IsNullOrEmpty(value))
            {
                try
                {
                    var image = new Bitmap(value);
                    BackgroundBrush = new ImageBrush(image) { Stretch = Stretch.UniformToFill };
                    return;
                }
                catch
                {
                }
            }

            var startColor = type switch
            {
                1 => Color.FromRgb(15, 12, 41),
                2 => Color.FromRgb(26, 35, 53),
                _ => Color.FromRgb(30, 30, 46)
            };
            var endColor = type switch
            {
                1 => Color.FromRgb(36, 36, 62),
                2 => Color.FromRgb(30, 50, 70),
                _ => Color.FromRgb(30, 30, 46)
            };

            BackgroundBrush = type switch
            {
                1 or 2 => new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops = new GradientStops
                    {
                        new GradientStop { Color = startColor, Offset = 0.0 },
                        new GradientStop { Color = endColor, Offset = 1.0 }
                    }
                },
                _ => new SolidColorBrush(startColor)
            };
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (CurrentViewModel is LoginViewModel loginViewModel)
            {
                loginViewModel.HasError = false;
                loginViewModel.ErrorMessage = string.Empty;
                var success = await _authService.LoginAsync(loginViewModel.Username, loginViewModel.Password);
                if (success)
                {
                    if (loginViewModel.RememberPassword)
                        loginViewModel.SaveCredentials();
                    else
                        loginViewModel.ClearSavedCredentials();

                    IsLoggedIn = true;
                    var user = await _authService.GetCurrentUserAsync();
                    Username = user?.RealName ?? "User";
                    CurrentViewModel = _serviceProvider.GetRequiredService<HomeViewModel>();
                }
                else
                {
                    loginViewModel.ErrorMessage = "用户名或密码错误";
                    loginViewModel.HasError = true;
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
            CurrentViewModel = _serviceProvider.GetRequiredService<HomeViewModel>();
        }

        [RelayCommand]
        private void NavigateToGames()
        {
            CurrentViewModel = _serviceProvider.GetRequiredService<GameListViewModel>();
        }

        [RelayCommand]
        private void NavigateToProducts()
        {
            CurrentViewModel = _serviceProvider.GetRequiredService<ProductViewModel>();
        }

        [RelayCommand]
        private void NavigateToTests()
        {
            CurrentViewModel = _serviceProvider.GetRequiredService<TestViewModel>();
        }

        [RelayCommand]
        private void NavigateToToolLibrary()
        {
            CurrentViewModel = _serviceProvider.GetRequiredService<ToolLibraryViewModel>();
        }

        [RelayCommand]
        private void NavigateToNotification()
        {
            CurrentViewModel = _serviceProvider.GetRequiredService<NotificationViewModel>();
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            CurrentViewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
        }

        [RelayCommand]
        private async Task ForgotPasswordAsync()
        {
            await _dialogService.ShowMessageAsync("忘记密码", "请联系管理员重置密码。");
        }
    }
}
