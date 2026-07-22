# ZlinksPackageSystem Desktop — 联网检测与离线模式实施计划

> **生成时间:** 2026-07-23
> **对应需求:** `work.txt` 「添加一个检测联网功能」
> **设计基础:** 三端贯通审计报告 (`audit-output/audit-report.md`)
> **实施阶段:** 6 个 Phase,按依赖顺序串行

---

## 一、目标行为总览

| 场景 | 当前行为 | 新行为 |
|---|---|---|
| 启动时**在线** | 显示登录窗口 → 输账密 → 登录 → 进入 Home | **不变** |
| 启动时**离线** | 显示登录窗口(但 API 调用必失败) | **跳过登录,直接进 Home 并显示「离线模式」徽章 + 各视图读缓存** |
| 运行中网络**断** | 各视图 API 调用失败弹错 | 切到「离线模式」徽章,各视图自动读缓存;断网前有缓存则显示缓存,无则「无法连接后端」 |
| 运行中网络**恢复** | 无变化 | 弹对话框「网络已恢复,是否立即登录?」 |
| Home / Game / Product / Parameter / Notification **离线加载** | API 调用失败 → 空数据 + 错误提示 | **优先读本地缓存**;无缓存 → 「无法连接后端」 |

---

## 二、架构改动图

```
┌──────────────────────────────────────────────────────────────────────┐
│  App.axaml.cs (DI 注册新增)                                          │
│   ↓                                                                  │
│  NetworkStatusService ◀──── NetworkChange.NetworkAvailabilityChanged │
│       │                                                              │
│       ├── IsOnline (PropertyChanged)                                 │
│       ├── CheckConnectivityAsync()  ← NetworkInterface + HEAD ping  │
│       └── Start/StopMonitoring                                       │
│                                                                      │
│  LocalCacheService<T> (新,通用)                                      │
│       ├── LoadAsync<T>() → T? | null                                 │
│       ├── SaveAsync<T>(data) → Task                                  │
│       ├── ClearAsync()                                               │
│       └── LastUpdated                                                │
│       ↓                                                              │
│  Per-domain cache files:                                             │
│    home.json · games.json · platforms.json · products.json ·         │
│    parameters.json · notifications.json · tools.json (扩展现有)      │
│                                                                      │
│  MainViewModel (改造)                                                │
│   ├── [ObservableProperty] IsOfflineMode                             │
│   ├── InitializeAsync() → 检测 → 在线?显示LoginView : 直接进Home    │
│   ├── HandleOnlineRecovery() → 弹「是否登录」对话框                  │
│   └── 网络状态变化时刷新 IsOfflineMode                               │
│                                                                      │
│  MainWindow.axaml (改造)                                             │
│   └── 顶部用户信息区右侧加「离线模式」徽章                            │
│                                                                      │
│  各 ViewModel (Home/Game/Product/Parameter/Notification/Tool)        │
│   └── LoadXxxAsync 改造为:                                           │
│         if (IsOnline) { try API → onsuccess 写缓存 → onfail 读缓存 }│
│         else        { 直接读缓存 → 空则提示 }                        │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 三、实施步骤

### Phase 1 — 联网检测基础设施

| # | 任务 | 文件 |
|---|---|---|
| 1.1 | 新建 `INetworkStatusService` 接口 + `NetworkStatusService` 实现 | `Services/INetworkStatusService.cs`、`Services/NetworkStatusService.cs` |
| 1.2 | 实现细节:`IsOnline` 属性 + `PropertyChanged` + `CheckConnectivityAsync()` (NetworkInterface + HEAD ping 到 `ApiBaseUrl/health`,3 秒超时) + `Start/StopMonitoring`(订阅 `NetworkChange.NetworkAvailabilityChanged`,Timer 30 秒兜底 HEAD ping) | 同上 |
| 1.3 | DI 注册 Singleton | `App.axaml.cs` |
| 1.4 | `OnFrameworkInitializationCompleted` 末尾 `StartMonitoring()` | `App.axaml.cs` |

### Phase 2 — 启动流程改造

| # | 任务 | 文件 |
|---|---|---|
| 2.1 | `MainViewModel` 注入 `INetworkStatusService`,新增 `[ObservableProperty] bool IsOfflineMode`、`string ConnectionStatusText` | `ViewModels/MainViewModel.cs` |
| 2.2 | 把 `CurrentViewModel = LoginViewModel` 初始化延后到 `InitializeAsync()`;新增 `InitializeAsync()` 先 await `CheckConnectivityAsync()` | 同上 |
| 2.3 | 离线分支:`IsOfflineMode=true`、`IsLoggedIn=true`(伪登录,只为 UI 不显示登录按钮)、`Username="离线模式"`、`CurrentViewModel = HomeViewModel` | 同上 |
| 2.4 | 订阅 `StatusChanged`:离线→恢复时触发 `HandleOnlineRecoveryAsync()` → `IDialogService.ShowConfirmAsync` → 切换 LoginView | 同上 |
| 2.5 | MainWindow 顶部用户信息区右侧加「● 离线模式」/「● 在线」徽章,绑定 `MainViewModel.IsOfflineMode` | `Views/MainWindow.axaml` |

### Phase 3 — 本地读缓存基础设施

| # | 任务 | 文件 |
|---|---|---|
| 3.1 | 新建通用 `ILocalCacheService` + `LocalCacheService`,泛型 `LoadAsync<T>(key)` / `SaveAsync<T>(key, data)` / `ClearAsync(key)` / `GetLastUpdated(key)`,JSON 文件存到 `LocalApplicationData/ZlinksPackageSystem/cache/{key}.json`,失败抛但被吞掉不阻塞主流程 | `Services/ILocalCacheService.cs`、`Services/LocalCacheService.cs` |
| 3.2 | DI 注册 Singleton | `App.axaml.cs` |
| 3.3 | 缓存键常量定义 | `Constants/CacheKeys.cs` |

### Phase 4 — 各 ViewModel 接入

通用改造模板:

```csharp
[RelayCommand]
private async Task LoadXxxAsync()
{
    if (_networkService.IsOnline)
    {
        try
        {
            var data = await _apiService.GetAsync<XxxDto>("/xxx");
            await _cache.SaveAsync(CacheKeys.Xxx, data);
            ApplyData(data);
        }
        catch
        {
            await LoadFromCacheAsync();
        }
    }
    else
    {
        await LoadFromCacheAsync();
    }
}

private async Task LoadFromCacheAsync()
{
    var cached = await _cache.LoadAsync<XxxDto>(CacheKeys.Xxx);
    if (cached?.Data != null)
    {
        ApplyData(cached.Data);
        EmptyStateMessage = $"离线模式 · 上次更新 {cached.LastUpdated:yyyy-MM-dd HH:mm}";
    }
    else
    {
        EmptyStateMessage = "离线模式 · 无本地数据,请联网后刷新";
    }
}
```

| # | 任务 | 文件 |
|---|---|---|
| 4.1 | HomeViewModel 接入 | `ViewModels/HomeViewModel.cs` |
| 4.2 | GameListViewModel 接入 | `ViewModels/GameListViewModel.cs` |
| 4.3 | ProductViewModel 接入 | `ViewModels/ProductViewModel.cs` |
| 4.4 | ParameterViewModel 接入 | `ViewModels/ParameterViewModel.cs` |
| 4.5 | NotificationViewModel 接入 | `ViewModels/NotificationViewModel.cs` |
| 4.6 | ToolLibraryViewModel 补**读降级** | `ViewModels/ToolLibraryViewModel.cs` |

### Phase 5 — 设置页缓存管理(可选)

| # | 任务 | 文件 |
|---|---|---|
| 5.1 | `SettingsView` 新增「本地缓存」区段:列出各缓存 key 的最后更新时间 + 「清空缓存」按钮 | `ViewModels/SettingsViewModel.cs`、`Views/SettingsView.axaml` |

### Phase 6 — 验证

| # | 命令 / 操作 | 期望结果 |
|---|---|---|
| 6.1 | `dotnet build` | 0 error |
| 6.2 | 关闭后端 → 启动 App | 跳过登录,直接进 Home 显示「离线模式」徽章 |
| 6.3 | Home / Game / Product / Parameter / Notification 各页 | 显示「离线模式 · 上次更新于 …」 |
| 6.4 | 启动后端 → 在离线模式状态下恢复 | 弹「网络已恢复,是否立即登录?」 |
| 6.5 | 启动后端 → 启动 App → 登录 | 正常登录,数据刷新,缓存被覆盖 |
| 6.6 | `cd desktop/.../SmokeTest && dotnet test` | 不挂 |

---

## 四、关键设计决策与权衡

1. **不改 `ApiService` 的错误吞咽逻辑**:`IsOnline` 已能区分意图;改 `ApiService` 影响面太大,Phase 1 的网络服务更轻。
2. **离线模式判定以「计划内的网络状态」为准,不依赖 API 调用结果**:避免「后端慢响应」被误判为离线。
3. **HEAD ping 超时 3 秒**:启动必须感觉流畅;太长会让用户以为应用卡死。在 `appsettings.json` 加 `NetworkCheckTimeoutMs` 可调。
4. **缓存策略采用「写穿」(write-through,在线成功后即落缓存)**:不需要单独刷新逻辑,VM 加载成功时已经写好了。
5. **缓存是全量不是分页**:为简化,每个缓存键存当前完整数据集。GameList/Product 当前在线只显示第一页,缓存也只缓存第一页;后续如果要支持分页缓存再扩。
6. **ToolLibrary 写缓存已存在,不重复造轮子**:Phase 4.6 只补**读降级**,不重写写缓存。

---

## 五、风险与回退点

| 风险 | 缓解 |
|---|---|
| 缓存 schema 与 API 不兼容(版本升级后旧 JSON 反序列化失败) | `LoadAsync` 套 `try/catch JsonException`,失败按"无缓存"处理 |
| 缓存文件无限增长(用户长期使用) | Phase 5 提供清空按钮;后续可加 LRU + 大小上限 |
| 离线模式 UI 与现有错误提示双重显示 | 离线分支不弹错对话框,仅显示 `EmptyStateMessage` |
| `SmokeTest` 的 stub `OfflineStubApiService` 失效 | `INetworkStatusService` 是新接口,smoke 不受影响;但若 stub 改了需回归 |

---

## 六、新增 / 修改文件总览

### 新增文件(5)
- `Services/INetworkStatusService.cs`
- `Services/NetworkStatusService.cs`
- `Services/ILocalCacheService.cs`
- `Services/LocalCacheService.cs`
- `Constants/CacheKeys.cs`

### 修改文件(13)
- `App.axaml.cs` — DI 注册 + 启动 `StartMonitoring`
- `ViewModels/MainViewModel.cs` — 启动流程改造
- `Views/MainWindow.axaml` — 离线模式徽章
- `ViewModels/HomeViewModel.cs` — 缓存接入
- `ViewModels/GameListViewModel.cs` — 缓存接入
- `ViewModels/ProductViewModel.cs` — 缓存接入
- `ViewModels/ParameterViewModel.cs` — 缓存接入
- `ViewModels/NotificationViewModel.cs` — 缓存接入
- `ViewModels/ToolLibraryViewModel.cs` — 读降级
- `ViewModels/SettingsViewModel.cs` — 缓存管理区段
- `Views/SettingsView.axaml` — 缓存管理 UI
- `appsettings.json` — 新增 `NetworkCheckTimeoutMs`
- (可选)`Services/ApiService.cs` — 暴露健康检查端点方法
