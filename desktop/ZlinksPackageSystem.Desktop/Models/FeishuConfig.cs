using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ZlinksPackageSystem.Desktop.Models
{
    public enum FeishuRobotType { Custom = 0, App = 1 }

    /// <summary>
    /// 飞书机器人渠道配置
    /// </summary>
    public partial class FeishuConfig : ObservableObject
    {
        [ObservableProperty]
        private FeishuRobotType _robotType = FeishuRobotType.Custom;

        /// <summary>自定义机器人 Webhook URL（含 access_token 查询串）</summary>
        [ObservableProperty] private string _webhookUrl = string.Empty;

        /// <summary>应用机器人 App ID</summary>
        [ObservableProperty] private string _appId = string.Empty;

        /// <summary>应用机器人 App Secret（敏感字段，UI 可一键显隐）</summary>
        [ObservableProperty] private string _appSecret = string.Empty;

        /// <summary>应用机器人 Receive ID（chat_id / open_id / email 之一）；留空跳过该渠道</summary>
        [ObservableProperty] private string _receiveId = string.Empty;

        /// <summary>@all</summary>
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