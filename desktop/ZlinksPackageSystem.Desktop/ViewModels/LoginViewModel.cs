using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ZlinksPackageSystem.Desktop.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        public LoginViewModel()
        {
            Title = "登录";
        }

        [RelayCommand]
        private void ClearError()
        {
            HasError = false;
            ErrorMessage = string.Empty;
        }
    }
}