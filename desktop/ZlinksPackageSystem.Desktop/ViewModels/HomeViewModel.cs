using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZlinksPackageSystem.Desktop.Models;
using ZlinksPackageSystem.Desktop.Services;

namespace ZlinksPackageSystem.Desktop.ViewModels
{
    public partial class HomeViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;
        private readonly IDialogService _dialogService;

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

        public ObservableCollection<AnnouncementItem> Announcements { get; } = new();
        public ObservableCollection<NotificationItem> Notifications { get; } = new();

        public HomeViewModel(IApiService apiService, IDialogService dialogService)
        {
            _apiService = apiService;
            _dialogService = dialogService;
            Title = "首页";
            _ = LoadDataAsync();
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            IsBusy = true;

            // 加载统计数据
            try
            {
                var gameTask = _apiService.GetAsync<EntityCountResult>("/games/counts");
                var productTask = _apiService.GetAsync<EntityCountResult>("/products/counts");
                var testTask = _apiService.GetAsync<EntityCountResult>("/tests/counts");

                await Task.WhenAll(gameTask, productTask, testTask);

                var gameCounts = gameTask.Result;
                if (gameCounts != null)
                {
                    GameCount = (int)gameCounts.Total;
                    PendingGameCount = (int)gameCounts.Pending;
                }

                var productCounts = productTask.Result;
                if (productCounts != null)
                {
                    ProductCount = (int)productCounts.Total;
                    PendingProductCount = (int)productCounts.Pending;
                }

                var testCounts = testTask.Result;
                if (testCounts != null)
                {
                    TestCount = (int)testCounts.Total;
                    PendingTestCount = (int)testCounts.Pending;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load counts: {ex.Message}");
            }

            // 加载公告（最新 5 条）
            Announcements.Clear();
            try
            {
                var annoList = await _apiService.GetAsync<List<NotificationEntity>>("/notifications/announcements");
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load announcements: {ex.Message}");
            }

            // 加载通知（置顶）
            Notifications.Clear();
            try
            {
                var pinned = await _apiService.GetAsync<List<NotificationEntity>>("/notifications/pinned");
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load notifications: {ex.Message}");
            }

            UnreadCount = Notifications.Count(n => !n.IsRead);
            IsBusy = false;
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
