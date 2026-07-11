# system/monitor 页面实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 在 ZlinksPackageSystem 前端补齐 5 个系统监控页面（服务监控 / 缓存监控 / 登录日志 / 在线用户 / 操作日志），引入最小化 `v-hasPermi` 按钮级权限控制，迁移现有 `views/log/LogListView.vue` 到 monitor 体系，删除冗余旧文件。

**架构：** 沿用现有 Vue 3 + TypeScript + Element Plus + Pinia 架构。新增 `directive/permission/hasPermi.ts` 单文件指令；将前端 `auth.ts.getUserInfo` 从 `/api/auth/info` 切换到 RuoYi 标准的 `/api/getInfo` 以拿到 `permissions` 列表；5 个 view 放在 `src/views/system/monitor/{module}/index.vue`（子目录 + index.vue，与 RuoYi-Vue3-js 参考项目一致）。axios 拦截器统一处理 403/500/网络错误。每个 view 一个 commit，最后一个 commit 路由 + 菜单。

**技术栈：** Vue 3.4（Composition API + `<script setup>`）、TypeScript 5.3、Vite 5、Element Plus 2.6、Pinia 2.1、Axios 1.6、ECharts 5.4（仅 ServerView 使用半环仪表盘）。

**规格文档：** `docs/superpowers/specs/2026-07-11-monitor-pages-design.md`（设计已用户批准，commit `06081ed`）

---

## 文件结构

### 新增文件（17 个）

| 文件 | 职责 |
|---|---|
| `frontend/src/api/monitor/server.ts` | 服务监控 API 客户端（仅 `getInfo`） |
| `frontend/src/api/monitor/cache.ts` | 缓存监控 API 客户端（6 个端点） |
| `frontend/src/api/monitor/logininfor.ts` | 登录日志 API 客户端（list/remove/clean/unlock） |
| `frontend/src/api/monitor/online.ts` | 在线用户 API 客户端（list/forceLogout） |
| `frontend/src/api/monitor/operlog.ts` | 操作日志 API 客户端（list/getInfo/remove/clean/export） |
| `frontend/src/directive/permission/hasPermi.ts` | 自定义指令 `v-hasPermi`（~30 行） |
| `frontend/src/types/monitor/server.ts` | ServerInfo / Cpu / Mem / Jvm / Sys / SysFile 接口 |
| `frontend/src/types/monitor/cache.ts` | CacheInfo 接口 |
| `frontend/src/types/monitor/logininfor.ts` | Logininfor 接口 |
| `frontend/src/types/monitor/online.ts` | UserOnline 接口 |
| `frontend/src/types/monitor/operlog.ts` | OperLog 接口 |
| `frontend/src/views/system/monitor/server/index.vue` | 服务监控 view（含 ECharts 半环仪表盘） |
| `frontend/src/views/system/monitor/cache/index.vue` | 缓存监控 view（左右两栏） |
| `frontend/src/views/system/monitor/logininfor/index.vue` | 登录日志 view |
| `frontend/src/views/system/monitor/online/index.vue` | 在线用户 view |
| `frontend/src/views/system/monitor/operlog/index.vue` | 操作日志 view（含详情对话框 + 导出） |

### 修改文件（7 个）

| 文件 | 变更 |
|---|---|
| `frontend/src/types/user.ts` | 加 `UserInfoResponse` 类型 |
| `frontend/src/api/auth.ts` | `getUserInfo` 端点切换 + 返回类型 |
| `frontend/src/stores/user.ts` | 加 `permissions` / `roles` state + `fetchUserInfo` 拆解响应 |
| `frontend/src/main.ts` | 注册 `v-hasPermi` 指令 |
| `frontend/src/api/index.ts` | 响应拦截器加 403 / 500 / 网络错误提示 |
| `frontend/src/router/index.ts` | 删 `/operation-logs`；加 5 个 monitor 路由 |
| `frontend/src/layouts/MainLayout.vue` | 删"日志管理"菜单；加"系统监控"父菜单（5 子项） |

### 删除文件（3 个 + 空目录）

- `frontend/src/api/operation-log.ts`
- `frontend/src/views/log/LogListView.vue`
- `frontend/src/types/operation-log.ts`
- `frontend/src/views/log/`（若为空）

---

## 任务概览

| 任务 | Commit | 描述 |
|---|---|---|
| 1 | `feat(auth): 切换 getUserInfo 到 /api/getInfo 拿 permissions/roles` | 切换 auth 端点 + 加 permissions state |
| 2 | `feat(monitor): 基础设施 — v-hasPermi 指令 + axios 错误拦截` | 指令 + 拦截器 + monitor 目录骨架 |
| 3 | `feat(monitor): 服务监控页面 + ECharts 半环仪表盘` | ServerView 完整实现 |
| 4 | `feat(monitor): 缓存监控页面（左右两栏）` | CacheView 完整实现 |
| 5 | `feat(monitor): 登录日志页面` | LogininforView 完整实现 |
| 6 | `feat(monitor): 在线用户页面` | OnlineView 完整实现 |
| 7 | `feat(monitor): 操作日志页面（RuoYi 化迁移）` | OperlogView 完整实现 |
| 8 | `refactor(monitor): 删除旧 operation-log API/视图/类型` | 删除 3 个旧文件 + 空目录 |
| 9 | `feat(router): 5 个 monitor 路由 + MainLayout 系统监控菜单` | 路由 + 菜单收尾 |

每任务完成一个 commit，**严格按照此顺序执行**。

---

## 任务 1：切换 auth 端点 + 加 permissions state

**Commit:** `feat(auth): 切换 getUserInfo 到 /api/getInfo 拿 permissions/roles`

**文件：**
- 修改：`frontend/src/types/user.ts`
- 修改：`frontend/src/api/auth.ts`
- 修改：`frontend/src/stores/user.ts`

- [ ] **步骤 1.1：在 `frontend/src/types/user.ts` 末尾新增 `UserInfoResponse`**

文件当前末尾（推测）含 `User` 接口。在其下方新增（先 `read_file` 确认现状再插入）：

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
```

`User` 接口本身**不动**——业务层不直接依赖 `User.permissions`（权限以 store 形式提供）。

- [ ] **步骤 1.2：修改 `frontend/src/api/auth.ts`**

将 `UserInfoResponse` 的 `data` 类型从 `User` 改为新结构，并修改 `getUserInfo` 端点：

```ts
import api from '@/api'
import type { User, UserInfoResponse } from '@/types/user'

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  code: number
  message: string
  data: string
}

export const authApi = {
  login(data: LoginRequest): Promise<LoginResponse> {
    return api.post('/auth/login', data)
  },

  getUserInfo(): Promise<UserInfoResponse> {
    return api.get('/getInfo')
  },

  register(user: Partial<User>): Promise<any> {
    return api.post('/auth/register', user)
  },
}
```

注意：`User` 仍需导入，因为 `register` 用到。

- [ ] **步骤 1.3：修改 `frontend/src/stores/user.ts`**

完整替换文件内容：

```ts
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { User } from '@/types/user'
import { authApi } from '@/api/auth'

export const useUserStore = defineStore('user', () => {
  const token = ref<string>(localStorage.getItem('token') || '')
  const user = ref<User | null>(null)
  const permissions = ref<string[]>([])
  const roles = ref<string[]>([])

  const isAuthenticated = computed(() => !!token.value)
  const currentUser = computed(() => user.value)

  async function login(username: string, password: string) {
    const response = await authApi.login({ username, password })
    token.value = response.data
    localStorage.setItem('token', response.data)
    await fetchUserInfo()
  }

  async function fetchUserInfo() {
    try {
      const response = await authApi.getUserInfo()
      user.value = response.data.user
      roles.value = response.data.roles ?? []
      permissions.value = response.data.permissions ?? []
    } catch (error) {
      logout()
    }
  }

  function logout() {
    token.value = ''
    user.value = null
    roles.value = []
    permissions.value = []
    localStorage.removeItem('token')
  }

  function setToken(newToken: string) {
    token.value = newToken
    localStorage.setItem('token', newToken)
  }

  return {
    token,
    user,
    permissions,
    roles,
    isAuthenticated,
    currentUser,
    login,
    fetchUserInfo,
    logout,
    setToken,
  }
})
```

- [ ] **步骤 1.4：构建验证**

```bash
cd frontend && npm run build
```

预期：TypeScript 编译 + Vite 构建成功，无报错。

- [ ] **步骤 1.5：Commit**

```bash
cd frontend && git add src/types/user.ts src/api/auth.ts src/stores/user.ts && GIT_EDITOR=true git commit -m "feat(auth): 切换 getUserInfo 到 /api/getInfo 拿 permissions/roles"
```

注意：需切回项目根目录再 `git add` 路径，或直接在 frontend 内 `git add` 完整相对路径。

---

## 任务 2：基础设施 — v-hasPermi 指令 + axios 错误拦截

**Commit:** `feat(monitor): 基础设施 — v-hasPermi 指令 + axios 错误拦截`

**文件：**
- 创建：`frontend/src/directive/permission/hasPermi.ts`
- 修改：`frontend/src/main.ts`
- 修改：`frontend/src/api/index.ts`

- [ ] **步骤 2.1：创建 `frontend/src/directive/permission/hasPermi.ts`**

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

- [ ] **步骤 2.2：在 `frontend/src/main.ts` 中注册指令**

修改文件，在 `app.mount('#app')` 之前插入：

```ts
import hasPermi from '@/directive/permission/hasPermi'

// ... 现有代码 ...

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(ElementPlus, { locale: zhCn })
app.directive('hasPermi', hasPermi)

for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
  app.component(key, component)
}

app.mount('#app')
```

`import hasPermi` 放在 `import App from './App.vue'` 附近，保持顶部 imports 整洁。

- [ ] **步骤 2.3：修改 `frontend/src/api/index.ts` 错误拦截**

在文件顶部加 `import { ElMessage } from 'element-plus'`。在 `error` 分支中，将原有 `if (error.response) { ... }` 替换为：

```ts
api.interceptors.response.use(
  (response) => {
    return response.data
  },
  (error) => {
    if (error.response) {
      const { status } = error.response
      const userStore = useUserStore()

      if (status === 401) {
        userStore.logout()
        router.push({ name: 'login' })
      } else if (status === 403) {
        ElMessage.error('您没有该操作权限')
      } else if (status >= 500) {
        ElMessage.error(error.response.data?.msg ?? '服务异常')
      }
    } else {
      ElMessage.error('网络异常,请稍后重试')
    }
    return Promise.reject(error)
  },
)
```

注意保留 `useUserStore` 和 `router` 的现有 import。

- [ ] **步骤 2.4：构建验证**

```bash
cd frontend && npm run build
```

预期：构建成功，无报错。

- [ ] **步骤 2.5：手动验证错误提示**

启动 dev server (`npm run dev`)，临时改某处 API 触发 403/500（可选跳过，本步骤仅做类型验证）。

- [ ] **步骤 2.6：Commit**

```bash
cd frontend && git add src/directive/permission/hasPermi.ts src/main.ts src/api/index.ts && GIT_EDITOR=true git commit -m "feat(monitor): 基础设施 — v-hasPermi 指令 + axios 错误拦截"
```

---

## 任务 3：服务监控页面（ServerView）

**Commit:** `feat(monitor): 服务监控页面 + ECharts 半环仪表盘`

**文件：**
- 创建：`frontend/src/types/monitor/server.ts`
- 创建：`frontend/src/api/monitor/server.ts`
- 创建：`frontend/src/views/system/monitor/server/index.vue`

- [ ] **步骤 3.1：创建 `frontend/src/types/monitor/server.ts`**

```ts
export interface CpuInfo {
  cpuNum: number
  used: number
  sys: number
  free: number
}

export interface MemInfo {
  total: number
  used: number
  free: number
  usage: number
}

export interface JvmInfo {
  total: number
  max: number
  used: number
  free: number
  usage: number
  name: string
  version: string
  startTime: number
  runTime: number
  home: string
  nonheapTotal: number
  nonheapUsed: number
}

export interface SysInfo {
  computerName: string
  computerIp: string
  osName: string
  osArch: string
  userDir: string
  userHome: string
  osVersion: string
}

export interface SysFileInfo {
  dirName: string
  sysTypeName: string
  typeName: string
  total: number
  free: number
  used: number
  usage: number
}

export interface ServerInfo {
  cpu: CpuInfo
  mem: MemInfo
  jvm: JvmInfo
  sys: SysInfo
  sysFiles: SysFileInfo[]
}
```

- [ ] **步骤 3.2：创建 `frontend/src/api/monitor/server.ts`**

```ts
import api from '@/api'
import type { ServerInfo } from '@/types/monitor/server'

export const serverApi = {
  getInfo(): Promise<{ data: ServerInfo }> {
    return api.get('/monitor/server')
  },
}
```

- [ ] **步骤 3.3：创建 `frontend/src/views/system/monitor/server/index.vue`（完整内容）**

```vue
<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick } from 'vue'
import * as echarts from 'echarts'
import { Refresh } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { serverApi } from '@/api/monitor/server'
import type { ServerInfo } from '@/types/monitor/server'

const loading = ref(false)
const server = ref<ServerInfo | null>(null)
const autoRefresh = ref(false)
const interval = ref(10)
let timer: number | null = null

const cpuChartRef = ref<HTMLDivElement>()
const memChartRef = ref<HTMLDivElement>()
let cpuChart: echarts.ECharts | null = null
let memChart: echarts.ECharts | null = null

const fetchData = async () => {
  loading.value = true
  try {
    const { data } = await serverApi.getInfo()
    server.value = data
    await nextTick()
    renderCharts()
  } catch (error) {
    console.error('获取服务监控数据失败:', error)
    ElMessage.error('获取服务监控数据失败')
  } finally {
    loading.value = false
  }
}

const renderCharts = () => {
  if (!server.value) return
  renderCpuChart()
  renderMemChart()
}

const renderCpuChart = () => {
  if (!cpuChartRef.value || !server.value) return
  if (!cpuChart) {
    cpuChart = echarts.init(cpuChartRef.value)
  }
  cpuChart.setOption({
    series: [
      {
        type: 'gauge',
        startAngle: 200,
        endAngle: -20,
        min: 0,
        max: 100,
        progress: { show: true, width: 14 },
        axisLine: { lineStyle: { width: 14 } },
        axisTick: { show: false },
        splitLine: { show: false },
        axisLabel: { distance: 20, fontSize: 12 },
        pointer: { width: 4 },
        detail: {
          valueAnimation: true,
          formatter: '{value}%',
          fontSize: 24,
          offsetCenter: [0, '60%'],
        },
        data: [{ value: server.value.cpu.used }],
      },
    ],
  })
}

const renderMemChart = () => {
  if (!memChartRef.value || !server.value) return
  if (!memChart) {
    memChart = echarts.init(memChartRef.value)
  }
  memChart.setOption({
    series: [
      {
        type: 'gauge',
        startAngle: 200,
        endAngle: -20,
        min: 0,
        max: 100,
        progress: { show: true, width: 14 },
        axisLine: { lineStyle: { width: 14 } },
        axisTick: { show: false },
        splitLine: { show: false },
        axisLabel: { distance: 20, fontSize: 12 },
        pointer: { width: 4 },
        detail: {
          valueAnimation: true,
          formatter: '{value}%',
          fontSize: 24,
          offsetCenter: [0, '60%'],
        },
        data: [{ value: server.value.mem.usage }],
      },
    ],
  })
}

const startAutoRefresh = () => {
  if (timer) clearInterval(timer)
  timer = window.setInterval(fetchData, interval.value * 1000)
}

const stopAutoRefresh = () => {
  if (timer) {
    clearInterval(timer)
    timer = null
  }
}

watch(autoRefresh, (val) => {
  if (val) startAutoRefresh()
  else stopAutoRefresh()
})

watch(interval, () => {
  if (autoRefresh.value) startAutoRefresh()
})

const handleResize = () => {
  cpuChart?.resize()
  memChart?.resize()
}

onMounted(() => {
  fetchData()
  window.addEventListener('resize', handleResize)
})

onUnmounted(() => {
  stopAutoRefresh()
  cpuChart?.dispose()
  memChart?.dispose()
  window.removeEventListener('resize', handleResize)
})

const formatDate = (timestamp: number) => {
  if (!timestamp) return '-'
  return new Date(timestamp).toLocaleString('zh-CN')
}

const formatUptime = (ms: number) => {
  const days = Math.floor(ms / (1000 * 60 * 60 * 24))
  const hours = Math.floor((ms % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60))
  const minutes = Math.floor((ms % (1000 * 60 * 60)) / (1000 * 60))
  return `${days}天 ${hours}小时 ${minutes}分钟`
}
</script>

<template>
  <div class="server-container" v-loading="loading">
    <el-card class="control-bar">
      <div class="control-row">
        <el-button type="primary" :icon="Refresh" @click="fetchData">刷新</el-button>
        <el-switch v-model="autoRefresh" active-text="自动刷新" inactive-text="手动" />
        <el-select v-model="interval" :disabled="!autoRefresh" style="width: 120px">
          <el-option label="5秒" :value="5" />
          <el-option label="10秒" :value="10" />
          <el-option label="30秒" :value="30" />
          <el-option label="60秒" :value="60" />
        </el-select>
      </div>
    </el-card>

    <el-row :gutter="12" v-if="server">
      <el-col :xs="24" :sm="12">
        <el-card>
          <template #header><span>CPU</span></template>
          <div ref="cpuChartRef" style="height: 200px"></div>
          <el-descriptions :column="1" size="small" border>
            <el-descriptions-item label="核心数">{{ server.cpu.cpuNum }}</el-descriptions-item>
            <el-descriptions-item label="用户使用率">{{ server.cpu.used }}%</el-descriptions-item>
            <el-descriptions-item label="系统使用率">{{ server.cpu.sys }}%</el-descriptions-item>
            <el-descriptions-item label="空闲率">{{ server.cpu.free }}%</el-descriptions-item>
          </el-descriptions>
        </el-card>
      </el-col>

      <el-col :xs="24" :sm="12">
        <el-card>
          <template #header><span>内存</span></template>
          <div ref="memChartRef" style="height: 200px"></div>
          <el-descriptions :column="1" size="small" border>
            <el-descriptions-item label="总内存">{{ server.mem.total }} GB</el-descriptions-item>
            <el-descriptions-item label="已用">{{ server.mem.used }} GB</el-descriptions-item>
            <el-descriptions-item label="可用">{{ server.mem.free }} GB</el-descriptions-item>
            <el-descriptions-item label="使用率">
              <span :class="{ 'text-danger': server.mem.usage > 80 }">{{ server.mem.usage }}%</span>
            </el-descriptions-item>
          </el-descriptions>
        </el-card>
      </el-col>

      <el-col :span="24">
        <el-card>
          <template #header><span>服务器信息</span></template>
          <el-descriptions :column="2" border>
            <el-descriptions-item label="计算机名称">{{ server.sys.computerName }}</el-descriptions-item>
            <el-descriptions-item label="操作系统">{{ server.sys.osName }}</el-descriptions-item>
            <el-descriptions-item label="服务器 IP">{{ server.sys.computerIp }}</el-descriptions-item>
            <el-descriptions-item label="系统架构">{{ server.sys.osArch }}</el-descriptions-item>
            <el-descriptions-item label="项目路径" :span="2">{{ server.sys.userDir }}</el-descriptions-item>
            <el-descriptions-item label="用户路径" :span="2">{{ server.sys.userHome }}</el-descriptions-item>
          </el-descriptions>
        </el-card>
      </el-col>

      <el-col :span="24">
        <el-card>
          <template #header><span>Java 虚拟机信息</span></template>
          <el-descriptions :column="2" border>
            <el-descriptions-item label="Java 名称">{{ server.jvm.name }}</el-descriptions-item>
            <el-descriptions-item label="Java 版本">{{ server.jvm.version }}</el-descriptions-item>
            <el-descriptions-item label="启动时间">{{ formatDate(server.jvm.startTime) }}</el-descriptions-item>
            <el-descriptions-item label="运行时长">{{ formatUptime(server.jvm.runTime) }}</el-descriptions-item>
            <el-descriptions-item label="堆总大小">{{ server.jvm.total }} MB</el-descriptions-item>
            <el-descriptions-item label="堆最大">{{ server.jvm.max }} MB</el-descriptions-item>
            <el-descriptions-item label="堆已用">{{ server.jvm.used }} MB</el-descriptions-item>
            <el-descriptions-item label="堆使用率">
              <span :class="{ 'text-danger': server.jvm.usage > 80 }">{{ server.jvm.usage }}%</span>
            </el-descriptions-item>
            <el-descriptions-item label="安装路径" :span="2">{{ server.jvm.home }}</el-descriptions-item>
          </el-descriptions>
        </el-card>
      </el-col>

      <el-col :span="24">
        <el-card>
          <template #header><span>磁盘状态</span></template>
          <el-table :data="server.sysFiles" border>
            <el-table-column prop="dirName" label="盘符路径" min-width="160" show-overflow-tooltip />
            <el-table-column prop="sysTypeName" label="文件系统" min-width="100" />
            <el-table-column prop="typeName" label="盘符类型" min-width="120" show-overflow-tooltip />
            <el-table-column prop="total" label="总大小(GB)" min-width="100" align="right" />
            <el-table-column prop="free" label="可用(GB)" min-width="100" align="right" />
            <el-table-column prop="used" label="已用(GB)" min-width="100" align="right" />
            <el-table-column label="使用率" min-width="100" align="right">
              <template #default="{ row }">
                <span :class="{ 'text-danger': row.usage > 80 }">{{ row.usage }}%</span>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<style scoped>
.server-container {
  padding: 16px;
}
.control-bar {
  margin-bottom: 12px;
}
.control-row {
  display: flex;
  align-items: center;
  gap: 16px;
}
.text-danger {
  color: #f56c6c;
  font-weight: 600;
}
.el-card {
  margin-bottom: 12px;
}
</style>
```

注意：上述 `<script setup>` 使用了 `watch` 但未 import。需要在顶部 `import { ref, onMounted, onUnmounted, nextTick, watch } from 'vue'`。修正步骤 3.3 中脚本开头的 imports：

```ts
import { ref, onMounted, onUnmounted, nextTick, watch } from 'vue'
```

- [ ] **步骤 3.4：构建验证**

```bash
cd frontend && npm run build
```

预期：构建成功，无 TS 报错。

- [ ] **步骤 3.5：手动验证**

`npm run dev` → 浏览器访问 `/system/monitor/server`：
- 5 个卡片正常渲染
- ECharts 半环仪表盘显示 CPU / 内存使用率
- "刷新"按钮工作
- "自动刷新"开关 + 间隔下拉联动工作
- 切换浏览器 tab 回来时 resize 正常（可选）

- [ ] **步骤 3.6：Commit**

```bash
cd frontend && git add src/types/monitor/server.ts src/api/monitor/server.ts src/views/system/monitor/server/index.vue && GIT_EDITOR=true git commit -m "feat(monitor): 服务监控页面 + ECharts 半环仪表盘"
```

---

## 任务 4：缓存监控页面（CacheView）

**Commit:** `feat(monitor): 缓存监控页面（左右两栏）`

**文件：**
- 创建：`frontend/src/types/monitor/cache.ts`
- 创建：`frontend/src/api/monitor/cache.ts`
- 创建：`frontend/src/views/system/monitor/cache/index.vue`

- [ ] **步骤 4.1：创建 `frontend/src/types/monitor/cache.ts`**

```ts
export interface CacheInfo {
  info: Record<string, string>
  dbSize: number
  commandStats: Array<{ name: string; value: string }>
}
```

- [ ] **步骤 4.2：创建 `frontend/src/api/monitor/cache.ts`**

```ts
import api from '@/api'
import type { CacheInfo } from '@/types/monitor/cache'

export const cacheApi = {
  getInfo(): Promise<{ data: CacheInfo }> {
    return api.get('/monitor/cache')
  },

  getNames(): Promise<{ data: string[] }> {
    return api.get('/monitor/cache/getNames')
  },

  getKeys(cacheName: string): Promise<{ data: string[] }> {
    return api.get(`/monitor/cache/getKeys/${encodeURIComponent(cacheName)}`)
  },

  getValue(cacheName: string, cacheKey: string): Promise<{ data: { cacheName: string; cacheKey: string; cacheValue: unknown; remark: string } }> {
    return api.get(`/monitor/cache/getValue/${encodeURIComponent(cacheName)}/${encodeURIComponent(cacheKey)}`)
  },

  clearCacheName(cacheName: string): Promise<void> {
    return api.delete(`/monitor/cache/clearCacheName/${encodeURIComponent(cacheName)}`)
  },

  clearCacheKey(cacheKey: string): Promise<void> {
    return api.delete(`/monitor/cache/clearCacheKey/${encodeURIComponent(cacheKey)}`)
  },

  clearCacheAll(): Promise<void> {
    return api.delete('/monitor/cache/clearCacheAll')
  },
}
```

- [ ] **步骤 4.3：创建 `frontend/src/views/system/monitor/cache/index.vue`（完整内容）**

```vue
<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Refresh, Delete, View } from '@element-plus/icons-vue'
import { cacheApi } from '@/api/monitor/cache'
import type { CacheInfo } from '@/types/monitor/cache'

const loading = ref(false)
const cacheInfo = ref<CacheInfo | null>(null)
const cacheNames = ref<string[]>([])
const cacheKeys = ref<string[]>([])
const selectedName = ref<string>('')
const selectedKey = ref<string>('')
const cacheValue = ref<unknown>(null)

const fetchInfo = async () => {
  loading.value = true
  try {
    const { data } = await cacheApi.getInfo()
    cacheInfo.value = data
  } catch (error) {
    console.error('获取缓存信息失败:', error)
    ElMessage.error('获取缓存信息失败')
  } finally {
    loading.value = false
  }
}

const fetchNames = async () => {
  try {
    const { data } = await cacheApi.getNames()
    cacheNames.value = data
  } catch (error) {
    console.error('获取缓存名称失败:', error)
    ElMessage.error('获取缓存名称失败')
  }
}

const handleSelectName = async (name: string) => {
  selectedName.value = name
  selectedKey.value = ''
  cacheValue.value = null
  cacheKeys.value = []
  if (!name) return
  try {
    const { data } = await cacheApi.getKeys(name)
    cacheKeys.value = data
  } catch (error) {
    console.error('获取缓存键失败:', error)
    ElMessage.error('获取缓存键失败')
  }
}

const handleSelectKey = async (key: string) => {
  selectedKey.value = key
  cacheValue.value = null
  if (!key || !selectedName.value) return
  try {
    const { data } = await cacheApi.getValue(selectedName.value, key)
    cacheValue.value = data.cacheValue
  } catch (error) {
    console.error('获取缓存值失败:', error)
    ElMessage.error('获取缓存值失败')
  }
}

const handleClearCacheAll = async () => {
  try {
    await ElMessageBox.confirm('确定要清空所有缓存吗?此操作不可恢复!', '警告', {
      confirmButtonText: '确定清空',
      cancelButtonText: '取消',
      type: 'warning',
    })
    await cacheApi.clearCacheAll()
    ElMessage.success('清空成功')
    cacheNames.value = []
    cacheKeys.value = []
    selectedName.value = ''
    selectedKey.value = ''
    cacheValue.value = null
    await fetchInfo()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('清空失败')
  }
}

const handleClearCacheName = async (name: string) => {
  try {
    await ElMessageBox.confirm(`确定要清空缓存 [${name}] 下的所有键吗?`, '提示', {
      type: 'warning',
    })
    await cacheApi.clearCacheName(name)
    ElMessage.success('清空成功')
    cacheKeys.value = []
    selectedKey.value = ''
    cacheValue.value = null
    if (selectedName.value === name) {
      handleSelectName('')
    }
    await fetchNames()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('清空失败')
  }
}

const handleClearCacheKey = async (key: string) => {
  try {
    await ElMessageBox.confirm(`确定要删除缓存键 [${key}] 吗?`, '提示', {
      type: 'warning',
    })
    await cacheApi.clearCacheKey(key)
    ElMessage.success('删除成功')
    if (selectedKey.value === key) {
      selectedKey.value = ''
      cacheValue.value = null
    }
    if (selectedName.value) {
      handleSelectName(selectedName.value)
    }
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('删除失败')
  }
}

const formatValue = (val: unknown): string => {
  if (val === null || val === undefined) return '-'
  if (typeof val === 'string') return val
  try {
    return JSON.stringify(val, null, 2)
  } catch {
    return String(val)
  }
}

watch(selectedName, (val) => {
  if (!val) {
    cacheKeys.value = []
    selectedKey.value = ''
    cacheValue.value = null
  }
})

onMounted(async () => {
  await Promise.all([fetchInfo(), fetchNames()])
})
</script>

<template>
  <div class="cache-container">
    <el-row :gutter="12">
      <el-col :span="8">
        <el-card v-loading="loading">
          <template #header>
            <div class="card-header">
              <span>缓存名称</span>
              <div>
                <el-button :icon="Refresh" size="small" @click="fetchNames">刷新</el-button>
                <el-button
                  v-hasPermi="['monitor:cache:list']"
                  type="danger"
                  :icon="Delete"
                  size="small"
                  @click="handleClearCacheAll"
                >清空全部</el-button>
              </div>
            </div>
          </template>
          <el-table
            :data="cacheNames"
            highlight-current-row
            height="500"
            @row-click="handleSelectName"
            empty-text="暂无缓存"
          >
            <el-table-column prop="label" label="缓存名称" min-width="200" show-overflow-tooltip>
              <template #default="{ row }">{{ row }}</template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>

      <el-col :span="16">
        <el-card>
          <template #header>
            <div class="card-header">
              <span>缓存键 {{ selectedName ? `(当前: ${selectedName})` : '' }}</span>
              <el-button
                v-if="selectedName"
                v-hasPermi="['monitor:cache:list']"
                type="danger"
                :icon="Delete"
                size="small"
                @click="handleClearCacheName(selectedName)"
              >清空该缓存</el-button>
            </div>
          </template>
          <el-table
            :data="cacheKeys"
            highlight-current-row
            height="240"
            @row-click="handleSelectKey"
            empty-text="请点击左侧缓存名称"
          >
            <el-table-column label="键" min-width="100%">
              <template #default="{ row }">
                <span>{{ row }}</span>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="120" align="center">
              <template #default="{ row }">
                <el-button
                  v-hasPermi="['monitor:cache:list']"
                  type="danger"
                  link
                  :icon="Delete"
                  @click.stop="handleClearCacheKey(row)"
                >删除</el-button>
              </template>
            </el-table-column>
          </el-table>

          <el-divider v-if="selectedKey">缓存值</el-divider>

          <div v-if="selectedKey" class="value-panel">
            <div class="value-header">
              <span><strong>键:</strong>{{ selectedKey }}</span>
              <el-button :icon="View" size="small" @click="handleSelectKey(selectedKey)">刷新</el-button>
            </div>
            <pre class="value-content">{{ formatValue(cacheValue) }}</pre>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-card v-if="cacheInfo" style="margin-top: 12px">
      <template #header><span>Redis 概览</span></template>
      <el-descriptions :column="4" border>
        <el-descriptions-item label="键总数">{{ cacheInfo.dbSize }}</el-descriptions-item>
        <el-descriptions-item label="Redis 版本">{{ cacheInfo.info?.redis_version ?? '-' }}</el-descriptions-item>
        <el-descriptions-item label="运行模式">{{ cacheInfo.info?.redis_mode ?? '-' }}</el-descriptions-item>
        <el-descriptions-item label="端口">{{ cacheInfo.info?.tcp_port ?? '-' }}</el-descriptions-item>
      </el-descriptions>
    </el-card>
  </div>
</template>

<style scoped>
.cache-container {
  padding: 16px;
}
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.value-panel {
  margin-top: 8px;
}
.value-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
  padding: 0 4px;
}
.value-content {
  background: #f5f7fa;
  padding: 12px;
  border-radius: 4px;
  max-height: 300px;
  overflow: auto;
  font-family: 'Consolas', 'Monaco', monospace;
  font-size: 13px;
  margin: 0;
  white-space: pre-wrap;
  word-break: break-all;
}
</style>
```

- [ ] **步骤 4.4：构建验证**

```bash
cd frontend && npm run build
```

预期：构建成功。

- [ ] **步骤 4.5：手动验证**

`npm run dev` → 访问 `/system/monitor/cache`：
- 左栏 cacheName 列表正常渲染
- 点击 cacheName → 右栏 cacheKey 列表填充
- 点击 cacheKey → 下方详情面板显示 cacheValue（JSON 格式化）
- 三个删除操作弹确认框 → 确认后成功
- 权限按钮在无权时隐藏

- [ ] **步骤 4.6：Commit**

```bash
cd frontend && git add src/types/monitor/cache.ts src/api/monitor/cache.ts src/views/system/monitor/cache/index.vue && GIT_EDITOR=true git commit -m "feat(monitor): 缓存监控页面(左右两栏)"
```

---

## 任务 5：登录日志页面（LogininforView）

**Commit:** `feat(monitor): 登录日志页面`

**文件：**
- 创建：`frontend/src/types/monitor/logininfor.ts`
- 创建：`frontend/src/api/monitor/logininfor.ts`
- 创建：`frontend/src/views/system/monitor/logininfor/index.vue`

- [ ] **步骤 5.1：创建 `frontend/src/types/monitor/logininfor.ts`**

```ts
export interface Logininfor {
  infoId: number
  userName: string
  ipaddr: string
  loginLocation: string
  browser: string
  os: string
  status: string
  msg: string
  loginTime: string
}

export interface LogininforQuery {
  current?: number
  size?: number
  userName?: string
  ipaddr?: string
  status?: string
}
```

- [ ] **步骤 5.2：创建 `frontend/src/api/monitor/logininfor.ts`**

```ts
import api from '@/api'
import type { Logininfor, LogininforQuery } from '@/types/monitor/logininfor'
import type { PageResult } from '@/types/common'

export const logininforApi = {
  list(params: LogininforQuery): Promise<{ data: PageResult<Logininfor> }> {
    return api.get('/monitor/logininfor/list', { params })
  },

  remove(infoIds: number[]): Promise<void> {
    return api.delete(`/monitor/logininfor/${infoIds.join(',')}`)
  },

  clean(): Promise<void> {
    return api.delete('/monitor/logininfor/clean')
  },

  unlock(userName: string): Promise<void> {
    return api.put(`/monitor/logininfor/unlock/${encodeURIComponent(userName)}`)
  },
}
```

**前提**：`PageResult<T>` 类型已在 `frontend/src/types/common.ts` 中定义（参考 `api/operation-log.ts` 中的用法）。若未定义，**先在 `common.ts` 中加**：

```ts
export interface PageResult<T> {
  records: T[]
  total: number
  size: number
  current: number
  pages: number
}
```

- [ ] **步骤 5.3：创建 `frontend/src/views/system/monitor/logininfor/index.vue`（完整内容）**

```vue
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Delete, Unlock } from '@element-plus/icons-vue'
import { logininforApi } from '@/api/monitor/logininfor'
import type { Logininfor } from '@/types/monitor/logininfor'

const loading = ref(false)
const list = ref<Logininfor[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const selectedIds = ref<number[]>([])

const searchForm = ref({
  userName: '',
  ipaddr: '',
  status: '',
})

const statusOptions = [
  { label: '成功', value: '0' },
  { label: '失败', value: '1' },
]

const fetchList = async () => {
  loading.value = true
  selectedIds.value = []
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.userName) params.userName = searchForm.value.userName
    if (searchForm.value.ipaddr) params.ipaddr = searchForm.value.ipaddr
    if (searchForm.value.status) params.status = searchForm.value.status
    const { data } = await logininforApi.list(params)
    list.value = data.records
    total.value = data.total
  } catch (error) {
    console.error('获取登录日志失败:', error)
    ElMessage.error('获取登录日志失败')
  } finally {
    loading.value = false
  }
}

const handleSearch = () => {
  currentPage.value = 1
  fetchList()
}

const handleReset = () => {
  searchForm.value = { userName: '', ipaddr: '', status: '' }
  handleSearch()
}

const handleSelectionChange = (rows: Logininfor[]) => {
  selectedIds.value = rows.map((r) => r.infoId)
}

const handleDelete = async () => {
  if (selectedIds.value.length === 0) {
    ElMessage.warning('请选择要删除的记录')
    return
  }
  try {
    await ElMessageBox.confirm(`确定要删除选中的 ${selectedIds.value.length} 条记录吗?`, '提示', {
      type: 'warning',
    })
    await logininforApi.remove(selectedIds.value)
    ElMessage.success('删除成功')
    fetchList()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('删除失败')
  }
}

const handleClean = async () => {
  try {
    await ElMessageBox.confirm('确定要清空所有登录日志吗?此操作不可恢复!', '警告', {
      confirmButtonText: '确定清空',
      cancelButtonText: '取消',
      type: 'warning',
    })
    await logininforApi.clean()
    ElMessage.success('清空成功')
    fetchList()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('清空失败')
  }
}

const handleUnlock = async (row: Logininfor) => {
  try {
    await ElMessageBox.confirm(`确定要解锁用户 [${row.userName}] 吗?`, '提示', {
      type: 'warning',
    })
    await logininforApi.unlock(row.userName)
    ElMessage.success('解锁指令已发送')
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('解锁失败')
  }
}

const getStatusType = (status: string): 'success' | 'danger' => {
  return status === '0' ? 'success' : 'danger'
}

const getStatusLabel = (status: string): string => {
  return status === '0' ? '成功' : '失败'
}

onMounted(() => fetchList())
</script>

<template>
  <div class="logininfor-container">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>登录日志</span>
          <div>
            <el-button
              v-hasPermi="['monitor:logininfor:remove']"
              type="danger"
              :icon="Delete"
              :disabled="selectedIds.length === 0"
              @click="handleDelete"
            >删除</el-button>
            <el-button
              v-hasPermi="['monitor:logininfor:remove']"
              type="danger"
              :icon="Delete"
              @click="handleClean"
            >清空</el-button>
          </div>
        </div>
      </template>

      <el-form :model="searchForm" inline class="search-form">
        <el-form-item label="用户名称">
          <el-input v-model="searchForm.userName" placeholder="请输入用户名称" clearable @keyup.enter="handleSearch" />
        </el-form-item>
        <el-form-item label="登录地址">
          <el-input v-model="searchForm.ipaddr" placeholder="请输入 IP 地址" clearable @keyup.enter="handleSearch" />
        </el-form-item>
        <el-form-item label="登录状态">
          <el-select v-model="searchForm.status" placeholder="请选择状态" clearable style="width: 140px">
            <el-option v-for="o in statusOptions" :key="o.value" :label="o.label" :value="o.value" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
          <el-button :icon="Refresh" @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>

      <el-table
        v-loading="loading"
        :data="list"
        border
        @selection-change="handleSelectionChange"
      >
        <el-table-column type="selection" width="50" align="center" />
        <el-table-column prop="userName" label="用户名称" min-width="120" show-overflow-tooltip />
        <el-table-column prop="ipaddr" label="登录地址" min-width="130" />
        <el-table-column prop="loginLocation" label="登录地点" min-width="120" show-overflow-tooltip />
        <el-table-column prop="browser" label="浏览器" min-width="100" show-overflow-tooltip />
        <el-table-column prop="os" label="操作系统" min-width="100" show-overflow-tooltip />
        <el-table-column label="登录状态" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)">{{ getStatusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="msg" label="消息" min-width="160" show-overflow-tooltip />
        <el-table-column prop="loginTime" label="登录时间" min-width="160" />
        <el-table-column label="操作" width="100" align="center" fixed="right">
          <template #default="{ row }">
            <el-button
              v-hasPermi="['monitor:logininfor:unlock']"
              type="primary"
              link
              :icon="Unlock"
              @click="handleUnlock(row)"
            >解锁</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-container">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="fetchList"
          @current-change="fetchList"
        />
      </div>
    </el-card>
  </div>
</template>

<style scoped>
.logininfor-container {
  padding: 16px;
}
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.card-header > div {
  display: flex;
  gap: 8px;
}
.search-form {
  margin-bottom: 16px;
}
.pagination-container {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
</style>
```

- [ ] **步骤 5.4：构建验证**

```bash
cd frontend && npm run build
```

预期：构建成功。

- [ ] **步骤 5.5：手动验证**

`npm run dev` → 访问 `/system/monitor/logininfor`：
- 列表渲染、分页正常
- 搜索/重置生效
- 多选删除、清空、解锁按钮按权限显示
- 状态 tag 正确（成功=success，失败=danger）

- [ ] **步骤 5.6：Commit**

```bash
cd frontend && git add src/types/monitor/logininfor.ts src/api/monitor/logininfor.ts src/views/system/monitor/logininfor/index.vue && GIT_EDITOR=true git commit -m "feat(monitor): 登录日志页面"
```

---

## 任务 6：在线用户页面（OnlineView）

**Commit:** `feat(monitor): 在线用户页面`

**文件：**
- 创建：`frontend/src/types/monitor/online.ts`
- 创建：`frontend/src/api/monitor/online.ts`
- 创建：`frontend/src/views/system/monitor/online/index.vue`

- [ ] **步骤 6.1：创建 `frontend/src/types/monitor/online.ts`**

```ts
export interface UserOnline {
  tokenId: string
  userName: string
  ipaddr: string
  loginLocation: string
  browser: string
  os: string
  roleKey: string
  loginTime: string
}

export interface OnlineQuery {
  current?: number
  size?: number
  ipaddr?: string
  userName?: string
}
```

- [ ] **步骤 6.2：创建 `frontend/src/api/monitor/online.ts`**

```ts
import api from '@/api'
import type { UserOnline, OnlineQuery } from '@/types/monitor/online'
import type { PageResult } from '@/types/common'

export const onlineApi = {
  list(params: OnlineQuery): Promise<{ data: PageResult<UserOnline> }> {
    return api.get('/monitor/online/list', { params })
  },

  forceLogout(tokenId: string): Promise<void> {
    return api.delete(`/monitor/online/${encodeURIComponent(tokenId)}`)
  },
}
```

- [ ] **步骤 6.3：创建 `frontend/src/views/system/monitor/online/index.vue`（完整内容）**

```vue
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Delete } from '@element-plus/icons-vue'
import { onlineApi } from '@/api/monitor/online'
import type { UserOnline } from '@/types/monitor/online'

const loading = ref(false)
const list = ref<UserOnline[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)

const searchForm = ref({
  ipaddr: '',
  userName: '',
})

const fetchList = async () => {
  loading.value = true
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.ipaddr) params.ipaddr = searchForm.value.ipaddr
    if (searchForm.value.userName) params.userName = searchForm.value.userName
    const { data } = await onlineApi.list(params)
    list.value = data.records
    total.value = data.total
  } catch (error) {
    console.error('获取在线用户失败:', error)
    ElMessage.error('获取在线用户失败')
  } finally {
    loading.value = false
  }
}

const handleSearch = () => {
  currentPage.value = 1
  fetchList()
}

const handleReset = () => {
  searchForm.value = { ipaddr: '', userName: '' }
  handleSearch()
}

const handleForceLogout = async (row: UserOnline) => {
  try {
    await ElMessageBox.confirm(`确定要强制下线用户 [${row.userName}] 吗?`, '提示', {
      type: 'warning',
    })
    await onlineApi.forceLogout(row.tokenId)
    ElMessage.success('下线成功')
    fetchList()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('下线失败')
  }
}

const truncateToken = (token: string) => {
  return token.length > 16 ? token.slice(0, 16) + '...' : token
}

onMounted(() => fetchList())
</script>

<template>
  <div class="online-container">
    <el-card>
      <template #header>
        <span>在线用户</span>
      </template>

      <el-form :model="searchForm" inline class="search-form">
        <el-form-item label="登录地址">
          <el-input v-model="searchForm.ipaddr" placeholder="请输入 IP 地址" clearable @keyup.enter="handleSearch" />
        </el-form-item>
        <el-form-item label="用户名称">
          <el-input v-model="searchForm.userName" placeholder="请输入用户名称" clearable @keyup.enter="handleSearch" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
          <el-button :icon="Refresh" @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>

      <el-table v-loading="loading" :data="list" border>
        <el-table-column label="会话 ID" min-width="180">
          <template #default="{ row }">
            <el-tooltip :content="row.tokenId" placement="top">
              <span>{{ truncateToken(row.tokenId) }}</span>
            </el-tooltip>
          </template>
        </el-table-column>
        <el-table-column prop="userName" label="登录用户" min-width="120" show-overflow-tooltip />
        <el-table-column prop="ipaddr" label="主机" min-width="130" />
        <el-table-column prop="loginLocation" label="登录地点" min-width="120" show-overflow-tooltip />
        <el-table-column prop="browser" label="浏览器" min-width="100" show-overflow-tooltip />
        <el-table-column prop="os" label="操作系统" min-width="100" show-overflow-tooltip />
        <el-table-column prop="roleKey" label="角色" min-width="120" show-overflow-tooltip />
        <el-table-column prop="loginTime" label="登录时间" min-width="160" />
        <el-table-column label="操作" width="100" align="center" fixed="right">
          <template #default="{ row }">
            <el-button
              v-hasPermi="['monitor:online:forceLogout']"
              type="danger"
              link
              :icon="Delete"
              @click="handleForceLogout(row)"
            >强退</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-container">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="fetchList"
          @current-change="fetchList"
        />
      </div>
    </el-card>
  </div>
</template>

<style scoped>
.online-container {
  padding: 16px;
}
.search-form {
  margin-bottom: 16px;
}
.pagination-container {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
</style>
```

- [ ] **步骤 6.4：构建验证**

```bash
cd frontend && npm run build
```

预期：构建成功。

- [ ] **步骤 6.5：手动验证**

`npm run dev` → 访问 `/system/monitor/online`：
- 列表渲染、分页正常
- 搜索/重置生效
- 强退按钮按权限显示 + 确认弹窗工作

- [ ] **步骤 6.6：Commit**

```bash
cd frontend && git add src/types/monitor/online.ts src/api/monitor/online.ts src/views/system/monitor/online/index.vue && GIT_EDITOR=true git commit -m "feat(monitor): 在线用户页面"
```

---

## 任务 7：操作日志页面（OperlogView）

**Commit:** `feat(monitor): 操作日志页面（RuoYi 化迁移）`

**文件：**
- 创建：`frontend/src/types/monitor/operlog.ts`
- 创建：`frontend/src/api/monitor/operlog.ts`
- 创建：`frontend/src/views/system/monitor/operlog/index.vue`

- [ ] **步骤 7.1：创建 `frontend/src/types/monitor/operlog.ts`**

```ts
export interface OperLog {
  operId: number
  title: string
  businessType: number
  method: string
  requestMethod: string
  operatorType: number
  operName: string
  deptName: string
  operUrl: string
  operIp: string
  operParam: string
  jsonResult: string
  status: number
  errorMsg: string
  operTime: string
}

export interface OperLogQuery {
  current?: number
  size?: number
  title?: string
  operName?: string
  businessType?: number | string
  status?: number | string
}
```

- [ ] **步骤 7.2：创建 `frontend/src/api/monitor/operlog.ts`**

```ts
import api from '@/api'
import type { OperLog, OperLogQuery } from '@/types/monitor/operlog'
import type { PageResult } from '@/types/common'

export const operlogApi = {
  list(params: OperLogQuery): Promise<{ data: PageResult<OperLog> }> {
    return api.get('/monitor/operlog/list', { params })
  },

  getInfo(operId: number): Promise<{ data: OperLog }> {
    return api.get(`/monitor/operlog/${operId}`)
  },

  remove(operIds: number[]): Promise<void> {
    return api.delete(`/monitor/operlog/${operIds.join(',')}`)
  },

  clean(): Promise<void> {
    return api.delete('/monitor/operlog/clean')
  },

  exportFile(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/monitor/operlog/export', { params: { format }, responseType: 'blob' })
  },
}
```

注意：`exportFile` 当前后端未实现 `SysOperlogController.export`（见 spec §6 风险 2），调用会 404；前端按钮保留，错误由全局拦截器提示。

- [ ] **步骤 7.3：创建 `frontend/src/views/system/monitor/operlog/index.vue`（完整内容）**

```vue
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Delete, View, Download, ArrowDown } from '@element-plus/icons-vue'
import { operlogApi } from '@/api/monitor/operlog'
import type { OperLog } from '@/types/monitor/operlog'

const loading = ref(false)
const exporting = ref(false)
const list = ref<OperLog[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const selectedIds = ref<number[]>([])

const searchForm = ref({
  title: '',
  operName: '',
  businessType: '',
  status: '',
})

const detailVisible = ref(false)
const detailLog = ref<OperLog | null>(null)

const businessTypeOptions = [
  { label: '其它', value: 0 },
  { label: '新增', value: 1 },
  { label: '修改', value: 2 },
  { label: '删除', value: 3 },
  { label: '授权', value: 4 },
  { label: '导出', value: 5 },
  { label: '导入', value: 6 },
  { label: '强退', value: 7 },
  { label: '生成代码', value: 8 },
  { label: '清空数据', value: 9 },
]

const businessTypeLabels: Record<number, string> = Object.fromEntries(
  businessTypeOptions.map((o) => [o.value, o.label]),
)

const businessTypeTagTypes: Record<number, 'info' | 'primary' | 'warning' | 'danger' | 'success'> = {
  0: 'info',
  1: 'primary',
  2: 'warning',
  3: 'danger',
  4: 'success',
  5: 'primary',
  6: 'primary',
  7: 'danger',
  8: 'primary',
  9: 'danger',
}

const statusOptions = [
  { label: '正常', value: 0 },
  { label: '异常', value: 1 },
]

const fetchList = async () => {
  loading.value = true
  selectedIds.value = []
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.title) params.title = searchForm.value.title
    if (searchForm.value.operName) params.operName = searchForm.value.operName
    if (searchForm.value.businessType !== '') params.businessType = searchForm.value.businessType
    if (searchForm.value.status !== '') params.status = searchForm.value.status
    const { data } = await operlogApi.list(params)
    list.value = data.records
    total.value = data.total
  } catch (error) {
    console.error('获取操作日志失败:', error)
    ElMessage.error('获取操作日志失败')
  } finally {
    loading.value = false
  }
}

const handleSearch = () => {
  currentPage.value = 1
  fetchList()
}

const handleReset = () => {
  searchForm.value = { title: '', operName: '', businessType: '', status: '' }
  handleSearch()
}

const handleSelectionChange = (rows: OperLog[]) => {
  selectedIds.value = rows.map((r) => r.operId)
}

const handleDelete = async () => {
  if (selectedIds.value.length === 0) {
    ElMessage.warning('请选择要删除的记录')
    return
  }
  try {
    await ElMessageBox.confirm(`确定要删除选中的 ${selectedIds.value.length} 条记录吗?`, '提示', {
      type: 'warning',
    })
    await operlogApi.remove(selectedIds.value)
    ElMessage.success('删除成功')
    fetchList()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('删除失败')
  }
}

const handleClean = async () => {
  try {
    await ElMessageBox.confirm('确定要清空所有操作日志吗?此操作不可恢复!', '警告', {
      confirmButtonText: '确定清空',
      cancelButtonText: '取消',
      type: 'warning',
    })
    await operlogApi.clean()
    ElMessage.success('清空成功')
    fetchList()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('清空失败')
  }
}

const handleDetail = async (row: OperLog) => {
  try {
    const { data } = await operlogApi.getInfo(row.operId)
    detailLog.value = data
    detailVisible.value = true
  } catch (error) {
    console.error('获取日志详情失败:', error)
    ElMessage.error('获取日志详情失败')
  }
}

const handleExportCommand = async (command: string) => {
  exporting.value = true
  try {
    const ext = command === 'json' ? 'json' : 'xlsx'
    const blob = await operlogApi.exportFile(command as 'xlsx' | 'json')
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `操作日志.${ext}`
    link.click()
    window.URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('导出失败')
  } finally {
    exporting.value = false
  }
}

const formatJson = (str: string): string => {
  if (!str) return '-'
  try {
    return JSON.stringify(JSON.parse(str), null, 2)
  } catch {
    return str
  }
}

onMounted(() => fetchList())
</script>

<template>
  <div class="operlog-container">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>操作日志</span>
          <div>
            <el-button
              v-hasPermi="['monitor:operlog:remove']"
              type="danger"
              :icon="Delete"
              :disabled="selectedIds.length === 0"
              @click="handleDelete"
            >删除</el-button>
            <el-button
              v-hasPermi="['monitor:operlog:remove']"
              type="danger"
              :icon="Delete"
              @click="handleClean"
            >清空</el-button>
            <el-dropdown trigger="click" @command="handleExportCommand">
              <el-button :icon="Download" :loading="exporting">
                导出数据<el-icon class="el-icon--right"><ArrowDown /></el-icon>
              </el-button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="xlsx">导出 Excel (.xlsx)</el-dropdown-item>
                  <el-dropdown-item command="json">导出 JSON (.json)</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </div>
      </template>

      <el-form :model="searchForm" inline class="search-form">
        <el-form-item label="系统模块">
          <el-input v-model="searchForm.title" placeholder="请输入模块名" clearable @keyup.enter="handleSearch" />
        </el-form-item>
        <el-form-item label="操作人员">
          <el-input v-model="searchForm.operName" placeholder="请输入操作人员" clearable @keyup.enter="handleSearch" />
        </el-form-item>
        <el-form-item label="业务类型">
          <el-select v-model="searchForm.businessType" placeholder="请选择类型" clearable style="width: 140px">
            <el-option v-for="o in businessTypeOptions" :key="o.value" :label="o.label" :value="o.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.status" placeholder="请选择状态" clearable style="width: 120px">
            <el-option v-for="o in statusOptions" :key="o.value" :label="o.label" :value="o.value" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
          <el-button :icon="Refresh" @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>

      <el-table
        v-loading="loading"
        :data="list"
        border
        @selection-change="handleSelectionChange"
      >
        <el-table-column type="selection" width="50" align="center" />
        <el-table-column prop="title" label="操作模块" min-width="120" show-overflow-tooltip />
        <el-table-column label="业务类型" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="businessTypeTagTypes[row.businessType] ?? 'info'">
              {{ businessTypeLabels[row.businessType] ?? '-' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="请求方式" width="100" align="center">
          <template #default="{ row }">
            <el-tag type="info">{{ row.requestMethod }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="operName" label="操作人员" min-width="100" />
        <el-table-column prop="deptName" label="部门" min-width="120" show-overflow-tooltip />
        <el-table-column prop="operUrl" label="操作地址" min-width="200" show-overflow-tooltip />
        <el-table-column prop="operIp" label="操作 IP" min-width="130" />
        <el-table-column label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.status === 0 ? 'success' : 'danger'">
              {{ row.status === 0 ? '正常' : '异常' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="operTime" label="操作时间" min-width="160" />
        <el-table-column label="操作" width="100" align="center" fixed="right">
          <template #default="{ row }">
            <el-button
              v-hasPermi="['monitor:operlog:query']"
              type="primary"
              link
              :icon="View"
              @click="handleDetail(row)"
            >详情</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-container">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="fetchList"
          @current-change="fetchList"
        />
      </div>
    </el-card>

    <el-dialog
      v-model="detailVisible"
      title="操作日志详情"
      width="800px"
      :close-on-click-modal="false"
    >
      <template v-if="detailLog">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="操作模块">{{ detailLog.title }}</el-descriptions-item>
          <el-descriptions-item label="业务类型">{{ businessTypeLabels[detailLog.businessType] ?? detailLog.businessType }}</el-descriptions-item>
          <el-descriptions-item label="请求方法">{{ detailLog.requestMethod }}</el-descriptions-item>
          <el-descriptions-item label="请求方法名" :span="2">{{ detailLog.method }}</el-descriptions-item>
          <el-descriptions-item label="操作人员">{{ detailLog.operName }}</el-descriptions-item>
          <el-descriptions-item label="部门名称">{{ detailLog.deptName }}</el-descriptions-item>
          <el-descriptions-item label="请求 URL" :span="2">{{ detailLog.operUrl }}</el-descriptions-item>
          <el-descriptions-item label="主机 IP" :span="2">{{ detailLog.operIp }}</el-descriptions-item>
          <el-descriptions-item label="操作时间" :span="2">{{ detailLog.operTime }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="detailLog.status === 0 ? 'success' : 'danger'">
              {{ detailLog.status === 0 ? '正常' : '异常' }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="错误消息">{{ detailLog.errorMsg || '-' }}</el-descriptions-item>
        </el-descriptions>

        <el-collapse style="margin-top: 16px">
          <el-collapse-item title="请求参数" name="param">
            <pre class="json-content">{{ formatJson(detailLog.operParam) }}</pre>
          </el-collapse-item>
          <el-collapse-item title="返回结果" name="result">
            <pre class="json-content">{{ formatJson(detailLog.jsonResult) }}</pre>
          </el-collapse-item>
        </el-collapse>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.operlog-container {
  padding: 16px;
}
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.card-header > div {
  display: flex;
  gap: 8px;
}
.search-form {
  margin-bottom: 16px;
}
.pagination-container {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
.json-content {
  background: #f5f7fa;
  padding: 12px;
  border-radius: 4px;
  max-height: 300px;
  overflow: auto;
  font-family: 'Consolas', 'Monaco', monospace;
  font-size: 13px;
  margin: 0;
  white-space: pre-wrap;
  word-break: break-all;
}
</style>
```

- [ ] **步骤 7.4：构建验证**

```bash
cd frontend && npm run build
```

预期：构建成功。

- [ ] **步骤 7.5：手动验证**

`npm run dev` → 访问 `/system/monitor/operlog`：
- 列表渲染、分页正常
- 搜索/重置生效
- 多选删除、清空、详情按钮按权限显示
- 详情对话框展示所有字段 + 折叠的 JSON 区
- 导出下拉菜单工作（注：若后端未实现 export，会触发 404 + 拦截器提示）

- [ ] **步骤 7.6：Commit**

```bash
cd frontend && git add src/types/monitor/operlog.ts src/api/monitor/operlog.ts src/views/system/monitor/operlog/index.vue && GIT_EDITOR=true git commit -m "feat(monitor): 操作日志页面(RuoYi 化迁移)"
```

---

## 任务 8：删除旧 operation-log 文件

**Commit:** `refactor(monitor): 删除旧 operation-log API/视图/类型`

**文件：**
- 删除：`frontend/src/api/operation-log.ts`
- 删除：`frontend/src/views/log/LogListView.vue`
- 删除：`frontend/src/types/operation-log.ts`
- 删除（若为空）：`frontend/src/views/log/` 目录

- [ ] **步骤 8.1：删除 3 个旧文件**

```bash
cd frontend
rm src/api/operation-log.ts
rm src/views/log/LogListView.vue
rm src/types/operation-log.ts
```

- [ ] **步骤 8.2：删除空的 views/log 目录**

```bash
rmdir src/views/log 2>/dev/null || ls src/views/log/
```

若 `rmdir` 失败（目录非空），先用 `ls` 检查并人工清理。

- [ ] **步骤 8.3：验证构建**

```bash
cd frontend && npm run build
```

预期：构建成功。失败则检查是否还有遗漏的引用（grep `operation-log` / `LogListView` / `views/log`）。

- [ ] **步骤 8.4：Commit**

```bash
cd frontend && git add -A && GIT_EDITOR=true git commit -m "refactor(monitor): 删除旧 operation-log API/视图/类型"
```

---

## 任务 9：5 个 monitor 路由 + MainLayout 系统监控菜单

**Commit:** `feat(router): 5 个 monitor 路由 + MainLayout 系统监控菜单`

**文件：**
- 修改：`frontend/src/router/index.ts`
- 修改：`frontend/src/layouts/MainLayout.vue`

- [ ] **步骤 9.1：修改 `frontend/src/router/index.ts`**

定位 `MainLayout` 子路由中的 `operation-logs` 路由（当前是第 12 条 children），删除它。然后在 `notifications` 路由之后、`operation-logs` 原本位置插入 5 条新路由：

```ts
{
  path: 'system/monitor/server',
  name: 'MonitorServer',
  component: () => import('@/views/system/monitor/server/index.vue'),
  meta: { title: '服务监控' },
},
{
  path: 'system/monitor/cache',
  name: 'MonitorCache',
  component: () => import('@/views/system/monitor/cache/index.vue'),
  meta: { title: '缓存监控' },
},
{
  path: 'system/monitor/logininfor',
  name: 'MonitorLogininfor',
  component: () => import('@/views/system/monitor/logininfor/index.vue'),
  meta: { title: '登录日志' },
},
{
  path: 'system/monitor/online',
  name: 'MonitorOnline',
  component: () => import('@/views/system/monitor/online/index.vue'),
  meta: { title: '在线用户' },
},
{
  path: 'system/monitor/operlog',
  name: 'MonitorOperlog',
  component: () => import('@/views/system/monitor/operlog/index.vue'),
  meta: { title: '操作日志' },
},
```

注意：必须在删除旧路由的同一编辑中插入这些，否则会出现中间状态错误。

- [ ] **步骤 9.2：修改 `frontend/src/layouts/MainLayout.vue` 菜单**

定位 `menuItems` 数组，删除最后一项：

```ts
{ index: '/operation-logs', icon: Tickets, title: '日志管理' },
```

然后在该位置替换为：

```ts
{ index: '/system/monitor/server', icon: Monitor, title: '服务监控' },
{ index: '/system/monitor/cache', icon: Coin, title: '缓存监控' },
{ index: '/system/monitor/logininfor', icon: Histogram, title: '登录日志' },
{ index: '/system/monitor/online', icon: UserFilled, title: '在线用户' },
{ index: '/system/monitor/operlog', icon: Document, title: '操作日志' },
```

同时检查 imports：`Monitor` 已在原数组用（首页菜单也用了 `HomeFilled` 但 `Monitor` 也是从 `@element-plus/icons-vue` 导入的）。需要确认 `Coin` / `Histogram` / `UserFilled` 三个新图标是否在顶部的 `import { ... } from '@element-plus/icons-vue'` 中存在，若无则追加。

期望的完整图标 import：

```ts
import {
  HomeFilled,
  Monitor,
  Goods,
  Connection,
  User,
  Lock,
  OfficeBuilding,
  Document,
  Bell,
  Key,
  SwitchButton,
  Fold,
  Expand,
  ArrowDown,
  Close,
  Coin,
  Histogram,
  UserFilled,
} from '@element-plus/icons-vue'
```

`Tickets` 若不再使用，从 imports 中移除。

- [ ] **步骤 9.3：构建验证**

```bash
cd frontend && npm run build
```

预期：构建成功。

- [ ] **步骤 9.4：手动验证菜单 + 路由**

`npm run dev` → 登录后：
- 侧边栏出现 5 个 monitor 菜单项（服务监控 / 缓存监控 / 登录日志 / 在线用户 / 操作日志）
- 没有"日志管理"旧菜单
- 点击每个菜单项 → 对应 view 正常渲染
- 面包屑正确显示
- 多标签页能正常切换
- 旧的 `/operation-logs` 路径 → 跳到 NotFound

- [ ] **步骤 9.5：Commit**

```bash
cd frontend && git add src/router/index.ts src/layouts/MainLayout.vue && GIT_EDITOR=true git commit -m "feat(router): 5 个 monitor 路由 + MainLayout 系统监控菜单"
```

---

## 自检记录

撰写完成后已自检（2026-07-11）：

### 1. 规格覆盖度

| 规格章节 | 对应任务 |
|---|---|
| §3.4 修改文件（7 个） | 任务 1（auth 三件）/ 任务 2（main + api 拦截）/ 任务 9（router + MainLayout）|
| §3.3 删除文件 | 任务 8 |
| §4.1 ServerView | 任务 3 |
| §4.2 CacheView | 任务 4 |
| §4.3 LogininforView | 任务 5 |
| §4.4 OnlineView | 任务 6 |
| §4.5 OperlogView | 任务 7 |
| §5.1 v-hasPermi | 任务 2 |
| §5.2 user store + auth.ts 切换 | 任务 1 |
| §5.3 axios 错误拦截 | 任务 2 |
| §8 commit 顺序（9 个 commit）| 任务 1-9 严格一一对应 |

### 2. 占位符扫描

无"待定"/"TODO"/"补充细节"/"后续补"等占位词。每个步骤要么是具体代码，要么是具体命令 + 预期输出。

### 3. 类型一致性

| 类型/方法 | 定义位置 | 使用位置 |
|---|---|---|
| `UserInfoResponse` | 任务 1 步骤 1.1 | 任务 1 步骤 1.2（api/auth.ts）、1.3（stores/user.ts）|
| `useUserStore().permissions` | 任务 1 步骤 1.3 | 任务 2 步骤 2.1（hasPermi.ts）|
| `PageResult<T>` | 任务 5 步骤 5.2 注（common.ts）| 任务 5/6/7 各 API |
| `Logininfor` 接口 | 任务 5 步骤 5.1 | 任务 5 步骤 5.3（view）|
| `UserOnline` 接口 | 任务 6 步骤 6.1 | 任务 6 步骤 6.3（view）|
| `OperLog` 接口 | 任务 7 步骤 7.1 | 任务 7 步骤 7.2（api）、7.3（view）|
| `ServerInfo` 接口 | 任务 3 步骤 3.1 | 任务 3 步骤 3.2（api）、3.3（view）|
| `CacheInfo` 接口 | 任务 4 步骤 4.1 | 任务 4 步骤 4.2（api）、4.3（view）|
| `v-hasPermi` 指令 | 任务 2 步骤 2.1 | 任务 2 步骤 2.2（main.ts 注册）、任务 4/5/6/7 view 使用 |
| `getInfo / list / remove / clean / forceLogout / unlock / exportFile` 等 API 方法 | 任务 3-7 API 创建步骤 | 对应 view 的 fetchList 等调用 |
| `businessTypeOptions` 数组 | 任务 7 步骤 7.3 | 同 view 内 `businessTypeLabels` / `businessTypeTagTypes` / 模板渲染 |

### 4. 模糊性检查

- 列表页 `page-size` 统一为 `[10, 20, 50, 100]`（任务 5/6/7 view）
- 删除/清空/强退/解锁统一使用 `ElMessageBox.confirm` + `type: 'warning'`（任务 4/5/6/7）
- `ElMessage.error` 文案统一（任务 1/2/5/6/7）
- `formatValue`（cache）/ `formatJson`（operlog）统一 `JSON.stringify(val, null, 2)`（任务 4/7）
- 自动刷新开关 `autoRefresh` + `interval` 在 server view 中联动（任务 3）
- 权限按钮在 monitor 5 页统一使用 `v-hasPermi="['monitor:xxx:xxx']"` 模式