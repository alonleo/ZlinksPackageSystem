using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public class DialogService : IDialogService
    {
        private static Window? Owner =>
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        public async Task ShowMessageAsync(string title, string message)
        {
            var owner = Owner;
            if (owner == null) return;

            var dialog = new Window
            {
                Title = title,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                MinWidth = 300,
                MinHeight = 150
            };

            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var okButton = new Button
            {
                Content = "确定",
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 100,
                Height = 32
            };

            okButton.Click += (_, _) => dialog.Close();

            var grid = new Grid
            {
                RowDefinitions = new RowDefinitions("*,Auto"),
                Children = { textBlock, okButton }
            };
            Grid.SetRow(okButton, 1);

            dialog.Content = grid;
            await dialog.ShowDialog(owner);
        }

        public async Task<bool> ShowNotificationDetailAsync(NotificationItem item)
        {
            var owner = Owner;
            if (owner == null) return false;

            var marked = false;

            var urgencyColor = item.Urgency switch
            {
                "高" => Color.Parse("#FFF56C6C"),
                "中" => Color.Parse("#FFE6A23C"),
                _ => Color.Parse("#FF52C41A")
            };

            var dialog = new Window
            {
                Title = "通知详情",
                Width = 500,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                SystemDecorations = SystemDecorations.None,
                Background = Brushes.Transparent
            };

            var titleBlock = new TextBlock
            {
                Text = "通知详情",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse("#FFBFcbd9"))
            };

            var separator = new Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(Color.Parse("#333355"))
            };

            var urgencyDot = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(urgencyColor),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 4, 10, 0)
            };

            var messageBlock = new TextBlock
            {
                Text = item.Message,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 15,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse("#FFBFcbd9"))
            };

            var contentBlock = new TextBlock
            {
                Text = item.Content,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.Parse("#99BFcbd9")),
                Margin = new Thickness(20, 6, 0, 0)
            };

            var urgencyLabel = new TextBlock
            {
                Text = "紧急程度：" + item.Urgency,
                FontSize = 12,
                Foreground = new SolidColorBrush(urgencyColor)
            };

            var publisherBlock = new TextBlock
            {
                Text = "发布人：" + item.Publisher,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#99FFFFFF"))
            };

            var timeBlock = new TextBlock
            {
                Text = item.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#99FFFFFF"))
            };

            var statusBlock = new TextBlock
            {
                Text = item.IsRead ? "状态：已读" : "状态：未读",
                FontSize = 12,
                Foreground = item.IsRead
                    ? new SolidColorBrush(Color.Parse("#99FFFFFF"))
                    : new SolidColorBrush(Color.Parse("#FF52C41A"))
            };

            var metaRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 0,
                Margin = new Thickness(20, 4, 0, 0)
            };
            metaRow.Children.Add(urgencyLabel);
            metaRow.Children.Add(new TextBlock
            {
                Text = "  |  ",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#444466"))
            });
            metaRow.Children.Add(publisherBlock);

            // 回复输入区域
            var replyInput = new TextBox
            {
                Watermark = "输入回复内容...",
                Height = 32,
                Background = new SolidColorBrush(Color.Parse("#0DFFFFFF")),
                BorderThickness = new Thickness(0)
            };

            var sendButton = new Button
            {
                Content = "发送",
                Width = 70,
                Height = 32,
                CornerRadius = new CornerRadius(0, 6, 6, 0)
            };

            var replyBox = new Border
            {
                IsVisible = false,
                CornerRadius = new CornerRadius(6),
                BorderBrush = new SolidColorBrush(Color.Parse("#33FFFFFF")),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Color.Parse("#0DFFFFFF")),
                Margin = new Thickness(20, 8, 20, 0),
                Child = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                    Children = { replyInput, sendButton }
                }
            };
            Grid.SetColumn(replyInput, 0);
            Grid.SetColumn(sendButton, 1);

            var contentPanel = new StackPanel
            {
                Margin = new Thickness(24),
                Spacing = 10
            };

            sendButton.Click += (_, _) =>
            {
                var replyText = replyInput.Text;
                if (!string.IsNullOrWhiteSpace(replyText))
                {
                    var replyBlock = new TextBlock
                    {
                        Text = "我的回复：" + replyText,
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.Parse("#FF1976D2")),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 6, 0, 0)
                    };
                    replyInput.Text = "";
                    replyBox.IsVisible = false;
                    contentPanel.Children.Add(replyBlock);
                }
            };

            var replyButton = new Button
            {
                Content = "回复",
                Width = 100,
                Height = 34
            };

            replyButton.Click += (_, _) =>
            {
                replyBox.IsVisible = !replyBox.IsVisible;
                if (replyBox.IsVisible)
                    replyInput.Focus();
            };

            // 已读/关闭按钮：未读时显示"已读"，点击后变为"关闭"
            var isMarkedRead = item.IsRead;
            var markReadButton = new Button
            {
                Width = 100,
                Height = 34,
                Content = item.IsRead ? "关闭" : "已读"
            };

            markReadButton.Click += (_, _) =>
            {
                if (!isMarkedRead)
                {
                    marked = true;
                    item.IsRead = true;
                    isMarkedRead = true;
                    markReadButton.Content = "关闭";
                }
                else
                {
                    dialog.Close();
                }
            };

            var actionPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 12
            };
            actionPanel.Children.Add(replyButton);
            actionPanel.Children.Add(markReadButton);

            var bottomPanel = new StackPanel
            {
                Spacing = 8
            };
            bottomPanel.Children.Add(replyBox);
            bottomPanel.Children.Add(actionPanel);

            contentPanel.Children.Add(titleBlock);
            contentPanel.Children.Add(separator);

            var messageRow = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*")
            };
            messageRow.Children.Add(urgencyDot);
            Grid.SetColumn(urgencyDot, 0);
            messageRow.Children.Add(messageBlock);
            Grid.SetColumn(messageBlock, 1);
            contentPanel.Children.Add(messageRow);

            contentPanel.Children.Add(contentBlock);
            contentPanel.Children.Add(metaRow);
            contentPanel.Children.Add(timeBlock);
            contentPanel.Children.Add(statusBlock);

            var rootGrid = new Grid
            {
                RowDefinitions = new RowDefinitions("*,Auto"),
                Children = { contentPanel, bottomPanel }
            };
            Grid.SetRow(bottomPanel, 1);

            dialog.Content = new Border
            {
                Child = rootGrid,
                Background = new SolidColorBrush(Color.Parse("#E01e1e2e")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(0, 0, 0, 20),
                Margin = new Thickness(4)
            };
            await dialog.ShowDialog(owner);
            return marked;
        }
    }
}
