using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        private readonly IRuntimeEnvironmentService _runtimeEnvService;
        private readonly IFilePickerService _filePickerService;

        [ObservableProperty]
        private ObservableCollection<ToolProject> _projects = new();

        [ObservableProperty]
        private ToolProject? _selectedProject;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isEditing;

        // 编辑表单字段 - 基础信息
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

        // 编辑表单字段 - 脚本执行
        [ObservableProperty]
        private string _editLanguage = "python";

        [ObservableProperty]
        private string _editInterpreterPath = string.Empty;

        [ObservableProperty]
        private string _editScriptPath = string.Empty;

        [ObservableProperty]
        private string _editWorkingDirectory = string.Empty;

        [ObservableProperty]
        private string _editEnvironmentVariables = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ToolArgument> _editArguments = new();

        // 运行时环境面板
                [ObservableProperty]
                private ObservableCollection<RuntimeEnvironment> _availableEnvironments = new();

                [ObservableProperty]
                private bool _isDetectingEnvironments;

                // 弹窗里语言下拉当前选中的环境（驱动 EditLanguage 同步）
                [ObservableProperty]
                private RuntimeEnvironment? _selectedEnvironment;

                // SelectedEnvironment 变更时，同步到 EditLanguage
                partial void OnSelectedEnvironmentChanged(RuntimeEnvironment? value)
                {
                    if (value != null)
                        EditLanguage = value.Language;
                }

        public ToolLibraryViewModel(
            IApiService apiService,
            IDialogService dialogService,
            IRuntimeEnvironmentService runtimeEnvService,
            IFilePickerService filePickerService)
        {
            Title = "工具库";
            _apiService = apiService;
            _dialogService = dialogService;
            _runtimeEnvService = runtimeEnvService;
            _filePickerService = filePickerService;
            _ = LoadProjectsAsync();
            _ = DetectEnvironmentsOnStartupAsync();
        }

        [RelayCommand]
        private async Task LoadProjectsAsync()
        {
            IsBusy = true;
            try
            {
                var result = await _apiService.GetAsync<PageResponse<ToolProject>>("/tools");
                if (result?.Records?.Count > 0)
                {
                    Projects = new ObservableCollection<ToolProject>(result.Records);
                }
                else
                {
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
                new() { Id = 1, Name = "APK 打包工具", Description = "Android APK 自动化打包、签名、对齐一站式工具", Category = "打包", Version = "v2.1.0", Status = "运行中", Manager = "张三", CreateTime = DateTime.Now.AddDays(-30), Language = "python", ScriptPath = @"C:\tools\apk_builder.py" },
                new() { Id = 2, Name = "资源压缩器", Description = "图片/音频资源无损压缩，减小包体积", Category = "优化", Version = "v1.5.3", Status = "运行中", Manager = "李四", CreateTime = DateTime.Now.AddDays(-20), Language = "node", ScriptPath = @"C:\tools\compress.js" },
                new() { Id = 3, Name = "渠道分包", Description = "多渠道自动分包与渠道号注入工具", Category = "分发", Version = "v3.0.0", Status = "维护中", Manager = "王五", CreateTime = DateTime.Now.AddDays(-15), Language = "java", ScriptPath = @"C:\tools\ChannelPackage.java" },
                new() { Id = 4, Name = "崩溃分析", Description = "Crash 日志采集、符号化、趋势分析平台", Category = "监控", Version = "v1.8.2", Status = "运行中", Manager = "赵六", CreateTime = DateTime.Now.AddDays(-10), Language = "go", ScriptPath = @"C:\tools\crash_analyzer.go" },
                new() { Id = 5, Name = "热更新平台", Description = "基于 IL2CPP 的热更新补丁生成与下发", Category = "更新", Version = "v2.3.1", Status = "运行中", Manager = "孙七", CreateTime = DateTime.Now.AddDays(-5), Language = "python", ScriptPath = @"C:\tools\hot_patch.py" },
                new() { Id = 6, Name = "性能测试", Description = "游戏帧率、内存、CPU 性能自动化测试", Category = "测试", Version = "v1.2.0", Status = "维护中", Manager = "周八", CreateTime = DateTime.Now.AddDays(-3), Language = "python", ScriptPath = @"C:\tools\perf_test.py" }
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

        // ===== 新增/编辑弹窗 =====

        [RelayCommand]
                private void OpenAddDialog()
                {
                    EditName = string.Empty;
                    EditDescription = string.Empty;
                    EditCategory = string.Empty;
                    EditVersion = string.Empty;
                    EditStatus = "运行中";
                    EditManager = string.Empty;
                    EditLanguage = "python";
                    SelectedEnvironment = AvailableEnvironments.FirstOrDefault(e => e.Language == "python")
                                         ?? AvailableEnvironments.FirstOrDefault();
                    EditInterpreterPath = string.Empty;
                    EditScriptPath = string.Empty;
                    EditWorkingDirectory = string.Empty;
                    EditEnvironmentVariables = string.Empty;
                    EditArguments = new ObservableCollection<ToolArgument>();
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
                    EditLanguage = project.Language;
                    SelectedEnvironment = AvailableEnvironments.FirstOrDefault(e => e.Language == project.Language);
                    EditInterpreterPath = project.InterpreterPath;
                    EditScriptPath = project.ScriptPath;
                    EditWorkingDirectory = project.WorkingDirectory;
                    EditEnvironmentVariables = project.EnvironmentVariables;
                    EditArguments = project.Arguments != null
                        ? new ObservableCollection<ToolArgument>(project.Arguments)
                        : new ObservableCollection<ToolArgument>();
                    IsEditing = true;
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
            // 解释器就是 .exe 或可执行文件
            var picked = await _filePickerService.PickFileAsync("选择解释器", "*");
            if (!string.IsNullOrEmpty(picked))
                EditInterpreterPath = picked;
        }

        [RelayCommand]
        private void AddArgument()
        {
            EditArguments.Add(new ToolArgument
            {
                Name = $"--arg{EditArguments.Count + 1}",
                RequireInput = false,
                InputType = ToolArgumentInputType.Text,
                Order = EditArguments.Count
            });
        }

        [RelayCommand]
        private void RemoveArgument(ToolArgument? arg)
        {
            if (arg == null) return;
            EditArguments.Remove(arg);
        }

        [RelayCommand]
        private void SaveProject()
        {
            if (string.IsNullOrWhiteSpace(EditName)) return;

            // 收集参数列表
            var args = EditArguments.ToList();

            if (SelectedProject == null)
            {
                var newProject = new ToolProject
                {
                    Id = Projects.Count > 0 ? Projects.Max(p => p.Id) + 1 : 1,
                    Name = EditName,
                    Description = EditDescription,
                    Category = EditCategory,
                    Version = EditVersion,
                    Status = EditStatus,
                    Manager = EditManager,
                    CreateTime = DateTime.Now,
                    Language = EditLanguage,
                    InterpreterPath = EditInterpreterPath,
                    ScriptPath = EditScriptPath,
                    WorkingDirectory = string.IsNullOrWhiteSpace(EditWorkingDirectory)
                        ? (string.IsNullOrEmpty(EditScriptPath) ? string.Empty : Path.GetDirectoryName(EditScriptPath) ?? string.Empty)
                        : EditWorkingDirectory,
                    EnvironmentVariables = EditEnvironmentVariables,
                    Arguments = args
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
                        Status = EditStatus,
                        Manager = EditManager,
                        CreateTime = SelectedProject.CreateTime,
                        Language = EditLanguage,
                        InterpreterPath = EditInterpreterPath,
                        ScriptPath = EditScriptPath,
                        WorkingDirectory = string.IsNullOrWhiteSpace(EditWorkingDirectory)
                            ? (string.IsNullOrEmpty(EditScriptPath) ? string.Empty : Path.GetDirectoryName(EditScriptPath) ?? string.Empty)
                            : EditWorkingDirectory,
                        EnvironmentVariables = EditEnvironmentVariables,
                        Arguments = args
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
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task OpenProjectAsync(ToolProject project)
        {
            if (project == null) return;
            await _dialogService.ShowMessageAsync(project.Name, $"进入工具：{project.Name}\n版本：{project.Version}\n描述：{project.Description}");
        }

        // ===== 运行工具 =====

        /// <summary>
        /// 评出最终命令行（用于预览），不真正执行
        /// </summary>
        public string BuildCommandPreview(ToolProject project, Dictionary<string, string>? userInputs = null)
        {
            if (project == null) return string.Empty;

            // 选解释器
            string interpreter;
            if (!string.IsNullOrWhiteSpace(project.InterpreterPath))
                interpreter = project.InterpreterPath;
            else
            {
                var env = AvailableEnvironments.FirstOrDefault(e => e.Language == project.Language && e.IsAvailable);
                interpreter = env?.ExecutablePath ?? project.Language;
            }

            // 部分语言直接执行脚本（powershell、bash、cmd），不需要解释器前缀
            bool interpreterIsScript = project.Language switch
            {
                "powershell" or "bash" or "bat" or "cmd" => true,
                _ => false
            };

            var sb = new StringBuilder();
            sb.Append(QuoteIfNeeded(interpreter));
            if (!interpreterIsScript && !string.IsNullOrEmpty(project.ScriptPath))
                sb.Append(' ').Append(QuoteIfNeeded(project.ScriptPath));

            // 拼接参数
            if (project.Arguments != null)
            {
                foreach (var arg in project.Arguments.OrderBy(a => a.Order))
                {
                    string value;
                    if (arg.RequireInput)
                    {
                        if (userInputs != null && userInputs.TryGetValue(arg.Name, out var v))
                            value = v;
                        else
                            value = arg.DefaultValue;
                    }
                    else
                    {
                        value = arg.DefaultValue;
                    }

                    sb.Append(' ').Append(QuoteIfNeeded(arg.Name));
                    if (!string.IsNullOrEmpty(value))
                    {
                        // bool 单独处理
                        if (arg.InputType == ToolArgumentInputType.Bool)
                            sb.Append(' ').Append(value == "true" ? "true" : "false");
                        else
                            sb.Append(' ').Append(QuoteIfNeeded(value));
                    }
                }
            }

            return sb.ToString();
        }

        private static string QuoteIfNeeded(string s)
        {
            if (string.IsNullOrEmpty(s)) return "\"\"";
            if (s.Contains(' ') || s.Contains('\t') || s.Contains('"'))
                return "\"" + s.Replace("\"", "\\\"") + "\"";
            return s;
        }

        [RelayCommand]
        private async Task RunToolAsync(ToolProject project)
        {
            if (project == null) return;
            if (string.IsNullOrEmpty(project.ScriptPath))
            {
                await _dialogService.ShowMessageAsync("错误", "工具未配置脚本路径，请先编辑。");
                return;
            }

            // 1. 检查脚本文件是否存在
            if (!File.Exists(project.ScriptPath))
            {
                await _dialogService.ShowMessageAsync("错误", $"脚本文件不存在：{project.ScriptPath}");
                return;
            }

            // 2. 检查运行时环境
            var env = AvailableEnvironments.FirstOrDefault(e => e.Language == project.Language);
            if (env == null || !env.IsAvailable)
            {
                env = await _runtimeEnvService.ReDetectAsync(project.Language);
            }
            if (!env.IsAvailable)
            {
                await _dialogService.ShowMessageAsync("错误",
                    $"未检测到 {project.Language} 运行环境。\n请确认该语言已安装后重试。");
                return;
            }

            // 3. 一次性表单收集需要入参的参数值
            var requireInputArgs = project.Arguments?.Where(a => a.RequireInput).ToList() ?? new List<ToolArgument>();
            Dictionary<string, string>? userInputs = null;
            if (requireInputArgs.Count > 0)
            {
                userInputs = await _dialogService.PromptArgumentsAsync(requireInputArgs);
                if (userInputs == null) return; // 用户取消
            }

            // 4. 评出最终命令并执行
            var commandLine = BuildCommandPreview(project, userInputs);
            var result = await ExecuteAsync(project, env, commandLine, userInputs);

            // 5. 弹窗显示结果（不论成功失败）
            await _dialogService.ShowOutputAsync(project.Name, result);
        }

        private async Task<ProcessRunResult> ExecuteAsync(
            ToolProject project,
            RuntimeEnvironment env,
            string commandLine,
            Dictionary<string, string>? userInputs)
        {
            var result = new ProcessRunResult { CommandLine = commandLine };
            var sw = Stopwatch.StartNew();

            try
            {
                // 选解释器
                string interpreter;
                if (!string.IsNullOrWhiteSpace(project.InterpreterPath))
                    interpreter = project.InterpreterPath;
                else
                    interpreter = env.ExecutablePath;

                bool interpreterIsScript = project.Language switch
                {
                    "powershell" or "bash" or "bat" or "cmd" => true,
                    _ => false
                };

                var psi = new ProcessStartInfo
                {
                    FileName = interpreter,
                    WorkingDirectory = string.IsNullOrWhiteSpace(project.WorkingDirectory)
                        ? Path.GetDirectoryName(project.ScriptPath) ?? Environment.CurrentDirectory
                        : project.WorkingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                // 拼参数
                if (interpreterIsScript)
                {
                    psi.ArgumentList.Add(project.ScriptPath);
                }
                else
                {
                    psi.ArgumentList.Add(project.ScriptPath);
                }

                if (project.Arguments != null)
                {
                    foreach (var arg in project.Arguments.OrderBy(a => a.Order))
                    {
                        string value;
                        if (arg.RequireInput && userInputs != null && userInputs.TryGetValue(arg.Name, out var v))
                            value = v;
                        else
                            value = arg.DefaultValue;

                        psi.ArgumentList.Add(arg.Name);
                        if (!string.IsNullOrEmpty(value))
                        {
                            if (arg.InputType == ToolArgumentInputType.Bool)
                                psi.ArgumentList.Add(value == "true" ? "true" : "false");
                            else
                                psi.ArgumentList.Add(value);
                        }
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

                using var proc = new Process { StartInfo = psi, EnableRaisingEvents = false };
                var stdout = new StringBuilder();
                var stderr = new StringBuilder();
                proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
                proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

                if (!proc.Start())
                {
                    result.Success = false;
                    result.ExitCode = -1;
                    result.StandardError = "进程启动失败";
                }
                else
                {
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                    await proc.WaitForExitAsync();
                    result.ExitCode = proc.ExitCode;
                    result.Success = proc.ExitCode == 0;
                    result.StandardOutput = stdout.ToString();
                    result.StandardError = stderr.ToString();
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ExitCode = -1;
                result.StandardError = $"执行异常: {ex.Message}";
            }
            finally
            {
                sw.Stop();
                result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            }

            return result;
        }
    }
}
