using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZlinksPackageSystem.Desktop.Constants;
using ZlinksPackageSystem.Desktop.Models;
using ZlinksPackageSystem.Desktop.Services;

namespace ZlinksPackageSystem.Desktop.ViewModels
{
    public partial class NotificationViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;
        private readonly IApiService _apiService;
        private readonly INetworkStatusService _networkService;
        private readonly ILocalCacheService _cacheService;

        [ObservableProperty]
        private int _unreadCount;

        [ObservableProperty]
        private string _offlineHint = string.Empty;

        public ObservableCollection<NotificationItem> Notifications { get; } = new();

        public NotificationViewModel(
            IDialogService dialogService,
            IApiService apiService,
            INetworkStatusService networkService,
            ILocalCacheService cacheService)
        {
            _dialogService = dialogService;
            _apiService = apiService;
            _networkService = networkService;
            _cacheService = cacheService;
            Title = "消息中心";
            _ = LoadDataAsync();
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            IsBusy = true;

            if (_networkService.IsOnline)
            {
                try
                {
                    var page = await _apiService.GetAsync<PageResponse<NotificationEntity>>(
                        "/notifications?current=1&size=50");

                    if (page == null)
                    {
                        await LoadFromCacheAsync();
                        return;
                    }

                    Notifications.Clear();
                    if (page.Records != null)
                    {
                        foreach (var e in page.Records)
                            Notifications.Add(MapToItem(e));
                    }

                    await _cacheService.SaveAsync(CacheKeys.Notifications,
                        new NotificationCache { Page = page, LastUpdated = DateTime.Now });
                    OfflineHint = string.Empty;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Notification] 加载失败:{ex.Message}");
                    await LoadFromCacheAsync();
                }
            }
            else
            {
                await LoadFromCacheAsync();
            }

            UnreadCount = Notifications.Count(n => !n.IsRead);
            IsBusy = false;
        }

        private async Task LoadFromCacheAsync()
        {
            var lastUpdated = await _cacheService.GetLastUpdatedAsync(CacheKeys.Notifications);
            var cached = await _cacheService.LoadAsync<NotificationCache>(CacheKeys.Notifications);
            Notifications.Clear();

            if (cached?.Page?.Records == null)
            {
                OfflineHint = _networkService.IsOnline
                    ? "加载通知失败,请检查后端服务"
                    : "离线模式 · 暂无通知数据,请联网后刷新";
                return;
            }

            foreach (var e in cached.Page.Records)
                Notifications.Add(MapToItem(e));
            OfflineHint = lastUpdated.HasValue
                ? $"离线模式 · 数据更新于 {lastUpdated:yyyy-MM-dd HH:mm}"
                : "离线模式 · 加载本地缓存";
        }

        private static NotificationItem MapToItem(NotificationEntity e) => new()
        {
            Id = e.Id,
            Title = e.Title,
            Message = string.IsNullOrEmpty(e.Content)
                ? e.Title
                : (e.Content.Length > 60 ? e.Content[..60] + "…" : e.Content),
            Content = e.Content,
            Time = ParseDateTime(e.CreateTime) ?? DateTime.Now,
            Urgency = "中",
            Publisher = e.SenderName ?? string.Empty,
            IsRead = e.Status == "1"
        };

        private static DateTime? ParseDateTime(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            if (DateTime.TryParse(s, out var d)) return d;
            return null;
        }

        [RelayCommand]
        private async Task ShowDetailAsync(NotificationItem item)
        {
            var result = await _dialogService.ShowNotificationDetailAsync(item);
            if (result)
            {
                var index = Notifications.IndexOf(item);
                if (index >= 0)
                {
                    var updated = new NotificationItem
                    {
                        Id = item.Id,
                        Title = item.Title,
                        Message = item.Message,
                        Content = item.Content,
                        Time = item.Time,
                        IsRead = true,
                        Urgency = item.Urgency,
                        Publisher = item.Publisher
                    };
                    Notifications[index] = updated;
                }
                UnreadCount = Notifications.Count(n => !n.IsRead);
            }
        }
    }
}
