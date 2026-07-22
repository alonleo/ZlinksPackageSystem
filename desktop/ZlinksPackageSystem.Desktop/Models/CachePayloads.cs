using System;
using System.Collections.Generic;

namespace ZlinksPackageSystem.Desktop.Models
{
    public class HomeCachePayload
    {
        public int GameCount { get; set; }
        public int PendingGameCount { get; set; }
        public int ProductCount { get; set; }
        public int PendingProductCount { get; set; }
        public int TestCount { get; set; }
        public int PendingTestCount { get; set; }
        public List<AnnouncementItem> Announcements { get; set; } = new();
        public List<NotificationItem> PinnedNotifications { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class GamePageCache
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public string SearchText { get; set; } = string.Empty;
        public PageResponse<Game> Page { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class NotificationCache
    {
        public PageResponse<NotificationEntity> Page { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }
}
