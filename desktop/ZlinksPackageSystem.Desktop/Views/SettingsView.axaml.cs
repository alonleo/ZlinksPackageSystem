using Avalonia.Controls;
using Avalonia.Interactivity;
using ZlinksPackageSystem.Desktop.Models;
using ZlinksPackageSystem.Desktop.ViewModels;

namespace ZlinksPackageSystem.Desktop.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        // 预设字号按钮:把 Tag(double)写回 ViewModel
        private void FontSizePreset_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: double size } && DataContext is SettingsViewModel vm)
            {
                vm.FontSize = size;
            }
        }

        // 应用按钮:把选中的 CustomFontInfo 设为当前字体
        private void ApplyCustomFont_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: CustomFontInfo info } && DataContext is SettingsViewModel vm)
            {
                vm.FontFamily = info.DisplayName;
            }
        }

        // 移除按钮:调用 VM 的删除命令
        private void RemoveCustomFont_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: CustomFontInfo info } && DataContext is SettingsViewModel vm)
            {
                if (vm.RemoveCustomFontCommand.CanExecute(info))
                    vm.RemoveCustomFontCommand.Execute(info);
            }
        }

        private void OnGlobalChannelRemoveRequested(object? sender, RoutedEventArgs e)
        {
            if (e.Source is Control { DataContext: FeishuConfig channel }
                && DataContext is SettingsViewModel vm)
            {
                vm.RemoveGlobalNotificationChannelCommand.Execute(channel);
            }
        }

        private void OnGlobalChannelTestRequested(object? sender, RoutedEventArgs e)
        {
            if (e.Source is Control { DataContext: FeishuConfig channel }
                && DataContext is SettingsViewModel vm)
            {
                vm.TestGlobalNotificationChannelCommand.Execute(channel);
            }
        }
    }
}