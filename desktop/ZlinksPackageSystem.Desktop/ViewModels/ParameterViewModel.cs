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
    public partial class ParameterViewModel : ViewModelBase
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
                if (!SetProperty(ref _selectedPlatform, value)) return;
                CurrentPage = 1;
                IsCurrentPlatformMapped = value != null && value.IsMapped;
                if (value == null || !value.IsMapped)
                {
                    Params = new ObservableCollection<AdParam>();
                    TotalCount = 0;
                    return;
                }
                _ = LoadParamsAsync();
            }
        }

        [ObservableProperty]
        private bool _isLoadingPlatforms;

        [ObservableProperty]
        private string _emptyStateMessage = string.Empty;

        [ObservableProperty]
        private ObservableCollection<AdParam> _params = new();

        [ObservableProperty]
        private AdParam? _selectedParam;

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

        [ObservableProperty]
        private ObservableCollection<Product> _products = new();

        [ObservableProperty]
        private bool _isCurrentPlatformMapped;

        private AdParam? _editProduct;
        public AdParam? EditProduct
        {
            get => _editProduct;
            private set => SetProperty(ref _editProduct, value);
        }

        public bool HasPlatforms => !IsLoadingPlatforms && Platforms.Count > 0;
        public bool IsPlatformsEmpty => !IsLoadingPlatforms && Platforms.Count == 0;

        public ParameterViewModel(IApiService apiService, IDialogService dialogService)
        {
            Title = "参数管理";
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

        private async Task LoadParamsAsync()
        {
            var route = SelectedPlatform == null ? null : ParamRouteRegistry.GetRoute(SelectedPlatform.PlatformCode);
            if (string.IsNullOrEmpty(route))
            {
                Params = new ObservableCollection<AdParam>();
                TotalCount = 0;
                return;
            }

            IsBusy = true;
            try
            {
                var endpoint = $"{route}?current={CurrentPage}&size={PageSize}";
                var page = await _apiService.GetAsync<PageResponse<AdParam>>(endpoint);
                Params = new ObservableCollection<AdParam>(page?.Records ?? new List<AdParam>());
                TotalCount = page?.Total ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadParams failed: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var list = await _apiService.GetAsync<List<Product>>("/products/all");
                Products = new ObservableCollection<Product>(list ?? new List<Product>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadProducts failed: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            CurrentPage = 1;
            await LoadParamsAsync();
        }

        [RelayCommand]
        private async Task NextPageAsync()
        {
            if (CurrentPage * PageSize < TotalCount)
            {
                CurrentPage++;
                await LoadParamsAsync();
            }
        }

        [RelayCommand]
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadParamsAsync();
            }
        }

        [RelayCommand]
        private async Task OpenAddDialogAsync()
        {
            if (SelectedPlatform == null || !IsCurrentPlatformMapped)
            {
                await _dialogService.ShowMessageAsync("提示", "当前平台暂未对接参数类型");
                return;
            }

            if (Products.Count == 0) await LoadProductsAsync();

            EditProduct = new AdParam
            {
                ProductId = 0,
                AdParamStatus = "pending",
                ListStatus = "listed",
            };
            EditTitle = $"新增参数 - {SelectedPlatform.PlatformName}";
            RebuildEditRows(EditProduct);
            IsEditing = true;
        }

        [RelayCommand]
        private async Task OpenEditDialogAsync(AdParam? row)
        {
            if (row == null) return;
            var route = ParamRouteRegistry.GetRoute(SelectedPlatform?.PlatformCode);
            if (string.IsNullOrEmpty(route))
            {
                await _dialogService.ShowMessageAsync("提示", "当前平台暂未对接参数类型");
                return;
            }

            AdParam? detail = null;
            try
            {
                detail = await _apiService.GetAsync<AdParam>($"{route}/{row.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetById failed: {ex.Message}");
            }

            if (detail == null)
            {
                await _dialogService.ShowMessageAsync("错误", "获取参数详情失败");
                return;
            }

            if (Products.Count == 0) await LoadProductsAsync();

            EditProduct = detail;
            EditTitle = $"编辑参数 - {(SelectedPlatform?.PlatformName ?? string.Empty)}";
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

            var route = ParamRouteRegistry.GetRoute(SelectedPlatform?.PlatformCode);
            if (string.IsNullOrEmpty(route))
            {
                await _dialogService.ShowMessageAsync("错误", "当前平台暂未对接参数类型");
                return;
            }

            var productIdStr = GetRowValue("ProductId");
            if (!long.TryParse(productIdStr, out var productId) || productId <= 0)
            {
                await _dialogService.ShowMessageAsync("提示", "产品ID 必填且必须为正整数");
                return;
            }

            ApplyRowsToAdParam(EditProduct);

            try
            {
                if (EditProduct.Id == 0)
                {
                    var created = await _apiService.PostAsync<AdParam>(route, EditProduct);
                    if (created == null)
                    {
                        await _dialogService.ShowMessageAsync("错误", "创建参数失败");
                        return;
                    }
                    Params.Insert(0, created);
                    TotalCount++;
                    await _dialogService.ShowMessageAsync("成功", "创建成功");
                }
                else
                {
                    var updated = await _apiService.PutAsync<AdParam>($"{route}/{EditProduct.Id}", EditProduct);
                    if (updated == null)
                    {
                        await _dialogService.ShowMessageAsync("错误", "更新参数失败");
                        return;
                    }
                    var idx = -1;
                    for (var i = 0; i < Params.Count; i++)
                    {
                        if (Params[i].Id == updated.Id)
                        {
                            idx = i;
                            break;
                        }
                    }
                    if (idx >= 0) Params[idx] = updated;
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
        private async Task DeleteProductAsync(AdParam? row)
        {
            if (row == null) return;
            var route = ParamRouteRegistry.GetRoute(SelectedPlatform?.PlatformCode);
            if (string.IsNullOrEmpty(route))
            {
                await _dialogService.ShowMessageAsync("错误", "当前平台暂未对接参数类型");
                return;
            }

            await _dialogService.ShowMessageAsync("确认", $"确认删除参数 {row.Id}?");
            try
            {
                var ok = await _apiService.DeleteAsync($"{route}/{row.Id}");
                if (!ok)
                {
                    await _dialogService.ShowMessageAsync("错误", "删除失败");
                    return;
                }
                Params.Remove(row);
                if (TotalCount > 0) TotalCount--;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete failed: {ex.Message}");
                await _dialogService.ShowMessageAsync("错误", $"删除失败: {ex.Message}");
            }
        }

        private void RebuildEditRows(AdParam source)
        {
            EditFieldRows.Clear();
            foreach (var f in ParamFieldDescriptor.EditFields)
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
                    case "ProductId":
                        row.Value = source.ProductId > 0 ? source.ProductId.ToString() : string.Empty;
                        break;
                    case "AdParamStatus":
                        row.Options = ParamFieldDescriptor.AdParamStatusOptions;
                        row.Value = string.IsNullOrEmpty(source.AdParamStatus)
                            ? ParamFieldDescriptor.AdParamStatusOptions.First()
                            : source.AdParamStatus;
                        break;
                    case "ListStatus":
                        row.Options = ParamFieldDescriptor.ListStatusOptions;
                        row.Value = string.IsNullOrEmpty(source.ListStatus)
                            ? ParamFieldDescriptor.ListStatusOptions.First()
                            : source.ListStatus;
                        break;
                    default:
                        row.Value = GetAdParamString(source, f.Key);
                        break;
                }

                EditFieldRows.Add(row);
            }
        }

        private static string GetAdParamString(AdParam p, string key)
        {
            return key switch
            {
                nameof(AdParam.PackageName) => p.PackageName ?? string.Empty,
                nameof(AdParam.AppId) => p.AppId ?? string.Empty,
                nameof(AdParam.AppSecret) => p.AppSecret ?? string.Empty,
                nameof(AdParam.MediaId) => p.MediaId ?? string.Empty,
                nameof(AdParam.ContractStatus) => p.ContractStatus ?? string.Empty,
                nameof(AdParam.AgconnectPath) => p.AgconnectPath ?? string.Empty,
                nameof(AdParam.TdAppId) => p.TdAppId ?? string.Empty,
                nameof(AdParam.Operator) => p.Operator ?? string.Empty,
                nameof(AdParam.Remark) => p.Remark ?? string.Empty,
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

        private void ApplyRowsToAdParam(AdParam p)
        {
            foreach (var r in EditFieldRows)
            {
                var v = r.Value ?? string.Empty;
                switch (r.Key)
                {
                    case "ProductId":
                        if (long.TryParse(v, out var pid)) p.ProductId = pid;
                        break;
                    case nameof(AdParam.PackageName): p.PackageName = v; break;
                    case nameof(AdParam.AppId): p.AppId = v; break;
                    case nameof(AdParam.AppSecret): p.AppSecret = v; break;
                    case nameof(AdParam.MediaId): p.MediaId = v; break;
                    case nameof(AdParam.ContractStatus): p.ContractStatus = v; break;
                    case nameof(AdParam.AgconnectPath): p.AgconnectPath = v; break;
                    case nameof(AdParam.TdAppId): p.TdAppId = v; break;
                    case nameof(AdParam.AdParamStatus): p.AdParamStatus = v; break;
                    case nameof(AdParam.ListStatus): p.ListStatus = v; break;
                    case nameof(AdParam.Operator): p.Operator = v; break;
                    case nameof(AdParam.Remark): p.Remark = v; break;
                }
            }
        }
    }
}