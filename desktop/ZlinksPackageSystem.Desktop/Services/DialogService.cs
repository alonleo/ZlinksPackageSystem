using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IFilePickerService _filePicker;
        private static Window? Owner =>
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        public DialogService(IFilePickerService filePicker)
        {
            _filePicker = filePicker;
        }

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
                        Height = 32,
                        Margin = new Thickness(0, 4, 0, 18)
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

                public async Task ShowEnvironmentResultAsync(string title, string message, bool success)
                {
                    var owner = Owner;
                    if (owner == null) return;

                    var iconColor = success
                        ? new SolidColorBrush(Color.Parse("#FF52C41A"))
                        : new SolidColorBrush(Color.Parse("#FFF56C6C"));
                    var icon = success ? "✅" : "❌";

                    var dialog = new Window
                    {
                        Title = title,
                        SizeToContent = SizeToContent.WidthAndHeight,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        CanResize = false,
                        MinWidth = 320,
                        MinHeight = 160,
                        SystemDecorations = SystemDecorations.None,
                        Background = new SolidColorBrush(Color.Parse("#F01e1e2e"))
                    };

                    var header = new Border
                    {
                        Background = new SolidColorBrush(Color.Parse("#22FFFFFF")),
                        Padding = new Thickness(18, 12),
                        Child = new TextBlock
                        {
                            Text = $"{icon}  {title}",
                            FontSize = 15,
                            FontWeight = FontWeight.Bold,
                            Foreground = iconColor
                        }
                    };

                    var messageBlock = new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(20, 22, 20, 18),
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.Parse("#FFBFcbd9"))
                    };

                    var okButton = new Button
                    {
                        Content = "确定",
                        Width = 110,
                        Height = 34,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Background = new SolidColorBrush(Color.Parse("#FF1976D2")),
                        BorderBrush = new SolidColorBrush(Color.Parse("#FF1976D2")),
                        Foreground = new SolidColorBrush(Colors.White),
                        Margin = new Thickness(0, 6, 0, 22)
                    };
                    okButton.Click += (_, _) => dialog.Close();

                    var root = new DockPanel { LastChildFill = true };
                    DockPanel.SetDock(header, Dock.Top);
                    root.Children.Add(header);
                    DockPanel.SetDock(okButton, Dock.Bottom);
                    root.Children.Add(okButton);
                    root.Children.Add(messageBlock);

                    dialog.Content = root;
                    await dialog.ShowDialog(owner);
                }

        public async Task<Dictionary<string, string>?> PromptArgumentsAsync(IEnumerable<ToolArgument> arguments)
        {
            var owner = Owner;
            if (owner == null) return null;

            var argList = arguments.Where(a => a.RequireInput).OrderBy(a => a.Order).ToList();
            if (argList.Count == 0) return new Dictionary<string, string>();

            // 每个参数一个控件
            var inputControls = new Dictionary<string, Control>();
            var formPanel = new StackPanel { Spacing = 10 };

            formPanel.Children.Add(new TextBlock
            {
                Text = $"请为以下 {argList.Count} 个参数提供运行值：",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.Parse("#FFBFcbd9")),
                Margin = new Thickness(0, 0, 0, 4)
            });

            foreach (var arg in argList)
            {
                var label = new TextBlock
                {
                    Text = string.IsNullOrEmpty(arg.Description) ? arg.Name : $"{arg.Name}  ({arg.Description})",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.Parse("#FFBFcbd9")),
                    FontWeight = FontWeight.SemiBold
                };
                formPanel.Children.Add(label);

                Control input = arg.InputType switch
                                {
                                    ToolArgumentInputType.Bool => new CheckBox
                                    {
                                        Content = "启用",
                                        IsChecked = false
                                    },
                                    ToolArgumentInputType.Number => new NumericUpDown
                                                        {
                                                            Minimum = decimal.MinValue,
                                                            Maximum = decimal.MaxValue,
                                                            Value = 0
                                                        },
                                    _ => new TextBox
                                    {
                                        Watermark = arg.InputType == ToolArgumentInputType.File
                                            ? "选择文件..."
                                            : arg.InputType == ToolArgumentInputType.Directory
                                                ? "选择目录..."
                                                : $"输入 {arg.Name} 的值"
                                    }
                                };

                // File / Directory 加浏览按钮
                if (arg.InputType is ToolArgumentInputType.File or ToolArgumentInputType.Directory)
                {
                    var capturedArg = arg;
                    var textBox = (TextBox)input;
                    var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
                    row.Children.Add(textBox);
                    Grid.SetColumn(textBox, 0);

                    var browse = new Button
                    {
                        Content = "📁 浏览",
                        Width = 90,
                        Margin = new Thickness(6, 0, 0, 0)
                    };
                    browse.Click += async (_, _) =>
                    {
                        string? picked = capturedArg.InputType == ToolArgumentInputType.File
                            ? await _filePicker.PickScriptFileAsync()
                            : await _filePicker.PickDirectoryAsync();
                        if (!string.IsNullOrEmpty(picked))
                            textBox.Text = picked;
                    };
                    row.Children.Add(browse);
                    Grid.SetColumn(browse, 1);
                    formPanel.Children.Add(row);
                }
                else
                {
                    formPanel.Children.Add(input);
                }

                inputControls[arg.Name] = input;
            }

            // 滚动容器
            var scroll = new ScrollViewer
            {
                Content = formPanel,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                MaxHeight = 500
            };

            Dictionary<string, string>? result = null;

                        var dialog = new Window
                        {
                            Title = "输入运行参数",
                            SizeToContent = SizeToContent.WidthAndHeight,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            CanResize = false,
                            MinWidth = 480,
                            MaxWidth = 720
                        };

                        var okBtn = new Button { Content = "确定运行", Width = 110, Height = 34 };
                        var cancelBtn = new Button { Content = "取消", Width = 90, Height = 34 };

                        okBtn.Click += (_, _) =>
                                                {
                                                    result = new Dictionary<string, string>();
                                                    foreach (var arg in argList)
                                                    {
                                                        var ctl = inputControls[arg.Name];
                                                        string value = arg.InputType switch
                                                        {
                                                            ToolArgumentInputType.Bool => ((CheckBox)ctl).IsChecked == true ? "true" : "false",
                                                            ToolArgumentInputType.Number => ((NumericUpDown)ctl).Value.ToString() ?? "0",
                                                            _ => ((TextBox)ctl).Text ?? string.Empty
                                                        };
                                                        result[arg.Name] = value;
                                                    }
                                                    dialog.Close();
                                                };
                        cancelBtn.Click += (_, _) => dialog.Close();

                        var btnRow = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Spacing = 10,
                            Margin = new Thickness(0, 12, 0, 0)
                        };
                        btnRow.Children.Add(cancelBtn);
                        btnRow.Children.Add(okBtn);

                        var root = new StackPanel
                        {
                            Margin = new Thickness(20),
                            Spacing = 8
                        };
                        root.Children.Add(scroll);
                        root.Children.Add(btnRow);

                        dialog.Content = root;

                        await dialog.ShowDialog(owner);
                        return result;
                    }

        public async Task ShowOutputAsync(string toolName, ProcessRunResult result)
        {
            var owner = Owner;
            if (owner == null) return;

            var successColor = result.Success
                ? new SolidColorBrush(Color.Parse("#FF52C41A"))
                : new SolidColorBrush(Color.Parse("#FFF56C6C"));

            var titleBlock = new TextBlock
            {
                Text = $"{(result.Success ? "✅" : "❌")} {toolName}  -  {(result.Success ? "执行成功" : "执行失败")}",
                FontSize = 15,
                FontWeight = FontWeight.Bold,
                Foreground = successColor
            };

            // 命令行（始终展示，让用户看到评出的命令）
            var cmdLabel = new TextBlock
            {
                Text = "📋 评出的命令：",
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse("#FFBFcbd9")),
                Margin = new Thickness(0, 8, 0, 4)
            };
            var cmdBox = new TextBox
            {
                Text = result.CommandLine,
                IsReadOnly = true,
                FontFamily = new FontFamily("Consolas, Menlo, Courier New, monospace"),
                FontSize = 12,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 50,
                Background = new SolidColorBrush(Color.Parse("#0DFFFFFF")),
                BorderBrush = new SolidColorBrush(Color.Parse("#33FFFFFF")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8)
            };

            // 元信息：退出码 + 耗时
            var meta = new TextBlock
            {
                Text = $"退出码: {result.ExitCode}    耗时: {result.ElapsedMilliseconds} ms",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#99FFFFFF")),
                Margin = new Thickness(0, 8, 0, 0)
            };

            // stdout
            var stdoutLabel = new TextBlock
            {
                Text = "📤 标准输出 (stdout)：",
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse("#FFBFcbd9")),
                Margin = new Thickness(0, 8, 0, 4)
            };
            var stdoutBox = new TextBox
            {
                Text = string.IsNullOrEmpty(result.StandardOutput) ? "(无输出)" : result.StandardOutput,
                IsReadOnly = true,
                FontFamily = new FontFamily("Consolas, Menlo, Courier New, monospace"),
                FontSize = 12,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.NoWrap,
                Background = new SolidColorBrush(Color.Parse("#0DFFFFFF")),
                BorderBrush = new SolidColorBrush(Color.Parse("#33FFFFFF")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8)
            };

            // stderr
            var stderrLabel = new TextBlock
            {
                Text = "⚠️  错误输出 (stderr)：",
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse("#FFBFcbd9")),
                Margin = new Thickness(0, 8, 0, 4)
            };
            var stderrBox = new TextBox
            {
                Text = string.IsNullOrEmpty(result.StandardError) ? "(无)" : result.StandardError,
                IsReadOnly = true,
                FontFamily = new FontFamily("Consolas, Menlo, Courier New, monospace"),
                FontSize = 12,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.NoWrap,
                Background = new SolidColorBrush(Color.Parse("#0DFFFFFF")),
                BorderBrush = new SolidColorBrush(Color.Parse("#33FFFFFF")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8),
                Foreground = string.IsNullOrEmpty(result.StandardError)
                    ? new SolidColorBrush(Color.Parse("#66FFFFFF"))
                    : new SolidColorBrush(Color.Parse("#FFFFB3B3"))
            };

            var leftScroll = new ScrollViewer
            {
                Content = stdoutBox,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                MaxHeight = 240
            };
            var rightScroll = new ScrollViewer
            {
                Content = stderrBox,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                MaxHeight = 240
            };

            var outGrid = new Grid
                        {
                            ColumnDefinitions = new ColumnDefinitions("*,*"),
                            Margin = new Thickness(0, 0, 0, 0)
                        };
                        outGrid.Children.Add(leftScroll);
                        Grid.SetColumn(leftScroll, 0);
                        outGrid.Children.Add(rightScroll);
                        Grid.SetColumn(rightScroll, 1);

                        var dialog = new Window
                        {
                            Title = $"执行结果 - {toolName}",
                            Width = 820,
                            Height = 620,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            CanResize = true
                        };

                        var copyBtn = new Button
                        {
                            Content = "📋 复制全部",
                            Width = 110,
                            Height = 32
                        };
                        copyBtn.Click += async (_, _) =>
                        {
                            var text =
                                $"Command: {result.CommandLine}\nExitCode: {result.ExitCode}\nElapsed: {result.ElapsedMilliseconds} ms\n\n--- stdout ---\n{result.StandardOutput}\n\n--- stderr ---\n{result.StandardError}";
                            var clipboard = TopLevel.GetTopLevel(dialog)?.Clipboard;
                            if (clipboard != null)
                                await clipboard.SetTextAsync(text);
                        };
                        var closeBtn = new Button
                        {
                            Content = "关闭",
                            Width = 90,
                            Height = 32
                        };
                        closeBtn.Click += (_, _) => dialog.Close();

                        var btnRow = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Spacing = 10,
                            Margin = new Thickness(0, 12, 0, 0)
                        };
                        btnRow.Children.Add(copyBtn);
                        btnRow.Children.Add(closeBtn);

                        var content = new StackPanel { Margin = new Thickness(20), Spacing = 4 };
                        content.Children.Add(titleBlock);
                        content.Children.Add(cmdLabel);
                        content.Children.Add(cmdBox);
                        content.Children.Add(meta);
                        content.Children.Add(stdoutLabel);
                        content.Children.Add(outGrid);
                        content.Children.Add(stderrLabel);
                        content.Children.Add(rightScroll);
                        content.Children.Add(btnRow);

                        dialog.Content = content;

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
