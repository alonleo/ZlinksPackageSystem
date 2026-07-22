using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ZlinksPackageSystem.Desktop.Models;
using ZlinksPackageSystem.Desktop.Services;
using ZlinksPackageSystem.Desktop.Views;

namespace ZlinksPackageSystem.Desktop.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly IServiceProvider _serviceProvider;
        private User? _currentUser;
        private readonly IDialogService _dialogService;
        private readonly INetworkStatusService _networkService;
        private int _lastBackgroundType;
        private string _lastBackgroundValue = string.Empty;
        private bool _startupCompleted;
        private bool _wasOffline;

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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanChangeAccount))]
        private bool _isOfflineMode;

        [ObservableProperty]
        private string _connectionStatusText = "在线";

        [ObservableProperty]
        private IBrush _connectionStatusBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanLoginToServer))]
        [NotifyPropertyChangedFor(nameof(CanChangeAccount))]
        [NotifyPropertyChangedFor(nameof(PrimaryRole))]
        [NotifyPropertyChangedFor(nameof(DisplayUsername))]
        private bool _isServerLoggedIn;

        public bool CanLoginToServer => !IsServerLoggedIn;

        public bool CanChangeAccount => IsServerLoggedIn && !IsOfflineMode;

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZlinksPackageSystem", "settings.json");

        public MainViewModel(
            IAuthService authService,
            IServiceProvider serviceProvider,
            IDialogService dialogService,
            INetworkStatusService networkService)
        {
            _authService = authService;
            _serviceProvider = serviceProvider;
            _dialogService = dialogService;
            _networkService = networkService;
            Title = "Zlinks Package System";
            LoadSettings();
            SettingsViewModel.ThemeChanged += OnThemeChanged;
            _networkService.StatusChanged += OnNetworkStatusChanged;
            UpdateConnectionUi(_networkService.IsOnline);
            CurrentViewModel = _serviceProvider.GetRequiredService<HomeViewModel>();
        }

        private void OnThemeChanged(bool isDark)
        {
            ApplyBackground(_lastBackgroundType, _lastBackgroundValue, BackgroundOpacity);
        }

        public User? CurrentUser
        {
            get => _currentUser;
            private set
            {
                if (SetProperty(ref _currentUser, value))
                {
                    OnPropertyChanged(nameof(IsHomeVisible));
                    OnPropertyChanged(nameof(IsGamesVisible));
                    OnPropertyChanged(nameof(IsProductsVisible));
                    OnPropertyChanged(nameof(IsParametersVisible));
                    OnPropertyChanged(nameof(IsTestsVisible));
                    OnPropertyChanged(nameof(IsToolLibraryVisible));
                    OnPropertyChanged(nameof(IsNotificationVisible));
                    OnPropertyChanged(nameof(IsSettingsVisible));
                    OnPropertyChanged(nameof(PrimaryRole));
                    OnPropertyChanged(nameof(DisplayUsername));
                }
            }
        }

        public string PrimaryRole =>
            IsServerLoggedIn
                ? (CurrentUser?.GroupNames?.FirstOrDefault() ?? "普通用户")
                : "本地";

        public string DisplayUsername =>
            IsServerLoggedIn
                ? (CurrentUser?.Username ?? "-")
                : "本地用户";

        public bool IsHomeVisible => CheckModule("home");
        public bool IsGamesVisible => CheckModule("games");
        public bool IsProductsVisible => CheckModule("products");
        public bool IsParametersVisible => CheckModule("parameters");
        public bool IsTestsVisible => CheckModule("tests");
        public bool IsToolLibraryVisible => CheckModule("tool-library");
        public bool IsNotificationVisible => CheckModule("notification");
        public bool IsSettingsVisible => true;  // 设置对所有登录用户始终可见

        private bool CheckModule(string key)
        {
            var user = CurrentUser;
            if (user == null) return true;
            var mods = user.DesktopModules;
            if (mods == null || mods.Count == 0) return true;
            if (mods.Contains("all")) return true;
            return mods.Contains(key);
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
            _lastBackgroundType = type;
            _lastBackgroundValue = value;
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

            var isDark = Application.Current?.RequestedThemeVariant != ThemeVariant.Light;

            var startColor = type switch
            {
                1 => isDark ? Color.FromRgb(15, 12, 41) : Color.FromRgb(220, 225, 240),
                2 => isDark ? Color.FromRgb(26, 35, 53) : Color.FromRgb(230, 235, 245),
                _ => isDark ? Color.FromRgb(30, 30, 46) : Color.FromRgb(250, 250, 250)
            };
            var endColor = type switch
            {
                1 => isDark ? Color.FromRgb(36, 36, 62) : Color.FromRgb(240, 242, 250),
                2 => isDark ? Color.FromRgb(30, 50, 70) : Color.FromRgb(240, 245, 255),
                _ => isDark ? Color.FromRgb(30, 30, 46) : Color.FromRgb(250, 250, 250)
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
        private async Task LoginToServerAsync()
        {
            var online = await _networkService.CheckConnectivityAsync();
            if (!online)
            {
                var msg = _networkService.IsLocalMode
                    ? "无法连接到远程服务器，请检查服务器是否已启动。"
                    : "当前处于离线模式，无法连接远程服务器。";
                await _dialogService.ShowMessageAsync("无法连接", msg);
                return;
            }

            CurrentViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (CurrentViewModel is LoginViewModel loginViewModel)
            {
                loginViewModel.HasError = false;
                loginViewModel.ErrorMessage = string.Empty;

                if (!_networkService.IsOnline)
                {
                    var canLoginOffline = loginViewModel.Username == "admin" && loginViewModel.Password == "admin";
                    if (!canLoginOffline)
                    {
                        loginViewModel.ErrorMessage = "当前处于离线模式,仅支持 admin/admin 本地账号登录";
                        loginViewModel.HasError = true;
                        return;
                    }
                }

                var success = await _authService.LoginAsync(loginViewModel.Username, loginViewModel.Password);
                if (success)
                {
                    if (loginViewModel.RememberPassword)
                        loginViewModel.SaveCredentials();
                    else
                        loginViewModel.ClearSavedCredentials();

                    IsServerLoggedIn = true;
                    IsLoggedIn = true;
                    IsOfflineMode = false;
                    _wasOffline = false;
                    var user = await _authService.GetCurrentUserAsync();
                    CurrentUser = user;
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
            IsServerLoggedIn = false;
            IsLoggedIn = false;
            CurrentUser = null;
            Username = string.Empty;
            IsOfflineMode = false;
            _wasOffline = false;
            CurrentViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
        }

        [RelayCommand]
        private void ExitApplication()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }

        public async Task InitializeAsync()
        {
            if (_startupCompleted) return;
            _startupCompleted = true;

            var online = await _networkService.CheckConnectivityAsync();
            UpdateConnectionUi(online);

            if (!online)
            {
                EnterOfflineMode();
            }
        }

        private void EnterOfflineMode()
        {
            IsOfflineMode = true;
            _wasOffline = true;
            CurrentViewModel = _serviceProvider.GetRequiredService<HomeViewModel>();
        }

        private void OnNetworkStatusChanged(object? sender, bool online)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                UpdateConnectionUi(online);

                if (online && _wasOffline)
                {
                    _wasOffline = false;
                    IsOfflineMode = false;
                    if (CurrentUser == null && CurrentViewModel is not LoginViewModel)
                    {
                        var confirmed = await _dialogService.ShowConfirmAsync(
                            "网络已恢复",
                            "网络连接已恢复,是否立即登录?",
                            "立即登录",
                            "稍后");
                        if (confirmed)
                        {
                            IsLoggedIn = false;
                            Username = string.Empty;
                            CurrentViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
                        }
                    }
                }
                else if (!online)
                {
                    if (!_wasOffline && CurrentViewModel is not LoginViewModel && CurrentUser == null)
                    {
                        EnterOfflineMode();
                    }
                    else
                    {
                        IsOfflineMode = true;
                        _wasOffline = true;
                    }
                }
            });
        }

        private void UpdateConnectionUi(bool online)
        {
            IsOfflineMode = !online;
            if (online)
            {
                ConnectionStatusText = "在线";
                ConnectionStatusBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            }
            else if (_networkService.IsLocalMode)
            {
                ConnectionStatusText = "本地模式";
                ConnectionStatusBrush = new SolidColorBrush(Color.FromRgb(230, 162, 60));
            }
            else
            {
                ConnectionStatusText = "离线模式";
                ConnectionStatusBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));
            }
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
        private void NavigateToParameters()
        {
            CurrentViewModel = _serviceProvider.GetRequiredService<ParameterViewModel>();
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

        [RelayCommand]
        private async Task ChangeAccountAsync()
        {
            if (CurrentUser == null) return;

            var dialog = new ChangeAccountDialog
            {
                DataContext = new ChangeAccountViewModel(_authService),
            };

            if (dialog.DataContext is ChangeAccountViewModel vm)
            {
                vm.Initialize(CurrentUser.Username);
            }

            var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (owner == null) return;

            await dialog.ShowDialog(owner);

            if (dialog.Succeeded)
            {
                CurrentUser = await _authService.GetCurrentUserAsync();
                Username = CurrentUser?.RealName ?? "User";
                await _dialogService.ShowMessageAsync("成功", "账号信息修改成功,需重新登录。");
                await LogoutAsync();
            }
        }
    }
}
