using System;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ZlinksPackageSystem.Desktop.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private static readonly string CredentialsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZlinksPackageSystem", "credentials.json");

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private bool _rememberPassword;

        public LoginViewModel()
        {
            Title = "登录";
            LoadSavedCredentials();
        }

        public void SaveCredentials()
        {
            try
            {
                var dir = Path.GetDirectoryName(CredentialsPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

                var data = new { Username, Password };
                File.WriteAllText(CredentialsPath, JsonSerializer.Serialize(data));
            }
            catch { }
        }

        public void ClearSavedCredentials()
        {
            try
            {
                if (File.Exists(CredentialsPath))
                    File.Delete(CredentialsPath);
            }
            catch { }
        }

        private void LoadSavedCredentials()
        {
            try
            {
                if (!File.Exists(CredentialsPath)) return;
                var json = File.ReadAllText(CredentialsPath);
                var data = JsonSerializer.Deserialize<CredentialsData>(json);
                if (data != null)
                {
                    Username = data.Username;
                    Password = data.Password;
                    RememberPassword = true;
                }
            }
            catch { }
        }

        [RelayCommand]
        private void ClearError()
        {
            HasError = false;
            ErrorMessage = string.Empty;
        }

        private class CredentialsData
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}