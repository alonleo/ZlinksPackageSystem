using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        private CancellationTokenSource? _cloneCts;

        [ObservableProperty]
        private ObservableCollection<ToolProject> _projects = new();

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

        // ===== 编辑表单字段 - 运行模式 =====
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
        [ObservableProperty] private bool _isNewProject;

        public ToolLibraryViewModel(
            IApiService apiService,
            IDialogService dialogService,
            IRuntimeEnvironmentService runtimeEnvService,
            IFilePickerService filePickerService,
            IProcessManagerService processManager,
            IGitService gitService,
            IToolPersistenceService persistence)
        {
            Title = "工具库";
            _apiService = apiService;
            _dialogService = dialogService;
            _runtimeEnvService = runtimeEnvService;
            _filePickerService = filePickerService;
            _processManager = processManager;
            _gitService = gitService;
            _persistence = persistence;

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
                var loaded = await _persistence.LoadAsync();
                if (loaded.Count > 0)
                {
                    Projects = new ObservableCollection<ToolProject>(loaded);
                }
                else
                {
                    LoadMockData();
                    _ = _persistence.SaveAsync(Projects);
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
                new() { Id = 1, Name = "APK 打包工具", Description = "Android APK 自动化打包、签名、对齐一站式工具", Category = "打包", Version = "v2.1.0", Status = "未运行", Manager = "张三", CreateTime = DateTime.Now.AddDays(-30), Language = "python", ScriptPath = @"C:\tools\apk_builder.py", DefaultArgumentPrefix = "--" },
                new() { Id = 2, Name = "资源压缩器", Description = "图片/音频资源无损压缩，减小包体积", Category = "优化", Version = "v1.5.3", Status = "未运行", Manager = "李四", CreateTime = DateTime.Now.AddDays(-20), Language = "node", ScriptPath = @"C:\tools\compress.js", DefaultArgumentPrefix = "--" },
                new() { Id = 3, Name = "渠道分包", Description = "多渠道自动分包与渠道号注入工具", Category = "分发", Version = "v3.0.0", Status = "未运行", Manager = "王五", CreateTime = DateTime.Now.AddDays(-15), Language = "java", ScriptPath = @"C:\tools\ChannelPackage.java", DefaultArgumentPrefix = "--" },
                new() { Id = 4, Name = "崩溃分析", Description = "Crash 日志采集、符号化、趋势分析平台", Category = "监控", Version = "v1.8.2", Status = "未运行", Manager = "赵六", CreateTime = DateTime.Now.AddDays(-10), Language = "go", ScriptPath = @"C:\tools\crash_analyzer.go", DefaultArgumentPrefix = "--" },
                new() { Id = 5, Name = "热更新平台", Description = "基于 IL2CPP 的热更新补丁生成与下发", Category = "更新", Version = "v2.3.1", Status = "未运行", Manager = "孙七", CreateTime = DateTime.Now.AddDays(-5), Language = "python", ScriptPath = @"C:\tools\hot_patch.py", DefaultArgumentPrefix = "--" },
                new() { Id = 6, Name = "性能测试", Description = "游戏帧率、内存、CPU 性能自动化测试", Category = "测试", Version = "v1.2.0", Status = "未运行", Manager = "周八", CreateTime = DateTime.Now.AddDays(-3), Language = "python", ScriptPath = @"C:\tools\perf_test.py", DefaultArgumentPrefix = "--" },
                new() { Id = 7, Name = "ffmpeg 转码器", Description = "直接调用本地 ffmpeg.exe 批量转码", Category = "本地工具", Version = "v6.0", Status = "未运行", Manager = "吴九", CreateTime = DateTime.Now.AddDays(-1), RunMode = ToolRunMode.LocalExecutable, ExecutablePath = @"C:\tools\ffmpeg.exe", DefaultArgumentPrefix = "-" }
            };
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

            EditRunMode = project.RunMode;
            EditLanguage = project.Language;
            SelectedEnvironment = AvailableEnvironments.FirstOrDefault(e => e.Language == project.Language);
            EditInterpreterPath = project.InterpreterPath;
            EditScriptPath = project.ScriptPath;
            EditExecutablePath = project.ExecutablePath;
            EditWorkingDirectory = project.WorkingDirectory;

            EditParameters.Clear();

            IsEditing = true;
            IsNewProject = false;
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
        private void SaveProject()
        {
            if (string.IsNullOrWhiteSpace(EditName)) return;

            if (SelectedProject == null)
            {
                var newProject = new ToolProject
                {
                    Id = Projects.Count > 0 ? Projects.Max(p => p.Id) + 1 : 1,
                    Name = EditName,
                    Description = EditDescription,
                    Category = EditCategory,
                    Version = EditVersion,
                    Status = string.IsNullOrEmpty(EditStatus) ? "未运行" : EditStatus,
                    Manager = EditManager,
                    CreateTime = DateTime.Now,
                    RunMode = EditRunMode,
                    Language = EditLanguage,
                    InterpreterPath = EditInterpreterPath,
                    ScriptPath = EditScriptPath,
                    ExecutablePath = EditExecutablePath,
                    WorkingDirectory = string.IsNullOrWhiteSpace(EditWorkingDirectory)
                        ? ResolveDefaultWorkingDirectory()
                        : EditWorkingDirectory,
                    GitUrl = EditGitUrl,
                    CloneDirectory = EditCloneDirectory
                };
                Projects.Add(newProject);
            }
            else
            {
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
                        Status = string.IsNullOrEmpty(EditStatus) ? "未运行" : EditStatus,
                        Manager = EditManager,
                        CreateTime = SelectedProject.CreateTime,
                        RunMode = EditRunMode,
                        Language = EditLanguage,
                        InterpreterPath = EditInterpreterPath,
                        ScriptPath = EditScriptPath,
                        ExecutablePath = EditExecutablePath,
                        WorkingDirectory = string.IsNullOrWhiteSpace(EditWorkingDirectory)
                            ? ResolveDefaultWorkingDirectory()
                            : EditWorkingDirectory,
                        GitUrl = SelectedProject.GitUrl,
                        CloneDirectory = SelectedProject.CloneDirectory,
                        // 保留运行状态
                        IsRunning = SelectedProject.IsRunning,
                        ProcessId = SelectedProject.ProcessId
                    };
                }
            }

            _ = _persistence.SaveAsync(Projects);
            IsEditing = false;
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
            Projects.Remove(project);
            _ = _persistence.SaveAsync(Projects);
            await Task.CompletedTask;
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
            if (project.RunMode == ToolRunMode.Script)
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

            var pid = _processManager.Start(psi);
            if (pid == 0)
            {
                await _dialogService.ShowMessageAsync("错误", $"进程启动失败。\n命令：{finalCmd}");
                return;
            }

            // 6) 标记项目为运行中
            project.IsRunning = true;
            project.ProcessId = pid;
            project.Status = "运行中";
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
            });
        }

        // =========================================================
        // 命令行拼装 / 进程启动
        // =========================================================
        public string BuildCommandPreview(ToolProject project, IEnumerable<EditableArgument>? args = null)
        {
            if (project == null) return string.Empty;

            string entry;
            bool interpreterIsScript = false;
            if (project.RunMode == ToolRunMode.LocalExecutable)
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
            if (project.RunMode == ToolRunMode.Script && !string.IsNullOrEmpty(project.ScriptPath))
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
            if (project.RunMode == ToolRunMode.LocalExecutable)
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
            if (project.RunMode == ToolRunMode.Script && !string.IsNullOrEmpty(project.ScriptPath))
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
            if (project.RunMode == ToolRunMode.Script && !string.IsNullOrEmpty(project.ScriptPath))
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
            if (project.RunMode == ToolRunMode.LocalExecutable && !string.IsNullOrEmpty(project.ExecutablePath))
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
    }
}
