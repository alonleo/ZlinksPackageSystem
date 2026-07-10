using System;
using System.Collections.ObjectModel;
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

        [ObservableProperty]
        private ObservableCollection<Parameter> _parameters = new();

        [ObservableProperty]
        private Parameter? _selectedParameter;

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
        private string _editParameterName = string.Empty;

        [ObservableProperty]
        private string _editCategory = string.Empty;

        [ObservableProperty]
        private string _editValueType = string.Empty;

        [ObservableProperty]
        private string _editDefaultValue = string.Empty;

        [ObservableProperty]
        private string _editUnit = string.Empty;

        [ObservableProperty]
        private string _editManager = string.Empty;

        [ObservableProperty]
        private string _editStatus = string.Empty;

        [ObservableProperty]
        private string _editPriority = string.Empty;

        [ObservableProperty]
        private string _editDescription = string.Empty;

        public ParameterViewModel(IApiService apiService)
        {
            Title = "参数管理";
            _apiService = apiService;
            LoadParametersCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private async Task LoadParametersAsync()
        {
            IsBusy = true;

            try
            {
                var endpoint = $"/parameters?current={CurrentPage}&size={PageSize}";
                if (!string.IsNullOrEmpty(SearchText))
                    endpoint += $"&parameterName={SearchText}";

                var result = await _apiService.GetAsync<PageResponse<Parameter>>(endpoint);
                if (result != null)
                {
                    Parameters = new ObservableCollection<Parameter>(result.Records);
                    TotalCount = result.Total;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load parameters: {ex.Message}");
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
            await LoadParametersAsync();
        }

        [RelayCommand]
        private async Task NextPageAsync()
        {
            if (CurrentPage * PageSize < TotalCount)
            {
                CurrentPage++;
                await LoadParametersAsync();
            }
        }

        [RelayCommand]
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadParametersAsync();
            }
        }

        [RelayCommand]
        private void OpenAddDialog()
        {
            SelectedParameter = null;
            EditParameterName = string.Empty;
            EditCategory = string.Empty;
            EditValueType = "字符串";
            EditDefaultValue = string.Empty;
            EditUnit = string.Empty;
            EditManager = string.Empty;
            EditStatus = "启用";
            EditPriority = "中";
            EditDescription = string.Empty;
            IsEditing = true;
        }

        [RelayCommand]
        private void OpenEditDialog(Parameter parameter)
        {
            if (parameter == null) return;
            SelectedParameter = parameter;
            EditParameterName = parameter.ParameterName;
            EditCategory = parameter.Category;
            EditValueType = parameter.ValueType;
            EditDefaultValue = parameter.DefaultValue;
            EditUnit = parameter.Unit;
            EditManager = parameter.Manager;
            EditStatus = parameter.Status;
            EditPriority = parameter.Priority;
            EditDescription = parameter.Description;
            IsEditing = true;
        }

        [RelayCommand]
        private void SaveParameter()
        {
            if (string.IsNullOrWhiteSpace(EditParameterName)) return;

            if (SelectedParameter == null)
            {
                Parameters.Insert(0, new Parameter
                {
                    Id = Parameters.Count + 1,
                    ParameterName = EditParameterName,
                    Category = EditCategory,
                    ValueType = EditValueType,
                    DefaultValue = EditDefaultValue,
                    Unit = EditUnit,
                    Manager = EditManager,
                    Status = EditStatus,
                    Priority = EditPriority,
                    Description = EditDescription,
                    CreateTime = DateTime.Now
                });
                TotalCount++;
            }
            else
            {
                var index = Parameters.IndexOf(SelectedParameter);
                if (index >= 0)
                {
                    Parameters[index] = new Parameter
                    {
                        Id = SelectedParameter.Id,
                        ParameterName = EditParameterName,
                        Category = EditCategory,
                        ValueType = EditValueType,
                        DefaultValue = EditDefaultValue,
                        Unit = EditUnit,
                        Manager = EditManager,
                        Status = EditStatus,
                        Priority = EditPriority,
                        Description = EditDescription,
                        CreateTime = SelectedParameter.CreateTime
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
        private async Task DeleteParameterAsync(Parameter parameter)
        {
            if (parameter == null) return;
            Parameters.Remove(parameter);
            TotalCount--;
        }
    }
}
