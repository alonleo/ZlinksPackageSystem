using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ZlinksPackageSystem.Desktop.Models;
using ZlinksPackageSystem.Desktop.ViewModels;

namespace ZlinksPackageSystem.Desktop.Views
{
    public partial class ToolLibraryView : UserControl
    {
        public ToolLibraryView()
        {
            InitializeComponent();
        }

        private void Card_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is ToolProject project
                && DataContext is ToolLibraryViewModel vm)
            {
                var point = e.GetCurrentPoint(border);
                if (point.Properties.IsLeftButtonPressed)
                {
                    // 正在运行的卡片不响应点击（避免误触）
                    if (project.IsRunning) return;
                    _ = vm.OpenProjectCommand.ExecuteAsync(project);
                }
            }
        }

        private void OnNotificationChannelRemoveRequested(object? sender, RoutedEventArgs e)
        {
            if (e.Source is Control { DataContext: FeishuConfig channel }
                && DataContext is ToolLibraryViewModel vm)
            {
                vm.RemoveNotificationChannelCommand.Execute(channel);
            }
        }
    }
}
