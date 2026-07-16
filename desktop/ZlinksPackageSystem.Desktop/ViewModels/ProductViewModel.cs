using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private ObservableCollection<Platform> _platforms = new();

        private Platform? _selectedPlatform;
        public Platform? SelectedPlatform
        {
            get => _selectedPlatform;
            set
            {
                if (SetProperty(ref _selectedPlatform, value) && value != null)
                {
                    CurrentPage = 1;
                    _ = LoadProductsAsync();
                }
            }
        }

        [ObservableProperty]
        private bool _isLoadingPlatforms;

        [ObservableProperty]
        private string _emptyStateMessage = string.Empty;

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
        private string _editTitle = string.Empty;

        [ObservableProperty]
        private ObservableCollection<FieldEditRow> _editFieldRows = new();

        private Product? _editProduct;
        public Product? EditProduct
        {
            get => _editProduct;
            private set => SetProperty(ref _editProduct, value);
        }

        public bool HasPlatforms => !IsLoadingPlatforms && Platforms.Count > 0;
        public bool IsPlatformsEmpty => !IsLoadingPlatforms && Platforms.Count == 0;

        public ProductViewModel(IApiService apiService, IDialogService dialogService)
        {
            Title = "产品管理";
            _apiService = apiService;
            _dialogService = dialogService;
            LoadPlatformsCommand.ExecuteAsync(null);
        }

        partial void OnPlatformsChanged(ObservableCollection<Platform> value)
        {
            OnPropertyChanged(nameof(HasPlatforms));
            OnPropertyChanged(nameof(IsPlatformsEmpty));
        }

        partial void OnIsLoadingPlatformsChanged(bool value)
        {
            OnPropertyChanged(nameof(HasPlatforms));
            OnPropertyChanged(nameof(IsPlatformsEmpty));
        }

        [RelayCommand]
        private async Task LoadPlatformsAsync()
        {
            IsLoadingPlatforms = true;
            EmptyStateMessage = string.Empty;

            try
            {
                var list = await _apiService.GetAsync<List<Platform>>("/platforms/all");
                Platforms.Clear();

                if (list == null)
                {
                    EmptyStateMessage = "加载平台失败，请检查后端服务";
                    return;
                }

                if (list.Count == 0)
                {
                    EmptyStateMessage = "暂无平台数据，请前往后台「平台管理」添加";
                    return;
                }

                foreach (var p in list.OrderBy(x => x.SortOrder).ThenByDescending(x => x.CreateTime))
                {
                    Platforms.Add(p);
                }

                SelectedPlatform = Platforms[0];
            }
            catch (Exception ex)
            {
                EmptyStateMessage = $"加载平台异常: {ex.Message}";
                Console.WriteLine($"LoadPlatforms failed: {ex.Message}");
            }
            finally
            {
                IsLoadingPlatforms = false;
            }
        }

        [RelayCommand]
        private async Task LoadProductsAsync()
        {
            if (SelectedPlatform == null)
            {
                Products = new ObservableCollection<Product>();
                TotalCount = 0;
                return;
            }

            IsBusy = true;
            try
            {
                var endpoint = $"/products?current={CurrentPage}&size={PageSize}&platformId={SelectedPlatform.Id}";
                var page = await _apiService.GetAsync<PageResponse<Product>>(endpoint);
                Products = new ObservableCollection<Product>(page?.Records ?? new List<Product>());
                TotalCount = page?.Total ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadProducts failed: {ex.Message}");
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
            if (SelectedPlatform == null)
            {
                _ = _dialogService.ShowMessageAsync("提示", "请先在上方选择一个平台 Tab");
                return;
            }

            var p = new Product
            {
                PlatformId = SelectedPlatform.Id,
                Status = "pending",
                PackageMode = string.Empty,
            };
            EditProduct = p;
            EditTitle = $"新增产品 - {SelectedPlatform.PlatformName}";
            RebuildEditRows(p);
            IsEditing = true;
        }

        [RelayCommand]
        private async Task OpenEditDialogAsync(Product? row)
        {
            if (row == null) return;

            Product? detail = null;
            try
            {
                detail = await _apiService.GetAsync<Product>($"/products/{row.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetById failed: {ex.Message}");
            }

            if (detail == null)
            {
                await _dialogService.ShowMessageAsync("错误", "获取产品详情失败");
                return;
            }

            EditProduct = detail;
            EditTitle = $"编辑产品 - {(SelectedPlatform?.PlatformName ?? detail.PlatformName ?? string.Empty)}";
            RebuildEditRows(detail);
            IsEditing = true;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditing = false;
            EditProduct = null;
            EditFieldRows.Clear();
        }

        [RelayCommand]
        private async Task SaveProductAsync()
        {
            if (EditProduct == null) return;

            if (string.IsNullOrWhiteSpace(GetRowValue("PackageName")))
            {
                await _dialogService.ShowMessageAsync("提示", "包名必填");
                return;
            }

            ApplyRowsToProduct(EditProduct);

            try
            {
                if (EditProduct.Id == 0)
                {
                    var created = await _apiService.PostAsync<Product>("/products", BuildRequest(EditProduct));
                    if (created == null)
                    {
                        await _dialogService.ShowMessageAsync("错误", "创建产品失败");
                        return;
                    }
                    Products.Insert(0, created);
                    TotalCount++;
                    await _dialogService.ShowMessageAsync("成功", "创建成功");
                }
                else
                {
                    var updated = await _apiService.PutAsync<Product>($"/products/{EditProduct.Id}", BuildRequest(EditProduct));
                    if (updated == null)
                    {
                        await _dialogService.ShowMessageAsync("错误", "更新产品失败");
                        return;
                    }
                    var idx = -1;
                    for (var i = 0; i < Products.Count; i++)
                    {
                        if (Products[i].Id == updated.Id)
                        {
                            idx = i;
                            break;
                        }
                    }
                    if (idx >= 0) Products[idx] = updated;
                    await _dialogService.ShowMessageAsync("成功", "更新成功");
                }

                IsEditing = false;
                EditProduct = null;
                EditFieldRows.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save failed: {ex.Message}");
                await _dialogService.ShowMessageAsync("错误", $"保存失败: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task DeleteProductAsync(Product? row)
        {
            if (row == null) return;
            await _dialogService.ShowMessageAsync("确认", $"确认删除产品 {row.PackageName}?");
            try
            {
                var ok = await _apiService.DeleteAsync($"/products/{row.Id}");
                if (!ok)
                {
                    await _dialogService.ShowMessageAsync("错误", "删除失败");
                    return;
                }
                Products.Remove(row);
                if (TotalCount > 0) TotalCount--;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete failed: {ex.Message}");
                await _dialogService.ShowMessageAsync("错误", $"删除失败: {ex.Message}");
            }
        }

        private void RebuildEditRows(Product source)
        {
            EditFieldRows.Clear();
            foreach (var f in ProductFieldDescriptor.EditFields)
            {
                var row = new FieldEditRow
                {
                    Key = f.Key,
                    Label = f.Label,
                    Editor = f.Editor,
                    Placeholder = f.Placeholder,
                    Required = f.Required,
                };

                switch (f.Key)
                {
                    case "PackageMode":
                        row.Options = ProductFieldDescriptor.PackageModeOptions;
                        row.Value = string.IsNullOrEmpty(source.PackageMode)
                            ? (ProductFieldDescriptor.PackageModeOptions.FirstOrDefault() ?? string.Empty)
                            : source.PackageMode;
                        break;
                    case "Status":
                        row.Options = ProductFieldDescriptor.StatusOptions;
                        row.Value = string.IsNullOrEmpty(source.Status)
                            ? (ProductFieldDescriptor.StatusOptions.FirstOrDefault() ?? "pending")
                            : source.Status;
                        break;
                    default:
                        row.Value = GetProductString(source, f.Key);
                        break;
                }

                EditFieldRows.Add(row);
            }
        }

        private static string GetProductString(Product p, string key)
        {
            return key switch
            {
                nameof(Product.PackageName) => p.PackageName,
                nameof(Product.SdkVersion) => p.SdkVersion,
                nameof(Product.ApkVersion) => p.ApkVersion,
                nameof(Product.Batch) => p.Batch,
                nameof(Product.PackageMode) => p.PackageMode,
                nameof(Product.Status) => p.Status,
                nameof(Product.Remark) => p.Remark,
                _ => string.Empty,
            };
        }

        private string GetRowValue(string key)
        {
            foreach (var r in EditFieldRows)
            {
                if (r.Key == key) return r.Value ?? string.Empty;
            }
            return string.Empty;
        }

        private void ApplyRowsToProduct(Product p)
        {
            foreach (var r in EditFieldRows)
            {
                var v = r.Value ?? string.Empty;
                switch (r.Key)
                {
                    case nameof(Product.PackageName): p.PackageName = v; break;
                    case nameof(Product.SdkVersion): p.SdkVersion = v; break;
                    case nameof(Product.ApkVersion): p.ApkVersion = v; break;
                    case nameof(Product.Batch): p.Batch = v; break;
                    case nameof(Product.PackageMode): p.PackageMode = v; break;
                    case nameof(Product.Status): p.Status = v; break;
                    case nameof(Product.Remark): p.Remark = v; break;
                }
            }
        }

        private object BuildRequest(Product p)
        {
            return new
            {
                copyrightId = p.CopyrightId,
                gameId = p.GameId,
                companyId = p.CompanyId,
                platformId = p.PlatformId ?? SelectedPlatform?.Id,
                packageName = p.PackageName,
                sdkVersion = p.SdkVersion,
                apkVersion = p.ApkVersion,
                batch = p.Batch,
                packageMode = p.PackageMode,
                status = p.Status,
                remark = p.Remark,
            };
        }
    }
}