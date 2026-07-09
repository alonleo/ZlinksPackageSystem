using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZlinksPackageSystem.Desktop.Services;
using ZlinksPackageSystem.Desktop.ViewModels;
using ZlinksPackageSystem.Desktop.Views;

namespace ZlinksPackageSystem.Desktop
{
    public partial class App : Application
    {
        private readonly IHost _host;

        public static IServiceProvider Services => ((App)Current)._host.Services;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Services
                    services.AddSingleton<IApiService, ApiService>();
                    services.AddSingleton<IAuthService, AuthService>();

                    // ViewModels
                    services.AddSingleton<MainViewModel>();
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<HomeViewModel>();
                    services.AddTransient<GameListViewModel>();
                    services.AddTransient<ProductViewModel>();
                    services.AddTransient<TestViewModel>();
                    services.AddTransient<ToolLibraryViewModel>();
                    services.AddTransient<SettingsViewModel>();

                    // Views
                    services.AddTransient<MainWindow>();
                    services.AddTransient<HomeView>();
                    services.AddTransient<GameListView>();
                    services.AddTransient<ProductView>();
                    services.AddTransient<TestView>();
                    services.AddTransient<ToolLibraryView>();
                    services.AddTransient<SettingsView>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync();
            }

            base.OnExit(e);
        }
    }
}
