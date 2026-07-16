using System.Collections.Generic;

namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// git clone 的结果：成功 / 失败 / 取消
    /// </summary>
    public class CloneResult
    {
        public bool Success { get; set; }
        public string RepoRoot { get; set; } = string.Empty;
        public string RepoName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public bool Cancelled { get; set; }
        public List<string> Logs { get; set; } = new();
    }
}