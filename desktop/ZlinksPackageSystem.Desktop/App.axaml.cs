using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZlinksPackageSystem.Desktop.Services;
using ZlinksPackageSystem.Desktop.ViewModels;
using ZlinksPackageSystem.Desktop.Views;

namespace ZlinksPackageSystem.Desktop;

public partial class App : Application
{
    private readonly IHost _host;

    public static IServiceProvider? Services => ((App?)Current)?._host.Services;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                // 加载 appsettings.json(从输出目录)
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
            })
            .ConfigureServices((context, services) =>
            {
                // Services
                services.AddSingleton<IApiService, ApiService>();
                services.AddSingleton<IAuthService, AuthService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<IFilePickerService, FilePickerService>();
                services.AddSingleton<IRuntimeEnvironmentService, RuntimeEnvironmentService>();
                services.AddSingleton<IProcessManagerService, ProcessManagerService>();
                services.AddSingleton<IGitService, GitService>();
                services.AddSingleton<IVenvService, VenvService>();
                services.AddSingleton<IToolPersistenceService, ToolPersistenceService>();
                services.AddSingleton<IGlobalNotificationService, GlobalNotificationService>();
                services.AddSingleton<INotificationService, NotificationService>();

                // ViewModels
                services.AddSingleton<MainViewModel>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<HomeViewModel>();
                services.AddTransient<GameListViewModel>();
                services.AddTransient<ProductViewModel>(sp =>
                                    new ProductViewModel(
                                        sp.GetRequiredService<IApiService>(),
                                        sp.GetRequiredService<IDialogService>()));
                services.AddTransient<ParameterViewModel>(sp =>
                                    new ParameterViewModel(
                                        sp.GetRequiredService<IApiService>(),
                                        sp.GetRequiredService<IDialogService>()));
                services.AddTransient<TestViewModel>();
                services.AddTransient<ToolLibraryViewModel>();
                services.AddTransient<NotificationViewModel>();
                services.AddTransient<SettingsViewModel>();

                // Views
                services.AddTransient<MainWindow>();
                services.AddTransient<HomeView>();
                services.AddTransient<GameListView>();
                services.AddTransient<ProductView>();
                services.AddTransient<ParameterView>();
                services.AddTransient<TestView>();
                services.AddTransient<ToolLibraryView>();
                services.AddTransient<NotificationView>();
                services.AddTransient<SettingsView>();
            })
            .Build();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

public override async void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                await _host.StartAsync();

                // 在窗口创建之前应用已保存的主题,避免点击设置时才切换
                SettingsViewModel.ApplyStartupTheme();

                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                desktop.MainWindow = mainWindow;
                mainWindow.Show();
            }

            base.OnFrameworkInitializationCompleted();
        }
}
