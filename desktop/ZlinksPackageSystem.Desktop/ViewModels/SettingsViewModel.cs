using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZlinksPackageSystem.Desktop.Models;
using ZlinksPackageSystem.Desktop.Services;

namespace ZlinksPackageSystem.Desktop.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZlinksPackageSystem", "settings.json");

        private readonly MainViewModel _mainViewModel;
        private readonly IFilePickerService _filePickerService;
        private bool _isLoading;

        [ObservableProperty]
        private bool _isDarkTheme;

        [ObservableProperty]
        private bool _autoCheckUpdates = true;

        [ObservableProperty]
        private int _backgroundType;

        [ObservableProperty]
        private string _backgroundValue = string.Empty;

        [ObservableProperty]
        private double _backgroundOpacity = 0.3;

        public SettingsViewModel(MainViewModel mainViewModel, IFilePickerService filePickerService)
        {
            _mainViewModel = mainViewModel;
            _filePickerService = filePickerService;
            Title = "设置";
            LoadSettings();
        }

        partial void OnIsDarkThemeChanged(bool value)
        {
            if (_isLoading) return;
            ApplyTheme(value);
            SaveSettings();
        }

        partial void OnAutoCheckUpdatesChanged(bool value)
        {
            if (_isLoading) return;
            SaveSettings();
        }

        partial void OnBackgroundTypeChanged(int value)
        {
            if (_isLoading) return;
            SaveSettings();
            _mainViewModel.ApplyBackground(value, BackgroundValue, BackgroundOpacity);
        }

        partial void OnBackgroundValueChanged(string value)
        {
            if (_isLoading) return;
            SaveSettings();
            _mainViewModel.ApplyBackground(BackgroundType, value, BackgroundOpacity);
        }

        partial void OnBackgroundOpacityChanged(double value)
        {
            if (_isLoading) return;
            SaveSettings();
            _mainViewModel.ApplyBackground(BackgroundType, BackgroundValue, value);
        }

        [RelayCommand]
        private async Task SelectImageFileAsync()
        {
            var filePath = await _filePickerService.PickImageFileAsync();
            if (!string.IsNullOrEmpty(filePath))
            {
                BackgroundType = 3;
                BackgroundValue = filePath;
            }
        }

        private void LoadSettings()
        {
            _isLoading = true;
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        IsDarkTheme = settings.Theme == "Dark";
                        AutoCheckUpdates = settings.AutoCheckUpdates;
                        BackgroundType = settings.BackgroundType;
                        BackgroundValue = settings.BackgroundValue ?? string.Empty;
                        BackgroundOpacity = settings.BackgroundOpacity > 0 ? settings.BackgroundOpacity : 0.3;
                        return;
                    }
                }
            }
            catch
            {
            }
            finally
            {
                _isLoading = false;
            }

            IsDarkTheme = true;
            AutoCheckUpdates = true;
            BackgroundType = 0;
            BackgroundValue = string.Empty;
            BackgroundOpacity = 0.3;
            _isLoading = false;
        }

        private void SaveSettings()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var settings = new AppSettings
                {
                    Theme = IsDarkTheme ? "Dark" : "Light",
                    AutoCheckUpdates = AutoCheckUpdates,
                    BackgroundType = BackgroundType,
                    BackgroundValue = BackgroundValue,
                    BackgroundOpacity = BackgroundOpacity
                };
                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
            }
        }

        private static void ApplyTheme(bool isDark)
        {
            Application.Current!.RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }
}
