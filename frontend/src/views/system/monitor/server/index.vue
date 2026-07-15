<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { serverApi } from '@/api/monitor/server'
import type { ServerInfo } from '@/types/server'
import { ElMessage } from 'element-plus'
import {
  Cpu,
  Tickets,
  Monitor,
  CoffeeCup,
  MessageBox,
  Refresh,
} from '@element-plus/icons-vue'

const loading = ref(false)
const data = ref<ServerInfo | null>(null)
const autoRefresh = ref(true)
const refreshTimer = ref<number | null>(null)

interface MemRow {
  label: string
  mem: string
  jvm: string
  danger: boolean
}

const memRows = computed<MemRow[]>(() => {
  const d = data.value
  if (!d) {
    return [
      { label: '总内存', mem: '-', jvm: '-', danger: false },
      { label: '已用内存', mem: '-', jvm: '-', danger: true },
      { label: '剩余内存', mem: '-', jvm: '-', danger: false },
      { label: '使用率', mem: '-', jvm: '-', danger: true },
    ]
  }
  return [
    { label: '总内存', mem: d.mem.total + ' G', jvm: d.jvm.total + ' M', danger: false },
    { label: '已用内存', mem: d.mem.used + ' G', jvm: d.jvm.used + ' M', danger: true },
    { label: '剩余内存', mem: d.mem.free + ' G', jvm: d.jvm.free + ' M', danger: false },
    {
      label: '使用率',
      mem: d.mem.usage + '%',
      jvm: d.jvm.usage + '%',
      danger: true,
    },
  ]
})

const fetchData = async () => {
  loading.value = true
  try {
    const res = await serverApi.getInfo()
    data.value = res.data
  } catch (err) {
    console.error('获取服务监控信息失败:', err)
    ElMessage.error('获取服务监控信息失败')
  } finally {
    loading.value = false
  }
}

const handleRefresh = () => fetchData()

const toggleAutoRefresh = () => {
  if (autoRefresh.value) {
    startAutoRefresh()
  } else {
    stopAutoRefresh()
  }
}

const startAutoRefresh = () => {
  stopAutoRefresh()
  refreshTimer.value = window.setInterval(fetchData, 10000)
}

const stopAutoRefresh = () => {
  if (refreshTimer.value !== null) {
    clearInterval(refreshTimer.value)
    refreshTimer.value = null
  }
}

const formatStartTime = (start: number | undefined) => {
  if (!start) return '-'
  return new Date(start).toLocaleString('zh-CN', { hour12: false })
}

const formatRunTime = (ms: number | undefined) => {
  if (!ms || ms < 0) return '-'
  const total = Math.floor(ms / 1000)
  const days = Math.floor(total / 86400)
  const hours = Math.floor((total % 86400) / 3600)
  const mins = Math.floor((total % 3600) / 60)
  const secs = total % 60
  return days + '天 ' + hours + '时 ' + mins + '分 ' + secs + '秒'
}

const cpuUsage = computed(() => data.value?.cpu.used ?? 0)
const memUsage = computed(() => data.value?.mem.usage ?? 0)
const jvmUsage = computed(() => data.value?.jvm.usage ?? 0)

const usageColor = (usage: number): string => {
  if (usage >= 90) return '#f56c6c'
  if (usage >= 75) return '#e6a23c'
  return '#67c23a'
}

onMounted(() => {
  fetchData()
  if (autoRefresh.value) startAutoRefresh()
})

onUnmounted(stopAutoRefresh)
</script>

<template>
  <div class="server-monitor-container" v-loading="loading">
    <el-card class="header-card">
      <div class="header-inner">
        <div class="title-block">
          <span class="title">服务监控</span>
          <span class="subtitle">实时查看服务器 CPU / 内存 / 磁盘 / JVM 状态</span>
        </div>
        <div class="actions">
          <el-switch
            v-model="autoRefresh"
            active-text="自动刷新"
            @change="toggleAutoRefresh"
          />
          <el-button type="primary" :icon="Refresh" @click="handleRefresh">
            刷新
          </el-button>
        </div>
      </div>
    </el-card>

    <el-row :gutter="12" class="card-row">
      <el-col :xs="24" :md="12">
        <el-card shadow="hover" class="info-card">
          <template #header>
            <div class="card-header">
              <el-icon class="card-icon"><Cpu /></el-icon>
              <span>CPU</span>
              <el-tag
                :color="usageColor(cpuUsage)"
                effect="dark"
                size="small"
                class="usage-tag"
              >
                {{ cpuUsage }}%
              </el-tag>
            </div>
          </template>
          <el-descriptions :column="1" border size="default">
            <el-descriptions-item label="核心数">
              {{ data?.cpu.cpuNum ?? '-' }}
            </el-descriptions-item>
            <el-descriptions-item label="用户使用率">
              <span :class="{ 'text-danger': cpuUsage > 80 }">
                {{ data?.cpu.used ?? '-' }}%
              </span>
              <el-progress
                :percentage="cpuUsage"
                :stroke-width="6"
                :color="usageColor(cpuUsage)"
                :show-text="false"
                class="usage-bar"
              />
            </el-descriptions-item>
            <el-descriptions-item label="系统使用率">
              {{ data?.cpu.sys ?? '-' }}%
            </el-descriptions-item>
            <el-descriptions-item label="当前空闲率">
              {{ data?.cpu.free ?? '-' }}%
            </el-descriptions-item>
          </el-descriptions>
        </el-card>
      </el-col>

      <el-col :xs="24" :md="12">
        <el-card shadow="hover" class="info-card">
          <template #header>
            <div class="card-header">
              <el-icon class="card-icon"><Tickets /></el-icon>
              <span>内存</span>
              <el-tag
                :color="usageColor(memUsage)"
                effect="dark"
                size="small"
                class="usage-tag"
              >
                {{ memUsage }}%
              </el-tag>
            </div>
          </template>
          <el-table :data="memRows" border size="small">
            <el-table-column prop="label" label="属性" width="100" />
            <el-table-column label="内存 (GB)">
              <template #default="{ row }">
                <span :class="{ 'text-danger': row.danger && memUsage > 80 }">
                  {{ row.mem }}
                </span>
              </template>
            </el-table-column>
            <el-table-column label="JVM (MB)">
              <template #default="{ row }">
                <span :class="{ 'text-danger': row.danger && jvmUsage > 80 }">
                  {{ row.jvm }}
                </span>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>
    </el-row>

    <el-row :gutter="12" class="card-row">
      <el-col :span="24">
        <el-card shadow="hover" class="info-card">
          <template #header>
            <div class="card-header">
              <el-icon class="card-icon"><Monitor /></el-icon>
              <span>服务器信息</span>
            </div>
          </template>
          <el-descriptions :column="2" border>
            <el-descriptions-item label="服务器名称">
              {{ data?.sys.computerName ?? '-' }}
            </el-descriptions-item>
            <el-descriptions-item label="操作系统">
              {{ data?.sys.osName ?? '-' }}
            </el-descriptions-item>
            <el-descriptions-item label="服务器IP">
              {{ data?.sys.computerIp ?? '-' }}
            </el-descriptions-item>
            <el-descriptions-item label="系统架构">
              {{ data?.sys.osArch ?? '-' }}
            </el-descriptions-item>
          </el-descriptions>
        </el-card>
      </el-col>
    </el-row>

    <el-row :gutter="12" class="card-row">
      <el-col :span="24">
        <el-card shadow="hover" class="info-card">
          <template #header>
            <div class="card-header">
              <el-icon class="card-icon"><CoffeeCup /></el-icon>
              <span>Java 虚拟机信息</span>
            </div>
          </template>
          <el-descriptions :column="2" border>
            <el-descriptions-item label="Java 名称">
              {{ data?.jvm.name ?? '-' }}
            </el-descriptions-item>
            <el-descriptions-item label="Java 版本">
              {{ data?.jvm.version ?? '-' }}
            </el-descriptions-item>
            <el-descriptions-item label="启动时间">
              {{ formatStartTime(data?.jvm.startTime) }}
            </el-descriptions-item>
            <el-descriptions-item label="运行时长">
              {{ formatRunTime(data?.jvm.runTime) }}
            </el-descriptions-item>
            <el-descriptions-item label="供应商">
              {{ data?.jvm.vendor ?? '-' }}
            </el-descriptions-item>
            <el-descriptions-item label="堆使用率">
              <span :class="{ 'text-danger': jvmUsage > 80 }">
                {{ jvmUsage }}%
              </span>
              <el-progress
                :percentage="jvmUsage"
                :stroke-width="6"
                :color="usageColor(jvmUsage)"
                :show-text="false"
                class="usage-bar"
              />
            </el-descriptions-item>
            <el-descriptions-item label="安装路径" :span="2">
              <span class="path-text">{{ data?.jvm.home ?? '-' }}</span>
            </el-descriptions-item>
            <el-descriptions-item label="项目路径" :span="2">
              <span class="path-text">{{ data?.sys.userDir ?? '-' }}</span>
            </el-descriptions-item>
            <el-descriptions-item label="运行参数" :span="2">
              <span class="args-text">{{ data?.jvm.inputArgs || '-' }}</span>
            </el-descriptions-item>
          </el-descriptions>
        </el-card>
      </el-col>
    </el-row>

    <el-row :gutter="12" class="card-row">
      <el-col :span="24">
        <el-card shadow="hover" class="info-card">
          <template #header>
            <div class="card-header">
              <el-icon class="card-icon"><MessageBox /></el-icon>
              <span>磁盘状态</span>
            </div>
          </template>
          <el-table :data="data?.sysFiles ?? []" border size="small">
            <el-table-column prop="dirName" label="盘符路径" min-width="160" show-overflow-tooltip />
            <el-table-column prop="sysTypeName" label="文件系统" width="120" />
            <el-table-column prop="typeName" label="盘符类型" width="140" show-overflow-tooltip />
            <el-table-column label="总大小 (GB)" width="120" align="right">
              <template #default="{ row }">{{ row.total }}</template>
            </el-table-column>
            <el-table-column label="可用大小 (GB)" width="120" align="right">
              <template #default="{ row }">{{ row.free }}</template>
            </el-table-column>
            <el-table-column label="已用大小 (GB)" width="120" align="right">
              <template #default="{ row }">{{ row.used }}</template>
            </el-table-column>
            <el-table-column label="已用百分比" width="220">
              <template #default="{ row }">
                <span :class="{ 'text-danger': row.usage > 80 }" style="margin-right: 8px;">
                  {{ row.usage }}%
                </span>
                <el-progress
                  :percentage="row.usage"
                  :stroke-width="6"
                  :color="usageColor(row.usage)"
                  :show-text="false"
                  class="usage-bar"
                />
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<style scoped>
.server-monitor-container {
  padding: 16px;
}

.header-card {
  margin-bottom: 12px;
}

.header-inner {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
}

.title-block {
  display: flex;
  flex-direction: column;
}

.title {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
}

.subtitle {
  margin-top: 4px;
  font-size: 12px;
  color: #909399;
}

.actions {
  display: flex;
  align-items: center;
  gap: 12px;
}

.card-row {
  margin-bottom: 12px;
}

.card-row:last-child {
  margin-bottom: 0;
}

.info-card {
  margin-bottom: 0;
}

.card-header {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
}

.card-icon {
  font-size: 18px;
  color: #409eff;
}

.usage-tag {
  margin-left: auto;
}

.usage-bar {
  margin-top: 4px;
}

.path-text,
.args-text {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 12px;
  color: #606266;
  word-break: break-all;
}

.text-danger {
  color: #f56c6c;
  font-weight: 600;
}
</style>