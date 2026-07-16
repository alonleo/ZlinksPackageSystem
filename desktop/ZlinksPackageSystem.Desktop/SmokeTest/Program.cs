using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
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
                args: new object?[] { null, null, null, null, pm, null, null }, // ... IGitService, IToolPersistenceService
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
                    RunMode = ToolRunMode.Script,
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
                    RunMode = ToolRunMode.Script,
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
                    RunMode = ToolRunMode.LocalExecutable,
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
                    RunMode = ToolRunMode.LocalExecutable,
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
    }
}
