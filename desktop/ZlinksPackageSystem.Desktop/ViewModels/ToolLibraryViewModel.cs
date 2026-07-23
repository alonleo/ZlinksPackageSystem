using System;
using System.Collections.Concurrent;
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
using ZlinksPackageSystem.Desktop.Constants;
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
        private readonly IVenvService _venvService;
        private readonly INetworkStatusService _networkService;
        private readonly ILocalCacheService _cacheService;
        private CancellationTokenSource? _cloneCts;
        private CancellationTokenSource? _pullCts;
        private CancellationTokenSource? _venvCts;

        [ObservableProperty]
        private ObservableCollection<ToolProject> _projects = new();

        /// <summary>
        /// 仅本地、尚未推送到后端的用户工具（SyncState = PendingCreate / PendingUpdate）。
        /// 这些工具也显示在主列表 Projects 中（带「🟠 待同步」徽标），并由 PendingSyncTools 跟踪同步状态。
        /// </summary>
        public ObservableCollection<ToolProject> PendingSyncTools { get; } = new();

        public bool HasPendingSync => PendingSyncTools.Count > 0;

        /// <summary>
        /// 批量同步（顶部「📤 同步本地」按钮）的忙碌状态。
        /// 防止与单卡同步并发重入，也用于 UI 显示加载中。
        /// </summary>
        [ObservableProperty]
        private bool _isBatchSyncing;

        /// <summary>
        /// 当前正在执行单卡同步的工具 ID（同一时刻只允许一条）。
        /// 由 <see cref="SyncSingleToolAsync"/> 维护；批量同步循环也会复用。
        /// </summary>
        [ObservableProperty]
        private long? _singleSyncingId;

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

        // ===== 编辑表单字段 - Python 虚拟环境(脚本模式且 Language==python 时启用)=====
        /// <summary>默认 PyPI 镜像(清华源),作为输入框 placeholder/默认值。</summary>
        public const string DefaultPipMirror = "https://pypi.tuna.tsinghua.edu.cn/simple";
        [ObservableProperty] private bool _editCreateVenv;
        [ObservableProperty] private string _editVenvDirectory = string.Empty;
        [ObservableProperty] private string _editRequirementsPath = string.Empty;
        [ObservableProperty] private bool _editAutoInstallRequirements;
        [ObservableProperty] private string _editPipMirrorUrl = DefaultPipMirror;

        // ===== Python venv 创建进度(类似 Git 克隆区域)=====
        [ObservableProperty] private bool _isCreatingVenv;
        [ObservableProperty] private string _venvProgressText = string.Empty;

        // ===== 编辑表单字段 - 参数列表 =====
        [ObservableProperty] private ObservableCollection<ParameterRow> _editParameters = new();

        // ===== 编辑表单字段 - 本地可执行程序 =====
        [ObservableProperty] private string _editExecutablePath = string.Empty;

        // ===== 缓存状态 =====
        [ObservableProperty] private DateTime? _lastCacheUpdateTime;
        [ObservableProperty] private bool _isFromCache;
        public string CacheUpdateTimeText => LastCacheUpdateTime.HasValue
            ? $"缓存快照: {LastCacheUpdateTime.Value:yyyy-MM-dd HH:mm:ss}"
            : string.Empty;

        partial void OnLastCacheUpdateTimeChanged(DateTime? value) => OnPropertyChanged(nameof(CacheUpdateTimeText));
        partial void OnIsFromCacheChanged(bool value) => OnPropertyChanged(nameof(CacheUpdateTimeText));

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
        [ObservableProperty] private string _editRemoteName = "origin";
        [ObservableProperty] private GitEnvironmentInfo? _gitEnvironment;
        [ObservableProperty] private bool _isDetectingGit;
        [ObservableProperty] private bool _isCloning;
        [ObservableProperty] private string _cloneProgressText = string.Empty;
        [ObservableProperty] private bool _isPulling;
        [ObservableProperty] private string _pullProgressText = string.Empty;
        [ObservableProperty] private bool _isNewProject;

        // ===== 本地 .git 自动检测(逐字防抖触发)=====
        [ObservableProperty] private string _localGitHint = string.Empty;
        [ObservableProperty] private bool _isDetectingLocalGit;

        /// <summary>上次检测到的本地 .git URL,运行期记录(不持久化)。供后续编辑时显示"原 URL"。</summary>
        private string _lastDetectedRemoteUrl = string.Empty;
        private string _lastDetectedRemoteName = "origin";

        /// <summary>防抖 timer:用户输入目录路径后 500ms 内若无新输入则触发检测。</summary>
        private readonly DispatcherTimer _localDetectTimer;

        /// <summary>跟踪正在运行的进程 → 命令行的映射,用于进程退出后展示结果弹窗。</summary>
        private readonly ConcurrentDictionary<int, string> _runningCommands = new();
        private readonly ConcurrentDictionary<int, DateTime> _runningStartTimes = new();

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
            INotificationService notificationService,
            IVenvService venvService,
            INetworkStatusService networkService,
            ILocalCacheService cacheService)
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
            _venvService = venvService;
            _networkService = networkService;
            _cacheService = cacheService;

            // 防抖:逐字修改 EditCloneDirectory 后 500ms 无新输入即触发本地 .git 检测
            _localDetectTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _localDetectTimer.Tick += async (_, _) =>
            {
                _localDetectTimer.Stop();
                await DetectLocalGitNowAsync();
            };

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
            try
            {
                IsBusy = true;
                StatusMessage = "正在加载工具库...";

                // 缓存优先 — 展示本地快照
                try
                {
                    var cachedTools = await _cacheService.LoadAsync<ToolsCache>(CacheKeys.Tools);
                    if (cachedTools?.Projects != null && cachedTools.Projects.Count > 0)
                    {
                        Projects = new ObservableCollection<ToolProject>(cachedTools.Projects);
                        foreach (var p in Projects) p.IsFromLocalSnapshot = true;
                        LastCacheUpdateTime = cachedTools.LastUpdated;
                        IsFromCache = true;
                    }
                }
                catch { }

                // 在线则拉取后端最新数据
                if (_networkService.IsOnline)
                {
                    StatusMessage = "正在从服务器同步...";
                    try
                    {
                        var page = await _apiService.GetAsync<PageResponse<ToolEntity>>(
                            "/tools?current=1&size=200");

                        List<ToolProject> backendProjects = new();
                        HashSet<long> backendIds = new();
                        if (page?.Records != null && page.Records.Count > 0)
                        {
                            backendProjects = page.Records.Select(MapToProject).ToList();
                            foreach (var p in backendProjects) backendIds.Add(p.Id);
                        }

                        List<ToolProject> pendingLocal = new();
                        try
                        {
                            var cached = await _persistence.LoadAsync();
                            foreach (var c in cached)
                            {
                                bool isPendingCreate = c.SyncState == ToolSyncState.PendingCreate
                                                      || (c.Id > 0 && !backendIds.Contains(c.Id));
                                bool isPendingUpdate = c.SyncState == ToolSyncState.PendingUpdate;
                                if (isPendingCreate || isPendingUpdate)
                                {
                                    c.SyncState = isPendingUpdate
                                        ? ToolSyncState.PendingUpdate
                                        : ToolSyncState.PendingCreate;
                                    pendingLocal.Add(c);
                                }
                            }
                        }
                        catch { }

                        var merged = new List<ToolProject>(pendingLocal);
                        merged.AddRange(backendProjects);
                        foreach (var p in merged) p.IsFromLocalSnapshot = false;
                        Projects = new ObservableCollection<ToolProject>(merged);
                        IsFromCache = false;

                        PendingSyncTools.Clear();
                        foreach (var p in pendingLocal) PendingSyncTools.Add(p);
                        OnPropertyChanged(nameof(HasPendingSync));

                        await _persistence.SaveAsync(AllPersistable());
                        await _cacheService.SaveAsync(CacheKeys.Tools, new ToolsCache
                        {
                            Projects = Projects.ToList(),
                            LastUpdated = DateTime.Now
                        });
                        LastCacheUpdateTime = DateTime.Now;

                        StatusMessage = $"已加载 {Projects.Count} 个工具";
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ToolLibrary] 后端加载失败，保留缓存：{ex.Message}");
                        if (Projects.Count == 0)
                        {
                            try
                            {
                                var cached = await _persistence.LoadAsync();
                                Projects = new ObservableCollection<ToolProject>(cached);
                                PendingSyncTools.Clear();
                                foreach (var c in cached)
                                {
                                    c.SyncState = ToolSyncState.PendingCreate;
                                    PendingSyncTools.Add(c);
                                }
                                OnPropertyChanged(nameof(HasPendingSync));
                            }
                            catch
                            {
                                Projects = new ObservableCollection<ToolProject>();
                            }
                        }
                        StatusMessage = "加载失败，已保留本地数据";
                    }
                }
                else
                {
                    try
                    {
                        var cached = await _persistence.LoadAsync();
                        foreach (var c in cached) c.SyncState = ToolSyncState.PendingCreate;
                        if (Projects.Count == 0)
                            Projects = new ObservableCollection<ToolProject>(cached);
                        PendingSyncTools.Clear();
                        foreach (var c in cached) PendingSyncTools.Add(c);
                        OnPropertyChanged(nameof(HasPendingSync));
                    }
                    catch { }
                    StatusMessage = "离线模式 - 仅显示本地工具";
                }
            }
            finally
            {
                IsBusy = false;
                // 3 秒后清除提示
                _ = ClearStatusMessageAsync();
            }
        }

        private bool _hasStatusMessage;
        public bool HasStatusMessage
        {
            get => _hasStatusMessage;
            set => SetProperty(ref _hasStatusMessage, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                SetProperty(ref _statusMessage, value);
                HasStatusMessage = !string.IsNullOrEmpty(value);
            }
        }

        private async Task ClearStatusMessageAsync()
        {
            await Task.Delay(3000);
            StatusMessage = string.Empty;
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
                RemoteName = string.IsNullOrWhiteSpace(e.RemoteName) ? "origin" : e.RemoteName,
                RunMode = ToolRunModes.ToStringValue(ToolRunModes.Parse(e.RunMode)),
                IsSystemBuiltin = e.IsSystemBuiltin == 1,
                SyncState = ToolSyncState.Synced,  // 后端拉来的数据天然是已同步
                CreateTime = ParseDateTime(e.CreateTime) ?? DateTime.Now,
                // Python 虚拟环境配置回读(后端 DTO 同步加字段后,这里必须把 4 字段带回来)
                CreateVenv = e.CreateVenv,
                VenvDirectory = e.VenvDirectory ?? string.Empty,
                RequirementsPath = e.RequirementsPath ?? string.Empty,
                PipMirrorUrl = e.PipMirrorUrl ?? string.Empty
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
            RemoteName = string.IsNullOrWhiteSpace(p.RemoteName) ? "origin" : p.RemoteName,
            ArgumentsJson = p.Arguments == null ? "[]" : JsonSerializer.Serialize(p.Arguments),
            NotificationJson = JsonSerializer.Serialize(p.Notification ?? new NotificationConfig()),
            // 用户自己新建的工具始终标记为非系统内置；后端管理员可后续通过 PUT 翻转该字段
            IsSystemBuiltin = p.IsSystemBuiltin ? 1 : 0,
            // Python 虚拟环境配置(后端 DTO 也必须有这 4 字段,否则保存后丢配置→首次运行退回到系统 Python)
            CreateVenv = p.CreateVenv,
            VenvDirectory = p.VenvDirectory ?? string.Empty,
            RequirementsPath = p.RequirementsPath ?? string.Empty,
            PipMirrorUrl = p.PipMirrorUrl ?? string.Empty
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

            var existing = AvailableEnvironments.FirstOrDefault(e => e.Language == language);
            if (existing != null) env.IsExpanded = existing.IsExpanded;

            var idx = AvailableEnvironments.IndexOf(existing!);
            if (idx >= 0) AvailableEnvironments[idx] = env;
            else AvailableEnvironments.Add(env);

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
            EditRemoteName = "origin";
            GitEnvironment = null;
            IsCloning = false;
            CloneProgressText = string.Empty;
            IsNewProject = true;
            LocalGitHint = string.Empty;
            IsDetectingLocalGit = false;
            _lastDetectedRemoteUrl = string.Empty;
            _lastDetectedRemoteName = "origin";
            EditNotification = new NotificationConfig { UseGlobalSettings = true };
            SelectedTabIndex = 0;
            IsSecretsVisible = false;

            // Python venv 默认勾选(创建+自动 pip install),方便一键启动
            EditCreateVenv = true;
            EditVenvDirectory = string.Empty;     // 空表示 {WorkingDirectory}/.venv
            EditRequirementsPath = string.Empty;
            EditAutoInstallRequirements = true;
            EditPipMirrorUrl = DefaultPipMirror;
            IsCreatingVenv = false;
            VenvProgressText = string.Empty;

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

            // Python venv:从已有 project 读取
            EditCreateVenv = project.CreateVenv;
            EditVenvDirectory = project.VenvDirectory;
            EditRequirementsPath = project.RequirementsPath;
            // AutoInstall 与 PipMirror 是 UI 辅助字段:若 RequirementsPath 为空,本字段无效
            EditAutoInstallRequirements = !string.IsNullOrWhiteSpace(project.RequirementsPath);
            EditPipMirrorUrl = string.IsNullOrWhiteSpace(project.PipMirrorUrl) ? DefaultPipMirror : project.PipMirrorUrl;
            IsCreatingVenv = false;
            VenvProgressText = string.Empty;

            // Git:从已有 project 读取
            EditGitUrl = project.GitUrl;
            EditCloneDirectory = project.CloneDirectory;
            EditRemoteName = string.IsNullOrWhiteSpace(project.RemoteName) ? "origin" : project.RemoteName;
            LocalGitHint = string.Empty;
            IsDetectingLocalGit = false;
            _lastDetectedRemoteUrl = string.Empty;
            _lastDetectedRemoteName = EditRemoteName;

            // 同步已有参数到编辑表单(修复 Bug B:之前只 Clear() 没有 Add,导致编辑打开工具后参数列表空白)
            EditParameters.Clear();
            foreach (var a in (project.Arguments ?? new List<ToolArgument>()).OrderBy(x => x.Order))
            {
                EditParameters.Add(new ParameterRow
                {
                    Prefix = a.UseDefaultPrefix
                        ? (project.DefaultArgumentPrefix ?? "--")
                        : (a.Prefix ?? string.Empty),
                    Name = a.Name ?? string.Empty,
                    Value = a.DefaultValue ?? string.Empty,
                    RequireInput = a.RequireInput
                });
            }

            IsEditing = true;
            IsNewProject = false;
            EditNotification = CloneNotification(project.Notification);
            SelectedTabIndex = 0;
            IsSecretsVisible = false;

            _ = RefreshGitEnvironmentAsync();
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
        private async Task BrowseRequirementsAsync()
        {
            var picked = await _filePickerService.PickFileAsync("选择 requirements.txt", "*.txt");
            if (!string.IsNullOrEmpty(picked))
                EditRequirementsPath = picked;
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
                    RemoteName = string.IsNullOrWhiteSpace(EditRemoteName) ? "origin" : EditRemoteName,
                    Arguments = args,
                    Notification = CloneNotification(EditNotification),
                    IsSystemBuiltin = false, // 用户新建的工具总是非系统内置
                    SyncState = ToolSyncState.Synced,
                    // Python venv 字段(仅 python 工具使用)
                    CreateVenv = EditCreateVenv,
                    VenvDirectory = EditVenvDirectory ?? string.Empty,
                    RequirementsPath = !EditCreateVenv || !EditAutoInstallRequirements
                        ? string.Empty
                        : (EditRequirementsPath ?? string.Empty),
                    PipMirrorUrl = !EditCreateVenv || !EditAutoInstallRequirements
                        ? string.Empty
                        : (EditPipMirrorUrl ?? string.Empty)
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
                    GitUrl = EditGitUrl,
                    CloneDirectory = EditCloneDirectory,
                    RemoteName = string.IsNullOrWhiteSpace(EditRemoteName) ? "origin" : EditRemoteName,
                    Arguments = args,
                    Notification = CloneNotification(EditNotification),
                    IsSystemBuiltin = SelectedProject.IsSystemBuiltin,
                    IsRunning = SelectedProject.IsRunning,
                    ProcessId = SelectedProject.ProcessId,
                    // Python venv 字段(仅 python 工具使用;未勾选自动安装时清空 requirements)
                    CreateVenv = EditCreateVenv,
                    VenvDirectory = EditVenvDirectory ?? string.Empty,
                    RequirementsPath = !EditCreateVenv || !EditAutoInstallRequirements
                        ? string.Empty
                        : (EditRequirementsPath ?? string.Empty),
                    PipMirrorUrl = !EditCreateVenv || !EditAutoInstallRequirements
                        ? string.Empty
                        : (EditPipMirrorUrl ?? string.Empty)
                };
                updateId = SelectedProject.Id;
            }

            try
            {
                ToolEntity? saved;
                // 选择正确的同步状态:新建 = PendingCreate;编辑现有待同步的 = PendingUpdate
                newProject.SyncState = isNew ? ToolSyncState.PendingCreate : ToolSyncState.PendingUpdate;

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
                    refreshed.SyncState = ToolSyncState.Synced;
                    // 保留客户端维护的运行状态
                    refreshed.IsRunning = newProject.IsRunning;
                    refreshed.ProcessId = newProject.ProcessId;

                    if (updateId is long uid)
                    {
                        var index = Projects.IndexOf(SelectedProject!);
                        if (index >= 0) Projects[index] = refreshed;
                        SelectedProject = refreshed;
                        // 编辑成功:不再需要 pending 跟踪
                        RemoveFromPending(refreshed.Id);
                    }
                    else
                    {
                        // 新建工具成功 → 推到主列表
                        Projects.Insert(0, refreshed);
                        SelectedProject = refreshed;
                        // 若之前是 fallback 落地的 pending 新建,清理 pending 集合与旧记录
                        if (newProject.LocalTempId != 0)
                            RemoveFromPending(newProject.LocalTempId);
                    }
                    await _persistence.SaveAsync(AllPersistable()); // 离线缓存写穿
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
        /// 后端不可达时，把新建/编辑的工具作为「待同步用户工具」:
        /// 1) 同时插入 Projects 头部（让用户在主列表看见，但带「待同步」徽标）；
        /// 2) 跟踪加入 PendingSyncTools；
        /// 3) await 真正落盘（修复旧实现"假装保存"的 bug）。
        /// </summary>
        private async Task FallbackToPendingAsync(ToolProject project, bool isNew)
        {
            project.IsSystemBuiltin = false;
            project.SyncState = isNew ? ToolSyncState.PendingCreate : ToolSyncState.PendingUpdate;

            if (isNew)
            {
                // 负时间戳作为本地临时 ID,不参与序列化;同步成功后被真实 ID 替换。
                if (project.Id <= 0)
                {
                    project.LocalTempId = -((long)DateTime.UtcNow.Ticks);
                    project.Id = project.LocalTempId;
                }
                Projects.Insert(0, project);
                PendingSyncTools.Add(project);
            }
            else
            {
                // 编辑现有记录:同 ID 替换 Projects 中的卡片;pending 集合同步替换
                var existing = Projects.FirstOrDefault(p => p.Id == project.Id);
                if (existing != null)
                {
                    var idx = Projects.IndexOf(existing);
                    Projects[idx] = project;
                }
                else
                {
                    Projects.Insert(0, project);
                }

                var matchPending = PendingSyncTools.FirstOrDefault(p => p.Id == project.Id);
                if (matchPending != null)
                {
                    var idx = PendingSyncTools.IndexOf(matchPending);
                    PendingSyncTools[idx] = project;
                }
                else
                {
                    PendingSyncTools.Add(project);
                }
            }

            OnPropertyChanged(nameof(HasPendingSync));

            try
            {
                await _persistence.SaveAsync(AllPersistable());
            }
            catch (Exception saveEx)
            {
                Debug.WriteLine($"[ToolLibrary] 本地持久化失败:{saveEx.Message}");
            }

            await _dialogService.ShowMessageAsync("已保存为本地用户工具",
                "后台管理系统暂不可达，该工具已显示在主列表（带「🟠 待同步」徽标）。" +
                "后端恢复后，点击「📤 同步本地」按钮或重启应用即可自动重试推送。");
        }

        /// <summary>
        /// 统一的待持久化集合 = 主列表 + 待同步列表（前者已包含后者,这里去重）。
        /// </summary>
        private List<ToolProject> AllPersistable()
        {
            var pendingIds = new HashSet<long>(PendingSyncTools.Select(p => p.Id));
            var mainOnly = Projects.Where(p => !pendingIds.Contains(p.Id)).ToList();
            return PendingSyncTools.Concat(mainOnly).ToList();
        }

        /// <summary>
        /// 从 PendingSyncTools 中移除指定 ID 的项(用于同步成功后的清理)。
        /// </summary>
        private void RemoveFromPending(long id)
        {
            for (int i = PendingSyncTools.Count - 1; i >= 0; i--)
            {
                if (PendingSyncTools[i].Id == id)
                {
                    PendingSyncTools.RemoveAt(i);
                    break;
                }
            }
            OnPropertyChanged(nameof(HasPendingSync));
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
                    RequireInput = row.RequireInput,
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
                // 待同步的(本地创建/编辑)工具:不调后端 API,直接在两个集合中移除并落盘
                if (project.SyncState == ToolSyncState.PendingCreate
                    || project.SyncState == ToolSyncState.PendingUpdate)
                {
                    Projects.Remove(project);
                    RemoveFromPending(project.Id);
                    await _persistence.SaveAsync(AllPersistable());
                    return;
                }

                // 已同步工具:走后端删除
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
                await _persistence.SaveAsync(AllPersistable()); // 离线缓存同步
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("删除失败", ex.Message);
            }
        }

        [RelayCommand]
        private async Task SyncLocalToolsAsync()
        {
            if (IsBatchSyncing || SingleSyncingId != null)
            {
                await _dialogService.ShowMessageAsync("提示", "已有同步任务进行中，请等待完成后再试。");
                return;
            }
            if (PendingSyncTools.Count == 0)
            {
                await _dialogService.ShowMessageAsync("提示", "当前无待同步的用户工具。");
                return;
            }

            IsBatchSyncing = true;
            int success = 0, failed = 0;
            // 用快照避免在迭代中修改集合;但替换卡片需要在 Projects 中找到原引用
            var snapshot = PendingSyncTools.ToList();
            foreach (var pending in snapshot)
            {
                var result = await SyncSingleToolAsync(pending, raiseUiEvents: false);
                if (result.Success) success++;
                else failed++;
            }

            IsBatchSyncing = false;
            OnPropertyChanged(nameof(HasPendingSync));
            // 一次原子持久化,避免旧实现两次 fire-and-forget 写同一文件造成竞态
            try
            {
                await _persistence.SaveAsync(AllPersistable());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ToolLibrary] 同步后持久化失败:{ex.Message}");
            }

            await _dialogService.ShowMessageAsync("同步完成",
                $"成功推送 {success} 个工具到后台管理系统。\n失败 {failed} 个（请检查网络/后端状态后重试）。");
        }

        /// <summary>
        /// 单个工具同步入口,同时被卡片按钮和批量同步循环复用。
        /// </summary>
        /// <param name="project">待同步的工具。</param>
        /// <param name="raiseUiEvents">
        /// true = 由卡片按钮触发,需要弹成功/失败对话框,并更新卡片同步状态;
        /// false = 由批量同步内部调用,只返回值,UI 反馈交给批量循环统一汇总。
        /// </param>
        private async Task<SyncToolResult> SyncSingleToolAsync(ToolProject project, bool raiseUiEvents)
        {
            if (project == null) return SyncToolResult.Failed("project is null");

            // 单条同步:同一时刻只允许一条;批量同步会先占住 IsBatchSyncing 来阻止并发
            if (SingleSyncingId != null && SingleSyncingId != project.Id)
            {
                return SyncToolResult.Failed("另一条工具正在同步中");
            }

            SingleSyncingId = project.Id;
            project.IsSyncing = true;
            try
            {
                ToolEntity? saved;
                if (project.SyncState == ToolSyncState.PendingUpdate && project.Id > 0)
                {
                    saved = await _apiService.PutAsync<ToolEntity>(
                        $"/tools/{project.Id}", MapToEntity(project));
                }
                else
                {
                    saved = await _apiService.PostAsync<ToolEntity>(
                        "/tools", MapToEntity(project));
                }

                if (saved != null)
                {
                    var refreshed = MapToProject(saved);
                    refreshed.SyncState = ToolSyncState.Synced;
                    // 保留客户端维护的运行状态(刷新后卡片不要被重置)
                    refreshed.IsRunning = project.IsRunning;
                    refreshed.ProcessId = project.ProcessId;
                    refreshed.Status = project.Status;

                    // 就地替换 Projects 中相同 ID 的卡片(避免重复行)
                    var existingIdx = -1;
                    for (int i = 0; i < Projects.Count; i++)
                    {
                        if (Projects[i].Id == project.Id) { existingIdx = i; break; }
                    }
                    if (existingIdx >= 0)
                        Projects[existingIdx] = refreshed;
                    else
                        Projects.Insert(0, refreshed);

                    // 从 pending 集合移除
                    RemoveFromPending(project.Id);
                    OnPropertyChanged(nameof(HasPendingSync));

                    if (raiseUiEvents)
                    {
                        await _persistence.SaveAsync(AllPersistable());
                        await _dialogService.ShowMessageAsync("同步成功",
                            $"「{refreshed.Name}」已推送到后台管理系统。");
                    }

                    return SyncToolResult.Ok();
                }

                if (raiseUiEvents)
                {
                    await _dialogService.ShowMessageAsync("同步失败",
                        $"「{project.Name}」未收到后端响应，请检查网络或后端状态后重试。");
                }
                return SyncToolResult.Failed("后端无响应");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ToolLibrary] 同步工具 {project.Name} 失败:{ex.Message}");
                if (raiseUiEvents)
                {
                    await _dialogService.ShowMessageAsync("同步失败",
                        $"「{project.Name}」推送失败:{ex.Message}");
                }
                return SyncToolResult.Failed(ex.Message);
            }
            finally
            {
                project.IsSyncing = false;
                SingleSyncingId = null;
            }
        }

        [RelayCommand]
        private async Task SyncSingleToolCommand(ToolProject project)
        {
            if (project == null) return;
            if (!project.IsPendingSync) return;
            if (IsBatchSyncing)
            {
                await _dialogService.ShowMessageAsync("提示", "批量同步进行中，请等待完成后再单独同步。");
                return;
            }
            await SyncSingleToolAsync(project, raiseUiEvents: true);
        }

        private readonly struct SyncToolResult
        {
            public bool Success { get; }
            public string? Error { get; }

            private SyncToolResult(bool success, string? error)
            {
                Success = success;
                Error = error;
            }

            public static SyncToolResult Ok() => new(true, null);
            public static SyncToolResult Failed(string? error) => new(false, error);
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

            // 2) Python 虚拟环境钩子(仅脚本模式 + Language=python + CreateVenv=true)
            if (project.RunModeEnum == ToolRunMode.Script
                && string.Equals(project.Language, "python", StringComparison.OrdinalIgnoreCase)
                && project.CreateVenv)
            {
                _venvCts ??= new CancellationTokenSource();
                IsCreatingVenv = true;
                VenvProgressText = "🐍 正在准备 Python 环境...";
                var venvResult = await _dialogService.ShowVenvProgressAsync(
                    "准备 Python 环境",
                    async (progress, ct) => await EnsurePythonVenvAsync(project, progress, ct),
                    _venvCts);
                IsCreatingVenv = false;
                if (!venvResult.Success)
                {
                    if (!venvResult.Cancelled)
                        await _dialogService.ShowMessageAsync("创建 venv 失败",
                            venvResult.ErrorMessage + (venvResult.Logs.Count > 0
                                ? "\n\n最近日志:\n" + string.Join("\n", venvResult.Logs.TakeLast(8))
                                : string.Empty));
                    return;
                }
                // 关键:本次运行覆盖 project.InterpreterPath 为 venv 的 python
                // (project 是运行期对象,本次有效;若用户保存工具,后端保留的是原 InterpreterPath)
                project.InterpreterPath = venvResult.PythonExePath;
            }

            // 3) 准备 EditableArgument：仅 RequireInput=true 的才在弹窗里展示（按需求 4）
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

            // 4) 构造初始命令预览
            var initialCmd = BuildCommandPreview(project, editable);

            // 5) 启动确认弹窗（除非用户之前勾选了"不再询问"）
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

            // 6) 用最终命令构造 ProcessStartInfo，启动进程
            var (psi, finalCmd) = BuildProcessStartInfo(project, confirmation?.Arguments ?? editable);

            var pid = _processManager.Start(psi, captureOutput: true);
            if (pid == 0)
            {
                await _dialogService.ShowMessageAsync("错误", $"进程启动失败。\n命令：{finalCmd}");
                return;
            }

            // 7) 标记项目为运行中
            project.IsRunning = true;
            project.ProcessId = pid;
            project.Status = "运行中";

            // 追踪命令和启动时间,供退出后展示结果弹窗
            _runningCommands[pid] = finalCmd;
            _runningStartTimes[pid] = DateTime.Now;

            // 8) 触发启动通知（后台异步 fire-and-forget）
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

        /// <summary>
        /// 创建/复用 Python venv(若勾选 CreateVenv 且 Language=python)。
        /// 失败时弹出错误对话框并返回 Success=false;成功返回完整 VenvResult。
        /// </summary>
        private async Task<VenvResult> EnsurePythonVenvAsync(ToolProject project,
            IProgress<string>? venvProgress = null, CancellationToken ct = default)
        {
            try
            {
                // 解析 venv 目录:用户未填 → 默认 {WorkingDirectory}/.venv
                var workingDir = string.IsNullOrWhiteSpace(project.WorkingDirectory)
                    ? ResolveDefaultWorkingDirectory()
                    : project.WorkingDirectory;
                var venvDir = string.IsNullOrWhiteSpace(project.VenvDirectory)
                    ? System.IO.Path.Combine(workingDir, ".venv")
                    : (System.IO.Path.IsPathRooted(project.VenvDirectory)
                        ? project.VenvDirectory
                        : System.IO.Path.Combine(workingDir, project.VenvDirectory));

                // 解析创建 venv 用的"宿主"python:优先用 InterpreterPath,否则自动探测
                string hostPython = project.InterpreterPath;
                if (string.IsNullOrWhiteSpace(hostPython))
                {
                    var env = AvailableEnvironments.FirstOrDefault(e =>
                        string.Equals(e.Language, project.Language, StringComparison.OrdinalIgnoreCase));
                    if (env == null || !env.IsAvailable)
                        env = await _runtimeEnvService.ReDetectAsync(project.Language);
                    hostPython = env?.ExecutablePath ?? string.Empty;
                }
                if (string.IsNullOrWhiteSpace(hostPython))
                {
                    return new VenvResult
                    {
                        ErrorMessage = "未找到可用的 Python 解释器,无法创建虚拟环境。\n请在「工具信息」面板的启动配置中指定解释器路径,或安装 Python 后重试。"
                    };
                }

                // 仅当 EditAutoInstallRequirements=true 时才传 requirements
                string reqPath = project.CreateVenv && !string.IsNullOrWhiteSpace(project.RequirementsPath)
                    ? project.RequirementsPath
                    : string.Empty;
                string mirror = reqPath.Length > 0 && !string.IsNullOrWhiteSpace(project.PipMirrorUrl)
                    ? project.PipMirrorUrl
                    : string.Empty;

                var result = await _venvService.EnsureVenvAsync(
                    hostPython, venvDir, reqPath, mirror, venvProgress, ct);

                return result;
            }
            catch (OperationCanceledException)
            {
                return new VenvResult { Cancelled = true, ErrorMessage = "已取消" };
            }
            catch (Exception ex)
            {
                return new VenvResult { ErrorMessage = ex.Message };
            }
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
            Dispatcher.UIThread.Post(() =>
            {
                var p = Projects.FirstOrDefault(x => x.ProcessId == pid);
                if (p == null) return;
                p.IsRunning = false;
                p.ProcessId = null;
                p.Status = exitCode == 0 ? "已停止" : "异常退出";

                var (stdout, stderr) = _processManager.GetDetailedOutput(pid);
                _runningCommands.TryRemove(pid, out var cmdLine);
                _runningStartTimes.TryRemove(pid, out var startTime);
                var elapsed = startTime != default ? (long)(DateTime.Now - startTime).TotalMilliseconds : 0;
                var result = new ProcessRunResult
                {
                    Success = exitCode == 0,
                    ExitCode = exitCode,
                    StandardOutput = stdout,
                    StandardError = stderr,
                    CommandLine = cmdLine ?? string.Empty,
                    ElapsedMilliseconds = elapsed,
                    ProcessId = pid
                };
                p.LastRunResult = result;

                var snapshot = new ToolRunSnapshot
                {
                    StartTime = startTime != default ? startTime : DateTime.Now.AddMilliseconds(-1000),
                    EndTime = DateTime.Now,
                    ProcessId = pid,
                    WorkingDirectory = p.WorkingDirectory,
                    ExitCode = exitCode,
                    Output = stdout,
                    Trigger = exitCode == 0 ? NotificationTrigger.Success : NotificationTrigger.Failure
                };
                _ = FireNotificationAsync(p, snapshot);

                _ = ShowRunResultDialogAsync(p, result);
            });
        }

        private async Task ShowRunResultDialogAsync(ToolProject project, ProcessRunResult result)
        {
            try
            {
                await _dialogService.ShowOutputAsync(project.Name, result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RunResult] 展示执行结果异常：{ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ViewToolLogAsync(ToolProject? project)
        {
            if (project?.LastRunResult == null) return;
            try
            {
                await _dialogService.ShowOutputAsync(project.Name, project.LastRunResult);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ViewLog] 展示日志异常：{ex.Message}");
            }
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
                // 修复:把 prefix 和 name 拼成单个 token (如 "--rows"),而不是分别以空格分隔
                // (旧实现产生 "-- rows",中间多一个空格,违反大多数 CLI 工具的规范)
                var flag = (prefix ?? string.Empty) + (ea.Source.Name ?? string.Empty);
                if (flag.Length > 0) sb.Append(' ').Append(QuoteIfNeeded(flag));
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
                // 修复:把 prefix 和 name 拼成单个 token (如 "--rows"),而不是分别以空格分隔
                var flag = (prefix ?? string.Empty) + (ea.Source.Name ?? string.Empty);
                if (flag.Length > 0) sb.Append(' ').Append(QuoteIfNeeded(flag));
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
            {
                EditCloneDirectory = picked;
                // 浏览后立即触发一次检测,跳过防抖延迟
                _localDetectTimer.Stop();
                await DetectLocalGitNowAsync();
            }
        }

        /// <summary>
        /// 逐字追踪 EditCloneDirectory 变化,500ms 防抖后自动检测本地 .git。
        /// </summary>
        partial void OnEditCloneDirectoryChanged(string value)
        {
            // 启动防抖;若用户继续输入则 timer 会被重置到下一次 Tick
            _localDetectTimer.Stop();
            if (string.IsNullOrWhiteSpace(value))
            {
                // 清空目录 → 清状态
                LocalGitHint = string.Empty;
                _lastDetectedRemoteUrl = string.Empty;
                _lastDetectedRemoteName = "origin";
                return;
            }
            _localDetectTimer.Start();
        }

        /// <summary>
        /// 从脚本路径向上搜索 .git，找到后设为克隆目录。
        /// </summary>
        partial void OnEditScriptPathChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var root = FindGitRoot(value);
            if (root != null)
            {
                EditCloneDirectory = root;
            }
        }

        /// <summary>向上搜索包含 .git 的目录</summary>
        private static string? FindGitRoot(string scriptPath)
        {
            var dir = System.IO.Path.GetDirectoryName(scriptPath);
            while (dir != null)
            {
                if (Directory.Exists(System.IO.Path.Combine(dir, ".git")))
                    return dir;
                var parent = System.IO.Path.GetDirectoryName(dir);
                if (string.Equals(parent, dir, StringComparison.OrdinalIgnoreCase)) break;
                dir = parent;
            }
            return null;
        }

        /// <summary>校验脚本路径是否在克隆目录内，不在则弹窗提示</summary>
        private async Task ValidateScriptInCloneDirAsync()
        {
            if (string.IsNullOrWhiteSpace(EditScriptPath) || string.IsNullOrWhiteSpace(EditCloneDirectory))
                return;
            var script = System.IO.Path.GetFullPath(EditScriptPath).TrimEnd('\\', '/');
            var clone = System.IO.Path.GetFullPath(EditCloneDirectory).TrimEnd('\\', '/');
            if (!script.StartsWith(clone, StringComparison.OrdinalIgnoreCase))
            {
                await _dialogService.ShowMessageAsync("路径不匹配",
                    $"脚本路径不在克隆目录中：\n  脚本：{EditScriptPath}\n  克隆目录：{EditCloneDirectory}\n\n" +
                    "请检查脚本路径是否属于该 Git 仓库。");
            }
        }

        /// <summary>
        /// 立即执行一次本地 .git 检测(被防抖 Tick 或手动"🔍 检测"按钮触发)。
        /// 不修改 EditWorkingDirectory,只更新 LocalGitHint / EditGitUrl / EditRemoteName / _lastDetectedRemoteUrl。
        /// </summary>
        [RelayCommand]
        public async Task DetectLocalGitNowAsync()
        {
            var dir = EditCloneDirectory;
            if (string.IsNullOrWhiteSpace(dir))
            {
                LocalGitHint = "请先填写克隆目标父目录或选择本地已含 .git 的目录。";
                return;
            }
            IsDetectingLocalGit = true;
            try
            {
                var (remoteName, url, logs) = await _gitService.DetectRemoteAsync(dir, "origin");
                if (url != null)
                {
                    _lastDetectedRemoteUrl = url;
                    _lastDetectedRemoteName = remoteName;
                    EditRemoteName = remoteName;
                    EditGitUrl = url;
                    LocalGitHint = $"🔍 已检测到本地仓库,remote={remoteName},URL={url}\n" +
                                   "Git URL 已自动填入。";
                    _ = ValidateScriptInCloneDirAsync();
                }
                else
                {
                    LocalGitHint = "❌ 该目录不是 Git 仓库或 .git/config 不可读。\n" +
                                   (logs.Count > 0 ? "最近日志:\n" + string.Join("\n", logs.TakeLast(4)) : string.Empty);
                }
            }
            catch (Exception ex)
            {
                LocalGitHint = "❌ 检测异常:" + ex.Message;
            }
            finally
            {
                IsDetectingLocalGit = false;
            }
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
                        $"仓库已克隆到：\n{result.RepoRoot}\n\n可在「工具信息」的启动配置中选择脚本。",
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
        // Python 虚拟环境:「📦 立即创建并安装依赖」按钮
        // =========================================================

        [RelayCommand]
        private async Task CreateVenvNowAsync()
        {
            if (!EditCreateVenv)
            {
                await _dialogService.ShowMessageAsync("提示", "请先勾选「启动前自动创建虚拟环境并安装依赖」再操作。");
                return;
            }
            if (string.IsNullOrWhiteSpace(EditWorkingDirectory))
            {
                await _dialogService.ShowMessageAsync("错误", "请先填写工作目录,虚拟环境将在工作目录下创建。");
                return;
            }
            _venvCts = new CancellationTokenSource();
            _ = RunVenvBackgroundAsync();
            await Task.CompletedTask;
        }

        [RelayCommand]
        private void CancelCreateVenvAsync()
        {
            try { _venvCts?.Cancel(); } catch { }
        }

        private async Task RunVenvBackgroundAsync()
        {
            IsCreatingVenv = true;
            VenvProgressText = "⏳ 准备创建虚拟环境…";
            // 用一个临时 ToolProject 投影当前 EditX → Project 模型
            var tmpProject = new ToolProject
            {
                Language = EditLanguage,
                InterpreterPath = EditInterpreterPath,
                WorkingDirectory = EditWorkingDirectory,
                CreateVenv = EditCreateVenv,
                VenvDirectory = EditVenvDirectory ?? string.Empty,
                RequirementsPath = (EditCreateVenv && EditAutoInstallRequirements)
                    ? (EditRequirementsPath ?? string.Empty)
                    : string.Empty,
                PipMirrorUrl = (EditCreateVenv && EditAutoInstallRequirements)
                    ? (EditPipMirrorUrl ?? string.Empty)
                    : string.Empty
            };

            try
            {
                _venvCts ??= new CancellationTokenSource();
                var progress = new Progress<string>(line => VenvProgressText = line);
                var result = await EnsurePythonVenvAsync(tmpProject, progress, _venvCts.Token);
                if (result.Success)
                {
                    var msg = result.VenvCreated
                        ? $"虚拟环境已创建：{result.VenvPath}"
                        : "虚拟环境已存在";
                    if (result.PipInstalled) msg += "\n依赖已按 requirements.txt 安装完成。";
                    else if (!string.IsNullOrEmpty(tmpProject.RequirementsPath)) msg += "\n(本次未执行 pip install)";
                    // 回填 venv 的 python 解释器路径,让 EditInterpreterPath 反映真正在用的解释器
                    if (!string.IsNullOrEmpty(result.PythonExePath))
                        EditInterpreterPath = result.PythonExePath;
                    await _dialogService.ShowMessageAsync("创建成功", msg);
                }
                else
                {
                    if (result.Cancelled)
                        await _dialogService.ShowMessageAsync("已取消", "虚拟环境创建已取消。");
                    else
                        await _dialogService.ShowMessageAsync("创建失败",
                            result.ErrorMessage + (result.Logs.Count > 0
                                ? "\n\n最近日志:\n" + string.Join("\n", result.Logs.TakeLast(8))
                                : string.Empty));
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("创建 venv 异常", ex.Message);
            }
            finally
            {
                IsCreatingVenv = false;
                VenvProgressText = string.Empty;
                _venvCts?.Dispose();
                _venvCts = null;
            }
        }

        // =========================================================
        // 拉取更新（仅编辑现有项目时使用，对应工作目录里已有仓库）
        // =========================================================

        [RelayCommand]
        private async Task StartPullAsync()
        {
            if (string.IsNullOrWhiteSpace(EditCloneDirectory))
            {
                await _dialogService.ShowMessageAsync("提示", "克隆目录为空，无法拉取。请先在「🧩 环境与依赖」- Git 仓库区域设置克隆目录。");
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
                // 缺 .git 时:若用户已设置 EditGitUrl,弹确认后让 PullAsync 先 git init + remote add
                string? initUrl = null;
                string initRemoteName = EditRemoteName ?? "origin";
                var dir = EditCloneDirectory;
                bool dirExists = !string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir);
                bool hasGit = dirExists && Directory.Exists(System.IO.Path.Combine(dir, ".git"));
                if (dirExists && !hasGit && !string.IsNullOrWhiteSpace(EditGitUrl))
                {
                    var confirmTitle = "初始化并绑定远端";
                    var confirmMsg =
                        $"克隆目录不是 Git 仓库:\n{dir}\n\n" +
                        $"将执行:\n  git init\n  git remote add {initRemoteName} {EditGitUrl}\n  git pull\n\n" +
                        "是否继续?";
                    var ok = await _dialogService.ShowConfirmAsync(confirmTitle, confirmMsg);
                    if (!ok)
                    {
                        IsPulling = false;
                        PullProgressText = string.Empty;
                        _pullCts?.Dispose();
                        _pullCts = null;
                        return;
                    }
                    initUrl = EditGitUrl;
                }
                else if (dirExists && !hasGit && string.IsNullOrWhiteSpace(EditGitUrl))
                {
                    await _dialogService.ShowMessageAsync("提示",
                        "该目录不是 Git 仓库,且未填写 Git 远程地址。\n请先在「📥 Git 仓库」区域填写 Git URL,系统会在拉取前自动 git init 并绑定远端。");
                    IsPulling = false;
                    PullProgressText = string.Empty;
                    _pullCts?.Dispose();
                    _pullCts = null;
                    return;
                }

                var result = await _gitService.PullAsync(
                    EditCloneDirectory, progress, _pullCts!.Token,
                    initUrl: initUrl, initRemoteName: initRemoteName);

                if (result.Success)
                {
                    await _dialogService.ShowCloneLogAsync(
                        "📥 拉取成功",
                        $"已更新到最新：\n{EditCloneDirectory}",
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
