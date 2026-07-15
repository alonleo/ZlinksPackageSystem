<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Refresh, Delete } from '@element-plus/icons-vue'
import { cacheApi } from '@/api/monitor/cache'
import type { CacheInfo } from '@/types/monitor/cache'

const loading = ref(false)
const cacheInfo = ref<CacheInfo | null>(null)
const cacheNames = ref<string[]>([])
const cacheKeys = ref<string[]>([])
const selectedName = ref('')
const selectedKey = ref('')
const cacheValue = ref<unknown>(null)

const fetchInfo = async () => {
  loading.value = true
  try { const { data } = await cacheApi.getInfo(); cacheInfo.value = data }
  catch { ElMessage.error('获取缓存信息失败') }
  finally { loading.value = false }
}

const fetchNames = async () => {
  try { const { data } = await cacheApi.getNames(); cacheNames.value = data }
  catch { ElMessage.error('获取缓存名称失败') }
}

const handleSelectName = async (name: string) => {
  selectedName.value = name; selectedKey.value = ''; cacheValue.value = null; cacheKeys.value = []
  if (!name) return
  try { const { data } = await cacheApi.getKeys(name); cacheKeys.value = data }
  catch { ElMessage.error('获取缓存键失败') }
}

const handleSelectKey = async (key: string) => {
  selectedKey.value = key; cacheValue.value = null
  if (!key || !selectedName.value) return
  try { const { data } = await cacheApi.getValue(selectedName.value, key); cacheValue.value = data.cacheValue }
  catch { ElMessage.error('获取缓存值失败') }
}

const handleClearCacheAll = async () => {
  try {
    await ElMessageBox.confirm('确定要清空所有缓存吗?', '警告', { confirmButtonText: '确定清空', cancelButtonText: '取消', type: 'warning' })
    await cacheApi.clearCacheAll(); ElMessage.success('清空成功')
    cacheNames.value = []; cacheKeys.value = []; selectedName.value = ''; selectedKey.value = ''; cacheValue.value = null
    await fetchInfo()
  } catch { /* cancelled */ }
}

const handleClearCacheName = async (name: string) => {
  try {
    await ElMessageBox.confirm('确定要清空缓存 [' + name + '] 下的所有键吗?', '提示', { type: 'warning' })
    await cacheApi.clearCacheName(name); ElMessage.success('清空成功')
    cacheKeys.value = []; selectedKey.value = ''; cacheValue.value = null
    if (selectedName.value === name) handleSelectName('')
    await fetchNames()
  } catch { /* cancelled */ }
}

const handleClearCacheKey = async (key: string) => {
  try {
    await ElMessageBox.confirm('确定要删除缓存键 [' + key + '] 吗?', '提示', { type: 'warning' })
    await cacheApi.clearCacheKey(key); ElMessage.success('删除成功')
    if (selectedKey.value === key) { selectedKey.value = ''; cacheValue.value = null }
    if (selectedName.value) handleSelectName(selectedName.value)
  } catch { /* cancelled */ }
}

const formatValue = (val: unknown): string => {
  if (val === null || val === undefined) return '-'
  if (typeof val === 'string') return val
  try { return JSON.stringify(val, null, 2) } catch { return String(val) }
}

onMounted(async () => { await Promise.all([fetchInfo(), fetchNames()]) })
</script>

<template>
  <div class="cache-container">
    <el-row :gutter="12">
      <el-col :span="8">
        <el-card v-loading="loading">
          <template #header>
            <div class="card-header">
              <span>缓存名称</span>
              <div style="display:flex;gap:8px">
                <el-button :icon="Refresh" size="small" @click="fetchNames">刷新</el-button>
                <el-button v-hasPermi="['monitor:cache:list']" type="danger" :icon="Delete" size="small" @click="handleClearCacheAll">清空全部</el-button>
              </div>
            </div>
          </template>
          <el-table :data="cacheNames.map(n => ({ name: n }))" highlight-current-row height="500" @row-click="(row: any) => handleSelectName(row.name)">
            <el-table-column label="缓存名称" min-width="200" show-overflow-tooltip>
              <template #default="{ row }">{{ row }}</template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>
      <el-col :span="16">
        <el-card>
          <template #header>
            <div class="card-header">
              <span>缓存键 {{ selectedName ? '(当前: ' + selectedName + ')' : '' }}</span>
              <el-button v-if="selectedName" v-hasPermi="['monitor:cache:list']" type="danger" :icon="Delete" size="small" @click="handleClearCacheName(selectedName)">清空该缓存</el-button>
            </div>
          </template>
          <el-table :data="cacheKeys.map(k => ({ key: k }))" highlight-current-row height="240" @row-click="(row: any) => handleSelectKey(row.key)">
            <el-table-column label="键" min-width="100%">
              <template #default="{ row }"><span>{{ row.key }}</span></template>
            </el-table-column>
            <el-table-column label="操作" width="120" align="center">
              <template #default="{ row }">
                <el-button v-hasPermi="['monitor:cache:remove']" type="danger" link :icon="Delete" @click.stop="handleClearCacheKey(row.key)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
          <div v-if="selectedKey" class="value-panel">
            <div class="value-header">
              <span><strong>键:</strong> {{ selectedKey }}</span>
              <el-button size="small" @click="handleSelectKey(selectedKey)">刷新</el-button>
            </div>
            <pre class="value-content">{{ formatValue(cacheValue) }}</pre>
          </div>
        </el-card>
      </el-col>
    </el-row>
    <el-card v-if="cacheInfo" style="margin-top:12px">
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
.cache-container { padding: 16px; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
.value-panel { margin-top: 8px; }
.value-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; padding: 0 4px; }
.value-content { background: #f5f7fa; padding: 12px; border-radius: 4px; max-height: 300px; overflow: auto; font-family: Consolas, Monaco, monospace; font-size: 13px; margin: 0; white-space: pre-wrap; word-break: break-all; }
</style>