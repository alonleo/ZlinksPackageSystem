using System;
using System.Collections.Generic;
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
    /// 工具项目模型
    /// </summary>
    public class ToolProject
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
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

        // ===== Git 仓库(仅新建时填写,编辑时不再修改)=====
        /// <summary>Git 仓库 URL(HTTPS 或 SSH),可选</summary>
        public string GitUrl { get; set; } = string.Empty;

        /// <summary>克隆目标父目录(如 D:\tools),可选</summary>
        public string CloneDirectory { get; set; } = string.Empty;

        // ===== 通知配置(Q3-C 混合模式)=====
        /// <summary>工具级通知配置(默认继承全局)</summary>
        public NotificationConfig Notification { get; set; } = new();

        // ===== 运行时状态(不参与持久化)=====
        /// <summary>是否正在运行</summary>
        [JsonIgnore]
        public bool IsRunning { get; set; }

        /// <summary>当前运行进程的 PID</summary>
        [JsonIgnore]
        public int? ProcessId { get; set; }

        /// <summary>运行期:克隆成功后实际生成的仓库根目录(= CloneDirectory/<repo名>)。不参与持久化。</summary>
        [JsonIgnore]
        public string ClonedRepoRoot { get; set; } = string.Empty;

        /// <summary>
        /// 后端管理员标记:是否系统内置工具。后端有值;本地新创建的工具此值为 false。
        /// </summary>
        public bool IsSystemBuiltin { get; set; }

        /// <summary>
        /// 桌面端:本工具尚未推送到后端(即只在本地缓存)。默认 false;新建失败兜底时设为 true。
        /// 不参与 JSON 序列化,仅运行期状态。
        /// </summary>
        [JsonIgnore]
        public bool IsUserOnly { get; set; }
    }
}
