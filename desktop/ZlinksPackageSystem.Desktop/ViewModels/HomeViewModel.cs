using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ZlinksPackageSystem.Desktop.ViewModels
{
    public partial class HomeViewModel : ViewModelBase
    {
        [ObservableProperty]
        private int _gameCount;

        [ObservableProperty]
        private int _productCount;

        [ObservableProperty]
        private int _testCount;

        public HomeViewModel()
        {
            Title = "首页";
            LoadDataAsync();
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            IsBusy = true;
            
            // Simulate loading data
            await Task.Delay(1000);
            
            GameCount = 25;
            ProductCount = 18;
            TestCount = 12;
            
            IsBusy = false;
        }
    }
}