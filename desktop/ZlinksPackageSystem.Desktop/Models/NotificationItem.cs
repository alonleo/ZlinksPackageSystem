using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ZlinksPackageSystem.Desktop.Models
{
    public partial class NotificationItem : ObservableObject
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Time { get; set; }
        public string Urgency { get; set; } = "低";
        public string Publisher { get; set; } = string.Empty;

        [ObservableProperty]
        private bool _isRead;
    }
}
