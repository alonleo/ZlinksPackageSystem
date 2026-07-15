<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete, CaretRight, Operation } from '@element-plus/icons-vue'
import { jobApi } from '@/api/monitor/job'
import type { SysJob } from '@/types/monitor/job'

const router = useRouter()
const loading = ref(false)
const jobList = ref<SysJob[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const open = ref(false)
const title = ref('')
const ids = ref<number[]>([])
const single = ref(true)
const multiple = ref(true)

const searchForm = ref({ jobName: '', jobGroup: '', status: '' })
const jobGroupOptions = ['SYSTEM', 'DEFAULT']
const statusOptions = [{ label: '正常', value: '0' }, { label: '暂停', value: '1' }]
const misfireOptions = [
  { label: '立即执行', value: '1' },
  { label: '执行一次', value: '2' },
  { label: '放弃执行', value: '3' },
]

const form = reactive<SysJob>({
  jobName: '', jobGroup: 'DEFAULT', invokeTarget: '', cronExpression: '',
  misfirePolicy: '3', concurrent: '1', status: '0',
})

const rules = {
  jobName: [{ required: true, message: '任务名称不能为空', trigger: 'blur' }],
  invokeTarget: [{ required: true, message: '调用目标字符串不能为空', trigger: 'blur' }],
  cronExpression: [{ required: true, message: 'cron执行表达式不能为空', trigger: 'blur' }],
}

const fetchList = async () => {
  loading.value = true
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.jobName) params.jobName = searchForm.value.jobName
    if (searchForm.value.jobGroup) params.jobGroup = searchForm.value.jobGroup
    if (searchForm.value.status) params.status = searchForm.value.status
    const { data } = await jobApi.list(params)
    jobList.value = data.records; total.value = data.total
  } catch { ElMessage.error('获取定时任务列表失败') }
  finally { loading.value = false }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => { searchForm.value = { jobName: '', jobGroup: '', status: '' }; handleSearch() }
const handleSelectionChange = (rows: any[]) => {
  ids.value = rows.map((r: any) => r.jobId).filter(Boolean) as number[]
  single.value = rows.length !== 1; multiple.value = !rows.length
}

const resetForm = () => {
  form.jobName = ''; form.jobGroup = 'DEFAULT'; form.invokeTarget = ''
  form.cronExpression = ''; form.misfirePolicy = '3'; form.concurrent = '1'; form.status = '0'
  form.jobId = undefined
}

const handleAdd = () => { resetForm(); title.value = '添加任务'; open.value = true }
const handleUpdate = async (row?: any) => {
  const jobId = row?.jobId ?? ids.value[0]
  if (!jobId) return
  try {
    const { data } = await jobApi.get(jobId)
    Object.assign(form, data)
    title.value = '修改任务'; open.value = true
  } catch { ElMessage.error('获取任务详情失败') }
}

const handleSubmit = async () => {
  try {
    if (form.jobId) {
      await jobApi.update(form)
      ElMessage.success('修改成功')
    } else {
      await jobApi.add(form)
      ElMessage.success('添加成功')
    }
    open.value = false; fetchList()
  } catch { ElMessage.error('操作失败') }
}

const handleDelete = async (row?: any) => {
  const jobIds = row?.jobId ? [row.jobId] : ids.value
  if (!jobIds.length) return
  try {
    await ElMessageBox.confirm('是否确认删除该定时任务?', '提示', { type: 'warning' })
    await jobApi.remove(jobIds)
    ElMessage.success('删除成功')
    fetchList()
  } catch { /* cancelled */ }
}

const handleStatusChange = async (row: any) => {
  const text = row.status === '0' ? '启用' : '停用'
  try {
    await ElMessageBox.confirm('确认要"' + text + '""' + row.jobName + '"任务吗?', '提示', { type: 'warning' })
    await jobApi.changeStatus(row.jobId!, row.status!)
    ElMessage.success(text + '成功')
  } catch { row.status = row.status === '0' ? '1' : '0' }
}

const handleRun = async (row: any) => {
  try {
    await ElMessageBox.confirm('确认要立即执行一次"' + row.jobName + '"任务吗?', '提示', { type: 'warning' })
    await jobApi.run(row.jobId!, row.jobGroup)
    ElMessage.success('执行成功')
  } catch { /* cancelled */ }
}

const handleJobLog = (row?: any) => {
  router.push('/system/monitor/job/log' + (row?.jobId ? '/' + row.jobId : ''))
}

onMounted(() => fetchList())
</script>

<template>
  <div class="job-container">
    <el-card>
      <el-form :model="searchForm" inline class="search-form">
        <el-form-item label="任务名称">
          <el-input v-model="searchForm.jobName" placeholder="请输入任务名称" clearable @keyup.enter="handleSearch" />
        </el-form-item>
        <el-form-item label="任务组名">
          <el-select v-model="searchForm.jobGroup" placeholder="请选择" clearable style="width:150px">
            <el-option v-for="g in jobGroupOptions" :key="g" :label="g" :value="g" />
          </el-select>
        </el-form-item>
        <el-form-item label="任务状态">
          <el-select v-model="searchForm.status" placeholder="请选择" clearable style="width:120px">
            <el-option v-for="o in statusOptions" :key="o.value" :label="o.label" :value="o.value" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
          <el-button :icon="Refresh" @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>

      <div class="toolbar">
        <el-button v-hasPermi="['monitor:job:add']" type="primary" :icon="Plus" @click="handleAdd">新增</el-button>
        <el-button v-hasPermi="['monitor:job:edit']" type="success" :icon="Edit" :disabled="single" @click="handleUpdate()">修改</el-button>
        <el-button v-hasPermi="['monitor:job:remove']" type="danger" :icon="Delete" :disabled="multiple" @click="handleDelete()">删除</el-button>
        <el-button v-hasPermi="['monitor:job:query']" type="info" :icon="Operation" @click="handleJobLog()">日志</el-button>
      </div>

      <el-table v-loading="loading" :data="jobList" border @selection-change="handleSelectionChange">
        <el-table-column type="selection" width="50" align="center" />
        <el-table-column prop="jobId" label="任务编号" width="100" align="center" />
        <el-table-column prop="jobName" label="任务名称" min-width="120" show-overflow-tooltip />
        <el-table-column prop="jobGroup" label="任务组名" width="100" />
        <el-table-column prop="invokeTarget" label="调用目标字符串" min-width="200" show-overflow-tooltip />
        <el-table-column prop="cronExpression" label="cron 执行表达式" min-width="140" show-overflow-tooltip />
        <el-table-column label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-switch v-model="row.status" active-value="0" inactive-value="1" @change="handleStatusChange(row)" />
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" align="center" fixed="right">
          <template #default="{ row }">
            <el-tooltip content="修改" placement="top">
              <el-button v-hasPermi="['monitor:job:edit']" link type="primary" :icon="Edit" @click="handleUpdate(row)" />
            </el-tooltip>
            <el-tooltip content="删除" placement="top">
              <el-button v-hasPermi="['monitor:job:remove']" link type="danger" :icon="Delete" @click="handleDelete(row)" />
            </el-tooltip>
            <el-tooltip content="执行一次" placement="top">
              <el-button v-hasPermi="['monitor:job:changeStatus']" link type="warning" :icon="CaretRight" @click="handleRun(row)" />
            </el-tooltip>
            <el-tooltip content="调度日志" placement="top">
              <el-button v-hasPermi="['monitor:job:query']" link type="info" :icon="Operation" @click="handleJobLog(row)" />
            </el-tooltip>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-container">
        <el-pagination v-model:current-page="currentPage" v-model:page-size="pageSize" :page-sizes="[10, 20, 50, 100]" :total="total" layout="total, sizes, prev, pager, next, jumper" @size-change="fetchList" @current-change="fetchList" />
      </div>
    </el-card>

    <el-dialog :title="title" v-model="open" width="700px" append-to-body>
      <el-form :model="form" :rules="rules" label-width="130px">
        <el-row>
          <el-col :span="12"><el-form-item label="任务名称" prop="jobName"><el-input v-model="form.jobName" placeholder="请输入任务名称" /></el-form-item></el-col>
          <el-col :span="12">
            <el-form-item label="任务分组">
              <el-select v-model="form.jobGroup" placeholder="请选择" style="width:100%">
                <el-option v-for="g in jobGroupOptions" :key="g" :label="g" :value="g" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="24"><el-form-item label="调用方法" prop="invokeTarget"><el-input v-model="form.invokeTarget" placeholder="请输入 target bean.method('arg')" /></el-form-item></el-col>
          <el-col :span="24"><el-form-item label="cron 表达式" prop="cronExpression"><el-input v-model="form.cronExpression" placeholder="请输入 cron 执行表达式" /></el-form-item></el-col>
          <el-col :span="12">
            <el-form-item label="执行策略"><el-select v-model="form.misfirePolicy" style="width:100%"><el-option v-for="m in misfireOptions" :key="m.value" :label="m.label" :value="m.value" /></el-select></el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="是否并发"><el-select v-model="form.concurrent" style="width:100%"><el-option label="允许" value="0" /><el-option label="禁止" value="1" /></el-select></el-form-item>
          </el-col>
          <el-col v-if="form.jobId" :span="12">
            <el-form-item label="状态"><el-select v-model="form.status" style="width:100%"><el-option v-for="o in statusOptions" :key="o.value" :label="o.label" :value="o.value" /></el-select></el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer><el-button @click="open = false">取消</el-button><el-button type="primary" @click="handleSubmit">确定</el-button></template>
    </el-dialog>
  </div>
</template>

<style scoped>
.job-container { padding: 16px; }
.search-form { margin-bottom: 8px; }
.toolbar { display: flex; gap: 8px; margin-bottom: 12px; }
.pagination-container { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>