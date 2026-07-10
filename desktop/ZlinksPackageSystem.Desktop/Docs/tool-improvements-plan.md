# 工具库改造计划

> 目标：把工具库从「一次性表单 + 同步执行」升级为「可视化参数编辑 + 启动确认 + 后台进程 + 分类化启动方式」。

---

## 0. 现状速览（基于代码梳理）

| 关注点 | 现状 |
| --- | --- |
| 参数模型 | `ToolArgument`：Name、DefaultValue、RequireInput、InputType、Order |
| 编辑参数 | `ToolLibraryView.axaml` 中的 `DataGrid` + 弹窗内「＋ 添加参数」按钮直接插入空行 |
| 启动流程 | 点击「▶ 运行」→ `RunToolAsync` → 检查环境 → 弹 `PromptArgumentsAsync`（仅 `RequireInput=true` 的参数）→ `ExecuteAsync` 同步 `WaitForExitAsync` → 弹 `ShowOutputAsync` |
| 工具卡片 | 始终显示「▶ 运行 / ✏ 编辑 / 🗑 删除」三按钮，状态来自 `project.Status`（静态字段） |
| 进程管理 | 没有 PID 跟踪，没有 `Process` 句柄，退出后才能看结果 |
| 启动方式 | 仅支持「解释器 + 脚本」一种模式，无「本地可执行程序」分支 |

---

## 1. 数据模型改动

### 1.1 `Models/ToolProject.cs`

新增运行模式与本地可执行程序相关字段：

```csharp
/// <summary>工具运行模式</summary>
public enum ToolRunMode
{
    /// <summary>通过检测到的运行时执行脚本</summary>
    Script,
    /// <summary>直接启动本地可执行程序</summary>
    LocalExecutable
}

public class ToolProject
{
    // ... 原有字段 ...

    // ===== 运行模式 =====
    public ToolRunMode RunMode { get; set; } = ToolRunMode.Script;

    // 本地可执行程序模式专用
    public string ExecutablePath { get; set; } = string.Empty;

    // ===== 参数前缀 =====
    /// <summary>默认参数前缀（如 "--"、"-"、"/"），新参数默认使用</summary>
    public string DefaultArgumentPrefix { get; set; } = "--";

    // ===== 运行时信息（非持久化字段，运行中由 ViewModel 维护）=====
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsRunning { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public int? ProcessId { get; set; }
}
```

> 注：`IsRunning` / `ProcessId` 用 `[JsonIgnore]` 标记，运行期由 `ToolLibraryViewModel` 维护，不入库。

### 1.2 `Models/ToolArgument.cs`

```csharp
public class ToolArgument
{
    public string Name { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;

    // ===== 新增 =====
    /// <summary>true=使用工具级 DefaultArgumentPrefix；false=使用 Prefix 自定义</summary>
    public bool UseDefaultPrefix { get; set; } = true;

    /// <summary>自定义参数前缀（UseDefaultPrefix=false 时生效）</summary>
    public string Prefix { get; set; } = string.Empty;

    public bool RequireInput { get; set; }
    public ToolArgumentInputType InputType { get; set; } = ToolArgumentInputType.Text;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
}
```

### 1.3 `Models/ProcessRunResult.cs`

新增进程跟踪字段：

```csharp
public class ProcessRunResult
{
    // ... 原有字段 ...
    public int? ProcessId { get; set; }  // 启动时填入，关闭按钮按此 PID 杀进程
}
```

---

## 2. 新增服务

### 2.1 `Services/IProcessManagerService.cs` + 实现

负责在后台跑进程、跟踪 PID、对接 Exited 事件。

```csharp
public interface IProcessManagerService
{
    /// <summary>启动一个进程并跟踪，返回 PID</summary>
    int Start(ProcessStartInfo psi);

    /// <summary>按 PID 杀进程（true=找到并已发出 kill）</summary>
    bool Kill(int processId);

    /// <summary>进程是否仍在运行</summary>
    bool IsRunning(int processId);

    /// <summary>进程退出事件，参数：PID、ExitCode</summary>
    event Action<int, int>? ProcessExited;
}
```

实现要点：
- 用 `Dictionary<int, Process>` 维护 PID → `Process` 句柄
- 启动时 `EnableRaisingEvents = true`，订阅 `Exited` 事件，触发 `ProcessExited` 回调后从字典移除
- `Kill(pid)`：先 `CloseMainWindow()`，等待 ~2s，未退出则 `Kill(entireProcessTree: true)`
- 跨平台提示：Windows 上 `taskkill /F /T /PID xxx` 是最稳的"杀进程树"做法；用 `Process.Kill(true)` 也可

### 2.2 `Services/App.axaml.cs` 注册

```csharp
services.AddSingleton<IProcessManagerService, ProcessManagerService>();
```

---

## 3. ViewModel 改动 — `ViewModels/ToolLibraryViewModel.cs`

### 3.1 新增字段

```csharp
// 弹窗内
[ObservableProperty] private ToolRunMode _editRunMode = ToolRunMode.Script;
[ObservableProperty] private string _editExecutablePath = string.Empty;
[ObservableProperty] private string _editDefaultArgumentPrefix = "--";

// 运行模式选项（给下拉框）
public IReadOnlyList<ToolRunMode> RunModes { get; } = new[] { ToolRunMode.Script, ToolRunMode.LocalExecutable };
public IReadOnlyList<string> RunModeDisplayNames { get; } = new[] { "🚀 脚本模式", "📦 本地可执行程序" };
```

### 3.2 重构启动流程

新的 `RunToolAsync` 流程：

```text
RunToolAsync(project)
  ├─ 1. 校验（脚本模式检查 ScriptPath + 解释器；可执行模式检查 ExecutablePath）
  ├─ 2. 准备 EditableArgument 列表（每个参数拷贝一份，前缀 = UseDefault ? DefaultArgumentPrefix : Prefix）
  ├─ 3. 构造"评出的命令" preview
  ├─ 4. _dialogService.ShowRunConfirmationAsync(project, preview, editableArgs)
  │     └─ 用户取消 → return
  │     └─ 用户确认（可编辑参数值）→ 拿到 RunConfirmation { Args, CommandLine }
  ├─ 5. 如果项目已在运行 → 提示"已在运行"，return
  ├─ 6. 通过 IProcessManagerService 启动
  ├─ 7. project.IsRunning = true; project.ProcessId = pid
  ├─ 8. project.Status = "运行中"  // 卡片显示
  ├─ 9. 订阅 ProcessExited → 恢复 IsRunning=false / Status / 刷新按钮
  └─ 10. 弹一个简短的"已启动"提示（不阻塞）
```

新增命令：

```csharp
[RelayCommand]
private void StopTool(ToolProject project)
{
    if (project?.ProcessId is int pid && _procMgr.IsRunning(pid))
    {
        _procMgr.Kill(pid);
        // ProcessExited 回调里会把 IsRunning/Status 还原
    }
}
```

### 3.3 改造 `BuildCommandPreview`

让它支持脚本 + 本地可执行程序两种模式，并按 `UseDefaultPrefix` 决定前缀：

```csharp
public string BuildCommandPreview(ToolProject project, IEnumerable<(ToolArgument arg, string value)>? overrides = null)
{
    // 1) 选 "入口"
    string entry;
    if (project.RunMode == ToolRunMode.LocalExecutable)
        entry = project.ExecutablePath;
    else
        entry = string.IsNullOrWhiteSpace(project.InterpreterPath)
            ? AvailableEnvironments.FirstOrDefault(e => e.Language == project.Language && e.IsAvailable)?.ExecutablePath ?? project.Language
            : project.InterpreterPath;

    // 2) 脚本模式下追加 ScriptPath
    var sb = new StringBuilder();
    sb.Append(QuoteIfNeeded(entry));
    if (project.RunMode == ToolRunMode.Script && !string.IsNullOrEmpty(project.ScriptPath))
        sb.Append(' ').Append(QuoteIfNeeded(project.ScriptPath));

    // 3) 拼参数
    foreach (var arg in project.Arguments.OrderBy(a => a.Order))
    {
        var prefix = arg.UseDefaultPrefix ? project.DefaultArgumentPrefix : arg.Prefix;
        var value = overrides?.FirstOrDefault(o => o.arg == arg).value ?? arg.DefaultValue;

        if (!string.IsNullOrEmpty(prefix)) sb.Append(' ').Append(QuoteIfNeeded(prefix));
        if (!string.IsNullOrEmpty(value))
            sb.Append(' ').Append(arg.InputType == ToolArgumentInputType.Bool
                ? (value == "true" ? "true" : "false")
                : QuoteIfNeeded(value));
    }
    return sb.ToString();
}
```

### 3.4 `ExecuteAsync` 改造

不再 `WaitForExitAsync` 阻塞。改为：

```csharp
private (int pid, Process proc) StartProcess(ToolProject project, string commandLine, Dictionary<string,string> env)
{
    // 选 FileName / WorkingDirectory / ArgumentList（保留原环境变量处理）
    // proc.EnableRaisingEvents = true
    // proc.Start()
    // 返回 (proc.Id, proc)
}
```

然后把 `proc` 交给 `IProcessManagerService` 接管，由它在 `Exited` 时触发回调。

---

## 4. DialogService 改动

### 4.1 `IDialogService.cs` 新增方法

```csharp
/// <summary>用户可编辑参数值的运行确认弹窗；返回 null 表示用户取消</summary>
Task<RunConfirmation?> ShowRunConfirmationAsync(
    ToolProject project,
    string commandLine,
    IEnumerable<EditableArgument> arguments);

/// <summary>参数编辑项</summary>
public class EditableArgument
{
    public ToolArgument Source { get; init; } = null!;
    public string Prefix { get; set; } = string.Empty;  // UseDefaultPrefix 时 = 工具默认
    public string Value { get; set; } = string.Empty;
    public bool UseDefaultPrefix { get; set; } = true;
    public string DisplayName => string.IsNullOrEmpty(Source.Description) ? Source.Name : $"{Source.Name}  ({Source.Description})";
}

public class RunConfirmation
{
    public List<EditableArgument> Arguments { get; init; } = new();
    public string CommandLine { get; init; } = string.Empty;
}
```

### 4.2 `DialogService.cs` 新增实现

弹窗布局（从下到上）：

1. 标题：`▶ 准备启动 - {project.Name}`
2. 完整命令预览框（只读、等宽字体）
3. **可编辑的参数表格**（与编辑弹窗同款）：
   - 列：前缀 / 参数名 / 当前值 / 类型
   - 前缀列：每行一个 `TextBox`，右侧有「使用默认前缀」勾选框（勾上时该 `TextBox` 禁用、显示 `project.DefaultArgumentPrefix`）
   - 值列：根据 `InputType` 渲染 `TextBox` / `NumericUpDown` / `CheckBox` / 路径选择
4. 「✔ 启动」+「✖ 取消」按钮
5. 用户修改任何参数值时，**重新计算**底部命令预览框（实时联动）

实现：监听参数变化，重算 `commandLine`，更新预览框。

---

## 5. View 改动 — `Views/ToolLibraryView.axaml`

### 5.1 弹窗（IsEditing 区域）新增

#### A. 运行模式下拉

放在「🚀 脚本执行配置」标题上方：

```xaml
<ComboBox ItemsSource="{Binding RunModeDisplayNames}"
          SelectedIndex="{Binding EditRunMode, Converter={StaticResource RunModeToIndexConverter}}"
          HorizontalAlignment="Stretch" />
```

#### B. 根据 `EditRunMode` 切换输入区

- **脚本模式**：显示 编程语言下拉、解释器路径、脚本路径（现状）
- **本地可执行程序模式**：隐藏上述三项，显示一个「可执行文件路径」输入框 + 「📁 浏览」按钮

用 `DataTrigger` / `IsVisible="{Binding EditRunMode, Converter=...}"` 切换。

#### C. 参数前缀输入框

紧贴「🎛️ 运行参数」标题右侧，与「＋ 添加参数」按钮同行：

```xaml
<Grid ColumnDefinitions="Auto,*,Auto,Auto">
    <TextBlock Grid.Column="0" Text="🎛️ 运行参数" FontSize="13" ... />
    <TextBlock Grid.Column="1" Text="默认前缀：" Margin="20,0,0,0" />
    <TextBox  Grid.Column="2" Text="{Binding EditDefaultArgumentPrefix}"
              Width="60" ToolTip.Tip="新建参数默认使用此前缀" />
    <Button   Grid.Column="3" Content="＋ 添加参数" Command="{Binding AddArgumentCommand}" .../>
</Grid>
```

#### D. 参数表格列重排

`EditArguments` DataGrid 列改为：

| 列 | 绑定 | 宽度 | 说明 |
| --- | --- | --- | --- |
| 操作 | 🗑 按钮 | 45 | 不变 |
| 使用默认前缀 | `UseDefaultPrefix` CheckBox | 70 | 勾选时禁用「前缀」列 |
| 前缀 | `Prefix` TextBox | 70 | `UseDefaultPrefix=true` 时禁用并显示默认前缀 |
| 参数名 | `Name` TextBox | 2* | 改名（如果 `UseDefaultPrefix` 则弹窗时用默认） |
| 类型 | `InputType` ComboBox | 80 | 不变 |
| 询问 | `RequireInput` CheckBox | 55 | 不变 |
| 默认值 | `DefaultValue` TextBox | 2* | 新增「默认值」列（之前是「默认值」，保留） |
| 顺序 | `Order` TextBox | 50 | 不变 |

`AddArgumentCommand` 改造：新增项的 `Prefix` 留空、`UseDefaultPrefix = true`。

### 5.2 工具卡片按钮区

替换「▶ 运行」按钮为模板选择：

```xaml
<Button Content="▶ 运行" IsVisible="{Binding !IsRunning}" ... />
<Button Content="⏹ 关闭" IsVisible="{Binding IsRunning}"
        BorderBrush="#FFF56C6C" Foreground="#FFF56C6C"
        Command="{Binding DataContext.StopToolCommand, RelativeSource={RelativeSource FindAncestor, AncestorType=ItemsControl}}"
        CommandParameter="{Binding}" />
```

状态徽章：把 `Status` 绑定从静态字段改为动态 — `IsRunning=true` 时强制显示「运行中」并加个绿色脉冲点（可选）。把 `Status` 的 `TextBlock.Foreground` 改成基于 `IsRunning` 切换的转换器。

### 5.3 `ToolLibraryView.axaml.cs`

`Card_PointerPressed` 改为：若 `IsRunning` 则不响应点击（或响应关闭）。

---

## 6. 转换器 / 工具类（Converters 目录）

- `RunModeToIndexConverter`：`ToolRunMode` → `int`（下拉用）
- `BoolToVisibilityConverter` / `EnumEqualsConverter`（可能已有）
- `ProcessIdToBoolConverter`：`int?` 是否有值 → 是否运行中（备用）

---

## 7. 改造点清单（按文件）

| # | 文件 | 改动 |
| - | --- | --- |
| 1 | `Models/ToolProject.cs` | 加 `RunMode`、`ExecutablePath`、`DefaultArgumentPrefix`、运行时字段 |
| 2 | `Models/ToolArgument.cs` | 加 `UseDefaultPrefix`、`Prefix` |
| 3 | `Models/ProcessRunResult.cs` | 加 `ProcessId` |
| 4 | `Services/IProcessManagerService.cs` + 实现 | **新增**，进程生命周期管理 |
| 5 | `Services/IDialogService.cs` | 加 `ShowRunConfirmationAsync` + `EditableArgument` + `RunConfirmation` |
| 6 | `Services/DialogService.cs` | 实现 `ShowRunConfirmationAsync`（可编辑参数 + 实时命令预览） |
| 7 | `ViewModels/ToolLibraryViewModel.cs` | 运行模式、启动确认、停止命令、运行状态跟踪 |
| 8 | `Views/ToolLibraryView.axaml` | 运行模式下拉、参数前缀框、参数表格列、运行/关闭按钮切换 |
| 9 | `Views/ToolLibraryView.axaml.cs` | 点击卡片时根据 `IsRunning` 决定行为 |
| 10 | `Converters/*.cs` | 必要时新增 `RunModeToIndexConverter` |
| 11 | `App.axaml.cs` | DI 注册 `IProcessManagerService` |
| 12 | `Models/Parameter.cs` | **本计划不涉及**，保持原样 |

---

## 8. 验收要点

- [ ] 编辑工具时，可切换「脚本 / 本地可执行程序」模式
- [ ] 「本地可执行程序」模式下隐藏解释器/脚本路径，改为单个可执行文件路径
- [ ] 「默认前缀」输入框紧贴「运行参数」标题
- [ ] 参数表新增列：使用默认前缀(✓)、前缀(可空)、参数名、类型、询问、默认值、顺序
- [ ] 「＋ 添加参数」插入的新行 `UseDefaultPrefix=true`
- [ ] 点击「▶ 运行」弹出确认弹窗，可编辑每个参数值，命令预览实时更新
- [ ] 「✔ 启动」后：卡片状态变「运行中」，按钮变「⏹ 关闭」（红色）
- [ ] 进程退出（正常或被杀）后，状态自动恢复，按钮变回「▶ 运行」
- [ ] 点击「⏹ 关闭」：按 PID 杀进程，进程消失后状态自动还原
- [ ] 多次连点「▶ 运行」不会重复启动（已运行时禁用/拦截）
- [ ] Mock 数据可正常显示「运行中」状态并能关闭

---

## 9. 风险 & 备注

1. **跨平台杀进程**：当前主要在 Windows 上跑，`Process.Kill(entireProcessTree: true)` 已能覆盖；如未来支持 Linux/macOS 再补 `kill -9` 分支。
2. **Avalonia DataGrid 模板列里的双向绑定**：现有代码已用 `Mode=TwoWay`，保持风格即可。
3. **持久化**：`IsRunning` / `ProcessId` 标注 `[JsonIgnore]`，下次启动时状态自然回到「未运行」，符合预期。
4. **进程退出后 UI 刷新**：用 `IProcessManagerService` 的 `ProcessExited` 事件 → `ToolLibraryViewModel` 中转 → 找到对应 `ToolProject` 改 `IsRunning` → Avalonia 绑定自动刷新卡片。事件要在 UI 线程 marshal（用 `Avalonia.Threading.Dispatcher.UIThread.Post`）。
5. **现有 `PromptArgumentsAsync` 行为**：原本「运行前必填表单」的设计已被新流程取代。新流程里**每个参数都展示可编辑**（不再分 `RequireInput`），`RequireInput` 字段保留但本次不再驱动弹窗逻辑；若之后需要可作为校验条件复用。
6. **现有 `ExecuteAsync` 同步等待**：将被新的「非阻塞启动 + 事件回调」取代；同步输出弹窗 (`ShowOutputAsync`) 仍保留，可用于未来「查看上次运行结果」功能。
