using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 工具运行模式枚举(用于 UI 显示和解析,持久化与后端交互使用 string)
    /// </summary>
    public enum ToolRunMode
    {
        /// <summary>通过检测到的运行时执行脚本</summary>
        Script = 0,
        /// <summary>直接启动本地可执行程序</summary>
        LocalExecutable = 1
    }

    /// <summary>
    /// 工具运行模式字符串常量(与后端 Tool.run_mode 字段对齐)
    /// </summary>
    public static class ToolRunModes
    {
        public const string Script = "Script";
        public const string LocalExecutable = "LocalExecutable";

        public static ToolRunMode Parse(string? value, ToolRunMode fallback = ToolRunMode.Script)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            return Enum.TryParse<ToolRunMode>(value, ignoreCase: true, out var result) ? result : fallback;
        }

        public static string ToStringValue(ToolRunMode mode) => mode switch
        {
            ToolRunMode.LocalExecutable => LocalExecutable,
            _ => Script
        };
    }

    /// <summary>
    /// 工具同步状态(持久化)
    /// - Synced:          已与后端同步
    /// - PendingCreate:   本地新建,尚未推送
    /// - PendingUpdate:   本地修改,尚未推送
    /// </summary>
    public enum ToolSyncState
    {
        Synced = 0,
        PendingCreate = 1,
        PendingUpdate = 2
    }

    /// <summary>
    /// 工具项目模型
    /// </summary>
    public class ToolProject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Status
        {
            get => _status;
            set => SetField(ref _status, value);
        }
        private string _status = string.Empty;

        public string Manager { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }

        // ===== 脚本执行相关 =====
        /// <summary>运行模式字符串(与后端 Tool.run_mode 字段对齐,值为 "Script" / "LocalExecutable")</summary>
        public string RunMode { get; set; } = ToolRunModes.Script;

        /// <summary>运行模式枚举(由 RunMode 派生,仅用于 UI 绑定)</summary>
        [JsonIgnore]
        public ToolRunMode RunModeEnum
        {
            get => ToolRunModes.Parse(RunMode);
            set => RunMode = ToolRunModes.ToStringValue(value);
        }

        /// <summary>编程语言:python / node / java / go / powershell / bash / dotnet</summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>解释器绝对路径,留空使用环境检测到的默认</summary>
        public string InterpreterPath { get; set; } = string.Empty;

        /// <summary>脚本绝对路径(脚本模式专用)</summary>
        public string ScriptPath { get; set; } = string.Empty;

        /// <summary>本地可执行程序绝对路径(本地可执行模式专用)</summary>
        public string ExecutablePath { get; set; } = string.Empty;

        /// <summary>工作目录,留空使用脚本/可执行文件所在目录</summary>
        public string WorkingDirectory { get; set; } = string.Empty;

        /// <summary>额外环境变量(KEY=VALUE,一行一个)</summary>
        public string EnvironmentVariables { get; set; } = string.Empty;

        /// <summary>默认参数前缀(如 "--"、"/"),新参数默认使用</summary>
        public string DefaultArgumentPrefix { get; set; } = "--";

        /// <summary>参数列表</summary>
        public List<ToolArgument> Arguments { get; set; } = new();

        // ===== Python 虚拟环境(仅 Language=python 时有效)=====

        /// <summary>
        /// 是否在工具运行前自动创建本地 Python 虚拟环境,并按需安装依赖。
        /// 适用于 <see cref="Language"/> == "python" 的脚本型工具。
        /// </summary>
        public bool CreateVenv { get; set; }

        /// <summary>
        /// 虚拟环境根目录(相对或绝对路径)。留空则默认 <c>{WorkingDirectory}/.venv</c>。
        /// 该字段是 venv 的位置,不参与 JSON 反序列化(运行期可解析)。
        /// </summary>
        public string VenvDirectory { get; set; } = string.Empty;

        /// <summary>
        /// requirements.txt 路径(相对 <see cref="WorkingDirectory"/> 或绝对)。
        /// 留空则只创建 venv,不安装依赖。
        /// </summary>
        public string RequirementsPath { get; set; } = string.Empty;

        /// <summary>
        /// pip 镜像源 URL(可选)。例如 <c>https://pypi.tuna.tsinghua.edu.cn/simple</c>。
        /// 留空则使用 PyPI 官方源。
        /// </summary>
        public string PipMirrorUrl { get; set; } = string.Empty;

        // ===== Git 仓库(仅新建时填写,编辑时不再修改)=====
        /// <summary>Git 仓库 URL(HTTPS 或 SSH),可选</summary>
        public string GitUrl { get; set; } = string.Empty;

        /// <summary>克隆目标父目录(如 D:\tools),可选</summary>
        public string CloneDirectory { get; set; } = string.Empty;

        /// <summary>Git 远程名(默认 origin)。运行时若本地 .git 存在,会自动从 .git/config 解析并填入。</summary>
        public string RemoteName { get; set; } = "origin";

        // ===== 通知配置(Q3-C 混合模式)=====
        /// <summary>工具级通知配置(默认继承全局)</summary>
        public NotificationConfig Notification { get; set; } = new();

        // ===== 运行时状态(不参与持久化)=====
        /// <summary>是否正在运行</summary>
        [JsonIgnore]
        public bool IsRunning
        {
            get => _isRunning;
            set => SetField(ref _isRunning, value);
        }
        private bool _isRunning;

        /// <summary>当前运行进程的 PID</summary>
        [JsonIgnore]
        public int? ProcessId
        {
            get => _processId;
            set => SetField(ref _processId, value);
        }
        private int? _processId;

        /// <summary>
        /// 当前是否在执行「单条同步」调用。XAML 用于按钮禁用与文案切换。
        /// 不参与持久化。
        /// </summary>
        [JsonIgnore]
        public bool IsSyncing
        {
            get => _isSyncing;
            set => SetField(ref _isSyncing, value);
        }
        private bool _isSyncing;

        /// <summary>运行期:克隆成功后实际生成的仓库根目录(= CloneDirectory/<repo名>)。不参与持久化。</summary>
        [JsonIgnore]
        public string ClonedRepoRoot { get; set; } = string.Empty;

        /// <summary>UI 运行期:是否从本地缓存加载(非服务器最新)。不参与持久化。</summary>
        [JsonIgnore]
        public bool IsFromLocalSnapshot { get; set; }

        /// <summary>上一次运行的详细结果(含输出日志)。运行期仅内存保留,不参与持久化。</summary>
        [JsonIgnore]
        public ProcessRunResult? LastRunResult
        {
            get => _lastRunResult;
            set
            {
                if (SetField(ref _lastRunResult, value))
                    OnPropertyChanged(nameof(HasRunLog));
            }
        }
        private ProcessRunResult? _lastRunResult;

        /// <summary>UI 便捷属性:是否有运行日志可查看。</summary>
        [JsonIgnore]
        public bool HasRunLog => LastRunResult != null;

        /// <summary>
        /// 后端管理员标记:是否系统内置工具。后端有值;本地新创建的工具此值为 false。
        /// </summary>
        public bool IsSystemBuiltin { get; set; }

        /// <summary>
        /// 与后端的同步状态(参与持久化)。默认 Synced。
        /// - 桌面端新建/编辑时,如后端不可达,置为 PendingCreate/PendingUpdate;
        /// - 同步成功后由同步逻辑置回 Synced;
        /// - 启动加载时,本地缓存中存在但后端缺失的记录被识别为 PendingCreate。
        /// </summary>
        public ToolSyncState SyncState { get; set; } = ToolSyncState.Synced;

        /// <summary>
        /// UI 便捷属性:是否处于待同步状态。XAML 直接 IsVisible="{Binding IsPendingSync}"。
        /// </summary>
        [JsonIgnore]
        public bool IsPendingSync => SyncState != ToolSyncState.Synced;

        /// <summary>
        /// UI 便捷属性:待同步徽标文字。XAML 直接 Text="{Binding SyncBadgeText}"。
        /// </summary>
        [JsonIgnore]
        public string SyncBadgeText => SyncState switch
        {
            ToolSyncState.PendingCreate => "🟠 待同步(新建)",
            ToolSyncState.PendingUpdate => "🟠 待同步(修改)",
            _ => string.Empty
        };

        /// <summary>
        /// 兼容旧字段 IsUserOnly(旧文件无 SyncState 字段时,反序列化后 IsUserOnly=true → SyncState=PendingCreate)。
        /// 新代码请直接使用 SyncState。
        /// </summary>
        [JsonIgnore]
        public bool IsUserOnly
        {
            get => SyncState != ToolSyncState.Synced;
            set => SyncState = value ? ToolSyncState.PendingCreate : ToolSyncState.Synced;
        }

        /// <summary>
        /// 本地临时 ID(仅 PendingCreate 时为负数时间戳,同步成功后会被后端真实 ID 替换)。
        /// 用于在本地缓存文件中区分"创建中"记录,不依赖后端主键。
        /// </summary>
        [JsonIgnore]
        public long LocalTempId { get; set; }
    }
}
