namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 进程执行结果
    /// </summary>
    public class ProcessRunResult
    {
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
        public string CommandLine { get; set; } = string.Empty;
        public long ElapsedMilliseconds { get; set; }
    }
}
