using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;
using ZlinksPackageSystem.Desktop.Services;
using ZlinksPackageSystem.Desktop.ViewModels;

namespace ZlinksPackageSystem.SmokeTest
{
    internal class Program
    {
        static int _failed = 0;
        static int _passed = 0;

        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=== 工具库改造 - 冒烟测试 ===\n");

            // 模拟运行时环境（让 ToolLibraryViewModel 的 BuildCommandPreview 不抛异常）
            var envs = new List<RuntimeEnvironment>
            {
                new() { Language = "python", DisplayName = "Python 3.11", Icon = "🐍", ExecutablePath = "python", IsAvailable = true },
                new() { Language = "node",   DisplayName = "Node 20",    Icon = "🟢", ExecutablePath = "node",   IsAvailable = true }
            };

            // 反射构造一个不依赖 Avalonia 服务的 ViewModel
            // 需要提供真实的 IProcessManagerService（构造函数会订阅其事件）
            var pm = new ProcessManagerService();
            var vm = (ToolLibraryViewModel)Activator.CreateInstance(
                typeof(ToolLibraryViewModel),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { null, null, null, null, pm, null, null, null, null }, // ... INotificationService, IVenvService
                culture: null)!;

            // 注入环境
            SetProp(vm, "AvailableEnvironments", new System.Collections.ObjectModel.ObservableCollection<RuntimeEnvironment>(envs));

            // ===== 1. 命令预览：脚本模式 + 默认前缀 =====
            Test("脚本模式 + 默认前缀 + 询问参数", () =>
            {
                var proj = new ToolProject
                {
                    Id = 1,
                    Name = "test",
                    RunMode = ToolRunModes.Script,
                    Language = "python",
                    InterpreterPath = "python",
                    ScriptPath = "C:\\tools\\build.py",
                    DefaultArgumentPrefix = "--",
                    Arguments = new List<ToolArgument>
                    {
                        new() { Name = "output", DefaultValue = "out", UseDefaultPrefix = true, RequireInput = true, Order = 0 },
                        new() { Name = "verbose", DefaultValue = "true", UseDefaultPrefix = true, RequireInput = false, InputType = ToolArgumentInputType.Bool, Order = 1 }
                    }
                };
                var preview = vm.BuildCommandPreview(proj);
                // 脚本路径不含空格 → 不加引号；prefix+name 合并 → "--output out --verbose true"
                var expected = "python C:\\tools\\build.py --output out --verbose true";
                AssertEq("preview", expected, preview);
            });

            // ===== 2. 命令预览：自定义前缀 =====
            Test("自定义前缀覆盖默认", () =>
            {
                var proj = new ToolProject
                {
                    Id = 1,
                    Name = "test",
                    RunMode = ToolRunModes.Script,
                    Language = "python",
                    InterpreterPath = "python",
                    ScriptPath = "C:\\tools\\build.py",
                    DefaultArgumentPrefix = "--",
                    Arguments = new List<ToolArgument>
                    {
                        new() { Name = "port", DefaultValue = "8080", UseDefaultPrefix = false, Prefix = "-", Order = 0 }
                    }
                };
                var preview = vm.BuildCommandPreview(proj);
                var expected = "python C:\\tools\\build.py -port 8080";
                AssertEq("preview", expected, preview);
            });

            // ===== 3. 命令预览：本地可执行程序模式 =====
            Test("本地可执行程序模式（无参）", () =>
            {
                var proj = new ToolProject
                {
                    Id = 1,
                    Name = "ffmpeg",
                    RunMode = ToolRunModes.LocalExecutable,
                    ExecutablePath = "C:\\tools\\ffmpeg.exe",
                    DefaultArgumentPrefix = "-"
                };
                var preview = vm.BuildCommandPreview(proj);
                // C:\tools\ffmpeg.exe 不含空格，不加引号
                var expected = "C:\\tools\\ffmpeg.exe";
                AssertEq("preview", expected, preview);
            });

            Test("本地可执行程序模式（带参）", () =>
            {
                var proj = new ToolProject
                {
                    Id = 1,
                    Name = "ffmpeg",
                    RunMode = ToolRunModes.LocalExecutable,
                    ExecutablePath = "C:\\tools\\ffmpeg.exe",
                    DefaultArgumentPrefix = "-",
                    Arguments = new List<ToolArgument>
                    {
                        new() { Name = "i", DefaultValue = "in.mp4", UseDefaultPrefix = true, Order = 0 },
                        new() { Name = "y", DefaultValue = "", UseDefaultPrefix = true, Order = 1 }
                    }
                };
                var preview = vm.BuildCommandPreview(proj);
                // "y" 默认值为空，因此只拼出 -i in.mp4（Name "y" 仍会出现作为 -y）
                var expected = "C:\\tools\\ffmpeg.exe -i in.mp4 -y";
                AssertEq("preview", expected, preview);
            });

            // ===== 4. 进程管理：启动 + Exited 事件 =====
            Test("进程管理：启动 + 自动 Exited 事件", () =>
            {
                var pm = new ProcessManagerService();
                int? exitedPid = null;
                int? exitedCode = null;
                pm.ProcessExited += (pid, code) => { exitedPid = pid; exitedCode = code; };

                var psi = new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
                    ArgumentList = { "/c", "echo hello" },
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                if (!OperatingSystem.IsWindows())
                {
                    psi.ArgumentList.Clear();
                    psi.ArgumentList.Add("-c");
                    psi.ArgumentList.Add("echo hello");
                }

                int pid = pm.Start(psi);
                Assert("pid != 0", pid != 0);
                Assert("IsRunning after start", pm.IsRunning(pid));

                // 等最多 5s 让事件触发
                var sw = Stopwatch.StartNew();
                while (exitedPid == null && sw.ElapsedMilliseconds < 5000)
                    Thread.Sleep(50);

                Assert("ProcessExited fired", exitedPid == pid);
                Assert("IsRunning after exit", !pm.IsRunning(pid));
                AssertEq("exit code", 0, exitedCode);
            });

            // ===== 5. 进程管理：长跑 + Kill =====
            Test("进程管理：启动 + 按 PID Kill", () =>
            {
                var pm = new ProcessManagerService();
                var psi = new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                if (OperatingSystem.IsWindows())
                    psi.ArgumentList.Add("/k");
                else
                    psi.ArgumentList.Add("-c");
                psi.ArgumentList.Add(OperatingSystem.IsWindows() ? "ping 127.0.0.1 -n 30" : "sleep 30");

                int pid = pm.Start(psi);
                Assert("pid != 0", pid != 0);
                Assert("IsRunning after start", pm.IsRunning(pid));

                Thread.Sleep(500);
                bool killed = pm.Kill(pid);
                Assert("Kill returned true", killed);

                // 等进程退出
                var sw = Stopwatch.StartNew();
                while (pm.IsRunning(pid) && sw.ElapsedMilliseconds < 5000)
                    Thread.Sleep(50);
                Assert("not IsRunning after Kill", !pm.IsRunning(pid));
            });

            // ===== 6. 进程管理：无效 PID 返回 false =====
            Test("进程管理：无效 PID", () =>
            {
                var pm = new ProcessManagerService();
                Assert("Kill invalid returns false", !pm.Kill(99999));
                Assert("IsRunning invalid returns false", !pm.IsRunning(99999));
            });

            // ===== 7. EditableArgument 转换 =====
            Test("EditableArgument：默认前缀回写到 Source", () =>
            {
                var ea = new EditableArgument
                {
                    Source = new ToolArgument { Name = "x", UseDefaultPrefix = true, Prefix = "--" },
                    UseDefaultPrefix = true,
                    Prefix = "--",
                    Value = "1"
                };
                AssertEq("default prefix", "--", ea.UseDefaultPrefix ? ea.Source.Prefix : ea.Prefix);
                AssertEq("value", "1", ea.Value);
            });

            // ===== 8. 模式枚举 =====
            Test("ToolRunMode 枚举值", () =>
            {
                AssertEq("Script=0", 0, (int)ToolRunMode.Script);
                AssertEq("LocalExecutable=1", 1, (int)ToolRunMode.LocalExecutable);
            });

            // ===== 9. GitUrl 解析 =====
            Test("GitUrl.ParseRepoName HTTPS with .git", () =>
            {
                AssertEq("repo name", "y", GitUrlParser.ParseRepoName("https://github.com/x/y.git"));
            });
            Test("GitUrl.ParseRepoName HTTPS without .git", () =>
            {
                AssertEq("repo name", "y", GitUrlParser.ParseRepoName("https://github.com/x/y"));
            });
            Test("GitUrl.ParseRepoName SSH", () =>
            {
                AssertEq("repo name", "y", GitUrlParser.ParseRepoName("git@github.com:x/y.git"));
            });
            Test("GitUrl.ParseRepoName with token", () =>
            {
                AssertEq("repo name", "y", GitUrlParser.ParseRepoName("https://token@github.com/x/y.git"));
            });
            Test("GitUrl.ParseRepoName invalid throws", () =>
            {
                try
                {
                    GitUrlParser.ParseRepoName("");
                    throw new Exception("未抛异常");
                }
                catch (ArgumentException) { /* 预期 */ }
            });
            Test("GitUrl.CombineRepoRoot", () =>
            {
                AssertEq("root", Path.Combine("D:\\tools", "y"),
                    GitUrlParser.CombineRepoRoot("D:\\tools", "y"));
                AssertEq("root unix", "/tools/y",
                    GitUrlParser.CombineRepoRoot("/tools", "y"));
            });

            // ===== 10. ToolPersistenceService Save→Load 往返（含 Git 字段） =====
            Test("ToolPersistenceService.Save→Load 往返", () =>
            {
                var tmp = Path.Combine(Path.GetTempPath(), "zlinks-persist-test-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmp);
                try
                {
                    var svc = new ToolPersistenceService(tmp);
                    var input = new List<ToolProject>
                    {
                        new() { Id = 1, Name = "t1", GitUrl = "https://x/y.git", CloneDirectory = @"D:\tools" },
                        new() { Id = 2, Name = "t2", GitUrl = "git@github.com:x/y.git", CloneDirectory = "" },
                        new() { Id = 3, Name = "t3" }
                    };
                    svc.SaveAsync(input).GetAwaiter().GetResult();
                    var loaded = svc.LoadAsync().GetAwaiter().GetResult();
                    AssertEq("count", 3, loaded.Count);
                    AssertEq("t1.GitUrl", "https://x/y.git", loaded[0].GitUrl);
                    AssertEq("t1.CloneDirectory", @"D:\tools", loaded[0].CloneDirectory);
                    AssertEq("t2.GitUrl", "git@github.com:x/y.git", loaded[1].GitUrl);
                    AssertEq("t3.GitUrl", "", loaded[2].GitUrl);
                }
                finally
                {
                    try { Directory.Delete(tmp, recursive: true); } catch { }
                }
            });

            // ===== 11. ToolPersistenceService.Import 错误文件返回 null =====
            Test("ToolPersistenceService.Import 错误文件", () =>
            {
                var tmp = Path.Combine(Path.GetTempPath(), "zlinks-import-test-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmp);
                try
                {
                    var svc = new ToolPersistenceService(tmp);
                    var badPath = Path.Combine(tmp, "bad.json");
                    File.WriteAllText(badPath, "this is not json {");
                    var result = svc.ImportAsync(badPath).GetAwaiter().GetResult();
                    Assert("Import bad json returns null", result == null);
                }
                finally
                {
                    try { Directory.Delete(tmp, recursive: true); } catch { }
                }
            });

            // ===== 12. GlobalNotificationConfig JSON 往返 =====
            Test("GlobalNotificationConfig JSON 往返", () =>
            {
                var tmp = Path.Combine(Path.GetTempPath(), "zlinks-globalnotif-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmp);
                try
                {
                    var svc = new GlobalNotificationService(tmp);
                    var input = new GlobalNotificationConfig
                    {
                        IsEnabled = true,
                        NotifyOnSuccess = true,
                        NotifyOnFailure = true,
                        MaxOutputChars = 2000,
                        Channels = new ObservableCollection<FeishuConfig>
                        {
                            new() { RobotType = FeishuRobotType.Custom, WebhookUrl = "https://x", AtAll = true },
                            new() { RobotType = FeishuRobotType.App, AppId = "cli_x", AppSecret = "secret", ReceiveId = "oc_xxx" }
                        }
                    };
                    svc.SaveAsync(input).GetAwaiter().GetResult();
                    var loaded = svc.LoadAsync().GetAwaiter().GetResult();
                    AssertEq("IsEnabled", true, loaded.IsEnabled);
                    AssertEq("MaxOutputChars", 2000, loaded.MaxOutputChars);
                    AssertEq("channels count", 2, loaded.Channels.Count);
                    AssertEq("c0.WebhookUrl", "https://x", loaded.Channels[0].WebhookUrl);
                    AssertEq("c1.ReceiveId", "oc_xxx", loaded.Channels[1].ReceiveId);
                }
                finally
                {
                    try { Directory.Delete(tmp, recursive: true); } catch { }
                }
            });

            // ===== 13. NotificationService.BuildCard JSON 结构 =====
            Test("NotificationService.BuildCard 含 header/elements/工具/退出码", () =>
            {
                var proj = new ToolProject { Name = "demo", WorkingDirectory = "D:\\tools" };
                var snap = new ToolRunSnapshot
                {
                    StartTime = new DateTime(2026, 7, 16, 10, 0, 0),
                    EndTime = new DateTime(2026, 7, 16, 10, 0, 5),
                    ProcessId = 1234,
                    ExitCode = 0,
                    WorkingDirectory = "D:\\tools",
                    CommandLine = "python main.py",
                    Output = "hello\nworld",
                    Trigger = NotificationTrigger.Success
                };
                var ch = new FeishuConfig { RobotType = FeishuRobotType.Custom, WebhookUrl = "https://x", AtAll = true };
                var json = NotificationService.BuildCardJson(proj, snap, ch);
                Assert("contains header", json.Contains("\"header\""));
                Assert("contains elements", json.Contains("\"elements\""));
                Assert("contains 工具", json.Contains("工具"));
                Assert("contains 退出码", json.Contains("退出码"));
                Assert("contains script output", json.Contains("脚本输出"));
                Assert("contains template green", json.Contains("\"green\""));
            });

            // ===== 14. NotificationService.SendAsync 完全继承（工具无渠道 → 用全局） =====
            Test("NotificationService 完全继承", () =>
            {
                int hits = 0;
                var handler = new MockHttpHandler(req => { hits++; return MockHttpHandler.OkJson("{\"ok\":true}"); });
                var global = new InMemoryGlobalNotificationService(new GlobalNotificationConfig
                {
                    IsEnabled = true,
                    NotifyOnSuccess = true,
                    Channels = new ObservableCollection<FeishuConfig>
                    {
                        new() { RobotType = FeishuRobotType.Custom, WebhookUrl = "https://x" }
                    }
                });
                var svc = new NotificationService(global, handler);
                var proj = new ToolProject
                {
                    Name = "demo",
                    Notification = new NotificationConfig { UseGlobalSettings = true }
                };
                var snap = new ToolRunSnapshot { Trigger = NotificationTrigger.Success, Output = "ok" };
                var results = svc.SendAsync(proj, snap).GetAwaiter().GetResult();
                AssertEq("hits", 1, hits);
                AssertEq("results count", 1, results.Count);
                Assert("result success", results[0].Success);
            });

            // ===== 15. NotificationService 完全覆盖（工具自配渠道） =====
            Test("NotificationService 完全覆盖", () =>
            {
                int hits = 0;
                var handler = new MockHttpHandler(req => { hits++; return MockHttpHandler.OkJson("{\"ok\":true}"); });
                var global = new InMemoryGlobalNotificationService(new GlobalNotificationConfig
                {
                    NotifyOnSuccess = true,
                    Channels = new ObservableCollection<FeishuConfig>
                    {
                        new() { RobotType = FeishuRobotType.Custom, WebhookUrl = "https://should-not-call" }
                    }
                });
                var svc = new NotificationService(global, handler);
                var proj = new ToolProject
                {
                    Name = "demo",
                    Notification = new NotificationConfig
                    {
                        UseGlobalSettings = false,
                        NotifyOnSuccess = true,
                        Channels = new ObservableCollection<FeishuConfig>
                        {
                            new() { RobotType = FeishuRobotType.Custom, WebhookUrl = "https://override" }
                        }
                    }
                };
                var snap = new ToolRunSnapshot { Trigger = NotificationTrigger.Success, Output = "ok" };
                svc.SendAsync(proj, snap).GetAwaiter().GetResult();
                AssertEq("hits", 1, hits);
                Assert("called override", hits == 1);
            });

// ===== 16. NotificationService 触发时机过滤 =====
            Test("NotificationService 触发时机过滤", () =>
            {
                int hits = 0;
                var handler = new MockHttpHandler(req => { hits++; return MockHttpHandler.OkJson("{\"ok\":true}"); });
                var global = new InMemoryGlobalNotificationService(new GlobalNotificationConfig
                {
                    NotifyOnStart = false,
                    NotifyOnSuccess = true,
                    Channels = new ObservableCollection<FeishuConfig>
                    {
                        new() { RobotType = FeishuRobotType.Custom, WebhookUrl = "https://x" }
                    }
                });
                var svc = new NotificationService(global, handler);
                var proj = new ToolProject { Name = "demo", Notification = new NotificationConfig { UseGlobalSettings = true } };
                var snap = new ToolRunSnapshot { Trigger = NotificationTrigger.Start, Output = "starting" };
                var results = svc.SendAsync(proj, snap).GetAwaiter().GetResult();
                AssertEq("hits", 0, hits);
                AssertEq("results count", 0, results.Count);
            });

            // ===== 17. NotificationConfig UseGlobalSettings PropertyChanged =====
            Test("NotificationConfig UseGlobalSettings PropertyChanged 触发", () =>
            {
                var cfg = new NotificationConfig { UseGlobalSettings = true };
                bool fired = false;
                cfg.PropertyChanged += (_, e) => { if (e.PropertyName == "UseGlobalSettings") fired = true; };
                cfg.UseGlobalSettings = false;
                Assert("PropertyChanged fired", fired);
            });

            // ===== 18. NotificationConfig.Channels ObservableCollection 通知 =====
            Test("NotificationConfig Channels ObservableCollection.CollectionChanged", () =>
            {
                var cfg = new NotificationConfig();
                bool fired = false;
                cfg.Channels.CollectionChanged += (_, _) => { fired = true; };
                cfg.Channels.Add(new FeishuConfig());
                Assert("CollectionChanged fired", fired);
            });

            // ===== 19. WeChatWork markdown 内容构造 =====
            Test("NotificationService.BuildWeChatMarkdownContent 含关键字段", () =>
            {
                var proj = new ToolProject { Name = "demo", WorkingDirectory = "D:\\tools" };
                var snap = new ToolRunSnapshot
                {
                    StartTime = new DateTime(2026, 7, 16, 10, 0, 0),
                    EndTime = new DateTime(2026, 7, 16, 10, 0, 5),
                    ProcessId = 1234,
                    ExitCode = 0,
                    WorkingDirectory = "D:\\tools",
                    CommandLine = "python main.py",
                    Output = "hello\nworld",
                    Trigger = NotificationTrigger.Success
                };
                var md = NotificationService.BuildWeChatMarkdownContent(proj, snap, "hello\nworld");
                Assert("contains 工具", md.Contains("工具"));
                Assert("contains demo name", md.Contains("demo"));
                Assert("contains 退出码", md.Contains("退出码"));
                Assert("contains script output", md.Contains("脚本输出"));
                Assert("contains 代码块", md.Contains("```"));
                Assert("not empty", !string.IsNullOrEmpty(md));
            });

            // ===== 20. WeChatWork SendAsync 通过 Mock HTTP =====
            Test("NotificationService WeChatWork 渠道单测", () =>
            {
                int hits = 0;
                string? captured = null;
                var handler = new MockHttpHandler(req =>
                {
                    hits++;
                    captured = req.RequestUri?.ToString() ?? "";
                    return MockHttpHandler.OkJson("{\"errcode\":0,\"errmsg\":\"ok\"}");
                });
                var global = new InMemoryGlobalNotificationService(new GlobalNotificationConfig
                {
                    IsEnabled = true,
                    NotifyOnSuccess = true,
                    Channels = new ObservableCollection<FeishuConfig>()
                });
                var svc = new NotificationService(global, handler);
                var proj = new ToolProject
                {
                    Name = "demo",
                    Notification = new NotificationConfig
                    {
                        UseGlobalSettings = false,
                        NotifyOnSuccess = true,
                        Channels = new ObservableCollection<FeishuConfig>
                        {
                            new()
                            {
                                ChannelType = ChannelType.WeChatWork,
                                WebhookUrl = "https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=test",
                                AtAll = true
                            }
                        }
                    }
                };
                var snap = new ToolRunSnapshot { Trigger = NotificationTrigger.Success, Output = "ok" };
                var results = svc.SendAsync(proj, snap).GetAwaiter().GetResult();
                AssertEq("hits", 1, hits);
                AssertEq("results count", 1, results.Count);
                Assert("WeChat result success", results[0].Success);
                AssertEq("label 微信企业机器人", "微信企业机器人", results[0].ChannelLabel);
                Assert("URL hit WeChat endpoint", captured != null && captured.Contains("qyapi.weixin.qq.com"));
            });

            // ===== 21. GitService.PullAsync 输入校验（不调用真实 git pull）=====
            Test("GitService.PullAsync 空路径", () =>
            {
                var svc = new GitService();
                var r = svc.PullAsync("").GetAwaiter().GetResult();
                Assert("not success", !r.Success);
                Assert("error mentions 仓库根目录", r.ErrorMessage.Contains("仓库根目录"));
            });

            Test("GitService.PullAsync 不存在的目录", () =>
            {
                var svc = new GitService();
                var r = svc.PullAsync(@"C:\nonexistent-zlinks-" + Guid.NewGuid().ToString("N"))
                    .GetAwaiter().GetResult();
                Assert("not success", !r.Success);
                Assert("error mentions 不存在", r.ErrorMessage.Contains("不存在"));
            });

            Test("GitService.PullAsync 非 Git 目录", () =>
            {
                var tmp = Path.Combine(Path.GetTempPath(), "zlinks-notgit-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmp);
                try
                {
                    var svc = new GitService();
                    var r = svc.PullAsync(tmp).GetAwaiter().GetResult();
                    Assert("not success", !r.Success);
                    Assert("error mentions .git", r.ErrorMessage.Contains(".git"));
                }
                finally
                {
                    try { Directory.Delete(tmp); } catch { }
                }
            });

            // ===== SettingsViewModel 构造冒烟测试 =====
            Test("SettingsViewModel 构造不抛异常", () =>
            {
                var fp = new FilePickerService();
                var gns = new InMemoryGlobalNotificationService(new GlobalNotificationConfig());
                var nfs = new StubNotificationService();
                var dlg = new StubDialogService();
                var sp = new StubServiceProvider();
                var mvm = new MainViewModel(null!, sp, dlg);
                var vm = new SettingsViewModel(mvm, fp, gns, nfs, dlg);
                if (vm.Categories.Count == 0) throw new Exception("Categories 为空");
                if (vm.SelectedCategory == null) throw new Exception("SelectedCategory 为空");
                if (vm.IsAppearanceVisible != true) throw new Exception("默认分类不是外观");
                Console.WriteLine($"     [info] Categories={vm.Categories.Count}, SelectedCategory={vm.SelectedCategory?.Title}, IsAppearanceVisible={vm.IsAppearanceVisible}");
            });

            // ===== 16. ToolSyncState 枚举 + IsPendingSync 派生属性 =====
            Test("ToolSyncState 默认值 + IsPendingSync 派生", () =>
            {
                var p1 = new ToolProject { Name = "synced" };
                AssertEq("默认 Synced", ToolSyncState.Synced, p1.SyncState);
                Assert("默认 IsPendingSync=false", !p1.IsPendingSync);
                AssertEq("badge 文本空", string.Empty, p1.SyncBadgeText);

                var p2 = new ToolProject { Name = "new", SyncState = ToolSyncState.PendingCreate };
                Assert("IsPendingSync=true", p2.IsPendingSync);
                Assert("badge 含『新建』", p2.SyncBadgeText.Contains("新建"));

                var p3 = new ToolProject { Name = "upd", SyncState = ToolSyncState.PendingUpdate };
                Assert("IsPendingSync=true", p3.IsPendingSync);
                Assert("badge 含『修改』", p3.SyncBadgeText.Contains("修改"));
            });

            // ===== 17. ToolProject JSON 持久化包含 SyncState（关键：旧 bug 是 [JsonIgnore]）=====
            Test("ToolProject 持久化包含 SyncState", () =>
            {
                var tmp = Path.Combine(Path.GetTempPath(), "zlinks-syncstate-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmp);
                try
                {
                    var svc = new ToolPersistenceService(tmp);
                    var input = new List<ToolProject>
                    {
                        new() { Id = 1, Name = "synced",    SyncState = ToolSyncState.Synced },
                        new() { Id = -6370000000000, Name = "pending", SyncState = ToolSyncState.PendingCreate }
                    };
                    svc.SaveAsync(input).GetAwaiter().GetResult();
                    var loaded = svc.LoadAsync().GetAwaiter().GetResult();
                    AssertEq("count", 2, loaded.Count);
                    AssertEq("synced.SyncState", ToolSyncState.Synced, loaded[0].SyncState);
                    AssertEq("pending.SyncState", ToolSyncState.PendingCreate, loaded[1].SyncState);
                    Assert("synced.IsPendingSync=false", !loaded[0].IsPendingSync);
                    Assert("pending.IsPendingSync=true", loaded[1].IsPendingSync);

                    // 验证磁盘 JSON 真的包含 "SyncState" 字段（修复 [JsonIgnore] 旧 bug 的核心断言）
                    var rawJson = File.ReadAllText(Path.Combine(tmp, "tools.json"));
                    Assert("JSON 包含 SyncState", rawJson.Contains("\"SyncState\""));
                }
                finally
                {
                    try { Directory.Delete(tmp, recursive: true); } catch { }
                }
            });

            // ===== 18. 旧文件兼容：缺 SyncState 字段 → 反序列化默认为 Synced（向后兼容）=====
            Test("旧 JSON 缺 SyncState 字段 → 默认 Synced", () =>
            {
                var tmp = Path.Combine(Path.GetTempPath(), "zlinks-oldfmt-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmp);
                try
                {
                    // 手动写一份老格式 JSON（无 SyncState 字段）
                    var legacy =
                        "[{\"Id\":7,\"Name\":\"legacy\",\"RunMode\":\"Script\",\"Language\":\"python\"," +
                        "\"InterpreterPath\":\"\",\"ScriptPath\":\"\",\"ExecutablePath\":\"\"," +
                        "\"WorkingDirectory\":\"\",\"EnvironmentVariables\":\"\"," +
                        "\"DefaultArgumentPrefix\":\"--\",\"Arguments\":[],\"GitUrl\":\"\"," +
                        "\"CloneDirectory\":\"\",\"Notification\":{},\"IsSystemBuiltin\":false}]";
                    File.WriteAllText(Path.Combine(tmp, "tools.json"), legacy);

                    var svc = new ToolPersistenceService(tmp);
                    var loaded = svc.LoadAsync().GetAwaiter().GetResult();
                    AssertEq("count", 1, loaded.Count);
                    AssertEq("name", "legacy", loaded[0].Name);
                    AssertEq("默认 Synced", ToolSyncState.Synced, loaded[0].SyncState);
                    Assert("IsPendingSync=false", !loaded[0].IsPendingSync);
                }
                finally
                {
                    try { Directory.Delete(tmp, recursive: true); } catch { }
                }
            });

            // ===== 19. ParameterRow.RequireInput 字段 + OpenEditDialog 同步(修复 Bug A & B)=====
            // 验证:
            //  - ParameterRow 新增了 RequireInput 字段
            //  - OpenEditDialog 从 project.Arguments 同步到 EditParameters(包括 RequireInput)
            //  - BuildArgumentsFromEditRows 把 row.RequireInput 正确写回 ToolArgument(取消硬编码 false)
            Test("OpenEditDialog 同步参数列表(含 RequireInput)", () =>
            {
                // 准备一个含 3 个不同参数的工具(含必填/非必填/自定义前缀三种典型场景)
                var project = new ToolProject
                {
                    Id = 42,
                    Name = "test-tool",
                    RunMode = ToolRunModes.Script,
                    Language = "python",
                    DefaultArgumentPrefix = "--",
                    Arguments = new List<ToolArgument>
                    {
                        new() { Name = "rows", DefaultValue = "",         UseDefaultPrefix = true,  Prefix = "--", RequireInput = false, Order = 0 },
                        new() { Name = "env",  DefaultValue = "dev",      UseDefaultPrefix = true,  Prefix = "--", RequireInput = true,  Order = 1 },
                        new() { Name = "port", DefaultValue = "8080",     UseDefaultPrefix = false, Prefix = "-",  RequireInput = false, Order = 2 }
                    }
                };

                // 反射调用 OpenEditDialog(project)
                var openEdit = typeof(ToolLibraryViewModel).GetMethod(
                    "OpenEditDialog",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert("OpenEditDialog 方法存在", openEdit != null);
                openEdit!.Invoke(vm, new object[] { project });

                // 断言 1: 参数数量同步
                AssertEq("EditParameters 数量", 3, vm.EditParameters.Count);

                // 断言 2: 字段正确映射
                AssertEq("rows.Name", "rows", vm.EditParameters[0].Name);
                AssertEq("rows.Prefix", "--", vm.EditParameters[0].Prefix);
                AssertEq("rows.Value", "", vm.EditParameters[0].Value);
                Assert("rows.RequireInput=false", vm.EditParameters[0].RequireInput == false);

                AssertEq("env.Name", "env", vm.EditParameters[1].Name);
                AssertEq("env.Value", "dev", vm.EditParameters[1].Value);
                Assert("env.RequireInput=true(关键)", vm.EditParameters[1].RequireInput == true);

                AssertEq("port.Name", "port", vm.EditParameters[2].Name);
                AssertEq("port.Prefix(自定义)", "-", vm.EditParameters[2].Prefix);
                Assert("port.RequireInput=false", vm.EditParameters[2].RequireInput == false);

                // 断言 3: 往返一致(EditParameters → ToolArgument)
                var buildArgs = typeof(ToolLibraryViewModel).GetMethod(
                    "BuildArgumentsFromEditRows",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var rebuilt = (System.Collections.Generic.List<ToolArgument>)
                    buildArgs!.Invoke(vm, null)!;
                AssertEq("rebuilt.Count", 3, rebuilt.Count);
                AssertEq("rebuilt[1].RequireInput(取消硬编码 false)", true, rebuilt[1].RequireInput);
                Assert("rebuilt[0].RequireInput", rebuilt[0].RequireInput == false);
                Assert("rebuilt[2].RequireInput", rebuilt[2].RequireInput == false);
                AssertEq("rebuilt[2].Prefix(自定义)", "-", rebuilt[2].Prefix);

                // 断言 4: ParameterRow 字段可读写
                var row = new ParameterRow { Name = "x", RequireInput = true };
                Assert("ParameterRow.RequireInput 可读", row.RequireInput);
                row.RequireInput = false;
                Assert("ParameterRow.RequireInput 可写", row.RequireInput == false);
            });

            // ===== 20. Fallback 落地数据跨"重启"可恢复（核心回归测试）=====
            // 模拟:OfflineApi 返回 null → SaveProjectAsync 走 FallbackToPendingAsync →
            //       关闭 VM → 重新构造 VM → 加载本地缓存 → pending 工具出现在 Projects
            Test("Fallback 落地的工具在重启后仍可见", () =>
            {
                var tmp = Path.Combine(Path.GetTempPath(), "zlinks-restart-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmp);
                try
                {
                    var persistence = new ToolPersistenceService(tmp);
                    var offlineApi = new OfflineStubApiService(); // 后端全返回 null
                    var dlg = new RecordingDialogService();

                    // ===== Phase 1: 第一次会话 - 后端离线,新建工具 fallback =====
                    var pm = new ProcessManagerService();
                    var vm1 = BuildVm(offlineApi, dlg, persistence, pm);

                    // 直接调用 internal FallbackToPendingAsync,模拟保存失败落地的结果
                    var pending = new ToolProject
                    {
                        Id = -1234567, // 模拟本地临时负 ID
                        Name = "offline-tool",
                        RunMode = ToolRunModes.Script,
                        Language = "python",
                        ScriptPath = "C:\\tools\\offline.py",
                        SyncState = ToolSyncState.PendingCreate
                    };
                    var fallbackMethod = typeof(ToolLibraryViewModel).GetMethod(
                        "FallbackToPendingAsync",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    // 必须 await 反射返回的 Task,否则 phase1 的 await _persistence.SaveAsync()
                    // 还没写完缓存文件 phase2 就开始读了 → 偶发 phase2:Projects.Count==0
                    var fallbackTask = (Task)fallbackMethod!.Invoke(vm1, new object[] { pending, true })!;
                    fallbackTask.GetAwaiter().GetResult();

                    AssertEq("phase1: Projects 含 1 条", 1, vm1.Projects.Count);
                    AssertEq("phase1: PendingSyncTools 含 1 条", 1, vm1.PendingSyncTools.Count);

                    // ===== Phase 2: 重启 - 清空内存,重新构造 VM,加载本地缓存 =====
                    var vm2 = BuildVm(offlineApi, dlg, persistence, pm);

                    // LoadProjectsAsync 是 async fire-and-forget,等待它完成
                    var loadMethod = typeof(ToolLibraryViewModel).GetMethod(
                        "LoadProjectsAsync",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    var loadTask = (Task)loadMethod!.Invoke(vm2, null)!;
                    loadTask.GetAwaiter().GetResult();

                    // ===== 断言:重启后本地工具依然在 Projects(这是用户报的核心 bug) =====
                    AssertEq("phase2: Projects 含 1 条(关键)", 1, vm2.Projects.Count);
                    AssertEq("phase2: PendingSyncTools 含 1 条", 1, vm2.PendingSyncTools.Count);
                    AssertEq("phase2: name 保留", "offline-tool", vm2.Projects[0].Name);
                    AssertEq("phase2: SyncState=PendingCreate", ToolSyncState.PendingCreate, vm2.Projects[0].SyncState);
                    Assert("phase2: IsPendingSync=true", vm2.Projects[0].IsPendingSync);
                }
                finally
                {
                    try { Directory.Delete(tmp, recursive: true); } catch { }
                }
            });

            // ===== 21. VenvService:平台相关的 python 路径解析 + VenvExists 检测 =====
            Test("VenvService.ResolvePythonExePath 平台分支", () =>
            {
                var svc = new VenvService();
                var root = Path.Combine(Path.GetTempPath(), "zlinks-venvtest-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(root);
                try
                {
                    var resolved = svc.ResolvePythonExePath(root);
                    if (OperatingSystem.IsWindows())
                    {
                        Assert("Windows: 以 Scripts\\python.exe 结尾",
                            resolved.EndsWith(Path.Combine("Scripts", "python.exe"), StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        Assert("Unix: 以 bin/python 结尾",
                            resolved.EndsWith(Path.Combine("bin", "python")));
                    }
                    Assert("VenvExists=false(无 pyvenv.cfg)", !svc.VenvExists(root));
                    File.WriteAllText(Path.Combine(root, "pyvenv.cfg"), "home = /usr/bin");
                    Assert("VenvExists=true(有 pyvenv.cfg)", svc.VenvExists(root));
                    Assert("空目录 VenvExists=false", !svc.VenvExists(string.Empty));
                }
                finally
                {
                    try { Directory.Delete(root, recursive: true); } catch { }
                }
            });

            // ===== 22. VenvService.EnsureVenvAsync 防御性错误处理 =====
            Test("VenvService.EnsureVenvAsync 空 python → 失败", () =>
            {
                var svc = new VenvService();
                var tmp = Path.Combine(Path.GetTempPath(), "zlinks-venvempty-" + Guid.NewGuid().ToString("N"));
                var r = svc.EnsureVenvAsync(
                    pythonExe: "",
                    venvDirectory: tmp,
                    requirementsPath: "",
                    pipMirrorUrl: "").GetAwaiter().GetResult();
                Assert("空 pythonExe 不成功", !r.Success);
                Assert("错误信息非空", !string.IsNullOrEmpty(r.ErrorMessage));
                Assert("PythonExePath 留空", string.IsNullOrEmpty(r.PythonExePath));
            });

            // ===== 23. ToolProject 新增 Python venv 字段参与持久化 =====
            Test("ToolProject venv 字段持久化往返", () =>
            {
                var tmp = Path.Combine(Path.GetTempPath(), "zlinks-venvpersist-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmp);
                try
                {
                    var persist = new ToolPersistenceService(tmp);
                    var input = new List<ToolProject>
                    {
                        new()
                        {
                            Id = 1, Name = "py-tool", Language = "python",
                            RunMode = ToolRunModes.Script,
                            InterpreterPath = "C:\\Python\\python.exe",
                            WorkingDirectory = "C:\\tools\\demo",
                            CreateVenv = true,
                            VenvDirectory = ".venv",
                            RequirementsPath = "requirements.txt",
                            PipMirrorUrl = "https://pypi.tuna.tsinghua.edu.cn/simple"
                        },
                        new()
                        {
                            Id = 2, Name = "no-venv", Language = "node",
                            CreateVenv = false,
                            VenvDirectory = "",
                            RequirementsPath = "",
                            PipMirrorUrl = ""
                        }
                    };
                    persist.SaveAsync(input).GetAwaiter().GetResult();
                    var loaded = persist.LoadAsync().GetAwaiter().GetResult();
                    AssertEq("count", 2, loaded.Count);

                    AssertEq("CreateVenv[0]", true, loaded[0].CreateVenv);
                    AssertEq("VenvDirectory[0]", ".venv", loaded[0].VenvDirectory);
                    AssertEq("RequirementsPath[0]", "requirements.txt", loaded[0].RequirementsPath);
                    AssertEq("PipMirrorUrl[0]", "https://pypi.tuna.tsinghua.edu.cn/simple", loaded[0].PipMirrorUrl);

                    AssertEq("CreateVenv[1]", false, loaded[1].CreateVenv);
                    AssertEq("VenvDirectory[1]", "", loaded[1].VenvDirectory);

                    // 验证磁盘 JSON 里真的有这些字段
                    var raw = File.ReadAllText(Path.Combine(tmp, "tools.json"));
                    Assert("JSON 含 CreateVenv", raw.Contains("\"CreateVenv\""));
                    Assert("JSON 含 PipMirrorUrl", raw.Contains("\"PipMirrorUrl\""));
                    Assert("JSON 含 RequirementsPath", raw.Contains("\"RequirementsPath\""));
                }
                finally
                {
                    try { Directory.Delete(tmp, recursive: true); } catch { }
                }
            });

            // ===== 23.5 ToolEntity 4 字段往返无损(回归首次新建工具 venv 配置丢失 Bug)=====
            Test("ToolEntity venv 4 字段 POST→GET 往返无损", () =>
            {
                // 直接构造 ToolProject,模拟「带 venv 配置的新建」,走 MapToEntity → JSON → MapToProject 链
                var src = new ToolProject
                {
                    Id = 100,
                    Name = "py-venv-tool",
                    Language = "python",
                    RunMode = ToolRunModes.Script,
                    InterpreterPath = "",
                    WorkingDirectory = @"D:\tools\demo",
                    ScriptPath = @"D:\tools\demo\run.py",
                    CreateVenv = true,
                    VenvDirectory = "",
                    RequirementsPath = @"D:\tools\demo\requirements.txt",
                    PipMirrorUrl = "https://pypi.tuna.tsinghua.edu.cn/simple"
                };

                // 反射拿到私有的 MapToEntity / MapToProject
                var miEntity = typeof(ToolLibraryViewModel).GetMethod(
                    "MapToEntity",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                var miProject = typeof(ToolLibraryViewModel).GetMethod(
                    "MapToProject",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                Assert("MapToEntity 反射可见", miEntity != null);
                Assert("MapToProject 反射可见", miProject != null);

                var ent = (ToolEntity)miEntity!.Invoke(null, new object?[] { src })!;
                // 桌面 → 后端：4 字段都映射
                AssertEq("MapToEntity.CreateVenv", true, ent.CreateVenv);
                AssertEq("MapToEntity.VenvDirectory", "", ent.VenvDirectory);
                AssertEq("MapToEntity.RequirementsPath",
                    @"D:\tools\demo\requirements.txt", ent.RequirementsPath);
                AssertEq("MapToEntity.PipMirrorUrl",
                    "https://pypi.tuna.tsinghua.edu.cn/simple", ent.PipMirrorUrl);

                // 模拟后端回包:经过 Newtonsoft 序列化/反序列化往返(桌面端 API 调用实情)
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(ent);
                var ent2 = Newtonsoft.Json.JsonConvert.DeserializeObject<ToolEntity>(json);
                Assert("JSON 含 CreateVenv", json.Contains("\"CreateVenv\""));
                Assert("JSON 含 PipMirrorUrl", json.Contains("\"PipMirrorUrl\""));

                // 后端 → 桌面：4 字段都能被带回
                var back = (ToolProject)miProject!.Invoke(null, new object?[] { ent2! })!;
                AssertEq("MapToProject.CreateVenv", true, back.CreateVenv);
                AssertEq("MapToProject.VenvDirectory", "", back.VenvDirectory);
                AssertEq("MapToProject.RequirementsPath",
                    @"D:\tools\demo\requirements.txt", back.RequirementsPath);
                AssertEq("MapToProject.PipMirrorUrl",
                    "https://pypi.tuna.tsinghua.edu.cn/simple", back.PipMirrorUrl);
            });

            // ===== 24. OpenAddDialog 默认勾选 venv(创建+自动 pip install) =====
            Test("OpenAddDialog 默认勾选 venv + EditVenvDirectory 默认空", () =>
            {
                var openAdd = typeof(ToolLibraryViewModel).GetMethod(
                    "OpenAddDialog",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                openAdd!.Invoke(vm, null);

                Assert("默认 EditCreateVenv=true(创建 venv)", vm.EditCreateVenv == true);
                Assert("默认 EditAutoInstallRequirements=true", vm.EditAutoInstallRequirements == true);
                Assert("默认 EditVenvDirectory 空(默认 .venv)", string.IsNullOrEmpty(vm.EditVenvDirectory));
                Assert("默认 EditRequirementsPath 空", string.IsNullOrEmpty(vm.EditRequirementsPath));
                Assert("默认 EditPipMirrorUrl = 清华源",
                    vm.EditPipMirrorUrl == "https://pypi.tuna.tsinghua.edu.cn/simple");
            });

            // ===== 25. GitUrlParser.ParseRemoteUrl:解析 .git/config 中的 [remote] url =====
            Test("GitUrlParser.ParseRemoteUrl 解析 origin HTTPS", () =>
            {
                var config = "[core]\n\trepositoryformatversion = 0\n[remote \"origin\"]\n\turl = https://github.com/x/y.git\n\tfetch = +refs/heads/*:refs/remotes/origin/*\n[branch \"main\"]\n";
                AssertEq("origin url", "https://github.com/x/y.git", GitUrlParser.ParseRemoteUrl(config, "origin"));
            });

            Test("GitUrlParser.ParseRemoteUrl 解析 SSH 含 @", () =>
            {
                var config = "[remote \"upstream\"]\n\turl = git@github.com:foo/bar.git\n\tfetch = +refs/heads/*:refs/remotes/upstream/*\n";
                AssertEq("upstream ssh", "git@github.com:foo/bar.git", GitUrlParser.ParseRemoteUrl(config, "upstream"));
                Assert("origin 不存在返回 null", GitUrlParser.ParseRemoteUrl(config, "origin") == null);
            });

            Test("GitUrlParser.ParseRemoteUrl 支持带引号 url", () =>
            {
                var config = "[remote \"origin\"]\n\turl = \"https://example.com/p ath.git\"\n";
                AssertEq("带引号 url(去引号)", "https://example.com/p ath.git", GitUrlParser.ParseRemoteUrl(config, "origin"));
            });

            Test("GitUrlParser.ParseRemoteUrl 多 remote 取指定名", () =>
            {
                var config = "[remote \"origin\"]\n\turl = https://a.com/x.git\n[remote \"upstream\"]\n\turl = https://b.com/y.git\n";
                AssertEq("origin 选 a.com", "https://a.com/x.git", GitUrlParser.ParseRemoteUrl(config, "origin"));
                AssertEq("upstream 选 b.com", "https://b.com/y.git", GitUrlParser.ParseRemoteUrl(config, "upstream"));
            });

            Test("GitUrlParser.ParseRemoteUrl 空/异常输入", () =>
            {
                Assert("空字符串返回 null", GitUrlParser.ParseRemoteUrl(string.Empty) == null);
                Assert("null 返回 null", GitUrlParser.ParseRemoteUrl(null!) == null);
                Assert("无段返回 null", GitUrlParser.ParseRemoteUrl("[core]\n\tbare = true\n") == null);
            });

            // ===== 26. GitService.DetectRemoteAsync 读取真实 .git/config =====
            Test("GitService.DetectRemoteAsync 在 stub .git/config 上工作", () =>
            {
                var tmp = Path.Combine(Path.GetTempPath(), "zlinks-detect-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmp);
                try
                {
                    var gitDir = Path.Combine(tmp, ".git");
                    Directory.CreateDirectory(gitDir);
                    File.WriteAllText(Path.Combine(gitDir, "config"),
                        "[core]\n\trepositoryformatversion = 0\n" +
                        "[remote \"origin\"]\n\turl = git@github.com:foo/demo.git\n\tfetch = +refs/heads/*:refs/remotes/origin/*\n");

                    var svc = new GitService();
                    var (remoteName, url, logs) = svc.DetectRemoteAsync(tmp, "origin").GetAwaiter().GetResult();
                    AssertEq("remoteName=origin", "origin", remoteName);
                    AssertEq("url 解析正确", "git@github.com:foo/demo.git", url);
                    Assert("logs 非空(成功)", logs.Count > 0);

                    // 缺段
                    var (remoteName2, url2, _) = svc.DetectRemoteAsync(tmp, "nonexistent").GetAwaiter().GetResult();
                    Assert("缺段 url=null", url2 == null);
                    AssertEq("remoteName 回退", "nonexistent", remoteName2);

                    // 不存在的目录
                    var (_, url3, logs3) = svc.DetectRemoteAsync(
                        Path.Combine(Path.GetTempPath(), "nope-" + Guid.NewGuid().ToString("N")), "origin")
                        .GetAwaiter().GetResult();
                    Assert("不存在目录 url=null", url3 == null);
                    Assert("不存在目录 logs 非空", logs3.Count > 0);
                }
                finally
                {
                    try { Directory.Delete(tmp, recursive: true); } catch { }
                }
            });

            // ===== 27. ToolProject 新增 RemoteName 字段参与持久化 =====
            Test("ToolProject RemoteName 持久化往返", () =>
            {
                var tmp = Path.Combine(Path.GetTempPath(), "zlinks-remotename-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmp);
                try
                {
                    var persist = new ToolPersistenceService(tmp);
                    var input = new List<ToolProject>
                    {
                        new()
                        {
                            Id = 1, Name = "r1", GitUrl = "https://x/y.git", CloneDirectory = @"D:\tools",
                            RemoteName = "upstream"
                        },
                        new()
                        {
                            Id = 2, Name = "r2", GitUrl = "", CloneDirectory = "",
                            RemoteName = "origin"
                        }
                    };
                    persist.SaveAsync(input).GetAwaiter().GetResult();
                    var loaded = persist.LoadAsync().GetAwaiter().GetResult();
                    AssertEq("count", 2, loaded.Count);
                    AssertEq("r1.RemoteName", "upstream", loaded[0].RemoteName);
                    AssertEq("r2.RemoteName 默认 origin", "origin", loaded[1].RemoteName);

                    var raw = File.ReadAllText(Path.Combine(tmp, "tools.json"));
                    Assert("JSON 含 RemoteName", raw.Contains("\"RemoteName\""));
                }
                finally
                {
                    try { Directory.Delete(tmp, recursive: true); } catch { }
                }
            });

            // ===== 28. OpenAddDialog 默认 EditRemoteName="origin" + LocalGitHint 清空 =====
            Test("OpenAddDialog 默认 RemoteName=origin + 清空 LocalGitHint", () =>
            {
                var openAdd = typeof(ToolLibraryViewModel).GetMethod(
                    "OpenAddDialog",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                openAdd!.Invoke(vm, null);
                AssertEq("默认 EditRemoteName=origin", "origin", vm.EditRemoteName);
                Assert("默认 LocalGitHint 为空", string.IsNullOrEmpty(vm.LocalGitHint));
            });

            Console.WriteLine();
            Console.WriteLine($"=== 结果：通过 {_passed}，失败 {_failed} ===");
            Environment.Exit(_failed == 0 ? 0 : 1);
        }

        // ===== helpers =====
        static void Test(string name, Action action)
        {
            try
            {
                action();
                Console.WriteLine($"  ✅ {name}");
                _passed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ {name}");
                Console.WriteLine($"     {ex.Message}");
                _failed++;
            }
        }

        static void Assert(string label, bool condition)
        {
            if (!condition) throw new Exception($"断言失败：{label}");
        }

        static void AssertEq<T>(string label, T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception($"断言失败：{label}\n     预期：{expected}\n     实际：{actual}");
        }

        static void SetProp(object obj, string name, object value)
        {
            var prop = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            prop?.SetValue(obj, value);
        }

        // ===== Test helpers =====
        class MockHttpHandler : System.Net.Http.HttpMessageHandler
        {
            private readonly Func<System.Net.Http.HttpRequestMessage, System.Net.Http.HttpResponseMessage> _handler;
            public MockHttpHandler(Func<System.Net.Http.HttpRequestMessage, System.Net.Http.HttpResponseMessage> handler)
            {
                _handler = handler;
            }
            protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage req, CancellationToken ct)
            {
                return Task.FromResult(_handler(req));
            }
            public static System.Net.Http.HttpResponseMessage OkJson(string body) => new(System.Net.HttpStatusCode.OK)
            {
                Content = new System.Net.Http.StringContent(body, System.Text.Encoding.UTF8, "application/json")
            };
        }

        class InMemoryGlobalNotificationService : IGlobalNotificationService
        {
            private readonly GlobalNotificationConfig _config;
            public InMemoryGlobalNotificationService(GlobalNotificationConfig config) { _config = config; }
            public string DefaultFilePath => "(in-memory)";
            public Task<GlobalNotificationConfig> LoadAsync(CancellationToken ct = default) => Task.FromResult(_config);
            public Task SaveAsync(GlobalNotificationConfig config, CancellationToken ct = default) => Task.CompletedTask;
        }

        class StubNotificationService : INotificationService
        {
            public Task<List<NotificationSendResult>> SendAsync(ToolProject project, ToolRunSnapshot snapshot, CancellationToken ct) =>
                Task.FromResult(new List<NotificationSendResult>());
        }

        class StubDialogService : IDialogService
        {
            public Task ShowMessageAsync(string title, string message) => Task.CompletedTask;
            public Task<bool> ShowConfirmAsync(string title, string message, string okText = "确定", string cancelText = "取消")
                => Task.FromResult(true);
            public Task<bool> ShowNotificationDetailAsync(NotificationItem item) => Task.FromResult(true);
            public Task<Dictionary<string, string>?> PromptArgumentsAsync(IEnumerable<ToolArgument> arguments) => Task.FromResult<Dictionary<string, string>?>(null);
            public Task ShowOutputAsync(string toolName, ProcessRunResult result) => Task.CompletedTask;
            public Task ShowEnvironmentResultAsync(string title, string message, bool success) => Task.CompletedTask;
            public Task<RunConfirmation?> ShowRunConfirmationAsync(ToolProject project, string initialCommandLine, IEnumerable<EditableArgument> arguments) => Task.FromResult<RunConfirmation?>(null);
            public Task<string?> PickScriptFileInDirectoryAsync(string directory) => Task.FromResult<string?>(null);
            public Task ShowCloneLogAsync(string title, string message, IReadOnlyList<string> logs, bool success) => Task.CompletedTask;
            public Task<VenvResult> ShowVenvProgressAsync(string title, Func<IProgress<string>, CancellationToken, Task<VenvResult>> workAsync, CancellationTokenSource cts)
                => workAsync(new Progress<string>(_ => { }), CancellationToken.None);
        }

        class StubServiceProvider : IServiceProvider
        {
            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(LoginViewModel)) return new LoginViewModel();
                return null;
            }
        }

        class StubApiService : IApiService
        {
            public string BaseUrl => "http://localhost";
            public void SetAuthToken(string token) { }
            public Task<T?> GetAsync<T>(string endpoint) where T : class => Task.FromResult<T?>(null);
            public Task<T?> PostAsync<T>(string endpoint, object? data = null) where T : class => Task.FromResult<T?>(null);
            public Task<T?> PutAsync<T>(string endpoint, object? data = null) where T : class => Task.FromResult<T?>(null);
            public Task<bool> DeleteAsync(string endpoint) => Task.FromResult(false);
        }

        // ===== 用于「重启恢复」测试的离线 API =====
        class OfflineStubApiService : IApiService
        {
            public string BaseUrl => "http://offline";
            public void SetAuthToken(string token) { }
            public Task<T?> GetAsync<T>(string endpoint) where T : class => Task.FromResult<T?>(null);
            public Task<T?> PostAsync<T>(string endpoint, object? data = null) where T : class => Task.FromResult<T?>(null);
            public Task<T?> PutAsync<T>(string endpoint, object? data = null) where T : class => Task.FromResult<T?>(null);
            public Task<bool> DeleteAsync(string endpoint) => Task.FromResult(false);
        }

        class RecordingDialogService : IDialogService
        {
            public List<string> Messages { get; } = new();
            public Task ShowMessageAsync(string title, string message)
            {
                Messages.Add($"{title}|{message}");
                return Task.CompletedTask;
            }
            public Task<bool> ShowConfirmAsync(string title, string message, string okText = "确定", string cancelText = "取消")
            {
                Messages.Add($"CONFIRM|{title}|{message}");
                return Task.FromResult(true);
            }
            public Task<bool> ShowNotificationDetailAsync(NotificationItem item) => Task.FromResult(true);
            public Task<Dictionary<string, string>?> PromptArgumentsAsync(IEnumerable<ToolArgument> arguments)
                => Task.FromResult<Dictionary<string, string>?>(null);
            public Task ShowOutputAsync(string toolName, ProcessRunResult result) => Task.CompletedTask;
            public Task ShowEnvironmentResultAsync(string title, string message, bool success) => Task.CompletedTask;
            public Task<RunConfirmation?> ShowRunConfirmationAsync(ToolProject project, string initialCommandLine, IEnumerable<EditableArgument> arguments)
                => Task.FromResult<RunConfirmation?>(null);
            public Task<string?> PickScriptFileInDirectoryAsync(string directory) => Task.FromResult<string?>(null);
            public Task ShowCloneLogAsync(string title, string message, IReadOnlyList<string> logs, bool success) => Task.CompletedTask;
            public Task<VenvResult> ShowVenvProgressAsync(string title, Func<IProgress<string>, CancellationToken, Task<VenvResult>> workAsync, CancellationTokenSource cts)
                => workAsync(new Progress<string>(_ => { }), CancellationToken.None);
        }

        class StubRuntimeEnvService : IRuntimeEnvironmentService
        {
            public Task<List<RuntimeEnvironment>> DetectAllAsync() => Task.FromResult(new List<RuntimeEnvironment>());
            public Task<RuntimeEnvironment> DetectAsync(string language)
            {
                var env = new RuntimeEnvironment { Language = language, DisplayName = language, IsAvailable = true };
                return Task.FromResult(env);
            }
            public Task<RuntimeEnvironment> ReDetectAsync(string language) => DetectAsync(language);
        }

        class StubFilePickerService : IFilePickerService
        {
            public Task<string?> PickImageFileAsync() => Task.FromResult<string?>(null);
            public Task<string?> PickFontFileAsync() => Task.FromResult<string?>(null);
            public Task<string?> PickScriptFileAsync() => Task.FromResult<string?>(null);
            public Task<string?> PickDirectoryAsync() => Task.FromResult<string?>(null);
            public Task<string?> PickFileAsync(string title, string pattern) => Task.FromResult<string?>(null);
        }

        class StubGitService : IGitService
        {
            public Task<GitEnvironmentInfo> DetectAsync(CancellationToken ct = default)
                => Task.FromResult(new GitEnvironmentInfo { IsInstalled = false, Version = "stub", ExecutablePath = "git" });
            public Task<CloneResult> CloneAsync(string url, string targetParentDir, IProgress<string>? progress = null, CancellationToken ct = default)
                => Task.FromResult(new CloneResult { Success = false, ErrorMessage = "stub" });
            public Task<CloneResult> PullAsync(string repoRoot, IProgress<string>? progress = null, CancellationToken ct = default,
                string? initUrl = null, string initRemoteName = "origin")
                => Task.FromResult(new CloneResult { Success = false, ErrorMessage = "stub" });
            public Task<(string RemoteName, string? Url, List<string> Logs)> DetectRemoteAsync(
                string dir, string remoteName = "origin", CancellationToken ct = default)
                => Task.FromResult((remoteName, (string?)null, new List<string> { "stub" }));
            public Task<CloneResult> EnsureRemoteAsync(
                string dir, string? url, string remoteName = "origin",
                bool initIfMissing = true, IProgress<string>? progress = null, CancellationToken ct = default)
                => Task.FromResult(new CloneResult { Success = false, ErrorMessage = "stub" });
        }

        /// <summary>
        /// 通过反射构造 ToolLibraryViewModel 并等待构造函数触发的 LoadProjectsAsync 完成。
        /// </summary>
        static ToolLibraryViewModel BuildVm(
            IApiService api,
            IDialogService dlg,
            IToolPersistenceService persistence,
            IProcessManagerService pm)
        {
            var rt = new StubRuntimeEnvService();
            var fp = new StubFilePickerService();
            var git = new StubGitService();
            var nfs = new StubNotificationService();
            var venv = new VenvService();
            var vm = (ToolLibraryViewModel)Activator.CreateInstance(
                typeof(ToolLibraryViewModel),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { api, dlg, rt, fp, pm, git, persistence, nfs, venv },
                culture: null)!;

            // 等待构造函数触发的 LoadProjectsAsync 协程(它读缓存 + 调后端,后端是 null 路径)
            // ApiService.GetAsync 立即返回 → 但 ObservableCollection 写入也在异步路径上
            // 简单用 SpinWait 100ms 足矣（测试环境，~50ms 已能完成）
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 500)
            {
                if (vm.Projects != null) break;
                Thread.Sleep(10);
            }
            Thread.Sleep(50);
            return vm;
        }
    }
}
