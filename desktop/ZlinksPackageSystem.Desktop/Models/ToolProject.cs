using System;
using System.Collections.Generic;

namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 工具项目模型
    /// </summary>
    public class ToolProject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Manager { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }

        // ===== 脚本执行相关 =====
        /// <summary>编程语言：python / node / java / go / powershell / bash</summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>解释器绝对路径，留空使用环境检测到的默认</summary>
        public string InterpreterPath { get; set; } = string.Empty;

        /// <summary>脚本绝对路径</summary>
        public string ScriptPath { get; set; } = string.Empty;

        /// <summary>工作目录，留空使用脚本所在目录</summary>
        public string WorkingDirectory { get; set; } = string.Empty;

        /// <summary>额外环境变量（KEY=VALUE，一行一个）</summary>
        public string EnvironmentVariables { get; set; } = string.Empty;

        /// <summary>参数列表</summary>
        public List<ToolArgument> Arguments { get; set; } = new();
    }
}
