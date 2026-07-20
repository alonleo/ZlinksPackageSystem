using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZlinksPackageSystem.Desktop.Services;

namespace ZlinksPackageSystem.Desktop.ViewModels
{
    public partial class ChangeAccountViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _currentPassword = string.Empty;

        [ObservableProperty]
        private string _newUsername = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private bool _verified;

        [ObservableProperty]
        private bool _verifying;

        [ObservableProperty]
        private bool _saving;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private string _originalUsername = string.Empty;

        [ObservableProperty]
        private bool _saveResult;

        public ChangeAccountViewModel(IAuthService authService)
        {
            _authService = authService;
        }

        public void Initialize(string currentUsername)
        {
            OriginalUsername = currentUsername;
            NewUsername = currentUsername;
        }

        [RelayCommand]
        private async Task VerifyPasswordAsync()
        {
            if (string.IsNullOrEmpty(CurrentPassword))
            {
                ErrorMessage = "请输入当前密码";
                return;
            }

            Verifying = true;
            ErrorMessage = string.Empty;
            try
            {
                var ok = await _authService.ChangeAccountAsync(CurrentPassword, null, null);
                if (ok)
                {
                    Verified = true;
                    ErrorMessage = string.Empty;
                }
                else
                {
                    Verified = false;
                    ErrorMessage = "当前密码错误";
                }
            }
            finally
            {
                Verifying = false;
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            SaveResult = false;

            if (!Verified)
            {
                ErrorMessage = "请先验证当前密码";
                return;
            }

            if (!string.IsNullOrEmpty(NewPassword) && NewPassword.Length < 6)
            {
                ErrorMessage = "新密码至少 6 位";
                return;
            }

            if (!string.IsNullOrEmpty(NewPassword) && NewPassword != ConfirmPassword)
            {
                ErrorMessage = "两次输入的密码不一致";
                return;
            }

            if (string.IsNullOrEmpty(NewPassword) && NewUsername == OriginalUsername)
            {
                ErrorMessage = "未做任何修改";
                return;
            }

            Saving = true;
            ErrorMessage = string.Empty;
            try
            {
                var ok = await _authService.ChangeAccountAsync(
                    CurrentPassword,
                    string.IsNullOrWhiteSpace(NewPassword) ? null : NewPassword,
                    NewUsername == OriginalUsername ? null : NewUsername);
                if (ok)
                {
                    Verified = false;
                    SaveResult = true;
                }
                else
                {
                    ErrorMessage = "修改失败";
                }
            }
            finally
            {
                Saving = false;
            }
        }
    }
}