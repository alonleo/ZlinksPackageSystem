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

        /// <summary>启动时填入的进程 ID，用于后续按 PID 杀进程</summary>
        public int? ProcessId { get; set; }
    }
}
