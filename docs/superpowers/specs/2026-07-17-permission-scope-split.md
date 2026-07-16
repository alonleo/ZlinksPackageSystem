# 权限组拆分：后台 / 桌面 规格说明

## 目标

将权限组从单一 `groupPermission.modules` 拆分为两个独立的 scope：`backend`（Web 后台管理系统）与 `desktop`（桌面端）。用于限制不同用户/用户组在两个系统中的模块可见性与操作能力。

## 数据模型

### 新表 permission_scope

| 列 | 类型 | 约束 |
|---|---|---|
| id | BIGINT | PK AUTO |
| group_id | BIGINT | FK → permission_group(id) |
| scope | VARCHAR(16) | 'backend' \| 'desktop' |
| modules_text | TEXT | JSON 数组 |
| create_by / create_time / update_by / update_time / is_deleted | 标准审计 | — |
| UNIQUE(group_id, scope) | | 防止重复 |

### 兼容旧字段

`permission_group.group_permission` 列保留（deprecated），前端不再写入；数据库保留以便历史数据可读。

## 模块白名单

### backend

| value | 中文 |
|---|---|
| home | 首页 |
| system-mgmt | 系统管理 |
| system-settings | 系统设置 |
| package | 打包管理 |
| monitor | 系统监控 |
| all | 全部 |

### desktop

| value | 中文 |
|---|---|
| home | 首页 |
| games | 游戏管理 |
| products | 产品管理 |
| parameters | 参数管理 |
| tests | 测试管理 |
| tool-library | 工具库 |
| notification | 消息中心 |
| settings | 设置 |
| all | 全部 |

## 合并规则（用户属于多组时）

- 取所有 `permission_scope.modules_text` 解析后数组的**并集**
- 任一 group 含 `'all'` → 该 scope 视作 `'all'`
- userId / groupIds 为空时返回空列表（视为无权限）
- JSON 解析失败时忽略该条记录

## API 端点

### 权限组模块范围

```
GET    /api/permission-groups/{groupId}/scopes                  # 列出某 group 全部 scope
GET    /api/permission-groups/{groupId}/scopes/{scope}          # 取单个 scope 的 modules
PUT    /api/permission-groups/{groupId}/scopes/{scope}          # upsert { modules: [...] }
DELETE /api/permission-groups/{groupId}/scopes/{scope}          # 删除单个 scope
```

### 登录端点扩展

- `/api/auth/info` 返回增加 `desktopModules: List<String>`
- `/api/getInfo` 返回增加 `modules: { backend: [...], desktop: [...] }`

## 前端 UI

### Web 后台

- `PermissionListView` 双 panel：后台模块 + 桌面端模块（各自 el-checkbox-group + all 通配）
- `MainLayout.vue` 4 个父菜单通过 `v-if="canShow('xxx')"` 按 `userStore.backendModules` 过滤
- `stores/user.ts` 新增 `backendModules` / `desktopModules` + `resetModules()` 方法

### 桌面端

- `Models/User.cs` 新增 `DesktopModules: List<string>`
- `Services/AuthService.cs` 新增 `FetchDesktopModulesAsync()` 预留方法（try/catch fallback `["all"]`）
- `ViewModels/MainViewModel.cs` 暴露 8 个 `IsXxxVisible` 计算属性 + `CheckModule(key)` 私有方法
- `Views/MainWindow.axaml` 8 个 MenuItem 用 `IsVisible="{Binding IsXxxVisible}"` 绑定

## 默认权限组迁移

| groupName | backend | desktop |
|---|---|---|
| 管理员组 (id=1) | ["all"] | ["all"] |
| 开发组 (id=2) | ["home","package"] | ["home","products"] |
| 测试组 (id=3) | ["home","package"] | ["home","tests"] |
| 运营组 (id=4) | ["home","package"] | ["home","products"] |

## 风险与缓解

| 风险 | 缓解 |
|---|---|
| 旧 groupPermission JSON 残留 | 保留列；前端不再写入；提供迁移文档 |
| 多组合并语义模糊 | 默认并集；`all` 通配优先；空列表表示无权限 |
| 父子菜单不联动 | 4 个父菜单按 value 独立判断 v-if |
| 桌面端 admin 本地登录无模块拉取 | `CheckModule` 在 user/desktopModules 为空时返回 true |
| 旧 auth/info 路径无 permissions | 两路径（legacy + RuoYi）后端都增加 modules 合并 |
| 桌面端 BackendUser.CurrentUser 不存在 | MainViewModel 内部加 `CurrentUser` 属性 |
| 前端 userStore.backendModules 类型缺失 | vue-tsc 通过；如严格类型校验报错，补 UserStore 类型声明 |
| pinia 持久化导致 stale modules | 不把 modules 写入 persist（store 当前未启用该 plugin） |