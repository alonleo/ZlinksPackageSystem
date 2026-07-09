using Avalonia.Controls;
using Avalonia.Input;
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
                    _ = vm.OpenProjectCommand.ExecuteAsync(project);
                }
            }
        }
    }
}
