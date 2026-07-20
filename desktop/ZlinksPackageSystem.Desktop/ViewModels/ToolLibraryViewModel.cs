using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
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
        private readonly IRuntimeEnvironmentService _runtimeEnvService;
        private readonly IFilePickerService _filePickerService;
        private readonly IProcessManagerService _processManager;
        private readonly IGitService _gitService;
        private readonly IToolPersistenceService _persistence;
        private readonly INotificationService _notificationService;
        private CancellationTokenSource? _cloneCts;
        private CancellationTokenSource? _pullCts;

        [ObservableProperty]
        private ObservableCollection<ToolProject> _projects = new();

        /// <summary>
        /// 仅本地、尚未推送到后端的用户工具（IsUserOnly=true）。
        /// 这些工具默认不显示在主页面上，仅持久化到本地缓存，待同步按钮触发后再 POST 到后端。
        /// </summary>
        public ObservableCollection<ToolProject> PendingSyncTools { get; } = new();

        public bool HasPendingSync => PendingSyncTools.Count > 0;

        [ObservableProperty]
        private ToolProject? _selectedProject;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isEditing;

        // ===== 编辑表单字段 - 基础信息 =====
        [ObservableProperty] private string _editName = string.Empty;
        [ObservableProperty] private string _editDescription = string.Empty;
        [ObservableProperty] private string _editCategory = string.Empty;
        [ObservableProperty] private string _editVersion = string.Empty;
        [ObservableProperty] private string _editStatus = string.Empty;
        [ObservableProperty] private string _editManager = string.Empty;

        // ===== 编辑表单字段 - 运行模式(用枚举方便 UI 绑定,保存时转字符串) =====
        [ObservableProperty] private ToolRunMode _editRunMode = ToolRunMode.Script;

        /// <summary>给弹窗里下拉用的模式显示文案（与 RunModes 一一对应）</summary>
        public IReadOnlyList<string> RunModeDisplayNames { get; } = new[]
        {
            "🚀 脚本模式（解释器+脚本）",
            "📦 本地可执行程序"
        };

        // ===== 编辑表单字段 - 脚本执行 =====
        [ObservableProperty] private string _editLanguage = "python";
        [ObservableProperty] private string _editInterpreterPath = string.Empty;
        [ObservableProperty] private string _editScriptPath = string.Empty;
        [ObservableProperty] private string _editWorkingDirectory = string.Empty;

        // ===== 编辑表单字段 - 参数列表 =====
        [ObservableProperty] private ObservableCollection<ParameterRow> _editParameters = new();

        // ===== 编辑表单字段 - 本地可执行程序 =====
        [ObservableProperty] private string _editExecutablePath = string.Empty;

        // ===== 运行时环境面板 =====
        [ObservableProperty] private ObservableCollection<RuntimeEnvironment> _availableEnvironments = new();
        [ObservableProperty] private bool _isDetectingEnvironments;

        [ObservableProperty]
        private RuntimeEnvironment? _selectedEnvironment;

        partial void OnSelectedEnvironmentChanged(RuntimeEnvironment? value)
        {
            if (value != null)
                EditLanguage = value.Language;
        }

        // ===== 会话级状态：记住用户是否勾选了「不再询问」 =====
        private bool _skipRunConfirmation;

        // ===== Git 区域（仅新建时显示）=====
        [ObservableProperty] private bool _isGitPanelExpanded = true;
        [ObservableProperty] private string _editGitUrl = string.Empty;
        [ObservableProperty] private string _editCloneDirectory = string.Empty;
        [ObservableProperty] private GitEnvironmentInfo? _gitEnvironment;
        [ObservableProperty] private bool _isDetectingGit;
        [ObservableProperty] private bool _isCloning;
        [ObservableProperty] private string _cloneProgressText = string.Empty;
        [ObservableProperty] private bool _isPulling;
        [ObservableProperty] private string _pullProgressText = string.Empty;
        [ObservableProperty] private bool _isNewProject;

        // ===== 通知 Tab =====
        [ObservableProperty] private NotificationConfig _editNotification = new();
        [ObservableProperty] private int _selectedTabIndex;
        [ObservableProperty] private bool _isSecretsVisible;

        public ToolLibraryViewModel(
            IApiService apiService,
            IDialogService dialogService,
            IRuntimeEnvironmentService runtimeEnvService,
            IFilePickerService filePickerService,
            IProcessManagerService processManager,
            IGitService gitService,
            IToolPersistenceService persistence,
            INotificationService notificationService)
        {
            Title = "工具库";
            _apiService = apiService;
            _dialogService = dialogService;
            _runtimeEnvService = runtimeEnvService;
            _filePickerService = filePickerService;
            _processManager = processManager;
            _gitService = gitService;
            _persistence = persistence;
            _notificationService = notificationService;

            _processManager.ProcessExited += OnProcessExited;

            _ = LoadProjectsAsync();
            _ = DetectEnvironmentsOnStartupAsync();
        }

        // =========================================================
        // 加载 / 检测
        // =========================================================
        [RelayCommand]
        private async Task LoadProjectsAsync()
        {
            IsBusy = true;
            try
            {
                // 从后台管理系统拉取（按 create_time 倒序，一次取 200 条；量更大时分页）
                var page = await _apiService.GetAsync<PageResponse<ToolEntity>>(
                    "/tools?current=1&size=200");

                var backendIds = new HashSet<long>();
                if (page?.Records != null && page.Records.Count > 0)
                {
                    var backendProjects = page.Records.Select(MapToProject).ToList();
                    foreach (var p in backendProjects) backendIds.Add(p.Id);
                    Projects = new ObservableCollection<ToolProject>(backendProjects);
                }
                else
                {
                    Projects = new ObservableCollection<ToolProject>();
                }

                // 找出本地缓存中尚未推送到后端的用户工具 → PendingSyncTools
                try
                {
                    var cached = await _persistence.LoadAsync();
                    PendingSyncTools.Clear();
                    foreach (var c in cached)
                    {
                        // 后端已有 (id > 0) 或 IsUserOnly=true 的视为待同步
                        if (!backendIds.Contains(c.Id) && c.Id > 0 == false || c.IsUserOnly || c.Id == 0)
                        {
                            c.IsUserOnly = true;
                            PendingSyncTools.Add(c);
                        }
                    }
                    OnPropertyChanged(nameof(HasPendingSync));
                }
                catch { /* 忽略 */ }

                // write-through offline cache：仅保存后端已确认 + 待同步用户工具
                var allPersistable = Projects.Concat(PendingSyncTools).ToList();
                _ = _persistence.SaveAsync(allPersistable);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ToolLibrary] 加载工具列表失败：{ex.Message}");
                // 回退本地缓存：所有都暂定为待同步用户工具
                try
                {
                    var cached = await _persistence.LoadAsync();
                    Projects = new ObservableCollection<ToolProject>();
                    PendingSyncTools.Clear();
                    foreach (var c in cached)
                    {
                        c.IsUserOnly = true;
                        PendingSyncTools.Add(c);
                    }
                    OnPropertyChanged(nameof(HasPendingSync));
                }
                catch
                {
                    Projects = new ObservableCollection<ToolProject>();
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 后端实体 → 桌面端模型，并反序列化 argumentsJson / notificationJson。
        /// </summary>
        private static ToolProject MapToProject(ToolEntity e)
        {
            var p = new ToolProject
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                Category = e.Category,
                Version = e.Version,
                Status = e.Status,
                Manager = e.Manager,
                Language = e.Language,
                InterpreterPath = e.InterpreterPath,
                ScriptPath = e.ScriptPath,
                ExecutablePath = e.ExecutablePath,
                WorkingDirectory = e.WorkingDirectory,
                DefaultArgumentPrefix = string.IsNullOrEmpty(e.DefaultArgumentPrefix) ? "--" : e.DefaultArgumentPrefix,
                GitUrl = e.GitUrl,
                CloneDirectory = e.CloneDirectory,
                RunMode = ToolRunModes.ToStringValue(ToolRunModes.Parse(e.RunMode)),
                IsSystemBuiltin = e.IsSystemBuiltin == 1,
                IsUserOnly = false,  // 后端拉来的数据天然不是 user-only
                CreateTime = ParseDateTime(e.CreateTime) ?? DateTime.Now
            };
            if (!string.IsNullOrEmpty(e.ArgumentsJson))
            {
                try
                {
                    var args = JsonSerializer.Deserialize<List<ToolArgument>>(e.ArgumentsJson);
                    if (args != null) p.Arguments = args;
                }
                catch { /* 忽略坏 JSON */ }
            }
            if (!string.IsNullOrEmpty(e.NotificationJson))
            {
                try
                {
                    var n = JsonSerializer.Deserialize<NotificationConfig>(e.NotificationJson);
                    if (n != null) p.Notification = n;
                }
                catch { /* 忽略坏 JSON */ }
            }
            return p;
        }

        /// <summary>
        /// 桌面端模型 → 后端实体，并把 Arguments / Notification 序列化为 JSON 列。
        /// </summary>
        private static ToolEntity MapToEntity(ToolProject p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Category = p.Category,
            Version = p.Version,
            Status = p.Status,
            Manager = p.Manager,
            RunMode = ToolRunModes.ToStringValue(p.RunModeEnum),
            Language = p.Language,
            InterpreterPath = p.InterpreterPath,
            ScriptPath = p.ScriptPath,
            ExecutablePath = p.ExecutablePath,
            WorkingDirectory = p.WorkingDirectory,
            DefaultArgumentPrefix = string.IsNullOrEmpty(p.DefaultArgumentPrefix) ? "--" : p.DefaultArgumentPrefix,
            GitUrl = p.GitUrl,
            CloneDirectory = p.CloneDirectory,
            ArgumentsJson = p.Arguments == null ? "[]" : JsonSerializer.Serialize(p.Arguments),
            NotificationJson = JsonSerializer.Serialize(p.Notification ?? new NotificationConfig()),
            // 用户自己新建的工具始终标记为非系统内置；后端管理员可后续通过 PUT 翻转该字段
            IsSystemBuiltin = p.IsSystemBuiltin ? 1 : 0
        };

        private static DateTime? ParseDateTime(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            if (DateTime.TryParse(s, out var d)) return d;
            return null;
        }

        [RelayCommand]
        private void FilterProjects() { }

        [RelayCommand]
        private async Task DetectEnvironmentsOnStartupAsync()
        {
            IsDetectingEnvironments = true;
            try
            {
                var envs = await _runtimeEnvService.DetectAllAsync();
                AvailableEnvironments = new ObservableCollection<RuntimeEnvironment>(envs);
            }
            finally
            {
                IsDetectingEnvironments = false;
            }
        }

        [RelayCommand]
        private async Task ReDetectEnvironmentsAsync()
        {
            await DetectEnvironmentsOnStartupAsync();
            var availableCount = AvailableEnvironments.Count(e => e.IsAvailable);
            await _dialogService.ShowEnvironmentResultAsync(
                "环境检测完成",
                $"共检测 {AvailableEnvironments.Count} 种语言，可用 {availableCount} 种。",
                availableCount > 0);
        }

        [RelayCommand]
        private async Task ReDetectSingleAsync(string? language)
        {
            if (string.IsNullOrEmpty(language)) return;
            var env = await _runtimeEnvService.ReDetectAsync(language);
            var idx = -1;
            for (int i = 0; i < AvailableEnvironments.Count; i++)
            {
                if (AvailableEnvironments[i].Language == language) { idx = i; break; }
            }
            if (idx >= 0) AvailableEnvironments[idx] = env;
            var msg = env.IsAvailable
                ? $"{env.Icon}  {env.DisplayName}\n\n路径：{env.ExecutablePath}\n版本：{env.Version}"
                : $"{env.Icon}  {env.Language}\n\n未检测到该运行环境，请确认已安装并加入 PATH。";
            await _dialogService.ShowEnvironmentResultAsync("环境检测结果", msg, env.IsAvailable);
        }

        // =========================================================
        // 新增 / 编辑弹窗
        // =========================================================
        [RelayCommand]
        private void OpenAddDialog()
        {
            EditName = string.Empty;
            EditDescription = string.Empty;
            EditCategory = string.Empty;
            EditVersion = string.Empty;
            EditStatus = "未运行";
            EditManager = string.Empty;

            EditRunMode = ToolRunMode.Script;
            EditLanguage = "python";
            SelectedEnvironment = AvailableEnvironments.FirstOrDefault(e => e.Language == "python")
                                 ?? AvailableEnvironments.FirstOrDefault();
            EditInterpreterPath = string.Empty;
            EditScriptPath = string.Empty;
            EditExecutablePath = string.Empty;
            EditWorkingDirectory = string.Empty;

            EditParameters.Clear();

            SelectedProject = null;
            IsEditing = true;

            EditGitUrl = string.Empty;
            EditCloneDirectory = string.Empty;
            GitEnvironment = null;
            IsCloning = false;
            CloneProgressText = string.Empty;
            IsNewProject = true;
            EditNotification = new NotificationConfig { UseGlobalSettings = true };
            SelectedTabIndex = 0;
            IsSecretsVisible = false;
            _ = RefreshGitEnvironmentAsync();
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

            EditRunMode = project.RunModeEnum;
            EditLanguage = project.Language;
            SelectedEnvironment = AvailableEnvironments.FirstOrDefault(e => e.Language == project.Language);
            EditInterpreterPath = project.InterpreterPath;
            EditScriptPath = project.ScriptPath;
            EditExecutablePath = project.ExecutablePath;
            EditWorkingDirectory = project.WorkingDirectory;

            EditParameters.Clear();

            IsEditing = true;
            IsNewProject = false;
            EditNotification = CloneNotification(project.Notification);
            SelectedTabIndex = 0;
            IsSecretsVisible = false;
        }

        private static NotificationConfig CloneNotification(NotificationConfig src)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(src);
            return System.Text.Json.JsonSerializer.Deserialize<NotificationConfig>(json) ?? new NotificationConfig();
        }

        [RelayCommand]
        private async Task BrowseScriptAsync()
        {
            var picked = await _filePickerService.PickScriptFileAsync();
            if (!string.IsNullOrEmpty(picked))
            {
                EditScriptPath = picked;
                if (string.IsNullOrEmpty(EditWorkingDirectory))
                    EditWorkingDirectory = Path.GetDirectoryName(picked) ?? string.Empty;
            }
        }

        [RelayCommand]
        private async Task BrowseWorkingDirectoryAsync()
        {
            var picked = await _filePickerService.PickDirectoryAsync();
            if (!string.IsNullOrEmpty(picked))
                EditWorkingDirectory = picked;
        }

        [RelayCommand]
        private async Task BrowseInterpreterAsync()
        {
            var picked = await _filePickerService.PickFileAsync("选择解释器", "*");
            if (!string.IsNullOrEmpty(picked))
                EditInterpreterPath = picked;
        }

        [RelayCommand]
        private async Task BrowseExecutableAsync()
        {
            var picked = await _filePickerService.PickFileAsync("选择可执行程序", "*.exe");
            if (!string.IsNullOrEmpty(picked))
            {
                EditExecutablePath = picked;
                if (string.IsNullOrEmpty(EditWorkingDirectory))
                    EditWorkingDirectory = Path.GetDirectoryName(picked) ?? string.Empty;
            }
        }

        [RelayCommand]
        private void AddParameter()
        {
            // 默认以 "--" 作为前缀，用户随时可改
            EditParameters.Add(new ParameterRow());
        }

        [RelayCommand]
        private void RemoveParameter(ParameterRow? row)
        {
            if (row == null) return;
            EditParameters.Remove(row);
        }

        [RelayCommand]
        private async Task SaveProjectAsync()
        {
            if (string.IsNullOrWhiteSpace(EditName))
            {
                await _dialogService.ShowMessageAsync("提示", "请先填写工具名称。");
                return;
            }

            // 同步参数列表：从 EditParameters (ParameterRow) 转换成 ToolArgument
            var args = BuildArgumentsFromEditRows();

            ToolProject? newProject;
            long? updateId = null;
            bool isNew = SelectedProject == null;
            if (isNew)
            {
                newProject = new ToolProject
                {
                    Id = 0, // 由后端生成
                    Name = EditName,
                    Description = EditDescription,
                    Category = EditCategory,
                    Version = EditVersion,
                    Status = string.IsNullOrEmpty(EditStatus) ? "未运行" : EditStatus,
                    Manager = EditManager,
                    CreateTime = DateTime.Now,
                    RunMode = ToolRunModes.ToStringValue(EditRunMode),
                    Language = EditLanguage,
                    InterpreterPath = EditInterpreterPath,
                    ScriptPath = EditScriptPath,
                    ExecutablePath = EditExecutablePath,
                    WorkingDirectory = string.IsNullOrWhiteSpace(EditWorkingDirectory)
                        ? ResolveDefaultWorkingDirectory()
                        : EditWorkingDirectory,
                    GitUrl = EditGitUrl,
                    CloneDirectory = EditCloneDirectory,
                    Arguments = args,
                    Notification = CloneNotification(EditNotification),
                    IsSystemBuiltin = false, // 用户新建的工具总是非系统内置
                    IsUserOnly = false
                };
            }
            else
            {
                newProject = new ToolProject
                {
                    Id = SelectedProject!.Id,
                    Name = EditName,
                    Description = EditDescription,
                    Category = EditCategory,
                    Version = EditVersion,
                    Status = string.IsNullOrEmpty(EditStatus) ? SelectedProject.Status : EditStatus,
                    Manager = EditManager,
                    CreateTime = SelectedProject.CreateTime,
                    RunMode = ToolRunModes.ToStringValue(EditRunMode),
                    Language = EditLanguage,
                    InterpreterPath = EditInterpreterPath,
                    ScriptPath = EditScriptPath,
                    ExecutablePath = EditExecutablePath,
                    WorkingDirectory = string.IsNullOrWhiteSpace(EditWorkingDirectory)
                        ? ResolveDefaultWorkingDirectory()
                        : EditWorkingDirectory,
                    GitUrl = SelectedProject.GitUrl,
                    CloneDirectory = SelectedProject.CloneDirectory,
                    Arguments = args,
                    Notification = CloneNotification(EditNotification),
                    IsSystemBuiltin = SelectedProject.IsSystemBuiltin,
                    IsRunning = SelectedProject.IsRunning,
                    ProcessId = SelectedProject.ProcessId
                };
                updateId = SelectedProject.Id;
            }

            try
            {
                ToolEntity? saved;
                if (updateId is long id && id > 0)
                {
                    saved = await _apiService.PutAsync<ToolEntity>($"/tools/{id}", MapToEntity(newProject));
                }
                else
                {
                    saved = await _apiService.PostAsync<ToolEntity>("/tools", MapToEntity(newProject));
                }

                if (saved != null)
                {
                    var refreshed = MapToProject(saved);
                    // 保留客户端维护的运行状态
                    refreshed.IsRunning = newProject.IsRunning;
                    refreshed.ProcessId = newProject.ProcessId;

                    if (updateId is long uid)
                    {
                        var index = Projects.IndexOf(SelectedProject!);
                        if (index >= 0) Projects[index] = refreshed;
                        SelectedProject = refreshed;
                    }
                    else
                    {
                        // 新建工具成功 → 推到主列表
                        Projects.Insert(0, refreshed);
                        SelectedProject = refreshed;
                    }
                    _ = _persistence.SaveAsync(Projects); // 离线缓存写穿
                }
                else
                {
                    // 后端无响应（或超时） → 兜底为本地用户工具
                    await FallbackToPendingAsync(newProject, isNew);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("保存失败", ex.Message);
                await FallbackToPendingAsync(newProject, isNew);
            }

            IsEditing = false;
        }

        /// <summary>
        /// 后端不可达时，把新建/编辑的工具作为「待同步用户工具」存到本地，不在主列表显示。
        /// </summary>
        private async Task FallbackToPendingAsync(ToolProject project, bool isNew)
        {
            project.IsUserOnly = true;
            project.IsSystemBuiltin = false;
            // 给它一个稳定本地负数 ID（仅运行期），避免与后端真实 ID 冲突
            if (project.Id <= 0)
                project.Id = -((long)DateTime.UtcNow.Ticks);

            if (isNew)
            {
                PendingSyncTools.Add(project);
            }
            else
            {
                var match = PendingSyncTools.FirstOrDefault(p => p.Id == project.Id);
                if (match != null)
                {
                    var idx = PendingSyncTools.IndexOf(match);
                    if (idx >= 0) PendingSyncTools[idx] = project;
                }
                else
                {
                    PendingSyncTools.Add(project);
                }
            }
            OnPropertyChanged(nameof(HasPendingSync));
            await _dialogService.ShowMessageAsync("已保存为本地用户工具",
                "后台管理系统暂不可达，该工具已保存到本地待同步列表（不在主页显示）。点击「📤 同步本地工具」按钮可稍后重试推送。");
        }

        /// <summary>
        /// 把弹窗内的 ParameterRow 列表转成 ToolArgument 列表（提交给后端）。
        /// </summary>
        private List<ToolArgument> BuildArgumentsFromEditRows()
        {
            var list = new List<ToolArgument>();
            int order = 0;
            foreach (var row in EditParameters)
            {
                if (string.IsNullOrWhiteSpace(row.Name)) continue;
                // 将 "Value" 既作为 DefaultValue 也作为 RequireInput=true 时用户回填的初值
                list.Add(new ToolArgument
                {
                    Name = row.Name.Trim(),
                    DefaultValue = row.Value ?? string.Empty,
                    RequireInput = false,
                    InputType = ToolArgumentInputType.Text,
                    Description = string.Empty,
                    Order = order++,
                    UseDefaultPrefix = string.IsNullOrEmpty(row.Prefix) || row.Prefix == "--",
                    Prefix = row.Prefix ?? string.Empty
                });
            }
            return list;
        }

        private string ResolveDefaultWorkingDirectory()
        {
            if (EditRunMode == ToolRunMode.LocalExecutable && !string.IsNullOrEmpty(EditExecutablePath))
                return Path.GetDirectoryName(EditExecutablePath) ?? string.Empty;
            if (!string.IsNullOrEmpty(EditScriptPath))
                return Path.GetDirectoryName(EditScriptPath) ?? string.Empty;
            return string.Empty;
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
            // 若正在运行，先杀掉
            if (project.IsRunning && project.ProcessId is int pid)
                _processManager.Kill(pid);

            try
            {
                if (project.Id > 0)
                {
                    var ok = await _apiService.DeleteAsync($"/tools/{project.Id}");
                    if (!ok)
                    {
                        await _dialogService.ShowMessageAsync("删除失败", "后台管理系统拒绝了删除请求。");
                        return;
                    }
                }
                Projects.Remove(project);
                _ = _persistence.SaveAsync(Projects); // 离线缓存同步
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("删除失败", ex.Message);
            }
        }

        [RelayCommand]
        private async Task SyncLocalToolsAsync()
        {
            if (PendingSyncTools.Count == 0)
            {
                await _dialogService.ShowMessageAsync("提示", "当前无待同步的用户工具。");
                return;
            }

            int success = 0, failed = 0;
            var snapshot = PendingSyncTools.ToList();
            foreach (var pending in snapshot)
            {
                try
                {
                    var saved = await _apiService.PostAsync<ToolEntity>("/tools", MapToEntity(pending));
                    if (saved != null)
                    {
                        var refreshed = MapToProject(saved);
                        refreshed.IsUserOnly = false;
                        Projects.Insert(0, refreshed);
                        PendingSyncTools.Remove(pending);
                        success++;
                    }
                    else
                    {
                        failed++;
                    }
                }
                catch
                {
                    failed++;
                }
            }

            OnPropertyChanged(nameof(HasPendingSync));
            _ = _persistence.SaveAsync(Projects);
            _ = _persistence.SaveAsync(PendingSyncTools);

            await _dialogService.ShowMessageAsync("同步完成",
                $"成功推送 {success} 个工具到后台管理系统。\n失败 {failed} 个（请检查网络/后端状态后重试）。");
        }

        [RelayCommand]
        private async Task OpenProjectAsync(ToolProject project)
        {
            if (project == null) return;
            // 运行中不响应点击
            if (project.IsRunning) return;
            await _dialogService.ShowMessageAsync(project.Name,
                $"进入工具：{project.Name}\n版本：{project.Version}\n描述：{project.Description}");
        }

        // =========================================================
        // 启动 / 停止
        // =========================================================

        [RelayCommand]
        private async Task RunToolAsync(ToolProject project)
        {
            if (project == null) return;

            if (project.IsRunning)
            {
                await _dialogService.ShowMessageAsync("提示", $"「{project.Name}」已经在运行中（PID: {project.ProcessId}）。");
                return;
            }

            // 1) 校验
            if (project.RunModeEnum == ToolRunMode.Script)
            {
                if (string.IsNullOrEmpty(project.ScriptPath))
                {
                    await _dialogService.ShowMessageAsync("错误", "工具未配置脚本路径，请先编辑。");
                    return;
                }
                if (!File.Exists(project.ScriptPath))
                {
                    await _dialogService.ShowMessageAsync("错误", $"脚本文件不存在：{project.ScriptPath}");
                    return;
                }

                // 解释器（仅当不是直接执行脚本的语言时才强制需要）
                bool interpreterIsScript = project.Language switch
                {
                    "powershell" or "bash" or "bat" or "cmd" => true,
                    _ => false
                };
                if (!interpreterIsScript && string.IsNullOrWhiteSpace(project.InterpreterPath))
                {
                    var env = AvailableEnvironments.FirstOrDefault(e => e.Language == project.Language);
                    if (env == null || !env.IsAvailable)
                        env = await _runtimeEnvService.ReDetectAsync(project.Language);
                    if (!env.IsAvailable)
                    {
                        await _dialogService.ShowMessageAsync("错误",
                            $"未检测到 {project.Language} 运行环境。\n请确认该语言已安装后重试。");
                        return;
                    }
                }
            }
            else // LocalExecutable
            {
                if (string.IsNullOrEmpty(project.ExecutablePath))
                {
                    await _dialogService.ShowMessageAsync("错误", "工具未配置可执行程序路径，请先编辑。");
                    return;
                }
                if (!File.Exists(project.ExecutablePath))
                {
                    await _dialogService.ShowMessageAsync("错误", $"可执行文件不存在：{project.ExecutablePath}");
                    return;
                }
            }

            // 2) 准备 EditableArgument：仅 RequireInput=true 的才在弹窗里展示（按需求 4）
            var queryableArgs = project.Arguments ?? new List<ToolArgument>();
            var editable = queryableArgs
                .OrderBy(a => a.Order)
                .Select(a => new EditableArgument
                {
                    Source = a,
                    UseDefaultPrefix = a.UseDefaultPrefix,
                    Prefix = a.Prefix,
                    Value = a.DefaultValue
                })
                .ToList();

            // 3) 构造初始命令预览
            var initialCmd = BuildCommandPreview(project, editable);

            // 4) 启动确认弹窗（除非用户之前勾选了"不再询问"）
            RunConfirmation? confirmation = null;
            if (!_skipRunConfirmation)
            {
                confirmation = await _dialogService.ShowRunConfirmationAsync(project, initialCmd, editable);
                if (confirmation == null) return; // 用户取消

                if (confirmation.DoNotAskAgain)
                {
                    _skipRunConfirmation = true;
                }
            }

            // 5) 用最终命令构造 ProcessStartInfo，启动进程
            var (psi, finalCmd) = BuildProcessStartInfo(project, confirmation?.Arguments ?? editable);

            var pid = _processManager.Start(psi, captureOutput: true);
            if (pid == 0)
            {
                await _dialogService.ShowMessageAsync("错误", $"进程启动失败。\n命令：{finalCmd}");
                return;
            }

            // 6) 标记项目为运行中
            project.IsRunning = true;
            project.ProcessId = pid;
            project.Status = "运行中";

            // 7) 触发启动通知（后台异步 fire-and-forget）
            var startSnapshot = new ToolRunSnapshot
            {
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
                ProcessId = pid,
                WorkingDirectory = psi.WorkingDirectory ?? string.Empty,
                CommandLine = finalCmd,
                ExitCode = 0,
                Output = string.Empty,
                Trigger = NotificationTrigger.Start
            };
            _ = FireNotificationAsync(project, startSnapshot);
        }

        [RelayCommand]
        private async Task StopToolAsync(ToolProject project)
        {
            if (project == null) return;
            if (!project.IsRunning) return;
            if (project.ProcessId is int pid)
            {
                _processManager.Kill(pid);
                // ProcessExited 事件会负责把 IsRunning/Status 还原
            }
            await Task.CompletedTask;
        }

        private void OnProcessExited(int pid, int exitCode)
        {
            // 进程退出事件可能在工作线程触发，统一 marshal 到 UI 线程
            Dispatcher.UIThread.Post(() =>
            {
                var p = Projects.FirstOrDefault(x => x.ProcessId == pid);
                if (p == null) return;
                p.IsRunning = false;
                p.ProcessId = null;
                p.Status = exitCode == 0 ? "已停止" : $"异常退出({exitCode})";

                // 触发成功/失败通知
                var output = _processManager.GetOutput(pid);
                var snapshot = new ToolRunSnapshot
                {
                    StartTime = DateTime.Now.AddMilliseconds(-1000),
                    EndTime = DateTime.Now,
                    ProcessId = pid,
                    WorkingDirectory = p.WorkingDirectory,
                    ExitCode = exitCode,
                    Output = output,
                    Trigger = exitCode == 0 ? NotificationTrigger.Success : NotificationTrigger.Failure
                };
                _ = FireNotificationAsync(p, snapshot);
            });
        }

        private async Task FireNotificationAsync(ToolProject project, ToolRunSnapshot snapshot)
        {
            try
            {
                await _notificationService.SendAsync(project, snapshot);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Notification] Fire 异常：{ex.Message}");
            }
        }

        // =========================================================
        // 命令行拼装 / 进程启动
        // =========================================================
        public string BuildCommandPreview(ToolProject project, IEnumerable<EditableArgument>? args = null)
        {
            if (project == null) return string.Empty;

            string entry;
            bool interpreterIsScript = false;
            if (project.RunModeEnum == ToolRunMode.LocalExecutable)
            {
                entry = project.ExecutablePath;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(project.InterpreterPath))
                {
                    entry = project.InterpreterPath;
                }
                else
                {
                    var env = AvailableEnvironments.FirstOrDefault(e => e.Language == project.Language && e.IsAvailable);
                    entry = env?.ExecutablePath ?? project.Language;
                }
                interpreterIsScript = project.Language switch
                {
                    "powershell" or "bash" or "bat" or "cmd" => true,
                    _ => false
                };
            }

            var sb = new StringBuilder();
            sb.Append(QuoteIfNeeded(entry));
            if (project.RunModeEnum == ToolRunMode.Script && !string.IsNullOrEmpty(project.ScriptPath))
                sb.Append(' ').Append(QuoteIfNeeded(project.ScriptPath));

            var argSource = args?.ToList() ?? (project.Arguments ?? new List<ToolArgument>())
                .OrderBy(a => a.Order)
                .Select(a => new EditableArgument
                {
                    Source = a,
                    UseDefaultPrefix = a.UseDefaultPrefix,
                    Prefix = a.Prefix,
                    Value = a.DefaultValue
                })
                .ToList();

            foreach (var ea in argSource)
            {
                var prefix = ea.UseDefaultPrefix ? project.DefaultArgumentPrefix : ea.Prefix;
                if (!string.IsNullOrEmpty(prefix)) sb.Append(' ').Append(QuoteIfNeeded(prefix));
                if (!string.IsNullOrEmpty(ea.Source.Name)) sb.Append(' ').Append(QuoteIfNeeded(ea.Source.Name));
                if (!string.IsNullOrEmpty(ea.Value))
                {
                    if (ea.Source.InputType == ToolArgumentInputType.Bool)
                        sb.Append(' ').Append(ea.Value == "true" ? "true" : "false");
                    else
                        sb.Append(' ').Append(QuoteIfNeeded(ea.Value));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 构造 ProcessStartInfo（使用 ArgumentList 安全转义）。
        /// 返回：(psi, 评出的命令行展示文本)
        /// </summary>
        private (ProcessStartInfo psi, string commandLine) BuildProcessStartInfo(
            ToolProject project, IEnumerable<EditableArgument> args)
        {
            string entry;
            bool interpreterIsScript = false;
            if (project.RunModeEnum == ToolRunMode.LocalExecutable)
            {
                entry = project.ExecutablePath;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(project.InterpreterPath))
                    entry = project.InterpreterPath;
                else
                {
                    var env = AvailableEnvironments.FirstOrDefault(e => e.Language == project.Language && e.IsAvailable);
                    entry = env?.ExecutablePath ?? project.Language;
                }
                interpreterIsScript = project.Language switch
                {
                    "powershell" or "bash" or "bat" or "cmd" => true,
                    _ => false
                };
            }

            var psi = new ProcessStartInfo
            {
                FileName = entry,
                WorkingDirectory = string.IsNullOrWhiteSpace(project.WorkingDirectory)
                    ? ResolveWorkingDirectory(project)
                    : project.WorkingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = false
            };

            // 脚本：先加脚本路径
            if (project.RunModeEnum == ToolRunMode.Script && !string.IsNullOrEmpty(project.ScriptPath))
            {
                if (interpreterIsScript)
                {
                    // powershell/bash：解释器 -File / bash <script>
                    psi.ArgumentList.Add(project.ScriptPath);
                }
                else
                {
                    psi.ArgumentList.Add(project.ScriptPath);
                }
            }

            // 参数
            foreach (var ea in args)
            {
                var prefix = ea.UseDefaultPrefix ? project.DefaultArgumentPrefix : ea.Prefix;
                if (!string.IsNullOrEmpty(prefix))
                    psi.ArgumentList.Add(prefix);
                if (!string.IsNullOrEmpty(ea.Source.Name))
                    psi.ArgumentList.Add(ea.Source.Name);
                if (!string.IsNullOrEmpty(ea.Value))
                {
                    if (ea.Source.InputType == ToolArgumentInputType.Bool)
                        psi.ArgumentList.Add(ea.Value == "true" ? "true" : "false");
                    else
                        psi.ArgumentList.Add(ea.Value);
                }
            }

            // 环境变量
            if (!string.IsNullOrWhiteSpace(project.EnvironmentVariables))
            {
                foreach (var line in project.EnvironmentVariables.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || !trimmed.Contains('=')) continue;
                    var idx = trimmed.IndexOf('=');
                    var key = trimmed.Substring(0, idx).Trim();
                    var val = trimmed.Substring(idx + 1).Trim();
                    if (!string.IsNullOrEmpty(key))
                        psi.Environment[key] = val;
                }
            }

            // 评出命令行（用于 UI 展示）
            var sb = new StringBuilder();
            sb.Append(QuoteIfNeeded(entry));
            if (project.RunModeEnum == ToolRunMode.Script && !string.IsNullOrEmpty(project.ScriptPath))
                sb.Append(' ').Append(QuoteIfNeeded(project.ScriptPath));
            foreach (var ea in args)
            {
                var prefix = ea.UseDefaultPrefix ? project.DefaultArgumentPrefix : ea.Prefix;
                if (!string.IsNullOrEmpty(prefix)) sb.Append(' ').Append(QuoteIfNeeded(prefix));
                if (!string.IsNullOrEmpty(ea.Source.Name)) sb.Append(' ').Append(QuoteIfNeeded(ea.Source.Name));
                if (!string.IsNullOrEmpty(ea.Value))
                {
                    if (ea.Source.InputType == ToolArgumentInputType.Bool)
                        sb.Append(' ').Append(ea.Value == "true" ? "true" : "false");
                    else
                        sb.Append(' ').Append(QuoteIfNeeded(ea.Value));
                }
            }

            return (psi, sb.ToString());
        }

        private static string ResolveWorkingDirectory(ToolProject project)
        {
            if (project.RunModeEnum == ToolRunMode.LocalExecutable && !string.IsNullOrEmpty(project.ExecutablePath))
                return Path.GetDirectoryName(project.ExecutablePath) ?? Environment.CurrentDirectory;
            if (!string.IsNullOrEmpty(project.ScriptPath))
                return Path.GetDirectoryName(project.ScriptPath) ?? Environment.CurrentDirectory;
            return Environment.CurrentDirectory;
        }

        private static string QuoteIfNeeded(string s)
        {
            if (string.IsNullOrEmpty(s)) return "\"\"";
            if (s.Contains(' ') || s.Contains('\t') || s.Contains('"'))
                return "\"" + s.Replace("\"", "\\\"") + "\"";
            return s;
        }

        // =========================================================
        // Git 区域
        // =========================================================

        [RelayCommand]
        private void ToggleGitPanel()
        {
            IsGitPanelExpanded = !IsGitPanelExpanded;
        }

        [RelayCommand]
        private async Task RefreshGitEnvironmentAsync()
        {
            IsDetectingGit = true;
            try
            {
                GitEnvironment = await _gitService.DetectAsync();
            }
            finally
            {
                IsDetectingGit = false;
            }
        }

        [RelayCommand]
        private async Task BrowseCloneDirectoryAsync()
        {
            var picked = await _filePickerService.PickDirectoryAsync();
            if (!string.IsNullOrEmpty(picked))
                EditCloneDirectory = picked;
        }

        [RelayCommand]
        private async Task StartCloneAsync()
        {
            if (string.IsNullOrWhiteSpace(EditGitUrl))
            {
                await _dialogService.ShowMessageAsync("提示", "请先填写 Git URL。");
                return;
            }
            if (string.IsNullOrWhiteSpace(EditCloneDirectory))
            {
                await _dialogService.ShowMessageAsync("提示", "请先选择克隆目标目录。");
                return;
            }
            if (GitEnvironment?.IsInstalled != true)
            {
                await _dialogService.ShowMessageAsync("提示", "未检测到本地 Git 环境，请先安装 Git。");
                return;
            }

            _cloneCts = new CancellationTokenSource();
            _ = RunCloneBackgroundAsync();
        }

        [RelayCommand]
        private void CancelClone()
        {
            _cloneCts?.Cancel();
        }

        private async Task RunCloneBackgroundAsync()
        {
            IsCloning = true;
            CloneProgressText = "⏳ 准备克隆…";
            var progress = new Progress<string>(line =>
            {
                CloneProgressText = line.Length > 120 ? line[..120] + "…" : line;
            });
            try
            {
                var result = await _gitService.CloneAsync(
                    EditGitUrl, EditCloneDirectory, progress, _cloneCts!.Token);

                if (result.Success)
                {
                    EditWorkingDirectory = result.RepoRoot;
                    var picked = await _dialogService.PickScriptFileInDirectoryAsync(result.RepoRoot);
                    if (!string.IsNullOrEmpty(picked)) EditScriptPath = picked;

                    await _dialogService.ShowCloneLogAsync(
                        "克隆成功",
                        $"仓库已克隆到：\n{result.RepoRoot}\n\n可在「启动方式」中选择脚本。",
                        result.Logs,
                        success: true);
                }
                else
                {
                    if (result.Cancelled)
                    {
                        TryDeleteCloneDirectory(EditCloneDirectory);
                    }
                    await _dialogService.ShowCloneLogAsync(
                        result.Cancelled ? "克隆已取消" : "克隆失败",
                        result.Cancelled ? "操作已取消，目标目录已清理。" : result.ErrorMessage,
                        result.Logs,
                        success: false);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowCloneLogAsync("克隆异常", ex.Message, Array.Empty<string>(), success: false);
            }
            finally
            {
                IsCloning = false;
                CloneProgressText = string.Empty;
                _cloneCts?.Dispose();
                _cloneCts = null;
            }
        }

        private static void TryDeleteCloneDirectory(string dir)
        {
            try
            {
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            catch { /* best-effort */ }
        }

        // =========================================================
        // 拉取更新（仅编辑现有项目时使用，对应工作目录里已有仓库）
        // =========================================================

        [RelayCommand]
        private async Task StartPullAsync()
        {
            if (string.IsNullOrWhiteSpace(EditWorkingDirectory))
            {
                await _dialogService.ShowMessageAsync("提示", "工作目录为空，无法拉取。请先在「启动方式」设置工作目录。");
                return;
            }
            if (GitEnvironment?.IsInstalled != true)
            {
                await _dialogService.ShowMessageAsync("提示", "未检测到本地 Git 环境，请先安装 Git。");
                return;
            }
            _pullCts = new CancellationTokenSource();
            _ = RunPullBackgroundAsync();
        }

        [RelayCommand]
        private void CancelPull()
        {
            _pullCts?.Cancel();
        }

        private async Task RunPullBackgroundAsync()
        {
            IsPulling = true;
            PullProgressText = "⏳ 准备拉取…";
            var progress = new Progress<string>(line =>
            {
                PullProgressText = line.Length > 120 ? line[..120] + "…" : line;
            });
            try
            {
                var result = await _gitService.PullAsync(
                    EditWorkingDirectory, progress, _pullCts!.Token);

                if (result.Success)
                {
                    await _dialogService.ShowCloneLogAsync(
                        "📥 拉取成功",
                        $"已更新到最新：\n{EditWorkingDirectory}",
                        result.Logs,
                        success: true);
                }
                else
                {
                    await _dialogService.ShowCloneLogAsync(
                        result.Cancelled ? "拉取已取消" : "拉取失败",
                        result.Cancelled ? "操作已取消。" : result.ErrorMessage,
                        result.Logs,
                        success: false);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowCloneLogAsync("拉取异常", ex.Message, Array.Empty<string>(), success: false);
            }
            finally
            {
                IsPulling = false;
                PullProgressText = string.Empty;
                _pullCts?.Dispose();
                _pullCts = null;
            }
        }

        // =========================================================
        // 导入 / 导出
        // =========================================================

        [RelayCommand]
        private async Task ImportToolsAsync()
        {
            var picked = await _filePickerService.PickFileAsync("选择要导入的 JSON", "*.json");
            if (string.IsNullOrEmpty(picked)) return;
            var list = await _persistence.ImportAsync(picked);
            if (list == null)
            {
                await _dialogService.ShowEnvironmentResultAsync("导入失败", "JSON 文件无效或无法解析。", false);
                return;
            }
            Projects = new ObservableCollection<ToolProject>(list);
            _ = _persistence.SaveAsync(Projects);
            await _dialogService.ShowEnvironmentResultAsync("导入成功",
                $"已导入 {list.Count} 个工具。", true);
        }

        [RelayCommand]
        private async Task ExportToolsAsync()
        {
            var picked = await _filePickerService.PickFileAsync("选择导出路径", "*.json");
            if (string.IsNullOrEmpty(picked)) return;
            try
            {
                await _persistence.ExportAsync(picked, Projects);
                await _dialogService.ShowEnvironmentResultAsync("导出成功",
                    $"已导出 {Projects.Count} 个工具到：\n{picked}", true);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowEnvironmentResultAsync("导出失败", ex.Message, false);
            }
        }

        // =========================================================
        // 通知 Tab 命令
        // =========================================================

        [RelayCommand]
        private void AddNotificationChannel()
        {
            EditNotification.Channels.Add(new FeishuConfig());
        }

        [RelayCommand]
        private void RemoveNotificationChannel(FeishuConfig? channel)
        {
            if (channel != null) EditNotification.Channels.Remove(channel);
        }

        [RelayCommand]
        private void ToggleSecretsVisibility()
        {
            IsSecretsVisible = !IsSecretsVisible;
        }

        [RelayCommand]
        private async Task TestNotificationAsync()
        {
            // 用一份 mock snapshot 测试当前配置（遍历全部渠道）
            var mockProject = new ToolProject
            {
                Name = "测试工具",
                WorkingDirectory = @"D:\tools",
                Notification = CloneNotification(EditNotification)
            };
            var mockSnapshot = BuildMockSnapshot();
            var results = await _notificationService.SendAsync(mockProject, mockSnapshot);

            var message = results.Count == 0
                ? "当前配置下没有可用的渠道（可能被关闭或未配置渠道）。"
                : string.Join("\n\n", results.Select(r =>
                    $"【{r.ChannelLabel}】\n{(r.Success ? "✅ 成功" : "❌ 失败")}\n{r.Message}"));

            await _dialogService.ShowCloneLogAsync("🧪 一键测试全部结果",
                $"测试渠道数：{results.Count}\n成功：{results.Count(r => r.Success)}\n失败：{results.Count(r => !r.Success)}",
                message.Split('\n').ToList(),
                results.All(r => r.Success) && results.Count > 0);
        }

        [RelayCommand]
        private async Task TestNotificationChannelAsync(FeishuConfig? channel)
        {
            if (channel == null) return;
            // 构造一次性 mock project，仅包含当前渠道
            var singleChannelNotification = CloneNotification(EditNotification);
            singleChannelNotification.Channels = new ObservableCollection<FeishuConfig> { channel };
            var mockProject = new ToolProject
            {
                Name = "测试工具",
                WorkingDirectory = @"D:\tools",
                Notification = singleChannelNotification
            };
            var mockSnapshot = BuildMockSnapshot();
            var results = await _notificationService.SendAsync(mockProject, mockSnapshot);

            var summary = channel.ChannelType switch
            {
                ChannelType.Feishu => $"{channel.ChannelType} · {channel.RobotType}",
                ChannelType.WeChatWork => channel.ChannelType.ToString(),
                _ => channel.ChannelType.ToString()
            };

            if (results.Count == 0)
            {
                await _dialogService.ShowCloneLogAsync("📤 单渠道测试",
                    $"渠道类型：{summary}\n结果：当前配置下没有可用的渠道",
                    new List<string>(), false);
                return;
            }

            var r = results[0];
            var lines = new List<string>
            {
                (r.Success ? "✅ 成功" : "❌ 失败"),
                r.Message ?? string.Empty
            };
            await _dialogService.ShowCloneLogAsync("📤 单渠道测试",
                $"渠道类型：{summary}\n结果：{(r.Success ? "成功" : "失败")}",
                lines, r.Success);
        }

        /// <summary>
        /// 构造通知测试用的模拟运行快照。
        /// 公共方法，便于单渠道测试与一键全部测试共用。
        /// </summary>
        private static ToolRunSnapshot BuildMockSnapshot() => new()
        {
            StartTime = DateTime.Now.AddSeconds(-5),
            EndTime = DateTime.Now,
            ProcessId = 99999,
            WorkingDirectory = @"D:\tools",
            CommandLine = "test command",
            ExitCode = 0,
            Output = "这是一条测试通知消息。\n如看到此卡片说明配置正确。",
            Trigger = NotificationTrigger.Success
        };
    }
}
