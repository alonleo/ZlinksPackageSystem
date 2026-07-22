using System;
using System.Collections.Generic;
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
    public partial class HomeViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;
        private readonly IDialogService _dialogService;
        private readonly INetworkStatusService _networkService;
        private readonly ILocalCacheService _cacheService;
        private bool _dataLoadedFromCache;

        [ObservableProperty]
        private int _gameCount;

        [ObservableProperty]
        private int _pendingGameCount;

        [ObservableProperty]
        private int _productCount;

        [ObservableProperty]
        private int _pendingProductCount;

        [ObservableProperty]
        private int _testCount;

        [ObservableProperty]
        private int _pendingTestCount;

        [ObservableProperty]
        private int _unreadCount;

        [ObservableProperty]
        private string _offlineHint = string.Empty;

        public ObservableCollection<AnnouncementItem> Announcements { get; } = new();
        public ObservableCollection<NotificationItem> Notifications { get; } = new();

        public HomeViewModel(
            IApiService apiService,
            IDialogService dialogService,
            INetworkStatusService networkService,
            ILocalCacheService cacheService)
        {
            _apiService = apiService;
            _dialogService = dialogService;
            _networkService = networkService;
            _cacheService = cacheService;
            Title = "首页";
            _ = LoadDataAsync();
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            IsBusy = true;
            _dataLoadedFromCache = false;

            if (_networkService.IsOnline)
            {
                await LoadCountsFromApiAsync();
                await LoadAnnouncementsFromApiAsync();
                await LoadPinnedFromApiAsync();

                if (_dataLoadedFromCache)
                {
                    OfflineHint = "数据来自本地缓存,请联网后点击「刷新」获取最新数据";
                }
                else
                {
                    OfflineHint = string.Empty;
                }
            }
            else
            {
                await LoadAllFromCacheAsync();
            }

            UnreadCount = Notifications.Count(n => !n.IsRead);
            IsBusy = false;
        }

        private async Task LoadCountsFromApiAsync()
        {
            try
            {
                var gameTask = _apiService.GetAsync<EntityCountResult>("/games/counts");
                var productTask = _apiService.GetAsync<EntityCountResult>("/products/counts");
                var testTask = _apiService.GetAsync<EntityCountResult>("/tests/counts");

                await Task.WhenAll(gameTask, productTask, testTask);

                var gameCounts = gameTask.Result;
                var productCounts = productTask.Result;
                var testCounts = testTask.Result;

                if (gameCounts != null)
                {
                    GameCount = (int)gameCounts.Total;
                    PendingGameCount = (int)gameCounts.Pending;
                }
                if (productCounts != null)
                {
                    ProductCount = (int)productCounts.Total;
                    PendingProductCount = (int)productCounts.Pending;
                }
                if (testCounts != null)
                {
                    TestCount = (int)testCounts.Total;
                    PendingTestCount = (int)testCounts.Pending;
                }

                await PersistHomeCacheAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load counts: {ex.Message}");
                await LoadCountsFromCacheAsync();
            }
        }

        private async Task LoadAnnouncementsFromApiAsync()
        {
            try
            {
                var annoList = await _apiService.GetAsync<List<NotificationEntity>>("/notifications/announcements");
                Announcements.Clear();
                if (annoList != null)
                {
                    foreach (var e in annoList)
                        Announcements.Add(new AnnouncementItem
                        {
                            Id = e.Id,
                            Title = e.Title,
                            Content = e.Content,
                            Time = ParseDateTime(e.CreateTime) ?? DateTime.Now,
                            Publisher = e.SenderName ?? string.Empty
                        });
                }
                await PersistHomeCacheAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load announcements: {ex.Message}");
                await LoadAnnouncementsFromCacheAsync();
            }
        }

        private async Task LoadPinnedFromApiAsync()
        {
            try
            {
                var pinned = await _apiService.GetAsync<List<NotificationEntity>>("/notifications/pinned");
                Notifications.Clear();
                if (pinned != null)
                {
                    foreach (var e in pinned)
                        Notifications.Add(new NotificationItem
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
                        });
                }
                await PersistHomeCacheAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load notifications: {ex.Message}");
                await LoadPinnedFromCacheAsync();
            }
        }

        private async Task LoadAllFromCacheAsync()
        {
            _dataLoadedFromCache = true;
            var lastUpdated = await _cacheService.GetLastUpdatedAsync(CacheKeys.Home);
            var cached = await _cacheService.LoadAsync<HomeCachePayload>(CacheKeys.Home);
            if (cached == null)
            {
                OfflineHint = "离线模式 · 无本地数据,请联网后刷新";
                return;
            }

            GameCount = cached.GameCount;
            PendingGameCount = cached.PendingGameCount;
            ProductCount = cached.ProductCount;
            PendingProductCount = cached.PendingProductCount;
            TestCount = cached.TestCount;
            PendingTestCount = cached.PendingTestCount;

            Announcements.Clear();
            foreach (var a in cached.Announcements) Announcements.Add(a);

            Notifications.Clear();
            foreach (var n in cached.PinnedNotifications) Notifications.Add(n);

            OfflineHint = lastUpdated.HasValue
                ? $"离线模式 · 数据更新于 {lastUpdated:yyyy-MM-dd HH:mm}"
                : "离线模式 · 加载本地缓存";
        }

        private async Task LoadCountsFromCacheAsync()
        {
            var cached = await _cacheService.LoadAsync<HomeCachePayload>(CacheKeys.Home);
            if (cached == null)
            {
                _dataLoadedFromCache = true;
                OfflineHint = "部分数据获取失败,无本地缓存可用";
                return;
            }
            GameCount = cached.GameCount;
            PendingGameCount = cached.PendingGameCount;
            ProductCount = cached.ProductCount;
            PendingProductCount = cached.PendingProductCount;
            TestCount = cached.TestCount;
            PendingTestCount = cached.PendingTestCount;
            _dataLoadedFromCache = true;
        }

        private async Task LoadAnnouncementsFromCacheAsync()
        {
            var cached = await _cacheService.LoadAsync<HomeCachePayload>(CacheKeys.Home);
            if (cached == null) return;
            Announcements.Clear();
            foreach (var a in cached.Announcements) Announcements.Add(a);
            _dataLoadedFromCache = true;
        }

        private async Task LoadPinnedFromCacheAsync()
        {
            var cached = await _cacheService.LoadAsync<HomeCachePayload>(CacheKeys.Home);
            if (cached == null) return;
            Notifications.Clear();
            foreach (var n in cached.PinnedNotifications) Notifications.Add(n);
            _dataLoadedFromCache = true;
        }

        private async Task PersistHomeCacheAsync()
        {
            var payload = new HomeCachePayload
            {
                GameCount = GameCount,
                PendingGameCount = PendingGameCount,
                ProductCount = ProductCount,
                PendingProductCount = PendingProductCount,
                TestCount = TestCount,
                PendingTestCount = PendingTestCount,
                Announcements = Announcements.ToList(),
                PinnedNotifications = Notifications.ToList(),
                LastUpdated = DateTime.Now
            };
            await _cacheService.SaveAsync(CacheKeys.Home, payload);
        }

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
                    Notifications[index] = new NotificationItem
                    {
                        Id = item.Id, Title = item.Title, Message = item.Message,
                        Content = item.Content, Time = item.Time, IsRead = true,
                        Urgency = item.Urgency, Publisher = item.Publisher
                    };
                }
                UnreadCount = Notifications.Count(n => !n.IsRead);
            }
        }
    }
}
