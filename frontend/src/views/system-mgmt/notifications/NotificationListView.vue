<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete, Upload, Download, ArrowDown } from '@element-plus/icons-vue'
import { notificationApi } from '@/api/notification'
import type { Notification, NotificationRequest } from '@/types/notification'

const loading = ref(false)
const list = ref<Notification[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const open = ref(false)
const title = ref('')
const fileInput = ref<HTMLInputElement>()
const importing = ref(false)
const exporting = ref(false)
const ids = ref<number[]>([])
const single = ref(true)
const multiple = ref(true)

const searchForm = ref({ title: '', module: '', status: '' })

const form = reactive<NotificationRequest>({
  title: '',
  content: '',
  module: '',
  targetId: undefined,
  targetType: '',
  receiverIds: [],
  receiverType: '',
  isPinned: 0,
  status: 'unread'
})

const rules = {
  title: [{ required: true, message: '请输入通知标题', trigger: 'blur' }]
}

const moduleOptions = [
  { label: '游戏管理', value: 'game' },
  { label: '产品管理', value: 'product' },
  { label: '平台管理', value: 'platform' },
  { label: '公司管理', value: 'company' },
  { label: '软著管理', value: 'copyright' },
  { label: '签名管理', value: 'sign-file' },
  { label: '权限管理', value: 'permission' },
  { label: '用户管理', value: 'user' },
  { label: '工具库', value: 'tool' }
]

const statusOptions = [
  { label: '未读', value: 'unread' },
  { label: '已读', value: 'read' }
]

const receiverTypeOptions = [
  { label: '全部用户', value: 'all' },
  { label: '指定用户', value: 'user' },
  { label: '权限组', value: 'group' }
]

const fetchList = async () => {
  loading.value = true
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.title) params.title = searchForm.value.title
    if (searchForm.value.module) params.module = searchForm.value.module
    if (searchForm.value.status) params.status = searchForm.value.status
    const { data } = await notificationApi.getList(params as any)
    list.value = data.records
    total.value = data.total
  } catch {
    ElMessage.error('获取通知列表失败')
  } finally {
    loading.value = false
  }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => {
  searchForm.value = { title: '', module: '', status: '' }
  handleSearch()
}

const handleSelectionChange = (rows: Notification[]) => {
  ids.value = rows.map((r) => r.id).filter(Boolean) as number[]
  single.value = rows.length !== 1
  multiple.value = !rows.length
}

const resetForm = () => {
  form.title = ''
  form.content = ''
  form.module = ''
  form.targetId = undefined
  form.targetType = ''
  form.receiverIds = []
  form.receiverType = ''
  form.isPinned = 0
  form.status = 'unread'
}

const handleAdd = () => { resetForm(); title.value = '发送通知'; open.value = true }

const handleUpdate = async (row?: Notification) => {
  const id = row?.id ?? ids.value[0]
  if (!id) return
  try {
    const { data } = await notificationApi.getById(id)
    Object.assign(form, {
      title: data.title,
      content: data.content,
      module: data.module,
      targetId: data.targetId ?? undefined,
      targetType: data.targetType,
      receiverIds: [],
      receiverType: data.receiverType,
      isPinned: data.isPinned,
      status: data.status
    })
    title.value = '编辑通知'
    open.value = true
  } catch {
    ElMessage.error('获取通知详情失败')
  }
}

const handleSubmit = async () => {
  try {
    if (form.title == null || form.title === '') {
      ElMessage.error('请输入通知标题')
      return
    }
    const id = (form as any).id
    if (id) {
      await notificationApi.update(id, form)
    } else {
      await notificationApi.create(form)
    }
    ElMessage.success(id ? '修改成功' : '发送成功')
    open.value = false
    fetchList()
  } catch {
    ElMessage.error('操作失败')
  }
}

const handleDelete = async (row?: Notification) => {
  const idsToDelete: number[] = row?.id ? [row.id] : ids.value
  if (!idsToDelete.length) return
  try {
    await ElMessageBox.confirm(`是否确认删除选中的 ${idsToDelete.length} 条通知?`, '提示', { type: 'warning' })
    for (const id of idsToDelete) await notificationApi.delete(id)
    ElMessage.success('删除成功')
    fetchList()
  } catch (e: any) {
    if (e !== 'cancel') ElMessage.error('删除失败')
  }
}

const handleImport = () => fileInput.value?.click()
const onFileChange = async (e: Event) => {
  const target = e.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return
  importing.value = true
  try {
    const { data } = await notificationApi.importFile(file)
    ElMessage.success(data || '导入成功')
    fetchList()
  } catch {
    ElMessage.error('导入失败')
  } finally {
    importing.value = false
    target.value = ''
  }
}

const handleExport = async (format: 'xlsx' | 'json' = 'xlsx') => {
  exporting.value = true
  try {
    const blob = await notificationApi.exportFile(format)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `通知数据.${format}`
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch {
    ElMessage.error('导出失败')
  } finally {
    exporting.value = false
  }
}

const handleDownloadTemplate = async (format: 'xlsx' | 'json' = 'xlsx') => {
  try {
    const blob = await notificationApi.downloadTemplate(format)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `通知导入模板.${format}`
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success('模板下载成功')
  } catch {
    ElMessage.error('模板下载失败')
  }
}

onMounted(fetchList)
</script>

<template>
  <div class="app-container">
    <el-form :model="searchForm" :inline="true" @submit.prevent="handleSearch">
      <el-form-item label="标题">
        <el-input v-model="searchForm.title" placeholder="请输入通知标题" clearable @keyup.enter="handleSearch" />
      </el-form-item>
      <el-form-item label="模块">
        <el-select v-model="searchForm.module" placeholder="请选择模块" clearable style="width: 160px">
          <el-option v-for="o in moduleOptions" :key="o.value" :label="o.label" :value="o.value" />
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

    <el-row :gutter="10" class="mb8">
      <el-col :span="1.5">
        <el-button v-hasPermi="'notification:add'" type="primary" plain :icon="Plus" @click="handleAdd">发送通知</el-button>
      </el-col>
      <el-col :span="1.5">
        <el-button v-hasPermi="'notification:edit'" type="success" plain :icon="Edit" :disabled="single" @click="handleUpdate()">修改</el-button>
      </el-col>
      <el-col :span="1.5">
        <el-button v-hasPermi="'notification:remove'" type="danger" plain :icon="Delete" :disabled="multiple" @click="handleDelete()">删除</el-button>
      </el-col>
      <el-col :span="1.5">
        <el-button v-hasPermi="'notification:import'" type="info" plain :icon="Upload" :loading="importing" @click="handleImport">导入</el-button>
        <input ref="fileInput" type="file" accept=".xlsx,.json" style="display:none" @change="onFileChange" />
      </el-col>
      <el-col :span="1.5">
        <el-dropdown v-hasPermi="'notification:export'" trigger="click" @command="handleExport">
          <el-button type="warning" plain :icon="Download" :loading="exporting">
            导出<el-icon class="el-icon--right"><ArrowDown /></el-icon>
          </el-button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="xlsx">导出为 Excel</el-dropdown-item>
              <el-dropdown-item command="json">导出为 JSON</el-dropdown-item>
              <el-dropdown-item divided @click="handleDownloadTemplate('xlsx')">下载 Excel 模板</el-dropdown-item>
              <el-dropdown-item @click="handleDownloadTemplate('json')">下载 JSON 模板</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </el-col>
    </el-row>

    <el-table v-loading="loading" :data="list" border row-key="id" @selection-change="handleSelectionChange">
      <el-table-column type="selection" width="50" align="center" />
      <el-table-column prop="id" label="ID" width="70" />
      <el-table-column prop="title" label="标题" min-width="180" show-overflow-tooltip />
      <el-table-column prop="module" label="模块" width="120" align="center" />
      <el-table-column label="发送人" width="120" align="center">
        <template #default="{ row }">{{ row.senderName || row.senderId || '-' }}</template>
      </el-table-column>
      <el-table-column label="接收人" min-width="180" show-overflow-tooltip>
        <template #default="{ row }">
          <span v-if="row.receiverType === 'all'">全部用户</span>
          <span v-else>{{ row.receiverNames || '-' }}</span>
        </template>
      </el-table-column>
      <el-table-column label="置顶" width="70" align="center">
        <template #default="{ row }">
          <el-tag v-if="row.isPinned === 1" type="warning">置顶</el-tag>
          <span v-else>-</span>
        </template>
      </el-table-column>
      <el-table-column label="状态" width="80" align="center">
        <template #default="{ row }">
          <el-tag :type="row.status === 'read' ? 'success' : 'info'">
            {{ row.status === 'read' ? '已读' : '未读' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="createTime" label="创建时间" width="170" align="center" />
      <el-table-column label="操作" width="160" align="center" fixed="right">
        <template #default="{ row }">
          <el-button v-hasPermi="'notification:edit'" link type="primary" :icon="Edit" @click="handleUpdate(row as Notification)">修改</el-button>
          <el-button v-hasPermi="'notification:remove'" link type="primary" :icon="Delete" @click="handleDelete(row as Notification)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-pagination
      v-model:current-page="currentPage"
      v-model:page-size="pageSize"
      :total="total"
      :page-sizes="[10, 20, 50, 100]"
      layout="total, sizes, prev, pager, next, jumper"
      style="margin-top: 16px; justify-content: flex-end"
      @current-change="fetchList"
      @size-change="fetchList"
    />

    <el-dialog v-model="open" :title="title" width="640px" append-to-body>
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="标题" prop="title">
          <el-input v-model="form.title" placeholder="请输入通知标题" />
        </el-form-item>
        <el-form-item label="内容">
          <el-input v-model="form.content" type="textarea" :rows="4" placeholder="请输入通知内容" />
        </el-form-item>
        <el-form-item label="模块">
          <el-select v-model="form.module" placeholder="请选择模块" clearable style="width: 100%">
            <el-option v-for="o in moduleOptions" :key="o.value" :label="o.label" :value="o.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="接收类型">
          <el-radio-group v-model="form.receiverType">
            <el-radio v-for="o in receiverTypeOptions" :key="o.value" :value="o.value">{{ o.label }}</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item v-if="form.receiverType && form.receiverType !== 'all'" label="接收人ID">
          <el-input :model-value="(form.receiverIds ?? []).join(',')" placeholder="多个 ID 用逗号分隔,后端转 JSON 数组" @update:model-value="(v: string) => form.receiverIds = v.split(',').map(Number).filter(n => !isNaN(n))" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="form.status" placeholder="请选择状态" style="width: 100%">
            <el-option v-for="o in statusOptions" :key="o.value" :label="o.label" :value="o.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="置顶">
          <el-switch v-model="form.isPinned" :active-value="1" :inactive-value="0" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="open = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>
