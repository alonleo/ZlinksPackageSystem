using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 单个工具的通知配置（Q3-C 混合模式）
    /// </summary>
    public class NotificationConfig
    {
        /// <summary>true = 继承全局；false = 用本工具覆盖</summary>
        public bool UseGlobalSettings { get; set; } = true;

        /// <summary>覆盖时启用：启动时通知</summary>
        public bool NotifyOnStart { get; set; }

        /// <summary>覆盖时启用：成功时通知</summary>
        public bool NotifyOnSuccess { get; set; }

        /// <summary>覆盖时启用：失败时通知</summary>
        public bool NotifyOnFailure { get; set; }

        /// <summary>覆盖时启用：脚本输出最大字符数</summary>
        public int MaxOutputChars { get; set; } = 4000;

        /// <summary>本工具专属的渠道列表（UseGlobalSettings=true 时不使用，UI 禁用）</summary>
        public List<FeishuConfig> Channels { get; set; } = new();

        /// <summary>仅运行期：错误日志，不参与持久化</summary>
        [JsonIgnore]
        public List<string> Logs { get; set; } = new();

        /// <summary>仅运行期：汇总状态文本，不参与持久化</summary>
        [JsonIgnore]
        public string LastAggregateStatus { get; set; } = string.Empty;
    }
}