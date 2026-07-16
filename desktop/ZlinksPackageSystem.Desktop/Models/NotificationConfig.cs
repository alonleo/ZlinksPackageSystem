using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 单个工具的通知配置（Q3-C 混合模式）
    /// </summary>
    public partial class NotificationConfig : ObservableObject
    {
        /// <summary>true = 继承全局；false = 用本工具覆盖</summary>
        [ObservableProperty] private bool _useGlobalSettings = true;

        /// <summary>覆盖时启用：启动时通知</summary>
        [ObservableProperty] private bool _notifyOnStart;

        /// <summary>覆盖时启用：成功时通知</summary>
        [ObservableProperty] private bool _notifyOnSuccess;

        /// <summary>覆盖时启用：失败时通知</summary>
        [ObservableProperty] private bool _notifyOnFailure;

        /// <summary>覆盖时启用：脚本输出最大字符数</summary>
        [ObservableProperty] private int _maxOutputChars = 4000;

        /// <summary>本工具专属的渠道列表（UseGlobalSettings=true 时不使用，UI 禁用）</summary>
        [ObservableProperty] private ObservableCollection<FeishuConfig> _channels = new();

        /// <summary>仅运行期：错误日志，不参与持久化</summary>
        [property: JsonIgnore]
        [ObservableProperty] private ObservableCollection<string> _logs = new();

        /// <summary>仅运行期：汇总状态文本，不参与持久化</summary>
        [property: JsonIgnore]
        [ObservableProperty] private string _lastAggregateStatus = string.Empty;
    }
}
