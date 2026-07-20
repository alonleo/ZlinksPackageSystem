using Avalonia.Controls;
using Avalonia.Interactivity;
using ZlinksPackageSystem.Desktop.ViewModels;

namespace ZlinksPackageSystem.Desktop.Views
{
    public partial class ChangeAccountDialog : Window
    {
        public bool Succeeded { get; private set; }

        public ChangeAccountDialog()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Succeeded = false;
            Close();
        }

        private async void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not ChangeAccountViewModel vm) return;

            await vm.SaveCommand.ExecuteAsync(null);
            if (vm.SaveResult)
            {
                Succeeded = true;
                Close();
            }
        }
    }
}