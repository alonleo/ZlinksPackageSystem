using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZlinksPackageSystem.Desktop.Models;
using ZlinksPackageSystem.Desktop.Services;

namespace ZlinksPackageSystem.Desktop.ViewModels
{
    public partial class ProductViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;

        [ObservableProperty]
        private ObservableCollection<Product> _products = new();

        [ObservableProperty]
        private Product? _selectedProduct;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _pageSize = 10;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private string _editProductName = string.Empty;

        [ObservableProperty]
        private string _editCategory = string.Empty;

        [ObservableProperty]
        private string _editVersion = string.Empty;

        [ObservableProperty]
        private string _editManager = string.Empty;

        [ObservableProperty]
        private string _editStatus = string.Empty;

        [ObservableProperty]
        private string _editPriority = string.Empty;

        public ProductViewModel(IApiService apiService)
        {
            Title = "产品管理";
            _apiService = apiService;
            LoadProductsCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private async Task LoadProductsAsync()
        {
            IsBusy = true;

            try
            {
                var endpoint = $"/products?current={CurrentPage}&size={PageSize}";
                if (!string.IsNullOrEmpty(SearchText))
                    endpoint += $"&productName={SearchText}";

                var result = await _apiService.GetAsync<PageResponse<Product>>(endpoint);
                if (result != null)
                {
                    Products = new ObservableCollection<Product>(result.Records);
                    TotalCount = result.Total;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load products: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            CurrentPage = 1;
            await LoadProductsAsync();
        }

        [RelayCommand]
        private async Task NextPageAsync()
        {
            if (CurrentPage * PageSize < TotalCount)
            {
                CurrentPage++;
                await LoadProductsAsync();
            }
        }

        [RelayCommand]
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadProductsAsync();
            }
        }

        [RelayCommand]
        private void OpenAddDialog()
        {
            SelectedProduct = null;
            EditProductName = string.Empty;
            EditCategory = string.Empty;
            EditVersion = string.Empty;
            EditManager = string.Empty;
            EditStatus = "开发中";
            EditPriority = "中";
            IsEditing = true;
        }

        [RelayCommand]
        private void OpenEditDialog(Product product)
        {
            if (product == null) return;
            SelectedProduct = product;
            EditProductName = product.ProductName;
            EditCategory = product.Category;
            EditVersion = product.Version;
            EditManager = product.Manager;
            EditStatus = product.Status;
            EditPriority = product.Priority;
            IsEditing = true;
        }

        [RelayCommand]
        private void SaveProduct()
        {
            if (string.IsNullOrWhiteSpace(EditProductName)) return;

            if (SelectedProduct == null)
            {
                Products.Insert(0, new Product
                {
                    Id = Products.Count + 1,
                    ProductName = EditProductName,
                    Category = EditCategory,
                    Version = EditVersion,
                    Manager = EditManager,
                    Status = EditStatus,
                    Priority = EditPriority,
                    CreateTime = DateTime.Now
                });
                TotalCount++;
            }
            else
            {
                var index = Products.IndexOf(SelectedProduct);
                if (index >= 0)
                {
                    Products[index] = new Product
                    {
                        Id = SelectedProduct.Id,
                        ProductName = EditProductName,
                        Category = EditCategory,
                        Version = EditVersion,
                        Manager = EditManager,
                        Status = EditStatus,
                        Priority = EditPriority,
                        CreateTime = SelectedProduct.CreateTime
                    };
                }
            }

            IsEditing = false;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditing = false;
        }

        [RelayCommand]
        private async Task DeleteProductAsync(Product product)
        {
            if (product == null) return;
            Products.Remove(product);
            TotalCount--;
        }
    }
}
