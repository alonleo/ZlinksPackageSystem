<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Delete } from '@element-plus/icons-vue'
import { jobLogApi } from '@/api/monitor/jobLog'
import type { SysJobLog } from '@/types/monitor/job'

const route = useRoute()
const loading = ref(false)
const list = ref<SysJobLog[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const selectedIds = ref<number[]>([])

const searchForm = ref({ jobName: '', jobGroup: '', status: '' })
const statusOptions = [{ label: '正常', value: '0' }, { label: '失败', value: '1' }]

const fetchList = async () => {
  loading.value = true; selectedIds.value = []
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.jobName) params.jobName = searchForm.value.jobName
    if (searchForm.value.jobGroup) params.jobGroup = searchForm.value.jobGroup
    if (searchForm.value.status) params.status = searchForm.value.status
    if (route.params.jobId) params.jobName = String(route.params.jobId)
    const { data } = await jobLogApi.list(params)
    list.value = data.records; total.value = data.total
  } catch { ElMessage.error('获取调度日志失败') }
  finally { loading.value = false }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => { searchForm.value = { jobName: '', jobGroup: '', status: '' }; handleSearch() }
const handleSelectionChange = (rows: any[]) => { selectedIds.value = rows.map((r: any) => r.jobLogId).filter(Boolean) as number[] }

const handleDelete = async () => {
  if (selectedIds.value.length === 0) { ElMessage.warning('请选择要删除的记录'); return }
  try {
    await ElMessageBox.confirm('确定要删除选中的记录吗?', '提示', { type: 'warning' })
    await jobLogApi.remove(selectedIds.value)
    ElMessage.success('删除成功')
    fetchList()
  } catch { /* cancelled */ }
}

const handleClean = async () => {
  try {
    await ElMessageBox.confirm('确定要清空所有调度日志吗?', '警告', { confirmButtonText: '确定清空', cancelButtonText: '取消', type: 'warning' })
    await jobLogApi.clean()
    ElMessage.success('清空成功')
    fetchList()
  } catch { /* cancelled */ }
}

const getStatusType = (s: string): 'success' | 'danger' => s === '0' ? 'success' : 'danger'

onMounted(() => fetchList())
</script>

<template>
  <div class="job-log-container">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>调度日志</span>
          <div class="card-header-actions">
            <el-button v-hasPermi="['monitor:job:remove']" type="danger" :icon="Delete" :disabled="selectedIds.length === 0" @click="handleDelete">删除</el-button>
            <el-button v-hasPermi="['monitor:job:remove']" type="danger" :icon="Delete" @click="handleClean">清空</el-button>
          </div>
        </div>
      </template>
      <el-form :model="searchForm" inline class="search-form">
        <el-form-item label="任务名称"><el-input v-model="searchForm.jobName" placeholder="请输入" clearable @keyup.enter="handleSearch" /></el-form-item>
        <el-form-item label="任务组名">
          <el-select v-model="searchForm.jobGroup" placeholder="请选择" clearable style="width:140px">
            <el-option v-for="g in ['SYSTEM', 'DEFAULT']" :key="g" :label="g" :value="g" />
          </el-select>
        </el-form-item>
        <el-form-item label="执行状态">
          <el-select v-model="searchForm.status" placeholder="请选择" clearable style="width:120px">
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
        <el-table-column prop="jobName" label="任务名称" min-width="120" show-overflow-tooltip />
        <el-table-column prop="jobGroup" label="任务组名" width="100" />
        <el-table-column prop="invokeTarget" label="调用目标字符串" min-width="200" show-overflow-tooltip />
        <el-table-column prop="cronExpression" label="cron 表达式" min-width="140" show-overflow-tooltip />
        <el-table-column label="状态" width="80" align="center">
          <template #default="{ row }"><el-tag :type="getStatusType(row.status)">{{ row.status === '0' ? '正常' : '失败' }}</el-tag></template>
        </el-table-column>
        <el-table-column prop="jobMessage" label="消息" min-width="160" show-overflow-tooltip />
        <el-table-column prop="startTime" label="开始时间" min-width="160" />
        <el-table-column prop="endTime" label="结束时间" min-width="160" />
      </el-table>
      <div class="pagination-container">
        <el-pagination v-model:current-page="currentPage" v-model:page-size="pageSize" :page-sizes="[10, 20, 50, 100]" :total="total" layout="total, sizes, prev, pager, next, jumper" @size-change="fetchList" @current-change="fetchList" />
      </div>
    </el-card>
  </div>
</template>

<style scoped>
.job-log-container { padding: 16px; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
.card-header-actions { display: flex; gap: 8px; }
.search-form { margin-bottom: 16px; }
.pagination-container { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>