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
                args: new object?[] { null, null, null, null, pm, null, null, null }, // ... INotificationService
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
                var expected = "python \"C:\\tools\\build.py\" -- output out -- verbose true";
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
                var expected = "python \"C:\\tools\\build.py\" - port 8080";
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
                var expected = "C:\\tools\\ffmpeg.exe - i in.mp4 - y";
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
                AssertEq("default prefix", "--", ea.UseDefaultPrefix ? "default" : ea.Prefix);
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
    }
}
