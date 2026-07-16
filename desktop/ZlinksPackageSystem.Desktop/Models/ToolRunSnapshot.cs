using System;

namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 工具运行一次快照，用于卡片渲染
    /// </summary>
    public class ToolRunSnapshot
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationMs => (int)(EndTime - StartTime).TotalMilliseconds;
        public int? ProcessId { get; set; }
        public string WorkingDirectory { get; set; } = string.Empty;
        public string CommandLine { get; set; } = string.Empty;
        public int ExitCode { get; set; }
        public string Output { get; set; } = string.Empty;
        public bool IsSuccess => ExitCode == 0;
        public NotificationTrigger Trigger { get; set; }
    }
}