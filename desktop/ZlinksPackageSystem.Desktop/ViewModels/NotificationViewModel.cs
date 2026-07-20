using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZlinksPackageSystem.Desktop.Models;
using ZlinksPackageSystem.Desktop.Services;

namespace ZlinksPackageSystem.Desktop.ViewModels
{
    public partial class NotificationViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;
        private readonly IApiService _apiService;

        [ObservableProperty]
        private int _unreadCount;

        public ObservableCollection<NotificationItem> Notifications { get; } = new();

        public NotificationViewModel(IDialogService dialogService, IApiService apiService)
        {
            _dialogService = dialogService;
            _apiService = apiService;
            Title = "消息中心";
            _ = LoadDataAsync();
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                // 从后台管理系统拉取通知（按置顶 + 时间倒序分页）
                var page = await _apiService.GetAsync<PageResponse<NotificationEntity>>(
                    "/notifications?current=1&size=50");
                Notifications.Clear();
                if (page?.Records != null)
                {
                    foreach (var e in page.Records)
                        Notifications.Add(MapToItem(e));
                }
                UnreadCount = Notifications.Count(n => !n.IsRead);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Notification] 加载失败：{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
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
                // 刷新当前项的已读状态
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
