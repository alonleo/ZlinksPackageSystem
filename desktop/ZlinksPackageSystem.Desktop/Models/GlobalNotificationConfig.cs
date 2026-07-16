using System.Collections.ObjectModel;

namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 全局通知默认配置（所有工具共享，工具可覆盖）
    /// </summary>
    public class GlobalNotificationConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool NotifyOnStart { get; set; }
        public bool NotifyOnSuccess { get; set; } = true;
        public bool NotifyOnFailure { get; set; } = true;
        public int MaxOutputChars { get; set; } = 4000;
        public ObservableCollection<FeishuConfig> Channels { get; set; } = new();
    }
}