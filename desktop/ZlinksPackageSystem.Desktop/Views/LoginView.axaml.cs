using Avalonia.Controls;
using Avalonia.Interactivity;
using ZlinksPackageSystem.Desktop.ViewModels;

namespace ZlinksPackageSystem.Desktop.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void LoginView_Loaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm && !string.IsNullOrEmpty(vm.Password))
            {
                PasswordBoxCtrl.Text = vm.Password;
            }
        }
    }
}
