using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 通知渠道类型（Q3-D：飞书 / 微信企业机器人）
    /// </summary>
    public enum ChannelType
    {
        /// <summary>飞书机器人（含 Custom 和 App 子类型）</summary>
        Feishu = 0,
        /// <summary>微信企业群机器人（Webhook）</summary>
        WeChatWork = 1
    }

    public enum FeishuRobotType { Custom = 0, App = 1 }

    /// <summary>
    /// 通知渠道配置（飞书 / 微信企业机器人共用一个数据模型）
    /// </summary>
    public partial class FeishuConfig : ObservableObject
    {
        /// <summary>渠道类型（飞书 / 微信），缺省值 Feishu 与历史持久化兼容</summary>
        [ObservableProperty]
        private ChannelType _channelType = ChannelType.Feishu;

        /// <summary>飞书机器人子类型（仅 ChannelType=Feishu 时有效）</summary>
        [ObservableProperty]
        private FeishuRobotType _robotType = FeishuRobotType.Custom;

        /// <summary>自定义机器人 / 微信 Webhook URL（含 access_token / key 查询串）</summary>
        [ObservableProperty] private string _webhookUrl = string.Empty;

        /// <summary>应用机器人 App ID（仅飞书 App 子类型使用）</summary>
        [ObservableProperty] private string _appId = string.Empty;

        /// <summary>应用机器人 App Secret（敏感字段，UI 可一键显隐）</summary>
        [ObservableProperty] private string _appSecret = string.Empty;

        /// <summary>应用机器人 Receive ID（chat_id / open_id / email 之一）；留空跳过该渠道</summary>
        [ObservableProperty] private string _receiveId = string.Empty;

        /// <summary>@all（飞书 Custom + 微信均生效）</summary>
        [ObservableProperty] private bool _atAll;

        /// <summary>@手机号列表（持久化）</summary>
        [ObservableProperty] private List<string> _atMobiles = new();

        /// <summary>
        /// 文本形式的手机号列表（XAML 双向绑定用）：逗号/换行分隔
        /// </summary>
        public string AtMobilesText
        {
            get => AtMobiles == null ? string.Empty : string.Join(",", AtMobiles);
            set
            {
                var list = (value ?? string.Empty)
                    .Split(new[] { ',', '\n', '\r', ' ' }, System.StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
                AtMobiles = list;
            }
        }
    }
}