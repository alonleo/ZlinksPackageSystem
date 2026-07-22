namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 与后端 <c>tool</c> 表对应的 DTO。所有字段下划线→驼峰由 ApiService 反序列化处理。
    /// </summary>
    public class ToolEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Manager { get; set; } = string.Empty;

        public string RunMode { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string InterpreterPath { get; set; } = string.Empty;
        public string ScriptPath { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public string EnvironmentVariables { get; set; } = string.Empty;
        public string DefaultArgumentPrefix { get; set; } = "--";

        public string GitUrl { get; set; } = string.Empty;
        public string CloneDirectory { get; set; } = string.Empty;
        public string RemoteName { get; set; } = "origin";

        public string ArgumentsJson { get; set; } = string.Empty;
        public string NotificationJson { get; set; } = string.Empty;

        /// <summary>是否系统内置工具（后端管理员标记）。1=系统内置，0=用户工具</summary>
        public int IsSystemBuiltin { get; set; }

        /// <summary>启动前是否自动创建 Python 虚拟环境（仅 Language=python 生效）</summary>
        public bool CreateVenv { get; set; }

        /// <summary>Python 虚拟环境目录；空表示默认 {WorkingDirectory}/.venv</summary>
        public string VenvDirectory { get; set; } = string.Empty;

        /// <summary>requirements.txt 路径</summary>
        public string RequirementsPath { get; set; } = string.Empty;

        /// <summary>pip 镜像源 URL</summary>
        public string PipMirrorUrl { get; set; } = string.Empty;

        public string CreateBy { get; set; } = string.Empty;
        public string CreateTime { get; set; } = string.Empty;
        public string UpdateBy { get; set; } = string.Empty;
        public string UpdateTime { get; set; } = string.Empty;
    }
}
