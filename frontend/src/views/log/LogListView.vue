<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Search, Refresh, Download, ArrowDown } from '@element-plus/icons-vue'
import { operationLogApi } from '@/api/operation-log'
import type { OperationLog } from '@/types/operation-log'

const loading = ref(false)
const exporting = ref(false)
const list = ref<OperationLog[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)

const searchForm = ref({
  username: '',
  module: '',
  action: '',
})

const moduleOptions = [
  '认证', '用户管理', '游戏管理', '产品管理', '公司管理',
  '签名管理', '软著管理', '通知管理', '权限管理', '日志管理',
]

const actionOptions = ['LOGIN', 'QUERY', 'CREATE', 'UPDATE', 'DELETE', 'IMPORT']

const actionLabels: Record<string, string> = {
  LOGIN: '登录',
  QUERY: '查询',
  CREATE: '创建',
  UPDATE: '更新',
  DELETE: '删除',
  IMPORT: '导入',
}

const getActionType = (a: string): 'success' | 'warning' | 'danger' | 'info' | 'primary' => {
  if (a === 'LOGIN') return 'success'
  if (a === 'CREATE' || a === 'IMPORT') return 'primary'
  if (a === 'UPDATE') return 'warning'
  if (a === 'DELETE') return 'danger'
  return 'info'
}

const fetchList = async () => {
  loading.value = true
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.username) params.username = searchForm.value.username
    if (searchForm.value.module) params.module = searchForm.value.module
    if (searchForm.value.action) params.action = searchForm.value.action
    const { data } = await operationLogApi.getList(params)
    list.value = data.records; total.value = data.total
  } catch {
    ElMessage.error('获取日志列表失败')
  } finally { loading.value = false }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => { searchForm.value = { username: '', module: '', action: '' }; fetchList() }

const handleExportCommand = async (command: string) => {
  if (command !== 'xlsx' && command !== 'json') return
  exporting.value = true
  try {
    const blob = await operationLogApi.exportFile(command as 'xlsx' | 'json')
    const ext = command === 'json' ? 'json' : 'xlsx'
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `操作日志.${ext}`
    link.click()
    window.URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch {
    ElMessage.error('导出失败')
  } finally { exporting.value = false }
}

onMounted(() => fetchList())
</script>

<template>
  <div class="log-container">
    <el-card>
      <el-form :model="searchForm" inline class="search-form">
        <el-form-item label="用户名">
          <el-input v-model="searchForm.username" placeholder="请输入用户名" clearable @keyup.enter="handleSearch" />
        </el-form-item>
        <el-form-item label="模块">
          <el-select v-model="searchForm.module" placeholder="请选择模块" clearable style="width:160px">
            <el-option v-for="m in moduleOptions" :key="m" :label="m" :value="m" />
          </el-select>
        </el-form-item>
        <el-form-item label="操作">
          <el-select v-model="searchForm.action" placeholder="请选择操作" clearable style="width:140px">
            <el-option v-for="a in actionOptions" :key="a" :label="actionLabels[a] || a" :value="a" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
          <el-button :icon="Refresh" @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
      <div class="toolbar">
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
      <el-table v-loading="loading" :data="list" border>
        <el-table-column prop="id" label="编号" width="80" align="center" />
        <el-table-column prop="username" label="用户" min-width="100" show-overflow-tooltip />
        <el-table-column prop="module" label="模块" min-width="120" show-overflow-tooltip />
        <el-table-column label="操作" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="getActionType(row.action)" size="small">{{ actionLabels[row.action] || row.action }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="target" label="目标" min-width="200" show-overflow-tooltip />
        <el-table-column prop="ipAddress" label="IP地址" min-width="130" show-overflow-tooltip />
        <el-table-column prop="createTime" label="操作时间" min-width="160" />
      </el-table>
      <div class="pagination"><el-pagination v-model:current-page="currentPage" v-model:page-size="pageSize" :page-sizes="[10, 20, 50, 100]" :total="total" layout="total, sizes, prev, pager, next, jumper" @size-change="fetchList" @current-change="fetchList" /></div>
    </el-card>
  </div>
</template>

<style scoped>
.log-container { padding: 16px; }
.search-form { margin-bottom: 8px; }
.toolbar { display: flex; gap: 8px; margin-bottom: 12px; }
.pagination { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>