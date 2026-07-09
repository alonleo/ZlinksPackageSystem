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

        [ObservableProperty]
        private int _unreadCount;

        public ObservableCollection<NotificationItem> Notifications { get; } = new();

        public NotificationViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            Title = "消息中心";
            _ = LoadDataAsync();
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            IsBusy = true;

                        // 通知数据（模拟）
                        Notifications.Clear();
            Notifications.Add(new NotificationItem
            {
                Id = 1,
                Title = "打包完成",
                Message = "游戏「梦幻西游」打包完成",
                Content = "游戏「梦幻西游」已于 14:30 完成打包，APK 大小 185MB，无异常。请及时进行验收测试。",
                Time = DateTime.Now.AddMinutes(-5),
                IsRead = false,
                Urgency = "低",
                Publisher = "张三"
            });
            Notifications.Add(new NotificationItem
            {
                Id = 2,
                Title = "审核通过",
                Message = "产品「A100」审核通过",
                Content = "产品「A100」提交的审核已通过，该产品已具备上线条件。如需上线，请联系运营团队安排发布计划。",
                Time = DateTime.Now.AddHours(-1),
                IsRead = false,
                Urgency = "中",
                Publisher = "李四"
            });
            Notifications.Add(new NotificationItem
            {
                Id = 3,
                Title = "测试完成",
                Message = "测试任务 #1283 执行完毕",
                Content = "测试任务 #1283 已执行完毕，通过率 98.5%。其中 3 个用例因环境问题被跳过，详见测试报告。",
                Time = DateTime.Now.AddHours(-2),
                IsRead = false,
                Urgency = "低",
                Publisher = "王五"
            });
            Notifications.Add(new NotificationItem
            {
                Id = 4,
                Title = "系统维护",
                Message = "系统将于今晚 02:00 维护",
                Content = "为提升系统稳定性，将于今晚 02:00-03:00 进行数据库优化维护。维护期间可能会出现短暂的服务中断。",
                Time = DateTime.Now.AddHours(-3),
                IsRead = true,
                Urgency = "高",
                Publisher = "系统管理员"
            });
            Notifications.Add(new NotificationItem
            {
                Id = 5,
                Title = "SDK 发布",
                Message = "新版本 SDK 已发布",
                Content = "新版本 SDK v3.2.0 已正式发布，主要更新：适配 Android 14、优化 IL2CPP 打包速度、修复内存泄漏问题。",
                Time = DateTime.Now.AddDays(-1),
                IsRead = true,
                Urgency = "中",
                Publisher = "赵六"
            });

            UnreadCount = Notifications.Count(n => !n.IsRead);
            IsBusy = false;
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
                    Notifications[index] = new NotificationItem
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
                }
                UnreadCount = Notifications.Count(n => !n.IsRead);
            }
        }
    }
}
