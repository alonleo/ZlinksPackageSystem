using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;
using ZlinksPackageSystem.Desktop.Models;
using ZlinksPackageSystem.Desktop.Services;

namespace ZlinksPackageSystem.Desktop.ViewModels
{
    public enum SettingsCategory
    {
        Appearance,
        Font,
        Background,
        Updates,
        Notification,
        About
    }

    public class SettingsCategoryItem
    {
        public SettingsCategory Key { get; init; }
        public string Icon { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string AccentColor { get; init; } = "#FF1976D2";
    }

    /// <summary>
    /// 用户加载的自定义字体记录。
    /// </summary>
    public class CustomFontInfo
        {
            /// <summary>TTF 文件内部的 family 名称(从 SkiaSharp 读出,作为 FontFamily(name) 引用)。</summary>
            public string FamilyName { get; set; } = string.Empty;

        /// <summary>UI 上展示的名字(带 📁 前缀,区别于系统字体)。</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>复制到本地的完整路径。</summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>原始文件名(用户选择的那个),用于显示。</summary>
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>Avalonia EmbeddedFontCollection 的 key,用于反注册。</summary>
        public string CollectionKey { get; set; } = string.Empty;
    }

    public partial class SettingsViewModel : ViewModelBase
    {
        public const string DefaultFontFamilyMarker = "默认(系统)";
        public const string CustomFontPrefix = "📁 ";

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZlinksPackageSystem", "settings.json");

        private static readonly string FontsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZlinksPackageSystem", "Fonts");

        // 字体大小预设(用于 Slider 刻度)
        public double[] FontSizePresets { get; } = { 10, 11, 12, 13, 14, 15, 16, 18, 20, 22, 24, 28 };

        private readonly MainViewModel _mainViewModel;
        private readonly IFilePickerService _filePickerService;
        private readonly IGlobalNotificationService _globalNotificationService;
        private readonly INotificationService _notificationService;
        private readonly IDialogService _dialogService;
        private bool _isLoading;

        [ObservableProperty]
        private bool _isDarkTheme;

        [ObservableProperty]
        private bool _autoCheckUpdates = true;

        // ===== 通知配置（全局）=====
        [ObservableProperty] private GlobalNotificationConfig _globalNotification = new();
        [ObservableProperty] private bool _isGlobalSecretsVisible;

        [ObservableProperty]
        private int _backgroundType;

        [ObservableProperty]
        private string _backgroundValue = string.Empty;

        [ObservableProperty]
        private double _backgroundOpacity = 0.3;

        [ObservableProperty]
        private string _fontFamily = DefaultFontFamilyMarker;

        [ObservableProperty]
        private double _fontSize = 14;

        [ObservableProperty]
        private bool _fontIsBold;

        [ObservableProperty]
        private bool _fontIsItalic;

        [ObservableProperty]
        private SettingsCategoryItem? _selectedCategory;

        // 静态信息(关于页面用)
        public string AppName { get; } = "ZlinksPackageSystem";
        public string AppVersion { get; } = GetVersionString();
        public string AppDescription { get; } =
            "一套面向游戏与产品的打包 / 测试 / 发布一体化桌面工具。";
        public string Copyright { get; } = $"© {DateTime.Now.Year} Zlinks. All rights reserved.";

        public ObservableCollection<string> InstalledFonts { get; } = new();
        public ObservableCollection<CustomFontInfo> CustomFonts { get; } = new();

        public ObservableCollection<SettingsCategoryItem> Categories { get; } = new()
        {
            new() { Key = SettingsCategory.Appearance, Icon = "🎨", Title = "外观主题", Description = "明暗模式", AccentColor = "#FF1976D2" },
            new() { Key = SettingsCategory.Font,       Icon = "🅰️", Title = "字体",       Description = "字体 / 字号 / 样式", AccentColor = "#FF9C27B0" },
            new() { Key = SettingsCategory.Background, Icon = "🖼️", Title = "背景",       Description = "背景图片与透明度", AccentColor = "#FFFF4081" },
            new() { Key = SettingsCategory.Updates,    Icon = "🔄", Title = "更新",       Description = "版本检测", AccentColor = "#FFE6A23C" },
            new() { Key = SettingsCategory.Notification, Icon = "📢", Title = "通知",   Description = "飞书机器人全局默认", AccentColor = "#FF1890FF" },
            new() { Key = SettingsCategory.About,      Icon = "ℹ️", Title = "关于",       Description = "应用信息", AccentColor = "#FF52C41A" },
        };

        // 各分类 IsVisible 绑定
        public bool IsAppearanceVisible => SelectedCategory?.Key == SettingsCategory.Appearance;
        public bool IsFontVisible => SelectedCategory?.Key == SettingsCategory.Font;
        public bool IsBackgroundVisible => SelectedCategory?.Key == SettingsCategory.Background;
        public bool IsUpdatesVisible => SelectedCategory?.Key == SettingsCategory.Updates;
        public bool IsNotificationVisible => SelectedCategory?.Key == SettingsCategory.Notification;
        public bool IsAboutVisible => SelectedCategory?.Key == SettingsCategory.About;

        // 预览文本用
        public FontWeight PreviewFontWeight => FontIsBold ? FontWeight.Bold : FontWeight.Normal;
        public FontStyle PreviewFontStyle => FontIsItalic ? FontStyle.Italic : FontStyle.Normal;
        public Avalonia.Media.FontFamily PreviewFontFamily => BuildFontFamily(FontFamily);

        public SettingsViewModel(MainViewModel mainViewModel, IFilePickerService filePickerService, IGlobalNotificationService globalNotificationService, INotificationService notificationService, IDialogService dialogService)
        {
            _mainViewModel = mainViewModel;
            _filePickerService = filePickerService;
            _globalNotificationService = globalNotificationService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            Title = "设置";

            LoadInstalledFonts();
            SelectedCategory = Categories[0];
            LoadSettings();
            ApplyTheme(IsDarkTheme);
            ApplyFont(FontFamily, FontSize, FontIsBold, FontIsItalic);
            _ = LoadGlobalNotificationAsync();
        }

        // ===== 通知配置命令 =====
        [RelayCommand]
        private async Task LoadGlobalNotificationAsync()
        {
            var cfg = await _globalNotificationService.LoadAsync();
            GlobalNotification = cfg;
        }

        [RelayCommand]
        private async Task SaveGlobalNotificationAsync()
        {
            await _globalNotificationService.SaveAsync(GlobalNotification);
        }

        [RelayCommand]
        private void AddGlobalNotificationChannel()
        {
            GlobalNotification.Channels.Add(new FeishuConfig());
        }

        [RelayCommand]
        private void RemoveGlobalNotificationChannel(FeishuConfig? channel)
        {
            if (channel != null) GlobalNotification.Channels.Remove(channel);
        }

        [RelayCommand]
        private void ToggleGlobalSecretsVisibility()
        {
            IsGlobalSecretsVisible = !IsGlobalSecretsVisible;
        }

        [RelayCommand]
        private async Task TestGlobalNotificationChannelAsync(FeishuConfig? channel)
        {
            if (channel == null) return;

            // 构造只含该渠道的临时配置
            var singleChannelConfig = new GlobalNotificationConfig
            {
                IsEnabled = true,
                NotifyOnStart = GlobalNotification.NotifyOnStart,
                NotifyOnSuccess = GlobalNotification.NotifyOnSuccess,
                NotifyOnFailure = GlobalNotification.NotifyOnFailure,
                MaxOutputChars = GlobalNotification.MaxOutputChars,
                Channels = new ObservableCollection<FeishuConfig> { channel }
            };
            var mockProject = new ToolProject
            {
                Name = "测试工具",
                WorkingDirectory = @"D:\tools",
                Notification = new NotificationConfig
                {
                    UseGlobalSettings = true
                }
            };
            var mockSnapshot = new ToolRunSnapshot
            {
                StartTime = DateTime.Now.AddSeconds(-5),
                EndTime = DateTime.Now,
                ProcessId = 99999,
                WorkingDirectory = @"D:\tools",
                CommandLine = "test command",
                ExitCode = 0,
                Output = "这是一条测试通知消息。\n如看到此卡片说明配置正确。",
                Trigger = NotificationTrigger.Success
            };

            var results = await _notificationService.SendAsync(mockProject, mockSnapshot, CancellationToken.None);

            if (results.Count == 0)
            {
                await _dialogService.ShowCloneLogAsync("📤 全局单渠道测试",
                    $"渠道类型：{channel.ChannelType}\n结果：当前配置下没有可用的渠道",
                    new List<string>(), false);
                return;
            }

            var r = results[0];
            var lines = new List<string>
            {
                (r.Success ? "✅ 成功" : "❌ 失败"),
                r.Message ?? string.Empty
            };
            await _dialogService.ShowCloneLogAsync("📤 全局单渠道测试",
                $"渠道类型：{channel.ChannelType}",
                lines, r.Success);
        }

        // ===== 分类变化 =====
        partial void OnSelectedCategoryChanged(SettingsCategoryItem? value)
        {
            OnPropertyChanged(nameof(IsAppearanceVisible));
            OnPropertyChanged(nameof(IsFontVisible));
            OnPropertyChanged(nameof(IsBackgroundVisible));
            OnPropertyChanged(nameof(IsUpdatesVisible));
            OnPropertyChanged(nameof(IsNotificationVisible));
            OnPropertyChanged(nameof(IsAboutVisible));
        }

        // ===== 字体设置变化回调 =====
        partial void OnFontFamilyChanged(string value)
        {
            if (value == null) return;
            if (!_isLoading) SaveSettings();
            ApplyFont(value, FontSize, FontIsBold, FontIsItalic);
            OnPropertyChanged(nameof(PreviewFontFamily));
        }

        partial void OnFontSizeChanged(double value)
        {
            if (!_isLoading) SaveSettings();
            ApplyFont(FontFamily, value, FontIsBold, FontIsItalic);
        }

        partial void OnFontIsBoldChanged(bool value)
        {
            if (!_isLoading) SaveSettings();
            ApplyFont(FontFamily, FontSize, value, FontIsItalic);
            OnPropertyChanged(nameof(PreviewFontWeight));
        }

        partial void OnFontIsItalicChanged(bool value)
        {
            if (!_isLoading) SaveSettings();
            ApplyFont(FontFamily, FontSize, FontIsBold, value);
            OnPropertyChanged(nameof(PreviewFontStyle));
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

        // ===== 命令 =====
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

        [RelayCommand]
        private void NavigateTo(SettingsCategoryItem item)
        {
            if (item != null) SelectedCategory = item;
        }

        [RelayCommand]
        private void ResetFont()
        {
            FontFamily = DefaultFontFamilyMarker;
            FontSize = 14;
            FontIsBold = false;
            FontIsItalic = false;
        }

        [RelayCommand]
        private async Task LoadCustomFontAsync()
        {
            var sourcePath = await _filePickerService.PickFontFileAsync();
            if (string.IsNullOrEmpty(sourcePath)) return;

            var info = TryRegisterFontFile(sourcePath);
            if (info == null) return;

            // 加进列表,自动选中
            CustomFonts.Add(info);
            InstalledFonts.Add(info.DisplayName);
            FontFamily = info.DisplayName;
            SaveSettings();
        }

        [RelayCommand]
        private void RemoveCustomFont(CustomFontInfo? info)
        {
            if (info == null) return;

            // 反注册
            if (Uri.TryCreate(info.CollectionKey, UriKind.Absolute, out var key))
            {
                try { FontManager.Current.RemoveFontCollection(key); } catch { }
            }

            // 删除文件
            try { if (File.Exists(info.FilePath)) File.Delete(info.FilePath); } catch { }

            // 从 UI 列表中移除
            InstalledFonts.Remove(info.DisplayName);
            CustomFonts.Remove(info);

            // 如果当前选的是它,切回默认
            if (FontFamily == info.DisplayName)
                FontFamily = DefaultFontFamilyMarker;

            SaveSettings();
        }

        // ===== 私有方法 =====
        private void LoadInstalledFonts()
        {
            InstalledFonts.Clear();
            InstalledFonts.Add(DefaultFontFamilyMarker);
            try
            {
                var names = Avalonia.Media.FontManager.Current.SystemFonts
                    .Select(f => f.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);
                foreach (var name in names)
                    InstalledFonts.Add(name);
            }
            catch
            {
                // 字体枚举失败时保持只有"默认"项,不阻塞其他功能
            }
        }

        /// <summary>
                /// 注册一个外部字体文件:复制到本地 Fonts 目录,创建 EmbeddedFontCollection,
                /// 用 SkiaSharp 提取 TTF 内部 family 名称,返回记录。
                /// </summary>
                private CustomFontInfo? TryRegisterFontFile(string sourceFilePath)
                {
                    try
                    {
                        if (!File.Exists(sourceFilePath)) return null;

                        Directory.CreateDirectory(FontsDirectory);

                        var originalName = Path.GetFileName(sourceFilePath);
                        var uniqueDest = Path.Combine(FontsDirectory, $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{originalName}");
                        File.Copy(sourceFilePath, uniqueDest, overwrite: true);

                        // 用 SkiaSharp 提取 TTF 内部的真实 family 名称
                        string familyName = string.Empty;
                        try
                        {
                            using var skTypeface = SKTypeface.FromFile(uniqueDest);
                            familyName = skTypeface?.FamilyName ?? string.Empty;
                        }
                        catch { }
                        if (string.IsNullOrWhiteSpace(familyName))
                            familyName = Path.GetFileNameWithoutExtension(originalName);

                        // 用 Avalonia FontManager 注册(后续可通过名字全局引用)
                        var key = new Uri($"fonts:{Guid.NewGuid():N}");
                        var sourceUri = new Uri(uniqueDest);
                        var collection = new EmbeddedFontCollection(key, sourceUri);
                        Avalonia.Media.FontManager.Current.AddFontCollection(collection);

                        return new CustomFontInfo
                        {
                            FamilyName = familyName,
                            DisplayName = $"{CustomFontPrefix}{familyName}  ({originalName})",
                            FilePath = uniqueDest,
                            OriginalFileName = originalName,
                            CollectionKey = key.ToString()
                        };
                    }
                    catch
                    {
                        return null;
                    }
                }

        /// <summary>
        /// 启动时把已持久化的自定义字体重新注册。
        /// </summary>
        private void ReloadSavedCustomFonts(IEnumerable<string> filePaths)
        {
            foreach (var path in filePaths)
            {
                if (!File.Exists(path)) continue;
                var info = TryRegisterFontFile(path);
                if (info == null) continue;
                CustomFonts.Add(info);
                InstalledFonts.Add(info.DisplayName);
            }
        }

        private Avalonia.Media.FontFamily BuildFontFamily(string displayName)
                {
                    if (string.IsNullOrWhiteSpace(displayName) || displayName == DefaultFontFamilyMarker)
                        return Avalonia.Media.FontFamily.Default;

                    // 自定义字体:从已加载列表里查找,直接用 TTF 内部的 family 名引用
                    // (该字体已通过 EmbeddedFontCollection 注册到 FontManager)
                    if (displayName.StartsWith(CustomFontPrefix, StringComparison.Ordinal))
                    {
                        var match = CustomFonts.FirstOrDefault(c => c.DisplayName == displayName);
                        if (match != null && !string.IsNullOrWhiteSpace(match.FamilyName))
                            return new Avalonia.Media.FontFamily(match.FamilyName);
                    }

                    // 系统字体:直接用名字
                    return new Avalonia.Media.FontFamily(displayName);
                }

        private void ApplyFont(string familyName, double size, bool isBold, bool isItalic)
        {
            try
            {
                if (Application.Current?.Resources is { } res)
                {
                    var family = BuildFontFamily(familyName);
                    res["ContentControlThemeFontFamily"] = family;

                    if (size >= 8 && size <= 48)
                        res["ContentControlThemeFontSize"] = size;
                }

                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d
                    && d.MainWindow != null)
                {
                    d.MainWindow.FontWeight = isBold ? FontWeight.Bold : FontWeight.Normal;
                    d.MainWindow.FontStyle = isItalic ? FontStyle.Italic : FontStyle.Normal;
                }
            }
            catch
            {
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

                        FontFamily = string.IsNullOrEmpty(settings.FontFamily)
                            ? DefaultFontFamilyMarker
                            : settings.FontFamily;
                        FontSize = settings.FontSize > 0 ? settings.FontSize : 14;
                        FontIsBold = settings.FontIsBold;
                        FontIsItalic = settings.FontIsItalic;

                        // 先注册自定义字体,再让 ComboBox 选中生效
                        if (settings.CustomFontFiles != null && settings.CustomFontFiles.Count > 0)
                            ReloadSavedCustomFonts(settings.CustomFontFiles);

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
            FontFamily = DefaultFontFamilyMarker;
            FontSize = 14;
            FontIsBold = false;
            FontIsItalic = false;
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
                    BackgroundOpacity = BackgroundOpacity,
                    FontFamily = FontFamily ?? string.Empty,
                    FontSize = FontSize,
                    FontIsBold = FontIsBold,
                    FontIsItalic = FontIsItalic,
                    CustomFontFiles = CustomFonts.Select(c => c.FilePath).ToList()
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

        private static string GetVersionString()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                var v = asm.GetName().Version;
                return v != null ? $"{v.Major}.{v.Minor}.{v.Build}" : "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }
    }
}