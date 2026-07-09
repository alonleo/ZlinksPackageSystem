using System;

namespace ZlinksPackageSystem.Desktop.Models
{
    public class AnnouncementItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Time { get; set; }
        public string Publisher { get; set; } = string.Empty;
    }
}
