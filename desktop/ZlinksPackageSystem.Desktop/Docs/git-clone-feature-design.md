# 工具库 Git 路径克隆区域 - 设计与实现计划

> 目标：在「工具库 → 新建工具」弹窗中新增 Git 仓库克隆区域，支持输入 Git 路径（HTTP/SSH）、自动检测本地 Git 环境、选目标目录、一键后台克隆（流式进度 + 可取消）、自动把脚本路径与工作目录回填到克隆出来的仓库根目录，并把工具库整体升级为 JSON 持久化 + 导入/导出。

---

## 0. 现状速览（基于代码梳理）

| 关注点 | 现状 |
| --- | --- |
| 新建工具弹窗 | `Views/ToolLibraryView.axaml` 中 `IsVisible="{Binding IsEditing}"`，位于第 216–403 行 |
| 编辑表单字段 | `ViewModels/ToolLibraryViewModel.cs:38–65` —— 名称/描述/版本/分类/负责人/运行模式/解释器/脚本/可执行/工作目录/参数 |
| 进程启动模板 | `Services/RuntimeEnvironmentService.cs:136–205` 已示范 `Process.Start("where"/"which", ...)` + `await p.StandardOutput.ReadToEndAsync()` |
| 进程跟踪 | `Services/ProcessManagerService.cs` 维护 `ConcurrentDictionary<int, Process>`，支持按 PID Kill 与 `ProcessExited` 事件 |
| 文件选择 | `Services/FilePickerService.cs:38–73` —— 目录/文件/脚本文件选择器 |
| 弹窗样式 | `Services/DialogService.cs:73–140` `ShowEnvironmentResultAsync` —— 无装饰栏 + 底部留白 + "✅/❌"，本次复用 |
| 持久化 | **当前完全没有持久化** —— `SaveProject:299–358` 仅写内存 `Projects`，重启即丢；只有 `MainViewModel.cs:37–70` 的 `settings.json` 是持久化范例 |
| 任何 Git 代码 | 桌面端**零命中**；后端 `Game.gitUrl` 仅做字符串存储，无 clone/pull |

---

## 1. 数据模型改动（`Models/`）

### 1.1 `ToolProject.cs`

新增字段（持久化到 JSON，向后兼容：旧文件无此字段时为默认值）：

```csharp
/// <summary>Git 仓库 URL（HTTPS 或 SSH）。仅新建时填写，编辑时不再修改。</summary>
public string GitUrl { get; set; } = string.Empty;

/// <summary>克隆目标父目录（如 D:\tools）。仅新建时填写，编辑时不再修改。</summary>
public string CloneDirectory { get; set; } = string.Empty;

/// <summary>运行期：克隆成功后实际生成的仓库根目录（= CloneDirectory/<repo名>）。不持久化。</summary>
[JsonIgnore]
public string ClonedRepoRoot { get; set; } = string.Empty;
```

> 说明：`IsRunning` / `ProcessId` 已用 `[JsonIgnore]` 标记，本次遵循同样模式。`ClonedRepoRoot` 同理——只在运行期用于回填脚本路径，不入库。

### 1.2 新增 `Models/GitEnvironmentInfo.cs`

```csharp
public class GitEnvironmentInfo
{
    public bool IsInstalled { get; set; }
    public string Version { get; set; } = string.Empty;       // 例如 "git version 2.47.0"
    public string ExecutablePath { get; set; } = string.Empty; // 例如 "/usr/bin/git" 或 "C:\Program Files\Git\bin\git.exe"
}
```

### 1.3 新增 `Models/CloneResult.cs`

```csharp
public class CloneResult
{
    public bool Success { get; set; }
    public string RepoRoot { get; set; } = string.Empty;       // 成功时 = CloneDirectory/<repo名>
    public string RepoName { get; set; } = string.Empty;       // 例如 "my-tool"
    public string ErrorMessage { get; set; } = string.Empty;
    public List<string> Logs { get; set; } = new();
}
```

---

## 2. 新增服务（`Services/`）

### 2.1 `IGitService` + `GitService`

负责检测本地 Git 环境与执行后台克隆（流式进度 + 取消）。仿 `RuntimeEnvironmentService.cs:136–205` 的进程启动模板。

```csharp
public interface IGitService
{
    /// <summary>检测本地 Git：跑 git --version + where/which git 拿绝对路径</summary>
    Task<GitEnvironmentInfo> DetectAsync(CancellationToken ct = default);

    /// <summary>
    /// 在 targetParentDir 下克隆仓库（默认子目录名 = URL 末段去 .git）。
    /// 流式 stderr 通过 IProgress<string> 推送，可通过 ct 取消。
    /// </summary>
    Task<CloneResult> CloneAsync(string url, string targetParentDir,
        IProgress<string>? progress = null, CancellationToken ct = default);
}
```

实现要点：

1. `DetectAsync`：先 `where git` / `which git` 拿路径，再 `git --version` 解析首行（`git version 2.47.0`）。任一步异常则 `IsInstalled = false`。
2. `CloneAsync`：构造 `ProcessStartInfo { FileName = "git", ArgumentList = { "clone", "--progress", url, targetParentDir }, RedirectStandardOutput/Error = true, UseShellExecute = false, CreateNoWindow = true }`。
3. 流式读取 `stderr`（git 的进度走 stderr）→ 每行推到 `IProgress<string>`，并追加到内部 `logs` 列表。
4. `ct.Register(() => { try { proc.Kill(entireProcessTree: true); } catch {} })` 实现取消杀进程。
5. 退出码 0 → 算 `RepoName = url 末段`（如 `https://github.com/x/y.git` → `y`；`git@github.com:x/y.git` → `y`）；`RepoRoot = Path.Combine(targetParentDir, RepoName)`；非 0 → 失败并保留日志。
6. **取消语义**：调用方取消时抛 `OperationCanceledException`；实现层捕获后返回 `Success=false, ErrorMessage="已取消"`（不抛异常给 UI 层）。日志全部保留。

> **关于后台进程跟踪**：Clone 不需要 `IProcessManagerService.Start` 注册（那是给用户工具运行用的）。Clone 走自己的 `Task.Run` + `CancellationTokenSource`，结束后自动释放 `Process`。

### 2.2 `IToolPersistenceService` + `ToolPersistenceService`

把 `Projects` 列表 JSON 化到本地文件，路径仿 `MainViewModel.cs:37–39`：

```csharp
public interface IToolPersistenceService
{
    Task<List<ToolProject>> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(IEnumerable<ToolProject> projects, CancellationToken ct = default);

    /// <summary>导出到用户选择的 JSON 文件</summary>
    Task ExportAsync(string filePath, IEnumerable<ToolProject> projects, CancellationToken ct = default);

    /// <summary>从用户选择的 JSON 文件导入；返回 null 表示失败</summary>
    Task<List<ToolProject>?> ImportAsync(string filePath, CancellationToken ct = default);

    /// <summary>默认文件路径</summary>
    string DefaultFilePath { get; }
}
```

实现要点：

- 文件路径：`Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZlinksPackageSystem", "tools.json")`。
- 用 `System.Text.Json`（项目已在用），开启 `WriteIndented = true` + `DefaultIgnoreCondition = WhenWritingNull`（不写 null 字段）。
- 加载失败/无文件 → 返回空列表（不抛异常）。
- 导出/导入走 `System.IO.File.WriteAllText` / `File.ReadAllText`，调用方先调 `IFilePickerService` 选路径。

---

## 3. ViewModel 变更（`ViewModels/ToolLibraryViewModel.cs`）

### 3.1 注入新服务

```csharp
private readonly IGitService _gitService;
private readonly IToolPersistenceService _persistence;

public ToolLibraryViewModel(
    IApiService apiService,
    IDialogService dialogService,
    IRuntimeEnvironmentService runtimeEnvService,
    IFilePickerService filePickerService,
    IProcessManagerService processManager,
    IGitService gitService,
    IToolPersistenceService persistence)
{
    // ... 现有赋值 ...
    _gitService = gitService;
    _persistence = persistence;
    // 现有逻辑 ...
    _ = LoadProjectsAsync();        // 改为：先从持久化读
    _ = DetectEnvironmentsOnStartupAsync();
}
```

### 3.2 新增可观察属性

```csharp
// ===== Git 区域（仅新建时显示）=====
[ObservableProperty] private bool _isGitPanelExpanded = true;       // 折叠状态
[ObservableProperty] private string _editGitUrl = string.Empty;
[ObservableProperty] private string _editCloneDirectory = string.Empty;
[ObservableProperty] private GitEnvironmentInfo? _gitEnvironment;   // null=未检测
[ObservableProperty] private bool _isDetectingGit;
[ObservableProperty] private bool _isCloning;
[ObservableProperty] private string _cloneProgressText = string.Empty; // 给后台 Toast 显示

// 是否处于「新建」模式（用于决定 Git 面板的可见性，Q8-D）
[ObservableProperty] private bool _isNewProject;
```

### 3.3 新增命令（`[RelayCommand]`）

| 命令 | 作用 |
| --- | --- |
| `RefreshGitEnvironmentAsync()` | 调 `_gitService.DetectAsync()`，更新 `GitEnvironment` 属性；同时在 UI 上切换 `IsDetectingGit` |
| `ToggleGitPanelCommand()` | 切换 `IsGitPanelExpanded`（折叠/展开） |
| `BrowseCloneDirectoryAsync()` | 调 `_filePickerService.PickDirectoryAsync()`，回填 `EditCloneDirectory` |
| `StartCloneAsync()` | 前置校验（Git 已装、URL 非空、目录已选、目录为空/不存在）→ 启动后台任务 `_ = RunCloneBackgroundAsync()` |
| `CancelCloneAsync()` | 触发内部 `_cloneCts?.Cancel()` |
| `ImportToolsAsync()` | 选 JSON 文件 → `_persistence.ImportAsync()` → 替换 `Projects` |
| `ExportToolsAsync()` | 选保存路径 → `_persistence.ExportAsync()` |

后台克隆实现要点（`RunCloneBackgroundAsync`）：

```csharp
private CancellationTokenSource? _cloneCts;
private string _lastCloneTargetDir = string.Empty; // 取消时用于清残骸

private async Task RunCloneBackgroundAsync()
{
    IsCloning = true;
    CloneProgressText = "⏳ 准备克隆…";
    _lastCloneTargetDir = EditCloneDirectory;
    var progress = new Progress<string>(line =>
    {
        CloneProgressText = line.Length > 120 ? line[..120] + "…" : line;
    });
    bool wasCancelled = false;
    try
    {
        var result = await _gitService.CloneAsync(
            EditGitUrl, EditCloneDirectory, progress, _cloneCts!.Token);
        if (result.Success)
        {
            // 回填脚本路径 / 工作目录（Q3-D：自动填 + 二级选脚本弹窗）
            EditWorkingDirectory = result.RepoRoot;

            // 弹"选脚本文件"二级选择器
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
            wasCancelled = result.ErrorMessage.Contains("已取消");
            // 取消时清残骸（Q11-B），失败时保留供排查
            if (wasCancelled) TryDeleteCloneDirectory(_lastCloneTargetDir);

            await _dialogService.ShowCloneLogAsync(
                wasCancelled ? "克隆已取消" : "克隆失败",
                wasCancelled
                    ? "操作已取消，目标目录已清理。"
                    : result.ErrorMessage,
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
```

### 3.4 修改现有方法

| 方法 | 改动 |
| --- | --- |
| `OpenAddDialog` (195–218) | 增加 `IsNewProject = true`；重置 `EditGitUrl / EditCloneDirectory / GitEnvironment / IsCloning = false`；触发 `_ = RefreshGitEnvironmentAsync()` |
| `OpenEditDialog` (220–243) | 增加 `IsNewProject = false`（隐藏 Git 面板，Q8-D） |
| `SaveProject` (299–358) | 写入 `GitUrl` / `CloneDirectory`（仅当 `IsNewProject=true` 时有意义）；其余字段保留 |
| `LoadProjectsAsync` (106–130) | **改为**：先 `_persistence.LoadAsync()`；为空或失败时回退 `LoadMockData()`；**不再**调 `_apiService`（Q1-A 桌面端自治） |
| `DeleteProjectAsync` (376–384) | 删除后调 `_persistence.SaveAsync(Projects)` |
| 构造函数 | 现有 `_ = LoadProjectsAsync()` 改为读持久化 |

### 3.5 持久化触发点

- `SaveProject` 成功后 → `_ = _persistence.SaveAsync(Projects)`
- `DeleteProjectAsync` 完成后 → `_ = _persistence.SaveAsync(Projects)`
- `ImportToolsAsync` 成功后 → `_ = _persistence.SaveAsync(Projects)`
- 失败一律吞掉（持久化是 best-effort，不阻塞用户操作）

---

## 4. UI 变更（`Views/ToolLibraryView.axaml`）

### 4.1 顶部条新增导入/导出按钮（Q5-C）

把第 60 行 `ColumnDefinitions="Auto,*,Auto,Auto"` 改为 `ColumnDefinitions="Auto,*,Auto,Auto,Auto,Auto"`，并在第 70–74 行 `＋ 新建工具` 按钮后追加两个按钮，分别占 `Grid.Column="4"` 与 `Grid.Column="5"`：

```xml
<Button Grid.Column="4" Content="📥 导入"
        Command="{Binding ImportToolsCommand}" Width="80" Margin="8,0,0,0" />
<Button Grid.Column="5" Content="📤 导出"
        Command="{Binding ExportToolsCommand}" Width="80" Margin="8,0,0,0" />
```

### 4.2 新建/编辑弹窗新增 Git 面板（Q8-D）

在弹窗第 248 行 `<Separator />`（基本信息与启动方式之间）**之前**插入一个 `IsVisible="{Binding IsNewProject}"` 的折叠面板：

```xml
<!-- ===== Git 仓库面板（仅新建时显示）===== -->
<StackPanel Spacing="8" IsVisible="{Binding IsNewProject}">
    <Grid ColumnDefinitions="*,Auto">
        <TextBlock Grid.Column="0" Text="📥 Git 仓库（可选 · 一次性克隆）"
                   FontSize="13" FontWeight="SemiBold" Foreground="White"
                   VerticalAlignment="Center" />
        <Button Grid.Column="1" Content="{Binding IsGitPanelExpanded, Converter={StaticResource BoolToExpandGlyphConverter}}"
                Command="{Binding ToggleGitPanelCommand}"
                Width="32" Padding="0" />
    </Grid>

    <StackPanel Spacing="6" IsVisible="{Binding IsGitPanelExpanded}">
        <!-- Git URL -->
        <Grid ColumnDefinitions="*,Auto">
            <TextBox Grid.Column="0" Text="{Binding EditGitUrl}"
                     Watermark="Git URL（HTTPS / SSH，留空跳过克隆）" Margin="0,0,6,0" />
        </Grid>

        <!-- 目标目录 -->
        <Grid ColumnDefinitions="*,Auto">
            <TextBox Grid.Column="0" Text="{Binding EditCloneDirectory}"
                     Watermark="克隆目标父目录（如 D:\tools）" Margin="0,0,6,0" />
            <Button Grid.Column="1" Content="📁 浏览" Width="80"
                    Command="{Binding BrowseCloneDirectoryCommand}" />
        </Grid>

        <!-- Git 环境徽标 -->
        <StackPanel Orientation="Horizontal" Spacing="6">
            <Ellipse Width="8" Height="8" VerticalAlignment="Center"
                     Fill="{Binding GitEnvironment.IsInstalled, Converter={StaticResource BoolToColorConverter}}" />
            <TextBlock VerticalAlignment="Center" FontSize="11" Foreground="#FFBFcbd9">
                <Run Text="本地 Git: " />
                <Run Text="{Binding GitEnvironment.Version, FallbackValue='未检测', TargetNullValue='未检测'}" />
            </TextBlock>
            <Button Content="🔄 刷新" Padding="6,2" FontSize="10"
                    Command="{Binding RefreshGitEnvironmentCommand}" />
        </StackPanel>

        <!-- 操作按钮：互斥显示 -->
        <StackPanel Orientation="Horizontal" Spacing="8">
            <Button Content="📥 克隆" Width="110"
                    Background="#FF1976D2" BorderBrush="#FF1976D2" Foreground="White"
                    IsVisible="{Binding IsCloning, Converter={StaticResource BooleanInverseConverter}}"
                    IsEnabled="{Binding GitEnvironment.IsInstalled}"
                    Command="{Binding StartCloneCommand}" />
            <Button Content="⏳ 正在克隆…" Width="140"
                    IsVisible="{Binding IsCloning}"
                    BorderBrush="#FFF56C6C" Foreground="#FFF56C6C"
                    Command="{Binding CancelCloneCommand}" />
            <TextBlock VerticalAlignment="Center" FontSize="11" Foreground="#99BFcbd9"
                       Text="{Binding CloneProgressText}" />
        </StackPanel>
    </StackPanel>

    <Separator />
</StackPanel>
```

### 4.3 状态栏 Toast（Q9-B / Q10-A）— MVP 简化方案

MVP **不**在主窗口加跨 VM Toast，改为在弹窗内 Git 面板的"⏳ 正在克隆…"按钮旁实时显示进度文本（已在 4.2 中实现），点击按钮直接取消。这样：

- 无需修改 `MainViewModel` 或 `MainWindow.axaml`；
- 进度只在该视图可见时显示，符合"弹窗打开 → 后台跑"的场景；
- 用户若关闭弹窗，`CloneProgressText` 状态仍保留在 VM，下次打开弹窗时恢复显示。

未来如需主窗口级 Toast，可把 `IsCloning / CloneProgressText / CurrentCloneTaskId` 桥接到 `MainViewModel`，但 MVP 不做。

### 4.4 新增转换器

- `BoolToExpandGlyphConverter`：`true → "▲"`、`false → "▼"`
- 复用现有：`BoolToColorConverter` / `BooleanInverseConverter`

---

## 5. DialogService 扩展（`Services/DialogService.cs` / `IDialogService.cs`）

新增两个方法：

```csharp
/// <summary>在指定目录里挑一个脚本文件（.py/.js/.ts/.java/.go/.ps1/.sh/.bat/.cmd）。返回 null=取消。</summary>
Task<string?> PickScriptFileInDirectoryAsync(string directory);

/// <summary>显示克隆日志详情弹窗（含可复制日志文本）。</summary>
Task ShowCloneLogAsync(string title, string message, IReadOnlyList<string> logs, bool success);
```

`PickScriptFileInDirectoryAsync` 实现：调用 `StorageProvider.OpenFilePickerAsync`，`FilePickerFileType` 含常用脚本后缀；初始目录用 `directory`。

`ShowCloneLogAsync` 实现：复用 `ShowEnvironmentResultAsync` 的样式，底部加 "📋 复制日志" 按钮 + 可滚动只读 `TextBox`。

---

## 6. 单测扩充（`SmokeTest/Program.cs`）

新增以下不依赖 GUI、不实际调 git 的可执行用例：

| 用例 | 验证 |
| --- | --- |
| `GitUrl.ParseRepoName()` | HTTPS `https://github.com/x/y.git` → `y`；HTTPS 无 `.git` → `y`；SSH `git@github.com:x/y.git` → `y`；空 / 非法 → 抛 |
| `GitUrl.GetRepoRoot(targetDir)` | `D:\tools` + `y` → `D:\tools\y` |
| `CloneResult` JSON 往返 | 序列化-反序列化字段一致 |
| `ToolPersistenceService.Save→Load` | 10 条 ToolProject 全部字段（含 GitUrl/CloneDirectory）保留 |
| `ToolPersistenceService.Import` 错误文件 | 返回 null 不抛 |
| 新增字段 `GitUrl/CloneDirectory` 默认值 | 空字符串（向后兼容旧 JSON） |
| `QuoteIfNeeded` 边界 | 空字符串 → `""`；含空格 → 加引号并 escape `"` |

SmokeTest 构造 `ToolLibraryViewModel` 时，把第 38 行 `args: new object?[] { null, null, null, null, pm }` 改为 7 个元素：`new object?[] { null, null, null, null, pm, null, null }`（新增的 `IGitService / IToolPersistenceService` 传 null，因为本次新单测不调用它们）。

---

## 7. 风险与对策

| 风险 | 对策 |
| --- | --- |
| 用户未装 Git | 自动检测 + 徽标 + 禁用"克隆"按钮 + "前往 git-scm.com"链接 |
| 大仓库超时 | 后台克隆 + 流式进度 + 可取消（杀进程 + 清理残骸） |
| 路径含空格 | 复用 `ToolLibraryViewModel.cs:711–717` `QuoteIfNeeded`；`ProcessStartInfo.ArgumentList` 已自动处理 |
| SSH 无 key | 失败时错误消息明确提示 "SSH key 未配置" |
| 私有 HTTPS | URL 内嵌 token（用户在 UI 输入），代码里不构造凭据 |
| 取消时残留 | `Directory.Delete(targetDir, recursive: true)`（仅取消时清；失败时保留供排查，Q11-B） |
| 持久化失败 | catch 后吞掉，不阻塞用户操作 |
| 进程退出后 `ProcessExited` 跨 VM 触发 | 已用 `Dispatcher.UIThread.Post` marshal 到 UI 线程 |
| `ShowEnvironmentResultAsync` 不支持日志展开 | 新增 `ShowCloneLogAsync`（5 节）专门展示可滚动日志 + 复制按钮 |
| 持久化与运行期字段冲突 | `IsRunning/ProcessId/ClonedRepoRoot` 全部 `[JsonIgnore]`，JSON 仅持久化业务字段 |
| 后端 `/api/tools` 缺失 | Q1-A：桌面端自治，`LoadProjectsAsync` 不再调 `_apiService.GetAsync` |
| SmokeTest 构造器参数变多 | 第 6 节末尾已明确改为 7 个 null 元素 |
| 取消时残骸目录含其他文件 | `Directory.Delete(targetParentDir, recursive: true)` 会一并删除——这是预期行为（取消即视为本次操作完全撤销），Q11-B 决策 |

---

## 8. 验收清单

- [ ] 桌面端 `dotnet build` 通过（net10.0）
- [ ] `SmokeTest` 所有用例通过
- [ ] 新建工具时弹窗显示 Git 折叠面板；编辑已有工具时**不显示** Git 面板
- [ ] 进入新建弹窗即自动检测 Git（≤2s 完成）；徽标正确显示绿色 ✅ / 红色 ❌
- [ ] 未安装 Git 时"📥 克隆"按钮禁用
- [ ] 输入 HTTPS URL + 选目标目录 → 点"📥 克隆" → 流式显示 git 进度 → 完成后弹"选脚本文件"二级选择器
- [ ] 克隆成功后 `EditScriptPath` 自动填选中脚本，`EditWorkingDirectory` 自动填 `<目标>/<repo名>/`
- [ ] 克隆过程中点"⏳ 正在克隆…"按钮 → 进程被杀，目标目录被清空
- [ ] 失败时（如无效 URL）弹出错误弹窗，含完整 git 日志，"复制日志"按钮可用
- [ ] 保存工具 → 退出应用 → 重启 → 工具列表、GitUrl、CloneDirectory 全部恢复
- [ ] 顶栏"📥 导入"按钮：选 JSON → 列表被替换
- [ ] 顶栏"📤 导出"按钮：选保存路径 → JSON 写入成功
- [ ] 现有 `RunToolAsync / StopToolAsync / BuildCommandPreview` 行为不变

---

## 9. 文件清单

| 性质 | 文件 | 备注 |
| --- | --- | --- |
| 新 | `Models/GitEnvironmentInfo.cs` | DTO |
| 新 | `Models/CloneResult.cs` | DTO |
| 新 | `Services/IGitService.cs` | 接口 |
| 新 | `Services/GitService.cs` | 实现 |
| 新 | `Services/IToolPersistenceService.cs` | 接口 |
| 新 | `Services/ToolPersistenceService.cs` | 实现 |
| 新 | `Converters/BoolToExpandGlyphConverter.cs` | ▲ / ▼ |
| 改 | `Models/ToolProject.cs` | 加 `GitUrl / CloneDirectory / ClonedRepoRoot` |
| 改 | `Services/IDialogService.cs` | 加两个方法 |
| 改 | `Services/DialogService.cs` | 实现两个方法 |
| 改 | `App.axaml.cs` | 注册两个新服务 |
| 改 | `ViewModels/ToolLibraryViewModel.cs` | 注入 + 新字段 + 新命令 + 改 Open*/Save*/Load*/Delete |
| 改 | `Views/ToolLibraryView.axaml` | 顶栏 + 弹窗 Git 面板 |
| 改 | `SmokeTest/Program.cs` | 7 个新用例 |
| 改 | `Docs/git-clone-feature-design.md` | 本文档 |

---

## 10. 验证命令

```bash
# 还原依赖并构建
dotnet restore desktop/ZlinksPackageSystem.Desktop/ZlinksPackageSystem.Desktop.csproj
dotnet build desktop/ZlinksPackageSystem.Desktop/ZlinksPackageSystem.Desktop.csproj

# 冒烟测试（Linux 下 .csproj 默认按 WinExe 构建，冒烟测试子工程不受影响）
dotnet run --project desktop/ZlinksPackageSystem.Desktop/SmokeTest/SmokeTest.csproj
```