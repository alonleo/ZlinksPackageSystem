# 工具库通知选项 - 设计与实现计划

> 目标：在「工具库 → 新建/编辑工具」弹窗底部增加「📢 通知」Tab（含飞书自定义机器人 + 应用机器人、@人配置），并在「⚙ 设置」页增加全局通知默认配置；工具运行时（启动/成功/失败）按配置异步发送富文本卡片到飞书。通知配置采用 Q3-C 混合模式：**全局默认 + 工具级可覆盖**。

---

## 0. 现状速览（基于代码梳理）

| 关注点 | 现状 |
| --- | --- |
| 消息中心 | `ViewModels/NotificationViewModel.cs` 仅维护 mock 通知列表（`Notifications`），无任何发送能力 |
| 飞书相关代码 | 全仓 grep `Feishu|Lark|webhook|Webhook` **零命中** |
| 进程输出捕获 | `ProcessManagerService.Start` 用 `RedirectStandardOutput = false`（`ToolLibraryViewModel.cs:629-630`），stdout/stderr 未被捕获 |
| 工具运行时 hook | `RunToolAsync` 完成后无任何"通知/回调"机制 |
| 持久化方案 | `ToolPersistenceService`（JSON）+ `MainViewModel.LoadSettings()`（JSON），已有复用模板 |
| 设置页 | `SettingsView` 存在（导航命令 `NavigateToSettings`），详情未读但具备扩展入口 |
| 弹窗结构 | `ToolLibraryView.axaml` 第 218–462 行的弹窗是一个 `<ScrollViewer>` 包裹单个 `<StackPanel>`，需改为 `TabControl` |

---

## 1. 数据模型改动（`Models/`）

### 1.1 新增 `FeishuConfig.cs`

```csharp
namespace ZlinksPackageSystem.Desktop.Models
{
    public enum FeishuRobotType { Custom = 0, App = 1 }

    /// <summary>
    /// 飞书机器人渠道配置
    /// </summary>
    public class FeishuConfig
    {
        public FeishuRobotType RobotType { get; set; } = FeishuRobotType.Custom;

        /// <summary>自定义机器人 Webhook URL（含 access_token 查询串）</summary>
        public string WebhookUrl { get; set; } = string.Empty;

        /// <summary>应用机器人 App ID</summary>
        public string AppId { get; set; } = string.Empty;

        /// <summary>应用机器人 App Secret（敏感字段，UI 可一键显隐）</summary>
        public string AppSecret { get; set; } = string.Empty;

        /// <summary>@all</summary>
        public bool AtAll { get; set; }

        /// <summary>@手机号列表</summary>
        public List<string> AtMobiles { get; set; } = new();
    }
}
```

### 1.2 新增 `NotificationConfig.cs`

```csharp
using System.Collections.Generic;

namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 单个工具的通知配置（Q3-C 混合模式）
    /// </summary>
    public class NotificationConfig
    {
        /// <summary>true = 继承全局；false = 用本工具覆盖</summary>
        public bool UseGlobalSettings { get; set; } = true;

        /// <summary>覆盖时启用：启动时通知</summary>
        public bool NotifyOnStart { get; set; }

        /// <summary>覆盖时启用：成功时通知</summary>
        public bool NotifyOnSuccess { get; set; }

        /// <summary>覆盖时启用：失败时通知</summary>
        public bool NotifyOnFailure { get; set; }

        /// <summary>覆盖时启用：脚本输出最大字符数</summary>
        public int MaxOutputChars { get; set; } = 4000;

        /// <summary>本工具专属的渠道列表（UseGlobalSettings=true 时不使用，UI 禁用）</summary>
        public List<FeishuConfig> Channels { get; set; } = new();

        /// <summary>仅运行期：错误日志，不参与持久化</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public List<string> Logs { get; set; } = new();

        /// <summary>仅运行期：汇总状态文本，不参与持久化</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public string LastAggregateStatus { get; set; } = string.Empty;
    }
}
```

### 1.3 新增 `GlobalNotificationConfig.cs`

```csharp
using System.Collections.Generic;

namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 全局通知默认配置（所有工具共享，工具可覆盖）
    /// </summary>
    public class GlobalNotificationConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool NotifyOnStart { get; set; }
        public bool NotifyOnSuccess { get; set; } = true;
        public bool NotifyOnFailure { get; set; } = true;
        public int MaxOutputChars { get; set; } = 4000;
        public List<FeishuConfig> Channels { get; set; } = new();
    }
}
```

### 1.4 新增 `NotificationSendResult.cs`

```csharp
namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 单个渠道发送结果
    /// </summary>
    public class NotificationSendResult
    {
        public string ChannelLabel { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
```

### 1.5 新增 `NotificationTrigger.cs`（枚举）

```csharp
namespace ZlinksPackageSystem.Desktop.Models
{
    public enum NotificationTrigger
    {
        Start,
        Success,
        Failure
    }
}
```

### 1.6 新增 `ToolRunSnapshot.cs`

```csharp
namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 工具运行一次快照，用于卡片渲染
    /// </summary>
    public class ToolRunSnapshot
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationMs => (int)(EndTime - StartTime).TotalMilliseconds;
        public int? ProcessId { get; set; }
        public string WorkingDirectory { get; set; } = string.Empty;
        public string CommandLine { get; set; } = string.Empty;
        public int ExitCode { get; set; }
        public string Output { get; set; } = string.Empty;
        public bool IsSuccess => ExitCode == 0;
        public NotificationTrigger Trigger { get; set; }
    }
}
```

### 1.7 `Models/ToolProject.cs`（修改）

新增持久化字段：
```csharp
public NotificationConfig Notification { get; set; } = new();
```

---

## 2. 服务层（`Services/`）

### 2.1 `IProcessManagerService.cs`（修改）

```csharp
public interface IProcessManagerService
{
    int Start(ProcessStartInfo psi, bool captureOutput = false);
    bool Kill(int processId);
    bool IsRunning(int processId);

    /// <summary>获取已退出进程的捕获输出（captureOutput=true 时有效）。不存在返回空串。</summary>
    string GetOutput(int processId);

    event Action<int, int>? ProcessExited;
    /// <summary>输出捕获事件（每 ~500ms 触发一次，含 PID）。订阅方负责 marshal 到 UI 线程。</summary>
    event Action<int>? OutputCaptured;
}
```

> **关键约束：** 现有 4 处 `Start` 调用方（`ToolLibraryViewModel.BuildProcessStartInfo` 调用 1 处）行为不变；新增 `captureOutput` 参数默认 `false`。`NotificationService` 触发的 `RunToolAsync` 调用改为 `captureOutput: true`。

### 2.2 `ProcessManagerService.cs`（修改）

新增内部数据结构：
```csharp
private readonly ConcurrentDictionary<int, StringBuilder> _outputs = new();
```

修改 `Start(ProcessStartInfo psi, bool captureOutput)`：
- 若 `captureOutput=true`：订阅 `OutputDataReceived` 与 `ErrorDataReceived`，设 `RedirectStandardOutput/Error = true`，`BeginOutputReadLine()` + `BeginErrorReadLine()`，回调里 `Append` 到 `_outputs[pid]`；用 `Timer` 每 500ms 触发 `OutputCaptured?.Invoke(pid)`；进程退出后保留输出 5 分钟清理。
- 若 `captureOutput=false`：行为与当前完全一致（不重定向、不订阅）。

新增 `GetOutput(int pid)`：从 `_outputs` 读 `ToString()`；不存在返回 `""`。

### 2.3 新增 `IGlobalNotificationService.cs`

```csharp
public interface IGlobalNotificationService
{
    string DefaultFilePath { get; }
    Task<GlobalNotificationConfig> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(GlobalNotificationConfig config, CancellationToken ct = default);
}
```

实现 `GlobalNotificationService`（仿 `ToolPersistenceService`，文件 `notification.json`，目录同 `tools.json`）。

### 2.4 新增 `INotificationService.cs`

```csharp
public interface INotificationService
{
    /// <summary>
    /// 按渠道顺序依次发送，全部完成后汇总。失败仅写日志，不抛异常。
    /// </summary>
    Task<List<NotificationSendResult>> SendAsync(
        ToolProject project,
        ToolRunSnapshot snapshot,
        CancellationToken ct = default);
}
```

实现 `NotificationService`：
- 构造函数：注入 `IGlobalNotificationService` + `ILogger`（可选，用 `Debug.WriteLine` 兜底）
- 内部维护 `HttpClient`（5s Timeout）
- `SendAsync` 流程：
  1. 解析 effective config：先看 `project.Notification.UseGlobalSettings`；true 时加载全局后回填三段式 + MaxOutputChars，false 用工具配置；渠道列表：工具非空 → 用工具；否则用全局。
  2. 检查 effective 是否允许该 trigger：`NotifyOn{Start|Success|Failure}` 与 `snapshot.Trigger` 匹配 → 否则返回空 list。
  3. 对每个渠道构造卡片 → POST → 收集 `NotificationSendResult`。
  4. 自定义机器人：`POST WebhookUrl`，body = `{ msg_type: "interactive", card: {...}, at: {...} }`。
  5. 应用机器人：
     - 先 `POST https://open.feishu.cn/open-apis/auth/v3/tenant_access_token/internal`，body = `{ app_id, app_secret }`，拿 `tenant_access_token`（缓存 2 小时）。
     - 再 `POST https://open.feishu.cn/open-apis/im/v1/messages?receive_id_type=chat_id`（`receive_id` 需用户配置；本 MVP 简化为发送到「机器人所在的群」：`receive_id_type=email` 或 `chat_id`，需要额外字段；为简化，本 MVP 仅支持自定义机器人 webhook 路径，App 机器人暂存代码但发送路径用 webhook 兜底）—— **设计简化**：App 机器人仅在「全局设置」或「测试按钮」路径使用，发到任意 chat_id 暂以 TODO 标记，主路径走 Custom。
- **简化决策（消除歧义）：** App 机器人需指定 `receive_id`（chat_id / email / open_id），UI 上新增可选 `ReceiveId` 字段；MVP 不强制，没填时跳过 App 渠道并记录日志。
- 输出截断：`if (snapshot.Output.Length > max) snapshot.Output = snapshot.Output[^max..]`；卡片里加省略号前缀 `...`。

### 2.5 卡片 JSON 结构（`BuildCard` 内部方法）

```json
{
  "msg_type": "interactive",
  "card": {
    "header": {
      "title": { "tag": "plain_text", "content": "▶ 工具启动 - <name>" },
      "template": "blue | green | red"
    },
    "elements": [
      { "tag": "div", "fields": [
        { "is_short": true, "text": { "tag": "lark_md", "content": "**工具**\n<name>" } },
        { "is_short": true, "text": { "tag": "lark_md", "content": "**状态**\n<status>" } },
        { "is_short": true, "text": { "tag": "lark_md", "content": "**耗时**\n<duration>" } },
        { "is_short": true, "text": { "tag": "lark_md", "content": "**PID**\n<pid>" } },
        { "is_short": true, "text": { "tag": "lark_md", "content": "**退出码**\n<exitcode>" } },
        { "is_short": true, "text": { "tag": "lark_md", "content": "**工作目录**\n<wd>" } }
      ]},
      { "tag": "hr" },
      { "tag": "div", "text": { "tag": "lark_md", "content": "**脚本输出（截断）**\n```\n<output>\n```" } },
      { "tag": "hr" },
      { "tag": "note", "elements": [
        { "tag": "plain_text", "content": "<start - end>" }
      ]}
    ]
  },
  "at": { "isAtAll": <bool>, "atMobiles": [...] }
}
```

> 卡片 JSON 构造用 `System.Text.Json` 序列化匿名对象，保证飞书协议兼容性。

---

## 3. ViewModel 变更（`ViewModels/`）

### 3.1 `ToolLibraryViewModel.cs`（大幅扩展）

新增字段：
```csharp
[ObservableProperty] private NotificationConfig _editNotification = new();
[ObservableProperty] private int _selectedTabIndex; // 0=基本 1=启动 2=通知
[ObservableProperty] private bool _isSecretsVisible;
private readonly IGlobalNotificationService _globalNotification;
private readonly INotificationService _notificationService;
```

注入构造器参数（从 7 → 9）：
```csharp
public ToolLibraryViewModel(
    IApiService, IDialogService, IRuntimeEnvironmentService,
    IFilePickerService, IProcessManagerService,
    IGitService, IToolPersistenceService,
    IGlobalNotificationService globalNotification,
    INotificationService notificationService)
```

修改 `OpenAddDialog`：`EditNotification = new()` + `IsSecretsVisible = false` + `SelectedTabIndex = 0`。
修改 `OpenEditDialog`：`EditNotification = Clone(project.Notification)` + `IsSecretsVisible = false` + `SelectedTabIndex = 0`。
修改 `SaveProject`：把 `Notification = EditNotification` 写入 `ToolProject`（新建/更新两分支）。
修改 `RunToolAsync`：三段式触发：
```csharp
var startTime = DateTime.Now;
var pid = _processManager.Start(psi, captureOutput: true);
// 启动时通知（独立 fire-and-forget Task，不 await）
_ = FireNotificationAsync(NotificationTrigger.Start, project, startSnapshot);
// OnProcessExited 现有逻辑不变，但触发成功/失败通知
```

修改 `OnProcessExited`（handler）：增加 `if (snapshot.IsSuccess) Fire(Success) else Fire(Failure)`。

新增命令：
- `AddNotificationChannel()`：`EditNotification.Channels.Add(new FeishuConfig())`
- `RemoveNotificationChannel(FeishuConfig?)`：删除指定
- `TestNotificationAsync(FeishuConfig?)`：用 mock snapshot 调 `_notification.SendAsync`，弹 `ShowCloneLogAsync` 风格的结果窗（成功/失败 + 错误消息）。
- `ToggleSecretsVisibility()`：切 `IsSecretsVisible`

私有方法：
```csharp
private async Task FireNotificationAsync(NotificationTrigger trigger, ToolProject project, ToolRunSnapshot snapshot)
{
    snapshot.Trigger = trigger;
    try { await _notificationService.SendAsync(project, snapshot); }
    catch (Exception ex) { Debug.WriteLine($"[Notification] {ex.Message}"); }
}
```

### 3.2 `SettingsViewModel.cs`（扩展）

- 注入 `IGlobalNotificationService`
- 新增 `[ObservableProperty] private GlobalNotificationConfig _globalNotification = new()`
- 加载时 `_ = LoadGlobalNotificationAsync()`
- 新增命令 `SaveGlobalNotificationAsync()`：调 `_globalNotification.SaveAsync(GlobalNotification)`
- 新增 `AddGlobalChannel / RemoveGlobalChannel / ToggleGlobalSecretsVisibility`（与工具级对称）

> 具体需先读现有 `SettingsViewModel.cs` 后才能精确扩展；本规格只约束接口。

### 3.3 `ViewModelBase` 不变

---

## 4. UI 变更（`Views/`）

### 4.1 `ToolLibraryView.axaml` 弹窗改 TabControl

定位：原弹窗 `<ScrollViewer>` 包裹的 `<StackPanel>` 拆成三个 `<TabItem>`：

```xml
<TabControl SelectedIndex="{Binding SelectedTabIndex}">
    <TabItem Header="📋 基本信息">
        <!-- 原基础信息字段：名称/版本/描述/分类/负责人 -->
    </TabItem>
    <TabItem Header="🚀 启动方式">
        <!-- 原运行模式/脚本/可执行/工作目录/参数列表 -->
        <!-- 注意：Git 仓库面板（IsVisible="{Binding IsNewProject}"）放在此 Tab 顶部 -->
    </TabItem>
    <TabItem Header="📢 通知">
        <!-- 见 4.3 -->
    </TabItem>
</TabControl>
```

按钮区（取消/保存）仍在弹窗底部，不动。

### 4.2 `FeishuChannelEditor.axaml(.cs)`（新增可复用 UserControl）

抽出来供「工具弹窗」和「设置页」共用：

```xml
<UserControl xmlns:... x:Class="ZlinksPackageSystem.Desktop.Views.FeishuChannelEditor">
    <StackPanel Spacing="6">
        <Grid ColumnDefinitions="Auto,*,Auto">
            <TextBlock Grid.Column="0" Text="类型" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <ComboBox Grid.Column="1" SelectedIndex="{Binding RobotType, Converter={StaticResource EnumToIntConverter}}">
                <ComboBoxItem Content="自定义机器人（Webhook）"/>
                <ComboBoxItem Content="应用机器人（App ID + Secret）"/>
            </ComboBox>
            <Button Grid.Column="2" Content="🗑" Command="{Binding RemoveCommand}" .../>
        </Grid>
        <TextBox Text="{Binding WebhookUrl}" Watermark="Webhook URL（含 access_token）"
                 IsVisible="{Binding RobotType, Converter={StaticResource EnumEqualsConverter}, ConverterParameter=Custom}"/>
        <TextBox Text="{Binding AppId}" Watermark="App ID"
                 IsVisible="{Binding RobotType, Converter={StaticResource EnumEqualsConverter}, ConverterParameter=App}"/>
        <TextBox Text="{Binding AppSecret}" Watermark="App Secret"
                 IsVisible="{Binding RobotType, Converter={StaticResource EnumEqualsConverter}, ConverterParameter=App}"
                 PasswordChar="{Binding IsSecretsVisible, Converter={StaticResource BoolToPasswordCharConverter}}"/>
        <CheckBox Content="@所有人" IsChecked="{Binding AtAll}"/>
        <TextBox Text="{Binding AtMobilesText, Mode=TwoWay}" Watermark="@手机号列表（逗号分隔）"/>
    </StackPanel>
</UserControl>
```

`FeishuChannelEditor` 通过 `DataContext` 接收一个 `FeishuConfig` + `RemoveCommand`（由父级传入）。

### 4.3 `ToolLibraryView.axaml` 通知 Tab 内容

```xml
<TabItem Header="📢 通知">
    <StackPanel Spacing="10">
        <!-- 顶部：使用全局设置 勾选 + 显隐切换 -->
        <Grid ColumnDefinitions="*,Auto,Auto">
            <CheckBox Grid.Column="0" Content="使用全局通知设置" IsChecked="{Binding EditNotification.UseGlobalSettings}"/>
            <Button Grid.Column="1" Content="👁" Command="{Binding ToggleSecretsVisibilityCommand}" Width="36"/>
            <Button Grid.Column="2" Content="📤 发送测试消息" Command="{Binding TestNotificationCommand}" Width="140"/>
        </Grid>

        <!-- 三段式触发 -->
        <StackPanel Orientation="Horizontal" Spacing="16">
            <CheckBox Content="▶ 启动时" IsChecked="{Binding EditNotification.NotifyOnStart}"/>
            <CheckBox Content="✅ 成功时" IsChecked="{Binding EditNotification.NotifyOnSuccess}"/>
            <CheckBox Content="❌ 失败时" IsChecked="{Binding EditNotification.NotifyOnFailure}"/>
        </StackPanel>

        <!-- 输出截断 -->
        <Grid ColumnDefinitions="Auto,*">
            <TextBlock Grid.Column="0" Text="脚本输出最大字符数：" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <NumericUpDown Grid.Column="1" Value="{Binding EditNotification.MaxOutputChars}" Minimum="0" Maximum="100000"/>
        </Grid>

        <Separator/>

        <!-- 渠道列表（继承时禁用） -->
        <TextBlock Text="📡 飞书渠道（继承全局时只读）" FontSize="12" Foreground="#99BFcbd9"/>
        <Border IsEnabled="{Binding !EditNotification.UseGlobalSettings}">
            <ItemsControl ItemsSource="{Binding EditNotification.Channels}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <local:FeishuChannelEditor/>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </Border>
        <Button Content="＋ 添加渠道" Command="{Binding AddNotificationChannelCommand}"
                HorizontalAlignment="Left"
                IsEnabled="{Binding !EditNotification.UseGlobalSettings}"/>
    </StackPanel>
</TabItem>
```

> `!EditNotification.UseGlobalSettings` 需新增 `InverseBoolConverter` 或复用现有 `BooleanInverseConverter` 直接绑 `UseGlobalSettings` 配合 `Border.IsEnabled` 的反逻辑（实际上 `IsEnabled="{Binding EditNotification.UseGlobalSettings}"` 是「继承时可编辑」，按 Q3-C 语义不对。修正：渠道区 **继承时禁用** = `IsEnabled="{Binding !EditNotification.UseGlobalSettings}"`，但 Avalonia 11.2 Binding 不直接支持 `!`；新增小转换器或用 `BoolNotConverter`，或改 `IsEnabled="{Binding EditNotification.UseGlobalSettings, Converter={StaticResource BooleanInverseConverter}}"`。

### 4.4 `SettingsView.axaml` 新增「📢 通知」分区

仿工具弹窗通知 Tab，但绑定 `GlobalNotification` 而非 `EditNotification`。包含：IsEnabled 开关 + 三段式 + MaxOutputChars + 渠道列表 + 测试按钮 + 保存按钮（与现有设置页保存逻辑合并）。

### 4.5 新增/复用转换器

- 复用：`EnumToIntConverter`、`EnumEqualsConverter`、`BooleanInverseConverter`
- 新增：`BoolToPasswordCharConverter`（`true→""`、`false→"●"`，用于 AppSecret 显示切换）

---

## 5. 单测扩充（`SmokeTest/Program.cs`）

新增可执行用例（不依赖 GUI、不实际发 HTTP）：

| # | 用例 | 验证 |
|---|---|---|
| 12 | `FeishuConfig` JSON 往返 | 含 `AtMobiles` 列表 |
| 13 | `NotificationConfig` JSON 往返 | 含 `UseGlobalSettings` 与三段式 |
| 14 | `GlobalNotificationConfig` JSON 往返 | |
| 15 | `ToolProject` 含 `Notification` 字段 JSON 往返 | |
| 16 | `ProcessManagerService.Start(psi, captureOutput:true)` 后 `GetOutput(pid)` 能拿到 echo 输出 | |
| 17 | `NotificationService.BuildCard(...)` 返回的 JSON 含 `interactive`、`header`、`elements`、`at` | |
| 18 | `NotificationService.SendAsync` 应用机器人分支：mock `HttpMessageHandler` 验证至少 2 次 HTTP | |
| 19 | `NotificationService.SendAsync` 完全继承：工具 `UseGlobalSettings=true` 无渠道 → 用全局渠道 | |
| 20 | `NotificationService.SendAsync` 完全覆盖：工具自配渠道 → 忽略全局 | |
| 21 | `ToolRunSnapshot.DurationMs` 计算正确 | |

SmokeTest 构造 `ToolLibraryViewModel` 参数从 7 → 9：`new object?[] { null, null, null, null, pm, null, null, null, null }`。

---

## 6. 风险与对策

| 风险 | 对策 |
|---|---|
| 现有 4 处 `ProcessManagerService.Start` 调用方被破坏 | 新参数默认 `false`，行为不变 |
| 飞书 API 鉴权 token 过期 | 应用机器人缓存 `tenant_access_token` 2 小时（飞书官方推荐） |
| 网络异常 | catch 后只写 `Debug.WriteLine`，UI 无提示（避免噪音） |
| 输出过长触发飞书 30KB 限制 | `MaxOutputChars` 默认 4000，截取最后 N 字符 |
| 卡片 JSON 中文编码 | `JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping` |
| AppSecret 明文 | UI 一键显隐；文件权限依赖用户（后续可加 DPAPI） |
| 工具级与全局渠道冲突 | UI 显式标注「继承/覆盖」；解析逻辑在 `NotificationService.SendAsync` 集中处理 |
| 多文件加载顺序 | `App.axaml.cs` 启动后 `GlobalNotificationService` 预加载全局缓存 |

---

## 7. 验收清单

- [ ] 桌面端 `dotnet build` 通过（net10.0）
- [ ] `SmokeTest` 新增 10 个用例全部通过，原有 13 个不回归
- [ ] 新建工具弹窗含三个 Tab（基本/启动/通知），切换正常
- [ ] 编辑工具弹窗同样含三个 Tab（Git 面板在启动 Tab 顶部，**仍只在新建时显示**）
- [ ] 「📢 通知」Tab 默认勾选「使用全局设置」，渠道区禁用
- [ ] 取消勾选后渠道区可编辑，可增删改
- [ ] 「👁」按钮切换 AppSecret 明文/掩码
- [ ] 「📤 发送测试消息」按钮弹结果窗（成功/失败）
- [ ] 设置页含「📢 通知」分区，可保存全局配置
- [ ] 工具运行时三段式触发：启动时 / 成功后 / 失败后 各发一次
- [ ] 卡片含工具名/状态/耗时/PID/退出码/工作目录/输出（前 N 字符）
- [ ] 通知发送完全后台异步，工具运行不阻塞
- [ ] 持久化：`tools.json` 写入 `Notification`；`notification.json` 写入全局配置

---

## 8. 文件清单

| 性质 | 文件 |
|---|---|
| 新 | `Models/FeishuConfig.cs` |
| 新 | `Models/NotificationConfig.cs` |
| 新 | `Models/GlobalNotificationConfig.cs` |
| 新 | `Models/NotificationSendResult.cs` |
| 新 | `Models/NotificationTrigger.cs` |
| 新 | `Models/ToolRunSnapshot.cs` |
| 新 | `Services/IGlobalNotificationService.cs` |
| 新 | `Services/GlobalNotificationService.cs` |
| 新 | `Services/INotificationService.cs` |
| 新 | `Services/NotificationService.cs` |
| 新 | `Converters/BoolToPasswordCharConverter.cs` |
| 新 | `Views/FeishuChannelEditor.axaml(.cs)` |
| 改 | `Models/ToolProject.cs`（+Notification） |
| 改 | `Services/IProcessManagerService.cs`（+captureOutput / +GetOutput / +OutputCaptured） |
| 改 | `Services/ProcessManagerService.cs`（实现） |
| 改 | `App.axaml.cs`（DI 注册 2 个新服务） |
| 改 | `ViewModels/ToolLibraryViewModel.cs`（注入 + Tab + 通知命令 + 三段式触发） |
| 改 | `ViewModels/SettingsViewModel.cs`（+全局通知） |
| 改 | `Views/ToolLibraryView.axaml`（弹窗改 TabControl） |
| 改 | `Views/SettingsView.axaml`（+通知分区） |
| 改 | `App.axaml`（+`BoolToPasswordCharConverter`） |
| 改 | `SmokeTest/Program.cs`（10 个新用例 + 构造器参数补齐） |
| 新 | `Docs/notification-feature-design.md`（本规格） |
| 新 | `Docs/notification-feature-plan.md`（实现计划） |

---

## 9. 验证命令

```bash
dotnet restore desktop/ZlinksPackageSystem.Desktop/ZlinksPackageSystem.Desktop.csproj
dotnet build desktop/ZlinksPackageSystem.Desktop/ZlinksPackageSystem.Desktop.csproj
dotnet run --project desktop/ZlinksPackageSystem.Desktop/SmokeTest/SmokeTest.csproj
```

---

## 10. 实施记录（实现完成后回填）

- 实施日期：[回填]
- 实施者：opencode
- 实现计划：`Docs/notification-feature-plan.md`
- SmokeTest 通过用例数：[回填]
- 已知限制：[如有]