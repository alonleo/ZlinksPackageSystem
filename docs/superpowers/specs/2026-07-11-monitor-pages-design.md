# system/monitor 页面设计规格

> 状态:待用户审查
> 日期:2026-07-11
> 范围:子项目 B(从"基础设施升级 / monitor 页面 / 旧文件清理"三选一中选定)

## 1. 目标

为 ZlinksPackageSystem 前端补齐后端已有的 5 个 RuoYi 系统监控接口的前端页面,并把现有 `views/log/LogListView.vue` 迁入统一的 monitor 体系,实现:

- 5 个新页面就绪:`/system/monitor/{server,cache,logininfor,online,operlog}`
- 引入最小化的按钮级权限控制(`v-hasPermi` 指令),为后续基础设施子项目铺路
- 删除与 operlog 重复的旧路径与文件,避免双路由双 API
- 路由表 / 菜单结构与后端 RBAC 权限标识一致

## 2. 范围

### 2.1 范围内

- 5 个前端 view 文件、对应 API 客户端、对应 TS 类型
- `v-hasPermi` 自定义指令(单文件,~30 行)
- `useUserStore` 增加 `permissions` 状态;`User` 接口增加 `permissions: string[]`
- axios 响应拦截器增加 403 / 500 / 网络错误统一提示
- `router/index.ts` 新增 5 条路由,删除 1 条旧路由
- `layouts/MainLayout.vue` 调整侧边栏菜单
- 删除 `views/log/`、`api/operation-log.ts`、`types/operation-log.ts` 三个旧文件

### 2.2 范围外(明确不做)

- 不引入 vitest / @vue/test-utils(项目无测试基础设施)
- 不做动态路由生成(子项目 A 的工作)
- 不做角色权限(`v-hasRole`)指令(后续基础设施阶段补)
- 不做"调度监控 / 定时任务"等 RuoYi 其他 monitor 子模块(后端未实现)
- 不做缓存命令统计图表(`CacheController.getCommandStats` 后端当前返回空列表,无数据可视化价值)
- 不修改任何已有非 monitor 页面(只修 `main.ts` / `router/index.ts` / `MainLayout.vue` / `api/index.ts` / `stores/user.ts` / `types/user.ts` 这 6 个全局文件)

## 3. 架构

### 3.1 路由与菜单

```
侧边栏菜单(hardcode,保持现有风格):
└── 系统监控 (Monitor)
    ├── 服务监控    /system/monitor/server
    ├── 缓存监控    /system/monitor/cache
    ├── 登录日志    /system/monitor/logininfor
    ├── 在线用户    /system/monitor/online
    └── 操作日志    /system/monitor/operlog
```

`MainLayout.vue` 的 `menuItems` 数组把"日志管理"替换为"系统监控"父项,内含 5 个 `el-sub-menu` 子项(若 Element Plus 不支持多级菜单则用扁平 + 路由 push 模拟展开)。

路由配置在 `router/index.ts` 的 `MainLayout` 子路由下新增 5 条记录,`meta` 与现有模式一致(`title` + `requiresAuth` 继承父级)。

### 3.2 目录结构

新增(全部):

```
src/api/monitor/
├── server.ts        GET /monitor/server
├── cache.ts         GET /monitor/cache 与 GET/DELETE /monitor/cache/*
├── logininfor.ts    GET/DELETE /monitor/logininfor/*
├── online.ts        GET/DELETE /monitor/online/*
└── operlog.ts       GET/DELETE /monitor/operlog/*  ← 旧 operation-log.ts 内容迁入

src/api/auth.ts      MOD: getUserInfo 切换到 GET /api/getInfo
src/types/user.ts    MOD: User 接口 + permissions: string[]; 新增 UserInfoResponse { user, roles, permissions }

src/directive/permission/
└── hasPermi.ts

src/types/monitor/
├── server.ts        Cpu / Mem / Jvm / Sys / SysFile
├── cache.ts         CacheInfo
├── logininfor.ts    Logininfor
├── online.ts        UserOnline
└── operlog.ts       OperLog  ← 旧 operation-log.ts 类型迁入

src/views/system/monitor/
├── server/
│   └── index.vue
├── cache/
│   └── index.vue
├── logininfor/
│   └── index.vue
├── online/
│   └── index.vue
└── operlog/
    └── index.vue
```

> **§3.2 注:view 文件命名**
> 沿用参考项目 RuoYi-Vue3-js 的风格 — 每个模块一个子目录,组件文件统一命名为 `index.vue`,路由 `import('@/views/system/monitor/operlog/index.vue')` 不暴露具体组件名。这与现有 `views/log/LogListView.vue` 扁平风格不同,但 monitor 5 个子页需要保持内部一致,且后续若扩展(operlog 详情子页、cache 详情子页等)有目录空间。

### 3.3 删除文件

- `src/api/operation-log.ts`
- `src/views/log/LogListView.vue`
- `src/types/operation-log.ts`
- 删除后若 `src/views/log/` 为空,一并移除该目录

### 3.4 修改文件(6 个)
修改文件(7 个)

| 文件 | 变更摘要 |
|---|---|
| `src/main.ts` | `import hasPermi from '@/directive/permission/hasPermi'` + `app.directive('hasPermi', hasPermi)` |
| `src/stores/user.ts` | 新增 `permissions = ref<string[]>([])` + `roles = ref<string[]>([])`;`fetchUserInfo` 解析 `{user, roles, permissions}` 三段式响应 |
| `src/types/user.ts` | `User` 接口加 `permissions: string[]`(默认 `[]`);新增 `UserInfoResponse { user: User; roles: string[]; permissions: string[] }` |
| `src/api/auth.ts` | `getUserInfo` 切换到 `GET /api/getInfo`,返回 `UserInfoResponse` |
| `src/router/index.ts` | 删 `/operation-logs` 路由;在 `MainLayout` 子路由下加 5 条 monitor 路由 |
| `src/layouts/MainLayout.vue` | 删 `menuItems` 中 `/operation-logs` 一项;新增"系统监控"父级 + 5 个子项 |
| `src/api/index.ts` | 响应拦截器 error 分支增加 403 / 500 / 网络错误提示 |

## 4. 各 View 详细规范

### 4.1 ServerView(`/system/monitor/server`)

**文件**:`src/views/system/monitor/server/index.vue`

**布局**:5 个 `el-card` 横向栅格排列,使用率 > 80% 文本标红。

| 卡片 | span | 内容 | 图表 |
|---|---|---|---|
| CPU | 12 | 核心数 / 用户使用率 / 系统使用率 / 空闲率 | ECharts 半圆仪表盘显示 `cpu.used` |
| 内存 | 12 | 总内存 / 已用 / 剩余 / 使用率(内存+JVM 双列) | ECharts 半环 + `el-progress` |
| 服务器信息 | 24 | 计算机名 / IP / 操作系统 / 系统架构 / 项目路径 / 用户路径 | 无 |
| Java 虚拟机信息 | 24 | Java 名称 / 版本 / 启动时间 / 运行时长 / 安装路径 / 堆配置 | 无 |
| 磁盘状态 | 24 | el-table(盘符 / 文件系统 / 类型 / 总大小 / 可用 / 已用 / 使用率) | 无 |

**数据契约**(对应 `GET /monitor/server` 响应 `data` 字段):

```ts
interface ServerInfo {
  cpu: { cpuNum: number; used: number; sys: number; free: number }   // used/sys/free 是百分比
  mem: { total: number; used: number; free: number; usage: number } // 容量单位 GB
  jvm: { total: number; max: number; used: number; free: number; usage: number;
         name: string; version: string; startTime: number; runTime: number;
         home: string; nonheapTotal: number; nonheapUsed: number }   // 容量单位 MB
  sys: { computerName: string; computerIp: string; osName: string;
         osArch: string; userDir: string; userHome: string; osVersion: string }
  sysFiles: Array<{ dirName: string; sysTypeName: string; typeName: string;
                    total: number; free: number; used: number; usage: number }>  // 容量单位 GB
}
```

**交互**:

- 顶部控制条:`el-button` "刷新" + `el-switch` "自动刷新" + `el-select` 间隔(5/10/30/60s,默认 10s),仅当开关为开时启用定时器
- `onMounted` 立即拉一次
- `onUnmounted` 清 `setInterval` 句柄,避免内存泄漏

### 4.2 CacheView(`/system/monitor/cache`)

**文件**:`src/views/system/monitor/cache/index.vue`

**布局**:`el-row` + `el-col` 左右两栏。

| 区域 | span | 内容 |
|---|---|---|
| 左栏 | 8 | cacheName 列表(el-table 单选高亮)+ 顶部"清空全部"按钮(v-hasPermi `monitor:cache:list`) |
| 右栏 | 16 | cacheKey 表格(随选中 cacheName 联动);选中行后下方详情面板显示 `cacheValue` |

**数据契约**:

```ts
interface CacheInfo {
  info: Record<string, string>   // Redis INFO 命令输出
  dbSize: number                 // 当前 db key 数量
  commandStats: Array<{ name: string; value: string }>   // 后端固定返回空数组,不展示
}
```

**交互**:

- 页面挂载时 `GET /monitor/cache` 拉一次,展示 `info` / `dbSize`
- 左栏 cacheName 列表由 `GET /monitor/cache/getNames` 拉取
- 点击 cacheName → `GET /monitor/cache/getKeys/{cacheName}` 填充右栏 cacheKey
- 点击 cacheKey → `GET /monitor/cache/getValue/{cacheName}/{cacheKey}` 拉详情,展示在右栏下方
- "清空全部" → `DELETE /monitor/cache/clearCacheAll`(ElMessageBox 二次确认)
- "清空该 cacheName" → `DELETE /monitor/cache/clearCacheName/{cacheName}`(ElMessageBox 二次确认)
- "删除该 key" → `DELETE /monitor/cache/clearCacheKey/{cacheKey}`(ElMessageBox 二次确认)
- 所有 v-hasPermi 权限标识统一为 `monitor:cache:list`

### 4.3 LogininforView(`/system/monitor/logininfor`)

**文件**:`src/views/system/monitor/logininfor/index.vue`

**布局**:标准 RuoYi 列表页(顶部搜索 + 工具栏 + el-table + 分页),沿用 `LogListView` 现有样式。

**数据契约**(对应 `SysLogininfor` 实体):

```ts
interface Logininfor {
  infoId: number
  userName: string
  ipaddr: string
  loginLocation: string
  browser: string
  os: string
  status: string   // "0" 成功 / "1" 失败
  msg: string
  loginTime: string  // yyyy-MM-dd HH:mm:ss
}
```

**搜索字段**:用户名称、登录地址(IP)、登录状态(下拉:`"0"` 成功 / `"1"` 失败)
**工具栏按钮**:

- 删除(v-hasPermi `monitor:logininfor:remove`):支持多选,`DELETE /monitor/logininfor/{infoIds}`(路径参数用逗号分隔)
- 清空(v-hasPermi `monitor:logininfor:remove`):`DELETE /monitor/logininfor/clean` + ElMessageBox 二次确认
- 解锁(v-hasPermi `monitor:logininfor:unlock`):单选时启用,`PUT /monitor/logininfor/unlock/{userName}`(后端占位实现,前端正常发请求)

**列**:用户 / IP / 登录地点 / 浏览器 / OS / 状态(el-tag) / 消息 / 登录时间
状态 tag:`status === "0"` → success,`status === "1"` → danger。

**分页**:沿用现有 `LogListView` 写法(`current` / `size` / `total`),`page-sizes = [10, 20, 50, 100]`。

### 4.4 OnlineView(`/system/monitor/online`)

**文件**:`src/views/system/monitor/online/index.vue`

**布局**:同 LogininforView。

**数据契约**(对应 `SysUserOnline` 实体):

```ts
interface UserOnline {
  tokenId: string
  userName: string
  ipaddr: string
  loginLocation: string
  browser: string
  os: string
  roleKey: string
  loginTime: string
}
```

**搜索字段**:登录地址(IP)、用户名称
**工具栏**:无
**列**:会话ID(`tokenId` 截断显示前 16 位 + 鼠标悬停看全文) / 用户 / IP / 登录地点 / 浏览器 / OS / 角色 / 登录时间 / 操作
**操作列**:强退按钮(v-hasPermi `monitor:online:forceLogout`)→ ElMessageBox 二次确认 → `DELETE /monitor/online/{tokenId}` → 重新拉列表
**分页**:同 LogininforView。

### 4.5 OperlogView(`/system/monitor/operlog`)

**文件**:`src/views/system/monitor/operlog/index.vue`

**布局**:同 LogininforView,带"详情"对话框。

**数据契约**(对应 `SysOperLog` 实体):

```ts
interface OperLog {
  operId: number
  title: string          // 模块名,如 "用户管理"
  businessType: number   // 业务类型,见下方枚举
  method: string         // 方法名,如 com.zlinks...UserController.add
  requestMethod: string  // HTTP 方法 GET/POST/PUT/DELETE
  operatorType: number   // 操作员类型
  operName: string       // 操作人员
  deptName: string
  operUrl: string
  operIp: string
  operParam: string      // 请求参数(JSON)
  jsonResult: string     // 返回结果(JSON)
  status: number         // 0 正常 / 1 异常
  errorMsg: string
  operTime: string       // yyyy-MM-dd HH:mm:ss
}
```

**businessType 枚举映射**(沿用后端 `BusinessType`):

| 值 | 标签 | tag 类型 |
|---|---|---|
| 0 | 其它 | info |
| 1 | 新增 | primary |
| 2 | 修改 | warning |
| 3 | 删除 | danger |
| 4 | 授权 | success |
| 5 | 导出 | primary |
| 6 | 导入 | primary |
| 7 | 强退 | danger |
| 8 | 生成代码 | primary |
| 9 | 清空数据 | danger |

**搜索字段**:系统模块(`title`)、操作人员(`operName`)、业务类型(`businessType` 下拉)、状态(`status` 下拉)
**工具栏**:

- 删除(v-hasPermi `monitor:operlog:remove`):多选,`DELETE /monitor/operlog/{operIds}`
- 清空(v-hasPermi `monitor:operlog:remove`):`DELETE /monitor/operlog/clean`
- 导出(无权限控制,沿用旧 `LogListView` 行为):下拉 `xlsx` / `json`,`GET /monitor/operlog/export?format=...`(**风险项**:`SysOperlogController` 当前未实现此接口,调用会 404,全局拦截器会提示"服务异常";按钮仍按 RuoYi 标准保留,等后端补 `export` 接口即可生效)

**列**:操作模块(`title`) / 业务类型(tag) / 请求方式(tag) / 操作人员(`operName`) / 部门(`deptName`) / 操作地址(`operUrl`,show-overflow-tooltip) / 操作IP / 状态(tag) / 操作时间 / 操作列(详情按钮)
**详情对话框**:宽度 800px,展示所有字段;`jsonResult` 与 `operParam` 折叠区(默认收起,可展开查看原始 JSON)。

### 4.6 (已并入 §3.2)

view 文件命名澄清已在 §3.2 注中给出:每个模块一个子目录,组件文件 `index.vue`,5 个 view 文件分别位于 `src/views/system/monitor/{server,cache,logininfor,online,operlog}/index.vue`。

## 5. 共享基础设施

### 5.1 v-hasPermi 指令

**文件**:`src/directive/permission/hasPermi.ts`

**注册**:`src/main.ts` 中 `app.directive('hasPermi', hasPermi)`

**实现**:

```ts
import type { Directive, DirectiveBinding } from 'vue'
import { useUserStore } from '@/stores/user'

const SUPER_PERMISSION = '*:*:*'

export default {
  mounted(el: HTMLElement, binding: DirectiveBinding<string | string[]>) {
    const flags = Array.isArray(binding.value) ? binding.value : [binding.value]
    const permissions = useUserStore().permissions
    const hasPermission = flags.some(
      (f) => permissions.includes(SUPER_PERMISSION) || permissions.includes(f),
    )
    if (!hasPermission) {
      el.parentNode?.removeChild(el)
    }
  },
} as Directive<HTMLElement, string | string[]>
```

**使用示例**:

```vue
<el-button v-hasPermi="['monitor:logininfor:remove']" @click="handleDelete">删除</el-button>
<el-button v-hasPermi="['monitor:online:forceLogout']" @click="handleForceLogout">强退</el-button>
```

### 5.2 user store 改造

**`src/types/user.ts`**:

```ts
export interface User {
  // ... 现有字段
  permissions: string[]   // 新增
}
```

**`src/stores/user.ts`** 新增:

```ts
const permissions = ref<string[]>([])
const roles = ref<string[]>([])

async function fetchUserInfo() {
  try {
    // 调 RuoYi 标准 /api/getInfo,响应是 { user, roles, permissions }
    const response = await authApi.getUserInfo()
    user.value = response.data.user
    roles.value = response.data.roles ?? []
    permissions.value = response.data.permissions ?? []
  } catch (error) {
    logout()
  }
}

return {
  token, user, permissions, roles,
  isAuthenticated, currentUser,
  login, fetchUserInfo, logout, setToken,
}
```

**配套修改 `src/api/auth.ts`**:

```ts
export interface UserInfoResponse {
  code: number
  message: string
  data: {
    user: User
    roles: string[]
    permissions: string[]
  }
}

export const authApi = {
  // ... 其他不变
  getUserInfo(): Promise<UserInfoResponse> {
    return api.get('/getInfo')   // RuoYi 标准端点,旧 /auth/info 不返回 permissions
  },
}
```

**配套修改 `src/types/user.ts`**:

```ts
export interface User {
  // ... 现有字段
  permissions?: string[]   // 仅在 UserInfoResponse.data.user 中可能出现,业务代码不依赖
}
```

### 5.3 axios 错误拦截

**`src/api/index.ts`** 响应拦截器 `error` 分支补丁:

```ts
import { ElMessage } from 'element-plus'

// ... 在 error 分支内,现有 401 处理之后:
if (status === 401) {
  // 现有逻辑:登出 + 跳登录
} else if (status === 403) {
  ElMessage.error('您没有该操作权限')
} else if (status >= 500) {
  ElMessage.error(error.response?.data?.msg ?? '服务异常')
} else if (!error.response) {
  ElMessage.error('网络异常,请稍后重试')
}
```

## 6. 风险与开放项

| # | 项 | 风险 | 应对 |
|---|---|---|---|
| 1 | 前端 `/auth/info` 不返回 `permissions` | `v-hasPermi` 失效 | **已解决**:把 `authApi.getUserInfo` 切换到 RuoYi 标准的 `GET /api/getInfo`,从响应拆出 `{user, roles, permissions}` |
| 2 | `SysOperlogController` 无 `export` 接口 | OperlogView "导出"按钮点击会 404 | 按钮保留(沿用 RuoYi 交互),调用失败时 axios 拦截器统一提示"服务异常";后端后续补 `export` 接口即生效 |
| 3 | `CacheController.getCommandStats` 返回空数组 | 即便做图也无数据 | §4.2 明确不展示命令统计图 |
| 4 | 后端 `OperLog` 实体的 `operTime` 字段格式化已通过 `@JsonFormat` 处理 | 无风险,直接用字符串 | 无需处理 |
| 5 | ECharts 半环图表在容器尺寸变化时的 resize | 默认无响应式 | `onMounted` 后 `chartInstance.value?.resize()` 监听 `window.resize`,`onUnmounted` 调 `dispose()` |
| 6 | 多标签页缓存:用户切走后再切回,server 监控的轮询状态丢失 | 不影响功能,但需要刷新才会重新拉数据 | 不在本期处理,后续可加 `<keep-alive>` 与轮询状态恢复 |
| 7 | Element Plus `el-sub-menu` 在 2.6.x 是否稳定 | 与现有单层菜单兼容性 | 实现时若不稳定,改用扁平菜单 + 路由 push 模拟展开 |

## 7. 测试与验证

不写单元测试。每个 view 完成后:

1. `npm run build` — 验证 TypeScript 类型与 Vite 构建通过
2. `npm run dev` — 手动验证 UI,清单:
   - [ ] 页面正常渲染,无控制台报错
   - [ ] 表格列对齐,搜索/分页/重置正常工作
   - [ ] 权限按钮在无权时隐藏,有权时显示
   - [ ] 删除/清空/强退操作的二次确认弹窗正确
   - [ ] server 监控自动刷新开关与间隔生效
   - [ ] cache 监控的三级联动(cacheName → cacheKey → cacheValue)正常
   - [ ] operlog 详情对话框字段完整
3. 后端联调:启动后端,逐页验证接口调用与错误提示(403 / 500 / 网络)

## 8. 增量交付(commit 顺序)

按以下顺序,每步一个 commit,中文 conventional commit 格式(`feat(monitor): xxx`)。

1. `feat(auth): 切换 getUserInfo 到 /api/getInfo 拿 permissions/roles`
2. `feat(monitor): 基础设施 — v-hasPermi 指令 + axios 错误拦截`
3. `feat(monitor): 服务监控页面 + ECharts 半环仪表盘`
4. `feat(monitor): 缓存监控页面(左右两栏)`
5. `feat(monitor): 登录日志页面`
6. `feat(monitor): 在线用户页面`
7. `feat(monitor): 操作日志页面(RuoYi 化迁移)`
8. `refactor(monitor): 删除旧 operation-log API/视图/类型`
9. `feat(router): 5 个 monitor 路由 + MainLayout 系统监控菜单`

## 9. 自检记录

撰写完成后已自检(2026-07-11 第二轮修订):

- [x] **占位符扫描**:无"待定"/"TODO"/"后续补"等占位词(§6 风险项已显式标注为"风险"而非"待办")
- [x] **内部一致性**:
  - view 文件命名统一为 `views/system/monitor/{module}/index.vue`(§3.2 与 §4.1-§4.5 一致)
  - `v-hasPermi` 指令名在 §3.4 / §5.1 / §8 引用一致
  - 权限标识 `monitor:xxx:xxx` 在 §4 各 view 与 §6 风险项 2 一致
  - `User.permissions` 类型在 §5.2 store / types / api 三处一致
  - `getUserInfo` 端点切换 `→ /api/getInfo` 在 §5.2 与 §8 第 1 个 commit 一致
- [x] **范围检查**:聚焦于 5 个 monitor 页面 + 权限基础设施 + operlog 迁移 + auth 端点切换,可由一份实现计划覆盖
- [x] **模糊性检查**:
  - cacheValue 展示格式统一为 `JSON.stringify(value, null, 2)`
  - 列表页 pageSize 统一为 `[10, 20, 50, 100]`
  - 操作日志 businessType 枚举值与后端 `BusinessType` 一一映射,见 §4.5 表
  - 删除/清空/强退统一使用 `ElMessageBox.confirm` 二次确认
  - view 目录命名风格统一为子目录 + `index.vue`(所有 5 个 view 一致)