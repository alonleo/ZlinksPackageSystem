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

const searchForm = ref({ userName: '', ipaddr: '', status: '' })
const statusOptions = [{ label: '成功', value: '0' }, { label: '失败', value: '1' }]

const fetchList = async () => {
  loading.value = true; selectedIds.value = []
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.userName) params.userName = searchForm.value.userName
    if (searchForm.value.ipaddr) params.ipaddr = searchForm.value.ipaddr
    if (searchForm.value.status) params.status = searchForm.value.status
    const { data } = await logininforApi.list(params)
    list.value = data.records; total.value = data.total
  } catch { ElMessage.error('获取登录日志失败') }
  finally { loading.value = false }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => { searchForm.value = { userName: '', ipaddr: '', status: '' }; handleSearch() }
const handleSelectionChange = (rows: any[]) => { selectedIds.value = rows.map((r: any) => r.infoId) }

const handleDelete = async () => {
  if (selectedIds.value.length === 0) { ElMessage.warning('请选择要删除的记录'); return }
  try {
    await ElMessageBox.confirm('确定要删除选中的 ' + selectedIds.value.length + ' 条记录吗?', '提示', { type: 'warning' })
    await logininforApi.remove(selectedIds.value)
    ElMessage.success('删除成功')
    fetchList()
  } catch { /* cancelled */ }
}

const handleClean = async () => {
  try {
    await ElMessageBox.confirm('确定要清空所有登录日志吗?', '警告', { confirmButtonText: '确定清空', cancelButtonText: '取消', type: 'warning' })
    await logininforApi.clean()
    ElMessage.success('清空成功')
    fetchList()
  } catch { /* cancelled */ }
}

const handleUnlock = async (row: any) => {
  try {
    await ElMessageBox.confirm('确定要解锁用户 [' + row.userName + '] 吗?', '提示', { type: 'warning' })
    await logininforApi.unlock(row.userName)
    ElMessage.success('解锁指令已发送')
  } catch { /* cancelled */ }
}

const getStatusType = (s: string): 'success' | 'danger' => s === '0' ? 'success' : 'danger'
const getStatusLabel = (s: string): string => s === '0' ? '成功' : '失败'

onMounted(() => fetchList())
</script>

<template>
  <div class="logininfor-container">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>登录日志</span>
          <div class="card-header-actions">
            <el-button v-hasPermi="['monitor:logininfor:remove']" type="danger" :icon="Delete" :disabled="selectedIds.length === 0" @click="handleDelete">删除</el-button>
            <el-button v-hasPermi="['monitor:logininfor:remove']" type="danger" :icon="Delete" @click="handleClean">清空</el-button>
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
      <el-table v-loading="loading" :data="list" border @selection-change="handleSelectionChange">
        <el-table-column type="selection" width="50" align="center" />
        <el-table-column prop="userName" label="用户名称" min-width="120" show-overflow-tooltip />
        <el-table-column prop="ipaddr" label="登录地址" min-width="130" />
        <el-table-column prop="loginLocation" label="登录地点" min-width="120" show-overflow-tooltip />
        <el-table-column prop="browser" label="浏览器" min-width="100" show-overflow-tooltip />
        <el-table-column prop="os" label="操作系统" min-width="100" show-overflow-tooltip />
        <el-table-column label="登录状态" width="90" align="center">
          <template #default="{ row }"><el-tag :type="getStatusType(row.status)">{{ getStatusLabel(row.status) }}</el-tag></template>
        </el-table-column>
        <el-table-column prop="msg" label="消息" min-width="160" show-overflow-tooltip />
        <el-table-column prop="loginTime" label="登录时间" min-width="160" />
        <el-table-column label="操作" width="100" align="center" fixed="right">
          <template #default="{ row }">
            <el-button v-hasPermi="['monitor:logininfor:unlock']" type="primary" link :icon="Unlock" @click="handleUnlock(row)">解锁</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination-container">
        <el-pagination v-model:current-page="currentPage" v-model:page-size="pageSize" :page-sizes="[10, 20, 50, 100]" :total="total" layout="total, sizes, prev, pager, next, jumper" @size-change="fetchList" @current-change="fetchList" />
      </div>
    </el-card>
  </div>
</template>

<style scoped>
.logininfor-container { padding: 16px; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
.card-header-actions { display: flex; gap: 8px; }
.search-form { margin-bottom: 16px; }
.pagination-container { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>