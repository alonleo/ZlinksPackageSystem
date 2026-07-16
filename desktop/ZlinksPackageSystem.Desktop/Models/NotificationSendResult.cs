namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 单个渠道发送结果
    /// </summary>
    public class NotificationSendResult
    {
        public string ChannelLabel { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}