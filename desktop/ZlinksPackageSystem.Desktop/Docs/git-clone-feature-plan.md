# 工具库 Git 路径克隆区域 - 实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 在桌面端「工具库 → 新建工具」弹窗中新增 Git 仓库克隆区域，支持 HTTP/SSH URL、自动检测本地 Git、选目标目录、后台克隆（流式进度 + 可取消），并自动把脚本路径与工作目录回填到克隆出的仓库根；同时把工具列表升级为 JSON 持久化 + 导入/导出。

**架构：** 新增 `IGitService`（detect/clone）+ `IToolPersistenceService`（load/save/import/export），DI 单例注入 `ToolLibraryViewModel`；新增 `PickScriptFileInDirectoryAsync` + `ShowCloneLogAsync` 两个 DialogService 方法；新增 `BoolToExpandGlyphConverter`；Git 面板仅在 `IsNewProject=true` 时显示。

**技术栈：** .NET 10 + Avalonia 11.2 + CommunityToolkit.Mvvm 8.2 + System.Text.Json + `System.Diagnostics.Process`。项目根：`desktop/ZlinksPackageSystem.Desktop/`。

---

## 文件结构

| 性质 | 路径 | 职责 |
| --- | --- | --- |
| 新 | `Models/GitEnvironmentInfo.cs` | DTO：Git 是否安装/版本/路径 |
| 新 | `Models/CloneResult.cs` | DTO：克隆结果（成功/失败/取消 + 日志） |
| 新 | `Converters/BoolToExpandGlyphConverter.cs` | `true→"▲"`、`false→"▼"` |
| 新 | `Services/IGitService.cs` | 接口：`DetectAsync / CloneAsync` |
| 新 | `Services/GitService.cs` | 实现：调 `git --version` 与 `git clone --progress` |
| 新 | `Services/IToolPersistenceService.cs` | 接口：`Load/Save/Import/Export + DefaultFilePath` |
| 新 | `Services/ToolPersistenceService.cs` | 实现：JSON 读写 |
| 改 | `Models/ToolProject.cs` | 加 `GitUrl / CloneDirectory`（持久化）+ `ClonedRepoRoot`（`[JsonIgnore]`） |
| 改 | `Services/IDialogService.cs` | 加两个方法签名 |
| 改 | `Services/DialogService.cs` | 实现两个方法 |
| 改 | `App.axaml.cs` | DI 注册两个新服务 |
| 改 | `ViewModels/ToolLibraryViewModel.cs` | 注入 + 新属性 + 新命令 + 改 Open*/Save*/Load*/Delete |
| 改 | `Views/ToolLibraryView.axaml` | 顶栏导入/导出 + 弹窗 Git 面板 |
| 改 | `SmokeTest/Program.cs` | 7 个新用例 + 构造器参数补齐 |

---

## 任务 1：扩展 `ToolProject` 数据模型

**文件：** 修改 `desktop/ZlinksPackageSystem.Desktop/Models/ToolProject.cs`

- [ ] **步骤 1：在第 60 行 `[JsonIgnore]` 区块前加入两个持久化字段 + 一个 `[JsonIgnore]` 字段**

定位：在 `Models/ToolProject.cs` 第 58 行 `public List<ToolArgument> Arguments { get; set; } = new();` 与第 60 行 `// ===== 运行时状态（不参与持久化）=====` 之间插入：

```csharp
        // ===== Git 仓库（仅新建时填写，编辑时不再修改）=====
        /// <summary>Git 仓库 URL（HTTPS 或 SSH），可选</summary>
        public string GitUrl { get; set; } = string.Empty;

        /// <summary>克隆目标父目录（如 D:\tools），可选</summary>
        public string CloneDirectory { get; set; } = string.Empty;
```

定位：在第 67 行 `public int? ProcessId { get; set; }` 后追加：

```csharp

        /// <summary>运行期：克隆成功后实际生成的仓库根目录（= CloneDirectory/<repo名>）。不参与持久化。</summary>
        [JsonIgnore]
        public string ClonedRepoRoot { get; set; } = string.Empty;
```

- [ ] **步骤 2：Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
git add desktop/ZlinksPackageSystem.Desktop/Models/ToolProject.cs
git commit -m "feat(desktop): ToolProject 增加 GitUrl/CloneDirectory/ClonedRepoRoot 字段"
```

---

## 任务 2：新增 DTO `GitEnvironmentInfo` 与 `CloneResult`

**文件：**
- 新建 `desktop/ZlinksPackageSystem.Desktop/Models/GitEnvironmentInfo.cs`
- 新建 `desktop/ZlinksPackageSystem.Desktop/Models/CloneResult.cs`

- [ ] **步骤 1：写 `GitEnvironmentInfo.cs`**

```csharp
namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 本机 Git 环境检测结果
    /// </summary>
    public class GitEnvironmentInfo
    {
        public bool IsInstalled { get; set; }
        public string Version { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
    }
}
```

- [ ] **步骤 2：写 `CloneResult.cs`**

```csharp
using System.Collections.Generic;

namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// git clone 的结果：成功 / 失败 / 取消
    /// </summary>
    public class CloneResult
    {
        public bool Success { get; set; }
        public string RepoRoot { get; set; } = string.Empty;
        public string RepoName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public bool Cancelled { get; set; }
        public List<string> Logs { get; set; } = new();
    }
}
```

- [ ] **步骤 3：Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
git add desktop/ZlinksPackageSystem.Desktop/Models/GitEnvironmentInfo.cs \
        desktop/ZlinksPackageSystem.Desktop/Models/CloneResult.cs
git commit -m "feat(desktop): 新增 GitEnvironmentInfo / CloneResult DTO"
```

---

## 任务 3：新增转换器 `BoolToExpandGlyphConverter`

**文件：**
- 新建 `desktop/ZlinksPackageSystem.Desktop/Converters/BoolToExpandGlyphConverter.cs`
- 修改 `desktop/ZlinksPackageSystem.Desktop/App.axaml`（注册静态资源）

- [ ] **步骤 1：参考 `EnumEqualsConverter` 写新转换器**

```csharp
using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ZlinksPackageSystem.Desktop.Converters
{
    public class BoolToExpandGlyphConverter : IValueConverter
    {
        public static readonly BoolToExpandGlyphConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool b && b ? "▲" : "▼";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
```

- [ ] **步骤 2：在 `App.axaml` 第 84–98 行的全局转换器 `Style.Resources` 中追加**

定位：在 `App.axaml` 找到 `<Style.Resources>` 或 `Application.Resources`，在 `EnumEqualsConverter` 后追加（参照现有 `<converters:EnumEqualsConverter x:Key="EnumEqualsConverter" />` 模式）：

```xml
<converters:BoolToExpandGlyphConverter x:Key="BoolToExpandGlyphConverter" />
```

> 具体语法参考 `App.axaml` 中已存在的 `<converters:BooleanInverseConverter x:Key="BooleanInverseConverter" />` 写法（在同一区域）。

- [ ] **步骤 3：Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
git add desktop/ZlinksPackageSystem.Desktop/Converters/BoolToExpandGlyphConverter.cs \
        desktop/ZlinksPackageSystem.Desktop/App.axaml
git commit -m "feat(desktop): 新增 BoolToExpandGlyphConverter + App.axaml 注册"
```

---

## 任务 4：`IGitService` 接口 + URL 解析纯函数（TDD）

**文件：**
- 新建 `desktop/ZlinksPackageSystem.Desktop/Services/IGitService.cs`
- 修改 `desktop/ZlinksPackageSystem.Desktop/SmokeTest/Program.cs`（写新测试）
- 新建 `desktop/ZlinksPackageSystem.Desktop/Services/GitUrlParser.cs`（静态纯函数）

- [ ] **步骤 1：写失败的 SmokeTest 用例**

定位：在 `SmokeTest/Program.cs` 第 222 行 `ToolRunMode 枚举值` 测试块**之后**，第 223 行 `Console.WriteLine();` **之前**插入：

```csharp
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
```

- [ ] **步骤 2：运行测试确认失败**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
dotnet run --project desktop/ZlinksPackageSystem.Desktop/SmokeTest/SmokeTest.csproj
```

预期：编译失败（`GitUrlParser` 未定义）。

- [ ] **步骤 3：写 `GitUrlParser.cs`**

新建 `desktop/ZlinksPackageSystem.Desktop/Services/GitUrlParser.cs`：

```csharp
using System;
using System.IO;
using System.Linq;

namespace ZlinksPackageSystem.Desktop.Services
{
    /// <summary>
    /// 纯函数：从 Git URL 解析仓库名 / 计算仓库根路径。
    /// 不做 I/O，便于单测。
    /// </summary>
    public static class GitUrlParser
    {
        public static string ParseRepoName(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Git URL 不能为空", nameof(url));

            var s = url.Trim();
            // 去掉结尾的 .git / /
            if (s.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                s = s[..^4];
            if (s.EndsWith("/", StringComparison.Ordinal))
                s = s[..^1];

            // 取最后一段（处理 SSH 的冒号、URL 的斜杠、token 的 @）
            int lastSlash = s.LastIndexOf('/');
            int lastColon = s.LastIndexOf(':');
            int start = Math.Max(lastSlash, lastColon) + 1;
            if (start >= s.Length)
                throw new ArgumentException($"无法解析仓库名：{url}", nameof(url));

            var name = s[start..];
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"无法解析仓库名：{url}", nameof(url));

            return name;
        }

        public static string CombineRepoRoot(string targetParentDir, string repoName)
        {
            if (string.IsNullOrEmpty(targetParentDir))
                throw new ArgumentException("targetParentDir 不能为空", nameof(targetParentDir));
            if (string.IsNullOrEmpty(repoName))
                throw new ArgumentException("repoName 不能为空", nameof(repoName));
            return Path.Combine(targetParentDir, repoName);
        }
    }
}
```

- [ ] **步骤 4：运行测试确认通过**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
dotnet run --project desktop/ZlinksPackageSystem.Desktop/SmokeTest/SmokeTest.csproj
```

预期：6 个新用例 ✅，其它原有用例仍 ✅，总计 通过 +6。

- [ ] **步骤 5：Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
git add desktop/ZlinksPackageSystem.Desktop/Services/GitUrlParser.cs \
        desktop/ZlinksPackageSystem.Desktop/SmokeTest/Program.cs
git commit -m "feat(desktop): 新增 GitUrlParser + SmokeTest 6 个用例"
```

---

## 任务 5：`IGitService` 接口 + `GitService` 实现

**文件：**
- 新建 `desktop/ZlinksPackageSystem.Desktop/Services/IGitService.cs`
- 新建 `desktop/ZlinksPackageSystem.Desktop/Services/GitService.cs`

- [ ] **步骤 1：写 `IGitService.cs`**

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public interface IGitService
    {
        /// <summary>检测本机 Git：git --version + where/which git</summary>
        Task<GitEnvironmentInfo> DetectAsync(CancellationToken ct = default);

        /// <summary>
        /// 后台克隆。stderr 通过 progress 推送。ct 取消时杀进程、清目标目录、返回 Cancelled=true。
        /// </summary>
        Task<CloneResult> CloneAsync(string url, string targetParentDir,
            IProgress<string>? progress = null, CancellationToken ct = default);
    }
}
```

- [ ] **步骤 2：写 `GitService.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public class GitService : IGitService
    {
        public async Task<GitEnvironmentInfo> DetectAsync(CancellationToken ct = default)
        {
            var info = new GitEnvironmentInfo();
            try
            {
                // 1) 找 git 可执行文件绝对路径
                var psi = new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "where" : "which",
                    ArgumentList = { "git" },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p1 = Process.Start(psi);
                if (p1 == null) return info;
                var pathOut = await p1.StandardOutput.ReadToEndAsync(ct);
                await p1.WaitForExitAsync(ct);
                if (p1.ExitCode != 0) return info;
                var path = pathOut.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                if (string.IsNullOrEmpty(path)) return info;
                info.ExecutablePath = path;

                // 2) 跑 git --version
                var verPsi = new ProcessStartInfo
                {
                    FileName = path,
                    ArgumentList = { "--version" },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p2 = Process.Start(verPsi);
                if (p2 == null) return info;
                var verOut = await p2.StandardOutput.ReadToEndAsync(ct);
                await p2.WaitForExitAsync(ct);
                if (p2.ExitCode != 0) return info;
                info.Version = verOut.Trim().Split('\n').FirstOrDefault()?.Trim() ?? string.Empty;
                info.IsInstalled = !string.IsNullOrEmpty(info.Version);
                return info;
            }
            catch
            {
                return info;
            }
        }

        public async Task<CloneResult> CloneAsync(string url, string targetParentDir,
            IProgress<string>? progress = null, CancellationToken ct = default)
        {
            var logs = new List<string>();
            void Log(string line)
            {
                if (string.IsNullOrEmpty(line)) return;
                logs.Add(line);
                try { progress?.Report(line); } catch { /* UI 关闭后报告失败，吞掉 */ }
            }

            var result = new CloneResult();
            if (string.IsNullOrWhiteSpace(url))
            {
                result.ErrorMessage = "Git URL 为空";
                return result;
            }
            if (string.IsNullOrWhiteSpace(targetParentDir))
            {
                result.ErrorMessage = "目标目录为空";
                return result;
            }

            string repoName;
            try { repoName = GitUrlParser.ParseRepoName(url); }
            catch (ArgumentException ex) { result.ErrorMessage = ex.Message; return result; }

            var repoRoot = GitUrlParser.CombineRepoRoot(targetParentDir, repoName);
            result.RepoName = repoName;
            result.RepoRoot = repoRoot;

            // 若目标父目录已存在且非空，git clone 会失败；提前友好提示
            if (Directory.Exists(targetParentDir)
                && Directory.EnumerateFileSystemEntries(targetParentDir).Any())
            {
                result.ErrorMessage = $"目标目录非空：{targetParentDir}\n请选择一个空目录或新建目录。";
                return result;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "git",
                ArgumentList = { "clone", "--progress", url, targetParentDir },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            try
            {
                if (!proc.Start())
                {
                    result.ErrorMessage = "无法启动 git 进程";
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"启动 git 失败：{ex.Message}";
                return result;
            }

            // 取消 → 杀整棵进程树
            using var reg = ct.Register(() =>
            {
                try
                {
                    if (!proc.HasExited)
                        proc.Kill(entireProcessTree: true);
                }
                catch { /* 已退出 */ }
            });

            // 流式读 stdout / stderr
            var stdoutTask = Task.Run(async () =>
            {
                while (!proc.StandardOutput.EndOfStream)
                {
                    var line = await proc.StandardOutput.ReadLineAsync(ct);
                    if (line != null) Log(line);
                }
            }, ct);
            var stderrTask = Task.Run(async () =>
            {
                while (!proc.StandardError.EndOfStream)
                {
                    var line = await proc.StandardError.ReadLineAsync(ct);
                    if (line != null) Log(line);
                }
            }, ct);

            try
            {
                await proc.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                result.Cancelled = true;
                result.ErrorMessage = "已取消";
                result.Logs = logs;
                return result;
            }

            // 等流读完
            try { await Task.WhenAll(stdoutTask, stderrTask); } catch { /* 取消时流被截断 */ }

            if (proc.ExitCode == 0 && Directory.Exists(repoRoot))
            {
                result.Success = true;
            }
            else
            {
                result.ErrorMessage = $"git clone 退出码 {proc.ExitCode}";
            }
            return result;
        }
    }
}
```

- [ ] **步骤 3：构建验证**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
dotnet build desktop/ZlinksPackageSystem.Desktop/ZlinksPackageSystem.Desktop.csproj
```

预期：BUILD SUCCESS，无警告。

- [ ] **步骤 4：Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
git add desktop/ZlinksPackageSystem.Desktop/Services/IGitService.cs \
        desktop/ZlinksPackageSystem.Desktop/Services/GitService.cs
git commit -m "feat(desktop): 新增 IGitService + GitService 实现（detect/clone + 取消）"
```

---

## 任务 6：`IToolPersistenceService` + `ToolPersistenceService`（TDD）

**文件：**
- 新建 `desktop/ZlinksPackageSystem.Desktop/Services/IToolPersistenceService.cs`
- 新建 `desktop/ZlinksPackageSystem.Desktop/Services/ToolPersistenceService.cs`
- 修改 `desktop/ZlinksPackageSystem.Desktop/SmokeTest/Program.cs`（加用例）

- [ ] **步骤 1：写失败的 SmokeTest 用例（用临时目录）**

在 `SmokeTest/Program.cs` 任务 4 测试块后追加：

```csharp
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
                        new() { Id = 3, Name = "t3" } // 验证默认值不丢
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
```

- [ ] **步骤 2：运行测试确认失败**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
dotnet run --project desktop/ZlinksPackageSystem.Desktop/SmokeTest/SmokeTest.csproj
```

预期：编译失败（`ToolPersistenceService` 未定义）。

- [ ] **步骤 3：写 `IToolPersistenceService.cs`**

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public interface IToolPersistenceService
    {
        string DefaultFilePath { get; }
        Task<List<ToolProject>> LoadAsync(CancellationToken ct = default);
        Task SaveAsync(IEnumerable<ToolProject> projects, CancellationToken ct = default);
        Task ExportAsync(string filePath, IEnumerable<ToolProject> projects, CancellationToken ct = default);
        Task<List<ToolProject>?> ImportAsync(string filePath, CancellationToken ct = default);
    }
}
```

- [ ] **步骤 4：写 `ToolPersistenceService.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public class ToolPersistenceService : IToolPersistenceService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _directory;
        private readonly string _defaultFile;

        public ToolPersistenceService(string? directoryOverride = null)
        {
            _directory = directoryOverride
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ZlinksPackageSystem");
            _defaultFile = Path.Combine(_directory, "tools.json");
        }

        public string DefaultFilePath => _defaultFile;

        public async Task<List<ToolProject>> LoadAsync(CancellationToken ct = default)
        {
            try
            {
                if (!File.Exists(_defaultFile)) return new List<ToolProject>();
                await using var fs = File.OpenRead(_defaultFile);
                var list = await JsonSerializer.DeserializeAsync<List<ToolProject>>(fs, JsonOptions, ct);
                return list ?? new List<ToolProject>();
            }
            catch
            {
                return new List<ToolProject>();
            }
        }

        public async Task SaveAsync(IEnumerable<ToolProject> projects, CancellationToken ct = default)
        {
            Directory.CreateDirectory(_directory);
            var list = new List<ToolProject>(projects);
            await using var fs = File.Create(_defaultFile);
            await JsonSerializer.SerializeAsync(fs, list, JsonOptions, ct);
        }

        public async Task ExportAsync(string filePath, IEnumerable<ToolProject> projects, CancellationToken ct = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
            var list = new List<ToolProject>(projects);
            await using var fs = File.Create(filePath);
            await JsonSerializer.SerializeAsync(fs, list, JsonOptions, ct);
        }

        public async Task<List<ToolProject>?> ImportAsync(string filePath, CancellationToken ct = default)
        {
            try
            {
                if (!File.Exists(filePath)) return null;
                await using var fs = File.OpenRead(filePath);
                var list = await JsonSerializer.DeserializeAsync<List<ToolProject>>(fs, JsonOptions, ct);
                return list ?? new List<ToolProject>();
            }
            catch
            {
                return null;
            }
        }
    }
}
```

- [ ] **步骤 5：运行测试确认通过**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
dotnet run --project desktop/ZlinksPackageSystem.Desktop/SmokeTest/SmokeTest.csproj
```

预期：2 个新用例 ✅，总计 通过 +2。

- [ ] **步骤 6：Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
git add desktop/ZlinksPackageSystem.Desktop/Services/IToolPersistenceService.cs \
        desktop/ZlinksPackageSystem.Desktop/Services/ToolPersistenceService.cs \
        desktop/ZlinksPackageSystem.Desktop/SmokeTest/Program.cs
git commit -m "feat(desktop): 新增 IToolPersistenceService + ToolPersistenceService + 2 个 SmokeTest"
```

---

## 任务 7：`IDialogService` 扩展 + `DialogService` 实现

**文件：**
- 修改 `desktop/ZlinksPackageSystem.Desktop/Services/IDialogService.cs`
- 修改 `desktop/ZlinksPackageSystem.Desktop/Services/DialogService.cs`

- [ ] **步骤 1：在 `IDialogService.cs` 第 36 行 `}` 前追加两个方法签名**

```csharp
        /// <summary>在指定目录里挑一个脚本文件（.py/.js/.ts/.java/.go/.ps1/.sh/.bat/.cmd）。返回 null=取消。</summary>
        Task<string?> PickScriptFileInDirectoryAsync(string directory);

        /// <summary>显示克隆日志详情弹窗（含可滚动只读日志 + 复制按钮）。</summary>
        Task ShowCloneLogAsync(string title, string message, IReadOnlyList<string> logs, bool success);
```

- [ ] **步骤 2：在 `DialogService.cs` 第 1163 行 `}` 前追加两个方法实现**

```csharp
        public async Task<string?> PickScriptFileInDirectoryAsync(string directory)
        {
            var owner = Owner;
            if (owner == null) return null;
            try
            {
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return null;

                var options = new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "选择脚本文件",
                    AllowMultiple = false,
                    SuggestedStartLocation = new Avalonia.Platform.Storage.PhysicalFolder(directory),
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("脚本文件")
                        {
                            Patterns = new[] { "*.py", "*.js", "*.ts", "*.java", "*.go", "*.ps1", "*.sh", "*.bat", "*.cmd" }
                        },
                        new Avalonia.Platform.Storage.FilePickerFileType("所有文件") { Patterns = new[] { "*" } }
                    }
                };

                var files = await owner.StorageProvider.OpenFilePickerAsync(options);
                var first = files?.FirstOrDefault();
                if (first == null) return null;
                return first.TryGetLocalPath() ?? first.Path.ToString();
            }
            catch
            {
                return null;
            }
        }

        public async Task ShowCloneLogAsync(string title, string message, IReadOnlyList<string> logs, bool success)
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
                CanResize = true,
                MinWidth = 480,
                MinHeight = 300,
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
                Margin = new Thickness(20, 16, 20, 12),
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.Parse("#FFBFcbd9"))
            };

            var logBox = new TextBox
            {
                Text = logs == null || logs.Count == 0 ? "（无日志）" : string.Join(Environment.NewLine, logs),
                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Consolas, Menlo, Courier New, monospace"),
                FontSize = 11,
                Height = 200,
                Margin = new Thickness(20, 0, 20, 8),
                Background = new SolidColorBrush(Color.Parse("#0DFFFFFF")),
                BorderBrush = new SolidColorBrush(Color.Parse("#33FFFFFF")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8)
            };

            var copyBtn = new Button
            {
                Content = "📋 复制日志",
                Width = 120,
                Height = 32,
                Margin = new Thickness(20, 0, 6, 0),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            copyBtn.Click += (_, _) =>
            {
                try
                {
                    var clipboard = TopLevel.GetTopLevel(dialog)?.Clipboard;
                    clipboard?.SetTextAsync(logBox.Text ?? string.Empty);
                }
                catch { /* best-effort */ }
            };

            var okBtn = new Button
            {
                Content = "确定",
                Width = 100,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = new SolidColorBrush(Color.Parse("#FF1976D2")),
                BorderBrush = new SolidColorBrush(Color.Parse("#FF1976D2")),
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 0, 20, 16)
            };
            okBtn.Click += (_, _) => dialog.Close();

            var btnRow = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                Margin = new Thickness(0, 4, 0, 0)
            };
            btnRow.Children.Add(copyBtn);
            Grid.SetColumn(copyBtn, 0);
            btnRow.Children.Add(okBtn);
            Grid.SetColumn(okBtn, 1);

            var root = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);
            DockPanel.SetDock(messageBlock, Dock.Top);
            root.Children.Add(messageBlock);
            DockPanel.SetDock(btnRow, Dock.Bottom);
            root.Children.Add(btnRow);
            root.Children.Add(logBox);

            dialog.Content = root;
            await dialog.ShowDialog(owner);
        }
```

- [ ] **步骤 3：构建验证**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
dotnet build desktop/ZlinksPackageSystem.Desktop/ZlinksPackageSystem.Desktop.csproj
```

预期：BUILD SUCCESS。

- [ ] **步骤 4：Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
git add desktop/ZlinksPackageSystem.Desktop/Services/IDialogService.cs \
        desktop/ZlinksPackageSystem.Desktop/Services/DialogService.cs
git commit -m "feat(desktop): DialogService 新增 PickScriptFileInDirectoryAsync + ShowCloneLogAsync"
```

---

## 任务 8：DI 注册新服务

**文件：** 修改 `desktop/ZlinksPackageSystem.Desktop/App.axaml.cs`

- [ ] **步骤 1：在第 25–30 行 `// Services` 块中追加两行**

定位：第 30 行 `services.AddSingleton<IProcessManagerService, ProcessManagerService>();` **之后**插入：

```csharp
                                services.AddSingleton<IGitService, GitService>();
                                services.AddSingleton<IToolPersistenceService, ToolPersistenceService>();
```

- [ ] **步骤 2：构建验证**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
dotnet build desktop/ZlinksPackageSystem.Desktop/ZlinksPackageSystem.Desktop.csproj
```

预期：BUILD SUCCESS（DI 注册不影响编译，但要确保引用了正确命名空间 `ZlinksPackageSystem.Desktop.Services`）。

- [ ] **步骤 3：Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
git add desktop/ZlinksPackageSystem.Desktop/App.axaml.cs
git commit -m "feat(desktop): DI 注册 IGitService / IToolPersistenceService"
```

---

## 任务 9：`ToolLibraryViewModel` 集成新服务

**文件：** 修改 `desktop/ZlinksPackageSystem.Desktop/ViewModels/ToolLibraryViewModel.cs`

- [ ] **步骤 1：补齐构造器参数（5 → 7）+ 字段**

定位：在第 23 行 `private readonly IProcessManagerService _processManager;` 后追加：

```csharp
        private readonly IGitService _gitService;
        private readonly IToolPersistenceService _persistence;
        private CancellationTokenSource? _cloneCts;
```

定位：把第 83–101 行构造器改为：

```csharp
        public ToolLibraryViewModel(
            IApiService apiService,
            IDialogService dialogService,
            IRuntimeEnvironmentService runtimeEnvService,
            IFilePickerService filePickerService,
            IProcessManagerService processManager,
            IGitService gitService,
            IToolPersistenceService persistence)
        {
            Title = "工具库";
            _apiService = apiService;
            _dialogService = dialogService;
            _runtimeEnvService = runtimeEnvService;
            _filePickerService = filePickerService;
            _processManager = processManager;
            _gitService = gitService;
            _persistence = persistence;

            _processManager.ProcessExited += OnProcessExited;

            _ = LoadProjectsAsync();
            _ = DetectEnvironmentsOnStartupAsync();
        }
```

- [ ] **步骤 2：新增可观察属性**

定位：在第 80 行 `private bool _skipRunConfirmation;` 前插入：

```csharp
        // ===== Git 区域（仅新建时显示）=====
        [ObservableProperty] private bool _isGitPanelExpanded = true;
        [ObservableProperty] private string _editGitUrl = string.Empty;
        [ObservableProperty] private string _editCloneDirectory = string.Empty;
        [ObservableProperty] private GitEnvironmentInfo? _gitEnvironment;
        [ObservableProperty] private bool _isDetectingGit;
        [ObservableProperty] private bool _isCloning;
        [ObservableProperty] private string _cloneProgressText = string.Empty;
        [ObservableProperty] private bool _isNewProject;
```

- [ ] **步骤 3：修改 `LoadProjectsAsync` 为读持久化**

定位：第 106–130 行整段替换为：

```csharp
        [RelayCommand]
        private async Task LoadProjectsAsync()
        {
            IsBusy = true;
            try
            {
                var loaded = await _persistence.LoadAsync();
                if (loaded.Count > 0)
                {
                    Projects = new ObservableCollection<ToolProject>(loaded);
                }
                else
                {
                    LoadMockData();
                    // 写入一次，下次启动就有数据
                    _ = _persistence.SaveAsync(Projects);
                }
            }
            catch
            {
                LoadMockData();
            }
            finally
            {
                IsBusy = false;
            }
        }
```

- [ ] **步骤 4：修改 `OpenAddDialog` 与 `OpenEditDialog`**

定位：第 195–218 行 `OpenAddDialog` 末尾追加：

```csharp
            EditGitUrl = string.Empty;
            EditCloneDirectory = string.Empty;
            GitEnvironment = null;
            IsCloning = false;
            CloneProgressText = string.Empty;
            IsNewProject = true;
            _ = RefreshGitEnvironmentAsync();
```

定位：第 220–243 行 `OpenEditDialog` 末尾追加：

```csharp
            IsNewProject = false;
```

- [ ] **步骤 5：修改 `SaveProject` 写入新字段 + 持久化**

定位：在第 357 行 `IsEditing = false;` **之前**插入（在两个分支 `Projects.Add(newProject);` 与 `Projects[index] = ...` 之后各加一次持久化调用）：

完整替换 `SaveProject` 方法（第 299–358 行）为：

```csharp
        [RelayCommand]
        private void SaveProject()
        {
            if (string.IsNullOrWhiteSpace(EditName)) return;

            if (SelectedProject == null)
            {
                var newProject = new ToolProject
                {
                    Id = Projects.Count > 0 ? Projects.Max(p => p.Id) + 1 : 1,
                    Name = EditName,
                    Description = EditDescription,
                    Category = EditCategory,
                    Version = EditVersion,
                    Status = string.IsNullOrEmpty(EditStatus) ? "未运行" : EditStatus,
                    Manager = EditManager,
                    CreateTime = DateTime.Now,
                    RunMode = EditRunMode,
                    Language = EditLanguage,
                    InterpreterPath = EditInterpreterPath,
                    ScriptPath = EditScriptPath,
                    ExecutablePath = EditExecutablePath,
                    WorkingDirectory = string.IsNullOrWhiteSpace(EditWorkingDirectory)
                        ? ResolveDefaultWorkingDirectory()
                        : EditWorkingDirectory,
                    GitUrl = EditGitUrl,
                    CloneDirectory = EditCloneDirectory
                };
                Projects.Add(newProject);
            }
            else
            {
                var index = Projects.IndexOf(SelectedProject);
                if (index >= 0)
                {
                    Projects[index] = new ToolProject
                    {
                        Id = SelectedProject.Id,
                        Name = EditName,
                        Description = EditDescription,
                        Category = EditCategory,
                        Version = EditVersion,
                        Status = string.IsNullOrEmpty(EditStatus) ? "未运行" : EditStatus,
                        Manager = EditManager,
                        CreateTime = SelectedProject.CreateTime,
                        RunMode = EditRunMode,
                        Language = EditLanguage,
                        InterpreterPath = EditInterpreterPath,
                        ScriptPath = EditScriptPath,
                        ExecutablePath = EditExecutablePath,
                        WorkingDirectory = string.IsNullOrWhiteSpace(EditWorkingDirectory)
                            ? ResolveDefaultWorkingDirectory()
                            : EditWorkingDirectory,
                        GitUrl = SelectedProject.GitUrl,
                        CloneDirectory = SelectedProject.CloneDirectory,
                        IsRunning = SelectedProject.IsRunning,
                        ProcessId = SelectedProject.ProcessId
                    };
                }
            }

            _ = _persistence.SaveAsync(Projects);
            IsEditing = false;
        }
```

- [ ] **步骤 6：修改 `DeleteProjectAsync` 加持久化**

定位：把第 382 行 `Projects.Remove(project);` 后追加：

```csharp
            _ = _persistence.SaveAsync(Projects);
```

- [ ] **步骤 7：在文件末尾（第 717 行 `QuoteIfNeeded` 之后）追加新命令**

```csharp
        // =========================================================
        // Git 区域
        // =========================================================

        [RelayCommand]
        private void ToggleGitPanel()
        {
            IsGitPanelExpanded = !IsGitPanelExpanded;
        }

        [RelayCommand]
        private async Task RefreshGitEnvironmentAsync()
        {
            IsDetectingGit = true;
            try
            {
                GitEnvironment = await _gitService.DetectAsync();
            }
            finally
            {
                IsDetectingGit = false;
            }
        }

        [RelayCommand]
        private async Task BrowseCloneDirectoryAsync()
        {
            var picked = await _filePickerService.PickDirectoryAsync();
            if (!string.IsNullOrEmpty(picked))
                EditCloneDirectory = picked;
        }

        [RelayCommand]
        private async Task StartCloneAsync()
        {
            if (string.IsNullOrWhiteSpace(EditGitUrl))
            {
                await _dialogService.ShowMessageAsync("提示", "请先填写 Git URL。");
                return;
            }
            if (string.IsNullOrWhiteSpace(EditCloneDirectory))
            {
                await _dialogService.ShowMessageAsync("提示", "请先选择克隆目标目录。");
                return;
            }
            if (GitEnvironment?.IsInstalled != true)
            {
                await _dialogService.ShowMessageAsync("提示", "未检测到本地 Git 环境，请先安装 Git。");
                return;
            }

            _cloneCts = new CancellationTokenSource();
            _ = RunCloneBackgroundAsync();
        }

        [RelayCommand]
        private void CancelClone()
        {
            _cloneCts?.Cancel();
        }

        private async Task RunCloneBackgroundAsync()
        {
            IsCloning = true;
            CloneProgressText = "⏳ 准备克隆…";
            var progress = new Progress<string>(line =>
            {
                CloneProgressText = line.Length > 120 ? line[..120] + "…" : line;
            });
            try
            {
                var result = await _gitService.CloneAsync(
                    EditGitUrl, EditCloneDirectory, progress, _cloneCts!.Token);

                if (result.Success)
                {
                    EditWorkingDirectory = result.RepoRoot;
                    var picked = await _dialogService.PickScriptFileInDirectoryAsync(result.RepoRoot);
                    if (!string.IsNullOrEmpty(picked)) EditScriptPath = picked;

                    await _dialogService.ShowCloneLogAsync(
                        "克隆成功",
                        $"仓库已克隆到：\n{result.RepoRoot}\n\n可在「启动方式」中选择脚本。",
                        result.Logs,
                        success: true);
                }
                else
                {
                    if (result.Cancelled)
                    {
                        TryDeleteCloneDirectory(EditCloneDirectory);
                    }
                    await _dialogService.ShowCloneLogAsync(
                        result.Cancelled ? "克隆已取消" : "克隆失败",
                        result.Cancelled ? "操作已取消，目标目录已清理。" : result.ErrorMessage,
                        result.Logs,
                        success: false);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowCloneLogAsync("克隆异常", ex.Message, Array.Empty<string>(), success: false);
            }
            finally
            {
                IsCloning = false;
                CloneProgressText = string.Empty;
                _cloneCts?.Dispose();
                _cloneCts = null;
            }
        }

        private static void TryDeleteCloneDirectory(string dir)
        {
            try
            {
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            catch { /* best-effort */ }
        }

        // =========================================================
        // 导入 / 导出
        // =========================================================

        [RelayCommand]
        private async Task ImportToolsAsync()
        {
            var picked = await _filePickerService.PickFileAsync("选择要导入的 JSON", "*.json");
            if (string.IsNullOrEmpty(picked)) return;
            var list = await _persistence.ImportAsync(picked);
            if (list == null)
            {
                await _dialogService.ShowEnvironmentResultAsync("导入失败", "JSON 文件无效或无法解析。", false);
                return;
            }
            Projects = new ObservableCollection<ToolProject>(list);
            _ = _persistence.SaveAsync(Projects);
            await _dialogService.ShowEnvironmentResultAsync("导入成功",
                $"已导入 {list.Count} 个工具。", true);
        }

        [RelayCommand]
        private async Task ExportToolsAsync()
        {
            var picked = await _filePickerService.PickFileAsync("选择导出路径", "*.json");
            if (string.IsNullOrEmpty(picked)) return;
            try
            {
                await _persistence.ExportAsync(picked, Projects);
                await _dialogService.ShowEnvironmentResultAsync("导出成功",
                    $"已导出 {Projects.Count} 个工具到：\n{picked}", true);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowEnvironmentResultAsync("导出失败", ex.Message, false);
            }
        }
```

- [ ] **步骤 8：补齐 SmokeTest 构造器参数**

定位：`SmokeTest/Program.cs` 第 38 行：

```csharp
                args: new object?[] { null, null, null, null, pm }, // IApiService, IDialogService, IRuntimeEnvironmentService, IFilePickerService, IProcessManagerService
```

改为：

```csharp
                args: new object?[] { null, null, null, null, pm, null, null }, // ... IGitService, IToolPersistenceService
```

- [ ] **步骤 9：构建验证**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
dotnet build desktop/ZlinksPackageSystem.Desktop/ZlinksPackageSystem.Desktop.csproj
```

预期：BUILD SUCCESS。

- [ ] **步骤 10：跑 SmokeTest 验证仍通过**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
dotnet run --project desktop/ZlinksPackageSystem.Desktop/SmokeTest/SmokeTest.csproj
```

预期：所有用例 ✅。

- [ ] **步骤 11：Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
git add desktop/ZlinksPackageSystem.Desktop/ViewModels/ToolLibraryViewModel.cs \
        desktop/ZlinksPackageSystem.Desktop/SmokeTest/Program.cs
git commit -m "feat(desktop): ToolLibraryViewModel 集成 GitService + 持久化 + 导入导出"
```

---

## 任务 10：`ToolLibraryView.axaml` UI 改造

**文件：** 修改 `desktop/ZlinksPackageSystem.Desktop/Views/ToolLibraryView.axaml`

- [ ] **步骤 1：顶栏加入导入/导出按钮**

定位：第 60 行 `ColumnDefinitions="Auto,*,Auto,Auto"` 改为：

```xml
            <Grid DockPanel.Dock="Top" Margin="0,0,0,16" ColumnDefinitions="Auto,*,Auto,Auto,Auto,Auto">
```

定位：在第 70–74 行 `＋ 新建工具` 按钮（占 `Grid.Column="3"`）**之后**追加：

```xml
            <Button Grid.Column="4" Content="📥 导入"
                    Command="{Binding ImportToolsCommand}" Width="80"
                    Margin="8,0,0,0" VerticalAlignment="Center" />
            <Button Grid.Column="5" Content="📤 导出"
                    Command="{Binding ExportToolsCommand}" Width="80"
                    Margin="8,0,0,0" VerticalAlignment="Center" />
```

- [ ] **步骤 2：在弹窗第 248 行 `<Separator />` 之前插入 Git 面板**

定位：在第 248 行 `<Separator />` **之前**插入（紧跟在「📂 分类 / 👤 负责人」`<Grid>` 后）：

```xml
                                        <!-- ===== Git 仓库面板（仅新建时显示）===== -->
                                        <StackPanel Spacing="8" IsVisible="{Binding IsNewProject}">
                                            <Grid ColumnDefinitions="*,Auto">
                                                <TextBlock Grid.Column="0" Text="📥 Git 仓库（可选 · 一次性克隆）"
                                                           FontSize="13" FontWeight="SemiBold" Foreground="White"
                                                           VerticalAlignment="Center" />
                                                <Button Grid.Column="1"
                                                        Content="{Binding IsGitPanelExpanded, Converter={StaticResource BoolToExpandGlyphConverter}}"
                                                        Command="{Binding ToggleGitPanelCommand}"
                                                        Width="34" Height="26" Padding="0" />
                                            </Grid>

                                            <StackPanel Spacing="6" IsVisible="{Binding IsGitPanelExpanded}">
                                                <TextBox Text="{Binding EditGitUrl}"
                                                         Watermark="Git URL（HTTPS 或 SSH，留空跳过克隆）" />

                                                <Grid ColumnDefinitions="*,Auto">
                                                    <TextBox Grid.Column="0" Text="{Binding EditCloneDirectory}"
                                                             Watermark="克隆目标父目录（如 D:\tools 或 /home/user/tools）"
                                                             Margin="0,0,6,0" />
                                                    <Button Grid.Column="1" Content="📁 浏览" Width="80"
                                                            Command="{Binding BrowseCloneDirectoryCommand}" />
                                                </Grid>

                                                <StackPanel Orientation="Horizontal" Spacing="8" Margin="0,4,0,0">
                                                    <Ellipse Width="8" Height="8" VerticalAlignment="Center"
                                                             Fill="{Binding GitEnvironment.IsInstalled, Converter={StaticResource BoolToColorConverter}, FallbackValue=#666666}" />
                                                    <TextBlock VerticalAlignment="Center" FontSize="11" Foreground="#FFBFcbd9">
                                                        <Run Text="本地 Git: " />
                                                        <Run Text="{Binding GitEnvironment.Version, FallbackValue='点击右侧刷新检测', TargetNullValue='点击右侧刷新检测'}" />
                                                    </TextBlock>
                                                    <Button Content="🔄 刷新" Padding="6,2" FontSize="10"
                                                            IsEnabled="{Binding IsDetectingGit, Converter={StaticResource BooleanInverseConverter}}"
                                                            Command="{Binding RefreshGitEnvironmentCommand}" />
                                                </StackPanel>

                                                <StackPanel Orientation="Horizontal" Spacing="8">
                                                    <Button Content="📥 克隆" Width="110"
                                                            Background="#FF1976D2" BorderBrush="#FF1976D2" Foreground="White"
                                                            IsVisible="{Binding IsCloning, Converter={StaticResource BooleanInverseConverter}}"
                                                            IsEnabled="{Binding GitEnvironment.IsInstalled}"
                                                            Command="{Binding StartCloneCommand}" />
                                                    <Button Content="⏳ 取消克隆" Width="110"
                                                            IsVisible="{Binding IsCloning}"
                                                            BorderBrush="#FFF56C6C" Foreground="#FFF56C6C"
                                                            Command="{Binding CancelCloneCommand}" />
                                                    <TextBlock VerticalAlignment="Center" FontSize="11" Foreground="#99BFcbd9"
                                                               Text="{Binding CloneProgressText}" />
                                                </StackPanel>
                                            </StackPanel>
                                        </StackPanel>

                                        <Separator />
```

> 注：`<Separator />` 之后是原有的「🚀 启动方式」章节，不动。

- [ ] **步骤 3：构建验证**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
dotnet build desktop/ZlinksPackageSystem.Desktop/ZlinksPackageSystem.Desktop.csproj
```

预期：BUILD SUCCESS（若有 XAML 解析错误，按错误提示调整绑定路径）。

- [ ] **步骤 4：跑 SmokeTest 仍通过**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
dotnet run --project desktop/ZlinksPackageSystem.Desktop/SmokeTest/SmokeTest.csproj
```

预期：所有用例 ✅。

- [ ] **步骤 5：Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
git add desktop/ZlinksPackageSystem.Desktop/Views/ToolLibraryView.axaml
git commit -m "feat(desktop): 顶栏导入/导出按钮 + 弹窗 Git 面板"
```

---

## 任务 11：最终验证与文档收尾

**文件：** 修改 `desktop/ZlinksPackageSystem.Desktop/Docs/git-clone-feature-design.md`（追加“实施记录”段）

- [ ] **步骤 1：完整构建 + 跑 SmokeTest**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
dotnet build desktop/ZlinksPackageSystem.Desktop/ZlinksPackageSystem.Desktop.csproj
dotnet run --project desktop/ZlinksPackageSystem.Desktop/SmokeTest/SmokeTest.csproj
```

预期：BUILD SUCCESS；SmokeTest 输出 `=== 结果：通过 N，失败 0 ===`（N ≥ 30）。

- [ ] **步骤 2：在设计文档末尾追加实施记录**

在 `Docs/git-clone-feature-design.md` 文件末尾追加：

```markdown
---

## 11. 实施记录（实现完成后回填）

- 实施日期：2026-07-16
- 实施者：opencode
- 实现计划：`Docs/git-clone-feature-plan.md`
- SmokeTest 通过用例数：[回填]
- 已知限制：[如有]
```

- [ ] **步骤 3：Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
git add desktop/ZlinksPackageSystem.Desktop/Docs/git-clone-feature-design.md
git commit -m "docs(desktop): Git 克隆功能实施记录"
```

---

## 自检

**1. 规格覆盖度：**
- ✅ ToolProject 字段 → 任务 1
- ✅ GitEnvironmentInfo/CloneResult → 任务 2
- ✅ BoolToExpandGlyphConverter → 任务 3
- ✅ IGitService + URL 解析 → 任务 4 + 任务 5
- ✅ IToolPersistenceService → 任务 6
- ✅ DialogService 扩展 → 任务 7
- ✅ DI 注册 → 任务 8
- ✅ ViewModel 集成 → 任务 9
- ✅ UI 顶栏 + 弹窗面板 → 任务 10
- ✅ 验证与文档 → 任务 11
- ✅ 所有 SmokeTest 用例 → 任务 4 / 6 / 9

**2. 占位符扫描：** 无 "TODO"/"待定"；所有代码块均为完整片段。

**3. 类型一致性：** `IGitService` 定义 `DetectAsync(ct)` / `CloneAsync(url, dir, progress?, ct)`；`GitService` 实现签名一致；`CloneResult.Cancelled` 在 `RunCloneBackgroundAsync` 中作为取消判断字段；`ToolPersistenceService(directoryOverride)` 构造器在测试中传入临时目录。所有方法名跨任务一致。

**4. SmokeTest 兼容性：** 第 9 任务步骤 8 补齐构造器参数，确保 11 个原有用例仍编译通过。