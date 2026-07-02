using System.Windows;
using ZlinksPackageSystem.Desktop.ViewModels;

namespace ZlinksPackageSystem.Desktop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}