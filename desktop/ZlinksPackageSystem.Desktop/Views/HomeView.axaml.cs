using Avalonia.Controls;
using Avalonia.Input;
using ZlinksPackageSystem.Desktop.Models;
using ZlinksPackageSystem.Desktop.ViewModels;

namespace ZlinksPackageSystem.Desktop.Views
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
        }

        private void NotificationItem_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is NotificationItem item
                && DataContext is HomeViewModel vm)
            {
                var point = e.GetCurrentPoint(border);
                if (point.Properties.IsLeftButtonPressed)
                {
                    _ = vm.ShowDetailCommand.ExecuteAsync(item);
                }
            }
        }
    }
}
