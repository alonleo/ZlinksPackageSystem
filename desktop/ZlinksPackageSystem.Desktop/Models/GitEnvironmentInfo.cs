namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 本机 Git 环境检测结果
    /// </summary>
    public class GitEnvironmentInfo
    {
        public bool IsInstalled { get; set; }
        public string Version { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
    }
}