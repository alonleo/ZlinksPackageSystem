# 通知功能实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 在「工具库 → 新建/编辑工具」弹窗加「📢 通知」Tab（含飞书机器人 + @人配置），并在设置页加全局通知默认配置；工具运行时按配置异步发送富文本卡片到飞书。Q3-C 混合模式：全局默认 + 工具级可覆盖。

**架构：** 新增 6 个数据模型 + 2 个持久化/发送服务（`IGlobalNotificationService / INotificationService`）+ `IProcessManagerService` 扩展（captureOutput + GetOutput）。`ToolLibraryViewModel` 集成通知 Tab 与三段式触发。`FeishuChannelEditor` 抽出 UserControl 供工具弹窗与设置页共用。

**技术栈：** .NET 10 + Avalonia 11.2 + CommunityToolkit.Mvvm + System.Text.Json + `System.Net.Http.HttpClient`。

---

## 任务 1：新增数据模型

**文件：**
- 新建 `Models/FeishuConfig.cs`、`Models/NotificationConfig.cs`、`Models/GlobalNotificationConfig.cs`、`Models/NotificationSendResult.cs`、`Models/NotificationTrigger.cs`、`Models/ToolRunSnapshot.cs`
- 修改 `Models/ToolProject.cs`

- [ ] **步骤 1：写 6 个新模型文件**

按规格文档 §1.1–§1.6 的代码块照搬。`ToolRunSnapshot.Trigger` 字段类型为 `NotificationTrigger`。

- [ ] **步骤 2：在 `ToolProject.cs` 添加 `Notification` 字段**

```csharp
public NotificationConfig Notification { get; set; } = new();
```

- [ ] **步骤 3：构建验证**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
dotnet build desktop/ZlinksPackageSystem.Desktop/ZlinksPackageSystem.Desktop.csproj
```

预期：BUILD SUCCESS。

- [ ] **步骤 4：Commit**

```bash
git add desktop/ZlinksPackageSystem.Desktop/Models/
git commit -m "feat(desktop): 新增 6 个通知数据模型 + ToolProject.Notification 字段"
```

---

## 任务 2：扩展 `IProcessManagerService`（captureOutput + GetOutput）

**文件：** 修改 `Services/IProcessManagerService.cs` 和 `Services/ProcessManagerService.cs`

- [ ] **步骤 1：修改接口**

```csharp
public interface IProcessManagerService
{
    int Start(ProcessStartInfo psi, bool captureOutput = false);
    bool Kill(int processId);
    bool IsRunning(int processId);
    string GetOutput(int processId);
    event Action<int, int>? ProcessExited;
    event Action<int>? OutputCaptured;
}
```

- [ ] **步骤 2：实现扩展 `ProcessManagerService.cs`**

新增字段：
```csharp
private readonly ConcurrentDictionary<int, StringBuilder> _outputs = new();
private System.Timers.Timer? _outputTimer;
```

修改 `Start` 签名加 `bool captureOutput = false`。若 `captureOutput=true`：
- 设 `psi.RedirectStandardOutput = true; psi.RedirectStandardError = true; psi.UseShellExecute = false`（注意：当前实现 `UseShellExecute = false` 但 `Redirect*` 为 false，需要覆盖）
- 订阅 `OutputDataReceived` 与 `ErrorDataReceived`，回调里 `_outputs.AddOrUpdate(pid, ..., (_, sb) => { sb.AppendLine(e.Data); return sb; })`
- `BeginOutputReadLine()` + `BeginErrorReadLine()`

新增 `GetOutput(int pid)`：
```csharp
return _outputs.TryGetValue(pid, out var sb) ? sb.ToString() : string.Empty;
```

新增定时器 `_outputTimer`（5 秒启动一次，遍历 `_outputs.Keys`，触发 `OutputCaptured?.Invoke(pid)`）。

- [ ] **步骤 3：构建验证 + 跑现有 SmokeTest（应不回归）**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
dotnet build desktop/ZlinksPackageSystem.Desktop/ZlinksPackageSystem.Desktop.csproj
dotnet run --project desktop/ZlinksPackageSystem.Desktop/SmokeTest/SmokeTest.csproj
```

预期：BUILD SUCCESS，原 13 个用例仍 ✅。

- [ ] **步骤 4：Commit**

```bash
git add desktop/ZlinksPackageSystem.Desktop/Services/IProcessManagerService.cs \
        desktop/ZlinksPackageSystem.Desktop/Services/ProcessManagerService.cs
git commit -m "feat(desktop): ProcessManagerService 支持 captureOutput + GetOutput + OutputCaptured"
```

---

## 任务 3：`IGlobalNotificationService` + `GlobalNotificationService`（TDD）

**文件：**
- 新建 `Services/IGlobalNotificationService.cs`、`Services/GlobalNotificationService.cs`
- 修改 `SmokeTest/Program.cs`（写新测试）

- [ ] **步骤 1：写失败的 SmokeTest 用例**

定位：在 `SmokeTest/Program.cs` 第 11 个测试块后追加：

```csharp
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
                        Channels = new List<FeishuConfig>
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
```

- [ ] **步骤 2：运行测试确认失败**

预期：编译失败（`GlobalNotificationService` 未定义）。

- [ ] **步骤 3：写 `IGlobalNotificationService.cs`**

```csharp
using System.Threading;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public interface IGlobalNotificationService
    {
        string DefaultFilePath { get; }
        Task<GlobalNotificationConfig> LoadAsync(CancellationToken ct = default);
        Task SaveAsync(GlobalNotificationConfig config, CancellationToken ct = default);
    }
}
```

- [ ] **步骤 4：写 `GlobalNotificationService.cs`**

仿 `ToolPersistenceService` 模板，把 `tools.json` 换成 `notification.json`。

- [ ] **步骤 5：运行测试确认通过**

预期：1 个新用例 ✅。

- [ ] **步骤 6：Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
git add desktop/ZlinksPackageSystem.Desktop/Services/IGlobalNotificationService.cs \
        desktop/ZlinksPackageSystem.Desktop/Services/GlobalNotificationService.cs \
        desktop/ZlinksPackageSystem.Desktop/SmokeTest/Program.cs
git commit -m "feat(desktop): GlobalNotificationService JSON 持久化 + 1 个 SmokeTest"
```

---

## 任务 4：`INotificationService` + `NotificationService`（卡片 + HTTP + Q3-C 解析）

**文件：**
- 新建 `Services/INotificationService.cs`
- 新建 `Services/NotificationService.cs`（构造函数支持注入 `HttpMessageHandler` 便于测试）
- 修改 `SmokeTest/Program.cs`

- [ ] **步骤 1：写失败的 SmokeTest 用例（用 mock HttpMessageHandler）**

新增 4 个用例：
- `NotificationService.BuildCard 含 interactive/header/elements/at`：构造 mock snapshot + FeishuConfig，调 `BuildCard`（需要 `internal` 暴露或 `InternalsVisibleTo`，或把 `BuildCard` 设为 `public static`）。
- `NotificationService.SendAsync 完全继承（UseGlobalSettings=true）`：mock 工具无渠道，注入全局 1 个渠道的 GlobalNotification，验证调用了 1 次 HTTP。
- `NotificationService.SendAsync 完全覆盖（UseGlobalSettings=false）`：mock 工具自配 1 个渠道，验证调用 1 次 HTTP。
- `NotificationService.SendAsync 触发时机过滤`：trigger=Start 但 NotifyOnStart=false，验证 0 次 HTTP。

为便于测试，把 `BuildCard` 设为 `public static`。`NotificationService` 构造器：

```csharp
public NotificationService(IGlobalNotificationService global, HttpMessageHandler? handler = null)
{
    _global = global;
    _http = new HttpClient(handler ?? new HttpClientHandler()) { Timeout = TimeSpan.FromSeconds(5) };
}
```

为让 SmokeTest 不传 `IGlobalNotificationService`（mock），新增 `NullGlobalNotificationService : IGlobalNotificationService` 在 SmokeTest 内定义，返回内存中的预置 `GlobalNotificationConfig`。

- [ ] **步骤 2：运行测试确认失败**

预期：编译失败。

- [ ] **步骤 3：写 `INotificationService.cs` + `NotificationService.cs`**

完整实现按规格 §2.4，提取：
- `public static object BuildCard(ToolProject project, ToolRunSnapshot snapshot, FeishuConfig channel)` 返回匿名对象（卡片 JSON 结构按规格 §2.5）
- 私有 `async Task<(string token, DateTime expiry)> GetAppTokenAsync(FeishuConfig cfg)`（2 小时缓存）
- `public async Task<List<NotificationSendResult>> SendAsync(ToolProject project, ToolRunSnapshot snapshot, CancellationToken ct = default)`：
  1. 解析 effective：`var effective = ResolveEffectiveConfig(project, global)`
  2. 检查 trigger：`if (!ShouldNotify(effective, snapshot.Trigger)) return empty`
  3. 对每个 `channel`：构造 body → POST → 收集结果
  4. 失败写 `Debug.WriteLine`，不抛

- [ ] **步骤 4：运行测试确认通过**

预期：4 个新用例 ✅。

- [ ] **步骤 5：Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
git add desktop/ZlinksPackageSystem.Desktop/Services/INotificationService.cs \
        desktop/ZlinksPackageSystem.Desktop/Services/NotificationService.cs \
        desktop/ZlinksPackageSystem.Desktop/SmokeTest/Program.cs
git commit -m "feat(desktop): NotificationService 卡片构造 + HTTP 发送 + Q3-C 解析 + 4 个 SmokeTest"
```

---

## 任务 5：DI 注册新服务

**文件：** 修改 `App.axaml.cs`

- [ ] **步骤 1：追加 2 个单例注册**

在 `IProcessManagerService` 注册后追加：

```csharp
services.AddSingleton<IGlobalNotificationService, GlobalNotificationService>();
services.AddSingleton<INotificationService, NotificationService>();
```

- [ ] **步骤 2：构建验证 + Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
dotnet build desktop/ZlinksPackageSystem.Desktop/ZlinksPackageSystem.Desktop.csproj
git add desktop/ZlinksPackageSystem.Desktop/App.axaml.cs
git commit -m "feat(desktop): DI 注册 IGlobalNotificationService / INotificationService"
```

---

## 任务 6：`FeishuChannelEditor` UserControl

**文件：** 新建 `Views/FeishuChannelEditor.axaml(.cs)`

- [ ] **步骤 1：写 `.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ZlinksPackageSystem.Desktop.Views
{
    public partial class FeishuChannelEditor : UserControl
    {
        public static readonly RoutedEvent<RoutedEventArgs> RemoveRequestedEvent =
            RoutedEvent.Register<FeishuChannelEditor, RoutedEventArgs>(
                nameof(RemoveRequested), RoutingStrategy.Bubble);

        public event EventHandler<RoutedEventArgs>? RemoveRequested
        {
            add => AddHandler(RemoveRequestedEvent, value);
            remove => RemoveHandler(RemoveRequestedEvent, value);
        }

        public FeishuChannelEditor()
        {
            InitializeComponent();
        }

        private void OnRemoveClicked(object? sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(RemoveRequestedEvent, this));
        }
    }
}
```

- [ ] **步骤 2：写 `.axaml`**

按规格 §4.2 的代码块照搬。`xmlns:converters="clr-namespace:ZlinksPackageSystem.Desktop.Converters"`。

- [ ] **步骤 3：构建验证 + Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
dotnet build desktop/ZlinksPackageSystem.Desktop/ZlinksPackageSystem.Desktop.csproj
git add desktop/ZlinksPackageSystem.Desktop/Views/FeishuChannelEditor.axaml \
        desktop/ZlinksPackageSystem.Desktop/Views/FeishuChannelEditor.axaml.cs
git commit -m "feat(desktop): 新增 FeishuChannelEditor 可复用 UserControl"
```

---

## 任务 7：`SettingsView/VM` 加全局通知分区

**文件：** 修改 `ViewModels/SettingsViewModel.cs` 和 `Views/SettingsView.axaml`

- [ ] **步骤 1：扩展 `SettingsViewModel.cs`**

- 添加 enum 值 `SettingsCategory.Notification`
- 添加 `SettingsCategoryItem { Key=Notification, Icon="📢", Title="通知", ... }` 到 `Categories`
- 注入 `IGlobalNotificationService`（构造器需先读现有签名再加参数；DI 容器会自动注入）
- 加 `GlobalNotification / IsGlobalSecretsVisible` 属性 + 6 个命令（Load/Save/Add/Remove/Toggle/...），按规格 §3.2 实现

> ⚠️ 若 `SettingsViewModel` 构造器目前不带参数或与 DI 不兼容，可改为带 `IGlobalNotificationService` 单参数；DI 容器会自动注入。

- [ ] **步骤 2：扩展 `SettingsView.axaml`**

- 在分类切换的 ContentControl（找到类似 `<ContentControl Content="{Binding ...}"/>`）里加 `Visibility="{Binding SelectedCategory.Key, Converter=..., ConverterParameter=Notification}"` 的通知分区 panel
- 通知分区包含：IsEnabled 开关 + 三段式勾选 + MaxOutputChars + `ItemsControl` 绑 `GlobalNotification.Channels` + `FeishuChannelEditor` + 添加/保存按钮 + 测试按钮
- 测试按钮复用 `INotificationService.SendAsync` 走 mock snapshot

- [ ] **步骤 3：构建验证 + Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
dotnet build desktop/ZlinksPackageSystem.Desktop/ZlinksPackageSystem.Desktop.csproj
git add desktop/ZlinksPackageSystem.Desktop/ViewModels/SettingsViewModel.cs \
        desktop/ZlinksPackageSystem.Desktop/Views/SettingsView.axaml
git commit -m "feat(desktop): SettingsView/VM 新增全局通知分区"
```

---

## 任务 8：`ToolLibraryViewModel` 集成通知

**文件：** 修改 `ViewModels/ToolLibraryViewModel.cs`

- [ ] **步骤 1：注入 2 个新服务（构造器参数 9 → 11，加 `IGlobalNotificationService / INotificationService`）**

```csharp
public ToolLibraryViewModel(
    IApiService apiService,
    IDialogService dialogService,
    IRuntimeEnvironmentService runtimeEnvService,
    IFilePickerService filePickerService,
    IProcessManagerService processManager,
    IGitService gitService,
    IToolPersistenceService persistence,
    IGlobalNotificationService globalNotification,
    INotificationService notificationService)
```

- [ ] **步骤 2：新增属性与命令**

按规格 §3.1 加 `_editNotification / _selectedTabIndex / _isSecretsVisible` + 4 个新命令（`AddNotificationChannel / RemoveNotificationChannel / TestNotificationAsync / ToggleSecretsVisibility`）。

- [ ] **步骤 3：`OpenAddDialog / OpenEditDialog / SaveProject` 接入**

`OpenAddDialog` 末尾追加：
```csharp
EditNotification = new NotificationConfig { UseGlobalSettings = true };
SelectedTabIndex = 0;
IsSecretsVisible = false;
```

`OpenEditDialog` 末尾追加：
```csharp
EditNotification = CloneNotification(project.Notification);
SelectedTabIndex = 0;
IsSecretsVisible = false;
```

`CloneNotification` 用 `System.Text.Json` 序列化-反序列化做深拷贝。

`SaveProject` 把 `Notification = EditNotification` 写入新建/更新分支（仿 GitUrl 模式）。

- [ ] **步骤 4：`RunToolAsync` 三段式触发**

把 `_processManager.Start(psi, captureOutput: true)`。
新增私有方法 `FireNotificationAsync(NotificationTrigger, ToolProject, ToolRunSnapshot)` → `_ = _notification.SendAsync(...)`。
在 `OnProcessExited` handler 里根据 `exitCode` 分发 Success/Failure。

- [ ] **步骤 5：补齐 SmokeTest 构造器参数（9 → 11）**

```csharp
args: new object?[] { null, null, null, null, pm, null, null, null, null },
```

- [ ] **步骤 6：构建验证 + 跑 SmokeTest**

预期：BUILD SUCCESS；原有 18 个用例 + 之前 5 个通知用例 = 仍 ✅。

- [ ] **步骤 7：Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
git add desktop/ZlinksPackageSystem.Desktop/ViewModels/ToolLibraryViewModel.cs \
        desktop/ZlinksPackageSystem.Desktop/SmokeTest/Program.cs
git commit -m "feat(desktop): ToolLibraryViewModel 集成通知 Tab + 三段式触发"
```

---

## 任务 9：`ToolLibraryView.axaml` 弹窗改 TabControl + 通知 Tab

**文件：** 修改 `Views/ToolLibraryView.axaml`

- [ ] **步骤 1：弹窗重构**

定位：弹窗内 `<ScrollViewer>` → `<StackPanel>` 结构替换为：

```xml
<TabControl SelectedIndex="{Binding SelectedTabIndex}">
    <TabItem Header="📋 基本信息">
        <!-- 复制原基础信息字段 -->
    </TabItem>
    <TabItem Header="🚀 启动方式">
        <!-- 顶部：Git 仓库面板（IsVisible="{Binding IsNewProject}"）-->
        <!-- 然后：运行模式/脚本/可执行/工作目录/参数列表 -->
    </TabItem>
    <TabItem Header="📢 通知">
        <!-- 按规格 §4.3 内容 -->
    </TabItem>
</TabControl>
```

- [ ] **步骤 2：通知 Tab 内容**

按规格 §4.3 的代码块照搬。`xmlns:local="clr-namespace:ZlinksPackageSystem.Desktop.Views"` 已存在，复用。

- [ ] **步骤 3：构建验证 + 跑 SmokeTest**

预期：BUILD SUCCESS（若有 XAML 解析错按提示修）。

- [ ] **步骤 4：Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
git add desktop/ZlinksPackageSystem.Desktop/Views/ToolLibraryView.axaml
git commit -m "feat(desktop): 工具弹窗改 TabControl + 通知 Tab + FeishuChannelEditor 集成"
```

---

## 任务 10：最终验证 + 文档收尾

- [ ] **步骤 1：完整构建 + SmokeTest**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
dotnet build desktop/ZlinksPackageSystem.Desktop/ZlinksPackageSystem.Desktop.csproj
dotnet run --project desktop/ZlinksPackageSystem.Desktop/SmokeTest/SmokeTest.csproj
```

预期：BUILD SUCCESS；SmokeTest 通过 ≥ 18（原 13 + 通知 5）。

- [ ] **步骤 2：追加实施记录到设计文档**

编辑 `Docs/notification-feature-design.md` 第 10 节「实施记录」，回填实施日期、SmokeTest 数、已知限制。

- [ ] **步骤 3：Commit**

```bash
cd /home/leo/文档/zl/ZlinksPackageSystem
git add desktop/ZlinksPackageSystem.Desktop/Docs/notification-feature-design.md
git commit -m "docs(desktop): 通知功能实施记录"
```

---

## 自检

**1. 规格覆盖度：**
- ✅ 6 个新模型 → 任务 1
- ✅ ProcessManagerService 扩展 → 任务 2
- ✅ IGlobalNotificationService → 任务 3
- ✅ INotificationService + 卡片 + Q3-C 解析 → 任务 4
- ✅ DI 注册 → 任务 5
- ✅ FeishuChannelEditor → 任务 6
- ✅ SettingsView/VM → 任务 7
- ✅ ToolLibraryViewModel 集成 + 三段式 → 任务 8
- ✅ ToolLibraryView.axaml TabControl → 任务 9
- ✅ 验证与文档 → 任务 10
- ✅ 所有 SmokeTest → 任务 3 / 4 / 8

**2. 占位符扫描：** 无 TODO/待定；所有步骤指向规格文档或具体代码块。

**3. 类型一致性：** `IGlobalNotificationService / INotificationService` 接口签名一致；`FeishuConfig.RobotType / AppSecret / ReceiveId` 在模型/服务/UI 三处一致；`ToolRunSnapshot.Trigger` 字段贯穿服务与 VM。

**4. SmokeTest 兼容：** 任务 8 步骤 5 显式补齐构造器参数到 11 个。