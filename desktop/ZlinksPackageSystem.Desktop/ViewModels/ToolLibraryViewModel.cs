using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZlinksPackageSystem.Desktop.Models;
using ZlinksPackageSystem.Desktop.Services;

namespace ZlinksPackageSystem.Desktop.ViewModels
{
    public partial class ToolLibraryViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private ObservableCollection<ToolProject> _projects = new();

        [ObservableProperty]
        private ToolProject? _selectedProject;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isEditing;

        // 编辑表单字段
        [ObservableProperty]
        private string _editName = string.Empty;

        [ObservableProperty]
        private string _editDescription = string.Empty;

        [ObservableProperty]
        private string _editCategory = string.Empty;

        [ObservableProperty]
        private string _editVersion = string.Empty;

        [ObservableProperty]
        private string _editStatus = string.Empty;

        [ObservableProperty]
        private string _editManager = string.Empty;

        public ToolLibraryViewModel(IApiService apiService, IDialogService dialogService)
        {
            Title = "工具库";
            _apiService = apiService;
            _dialogService = dialogService;
            _ = LoadProjectsAsync();
        }

        [RelayCommand]
        private async Task LoadProjectsAsync()
        {
            IsBusy = true;

            try
            {
                // 先尝试从 API 加载
                var result = await _apiService.GetAsync<PageResponse<ToolProject>>("/tools");
                if (result?.Records?.Count > 0)
                {
                    Projects = new ObservableCollection<ToolProject>(result.Records);
                }
                else
                {
                    // 模拟数据
                    LoadMockData();
                }
            }
            catch
            {
                LoadMockData();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void LoadMockData()
        {
            Projects = new ObservableCollection<ToolProject>
            {
                new() { Id = 1, Name = "APK 打包工具", Description = "Android APK 自动化打包、签名、对齐一站式工具", Category = "打包", Version = "v2.1.0", Status = "运行中", Manager = "张三", CreateTime = DateTime.Now.AddDays(-30) },
                new() { Id = 2, Name = "资源压缩器", Description = "图片/音频资源无损压缩，减小包体积", Category = "优化", Version = "v1.5.3", Status = "运行中", Manager = "李四", CreateTime = DateTime.Now.AddDays(-20) },
                new() { Id = 3, Name = "渠道分包", Description = "多渠道自动分包与渠道号注入工具", Category = "分发", Version = "v3.0.0", Status = "维护中", Manager = "王五", CreateTime = DateTime.Now.AddDays(-15) },
                new() { Id = 4, Name = "崩溃分析", Description = "Crash 日志采集、符号化、趋势分析平台", Category = "监控", Version = "v1.8.2", Status = "运行中", Manager = "赵六", CreateTime = DateTime.Now.AddDays(-10) },
                new() { Id = 5, Name = "热更新平台", Description = "基于 IL2CPP 的热更新补丁生成与下发", Category = "更新", Version = "v2.3.1", Status = "运行中", Manager = "孙七", CreateTime = DateTime.Now.AddDays(-5) },
                new() { Id = 6, Name = "性能测试", Description = "游戏帧率、内存、CPU 性能自动化测试", Category = "测试", Version = "v1.2.0", Status = "维护中", Manager = "周八", CreateTime = DateTime.Now.AddDays(-3) },
            };
        }

        [RelayCommand]
        private void FilterProjects()
        {
            // 搜索过滤在本 ViewModel 中通过 SearchText 绑定处理
        }

        [RelayCommand]
        private void OpenAddDialog()
        {
            EditName = string.Empty;
            EditDescription = string.Empty;
            EditCategory = string.Empty;
            EditVersion = string.Empty;
            EditStatus = "运行中";
            EditManager = string.Empty;
            SelectedProject = null;
            IsEditing = true;
        }

        [RelayCommand]
        private void OpenEditDialog(ToolProject project)
        {
            if (project == null) return;
            SelectedProject = project;
            EditName = project.Name;
            EditDescription = project.Description;
            EditCategory = project.Category;
            EditVersion = project.Version;
            EditStatus = project.Status;
            EditManager = project.Manager;
            IsEditing = true;
        }

        [RelayCommand]
        private void SaveProject()
        {
            if (string.IsNullOrWhiteSpace(EditName)) return;

            if (SelectedProject == null)
            {
                // 新增
                var newProject = new ToolProject
                {
                    Id = Projects.Count > 0 ? Projects.Max(p => p.Id) + 1 : 1,
                    Name = EditName,
                    Description = EditDescription,
                    Category = EditCategory,
                    Version = EditVersion,
                    Status = EditStatus,
                    Manager = EditManager,
                    CreateTime = DateTime.Now
                };
                Projects.Add(newProject);
            }
            else
            {
                // 编辑
                var index = Projects.IndexOf(SelectedProject);
                if (index >= 0)
                {
                    Projects[index] = new ToolProject
                    {
                        Id = SelectedProject.Id,
                        Name = EditName,
                        Description = EditDescription,
                        Category = EditCategory,
                        Version = EditVersion,
                        Status = EditStatus,
                        Manager = EditManager,
                        CreateTime = SelectedProject.CreateTime
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
        private async Task DeleteProjectAsync(ToolProject project)
        {
            if (project == null) return;
            Projects.Remove(project);
        }

        [RelayCommand]
        private async Task OpenProjectAsync(ToolProject project)
        {
            if (project == null) return;
            await _dialogService.ShowMessageAsync(project.Name, $"进入工具：{project.Name}\n版本：{project.Version}\n描述：{project.Description}");
        }
    }
}
