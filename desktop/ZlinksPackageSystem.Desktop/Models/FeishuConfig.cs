using System.Collections.Generic;

namespace ZlinksPackageSystem.Desktop.Models
{
    public enum FeishuRobotType { Custom = 0, App = 1 }

    /// <summary>
    /// 飞书机器人渠道配置
    /// </summary>
    public class FeishuConfig
    {
        public FeishuRobotType RobotType { get; set; } = FeishuRobotType.Custom;

        /// <summary>自定义机器人 Webhook URL（含 access_token 查询串）</summary>
        public string WebhookUrl { get; set; } = string.Empty;

        /// <summary>应用机器人 App ID</summary>
        public string AppId { get; set; } = string.Empty;

        /// <summary>应用机器人 App Secret（敏感字段，UI 可一键显隐）</summary>
        public string AppSecret { get; set; } = string.Empty;

        /// <summary>应用机器人 Receive ID（chat_id / open_id / email 之一）；留空跳过该渠道</summary>
        public string ReceiveId { get; set; } = string.Empty;

        /// <summary>@all</summary>
        public bool AtAll { get; set; }

        /// <summary>@手机号列表</summary>
        public List<string> AtMobiles { get; set; } = new();
    }
}