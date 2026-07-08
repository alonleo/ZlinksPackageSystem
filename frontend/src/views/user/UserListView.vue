<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { userApi } from '@/api/user'
import type { User } from '@/types/user'
import type { PermissionGroup } from '@/types/permission-group'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Search, Upload, Download, Document, ArrowDown } from '@element-plus/icons-vue'

const loading = ref(false)
const saving = ref(false)
const deleting = ref(false)
const importing = ref(false)
const exporting = ref(false)
const formRef = ref()
const fileInput = ref<HTMLInputElement>()
const groups = ref<PermissionGroup[]>([])

const form = ref<Partial<User> & { password?: string }>({
  username: '',
  password: '',
  realName: '',
  status: 'active',
  groupIds: [],
  remark: '',
})
const selectedId = ref<number | null>(null)
const isEditing = ref(false)

const list = ref<User[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const searchForm = ref({
  username: '',
  realName: '',
  status: '',
  groupId: undefined as number | undefined,
})

const isNew = computed(() => selectedId.value === null && isEditing.value)

const statusOptions = [
  { label: '启用', value: 'active' },
  { label: '禁用', value: 'disabled' },
]

const rules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
}

const fetchList = async () => {
  loading.value = true
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.username) params.username = searchForm.value.username
    if (searchForm.value.realName) params.realName = searchForm.value.realName
    if (searchForm.value.status) params.status = searchForm.value.status
    if (searchForm.value.groupId) params.groupId = searchForm.value.groupId
    const response = await userApi.getList(params)
    list.value = response.data.records
    total.value = response.data.total
  } catch (error) {
    console.error('获取用户列表失败:', error)
  } finally {
    loading.value = false
  }
}

const fetchGroups = async () => {
  try {
    const response = await userApi.getGroups()
    groups.value = response.data
  } catch (error) {
    console.error('获取权限组列表失败:', error)
  }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => {
  searchForm.value = { username: '', realName: '', status: '', groupId: undefined }
  handleSearch()
}
const handleSizeChange = (val: number) => { pageSize.value = val; fetchList() }
const handleCurrentChange = (val: number) => { currentPage.value = val; fetchList() }

const handleCreate = () => {
  selectedId.value = null
  isEditing.value = true
  form.value = { username: '', password: '', realName: '', status: 'active', groupIds: [], remark: '' }
}

const handleSelect = async (row: User) => {
  selectedId.value = row.id
  isEditing.value = true
  form.value = {
    username: row.username || '',
    password: '',
    realName: row.realName || '',
    status: row.status || 'active',
    groupIds: row.groupIds || [],
    remark: row.remark || '',
  }
}

const getErrorMessage = (error: any): string => {
  return error?.response?.data?.message || error?.message || '操作失败'
}

const handleSave = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid: boolean) => {
    if (valid) {
      if (isNew.value && !form.value.password) {
        ElMessage.warning('请输入密码')
        return
      }
      saving.value = true
      try {
        const data = { ...form.value }
        if (!isNew.value && !data.password) {
          delete data.password
        }
        if (isNew.value) {
          await userApi.create(data)
          ElMessage.success('创建成功')
        } else {
          await userApi.update(selectedId.value!, data)
          ElMessage.success('更新成功')
        }
        handleCancel()
        fetchList()
      } catch (error: any) {
        ElMessage.error(getErrorMessage(error))
      } finally {
        saving.value = false
      }
    }
  })
}

const handleCancel = () => {
  isEditing.value = false
  selectedId.value = null
  form.value = { username: '', password: '', realName: '', status: 'active', groupIds: [], remark: '' }
}

const handleDeleteCurrent = async () => {
  if (!selectedId.value) return
  try {
    await ElMessageBox.confirm(`确定要删除用户"${form.value.username}"吗？`, '提示', {
      confirmButtonText: '确定', cancelButtonText: '取消', type: 'warning',
    })
    deleting.value = true
    await userApi.delete(selectedId.value)
    ElMessage.success('删除成功')
    handleCancel()
    fetchList()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('删除失败')
  } finally {
    deleting.value = false
  }
}

const handleImport = () => {
  fileInput.value?.click()
}

const handleFileChange = async (event: Event) => {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return

  importing.value = true
  try {
    const res = await userApi.importFile(file)
    ElMessage.success(res.data)
    fetchList()
  } catch (error: any) {
    ElMessage.error(error?.response?.data?.message || '导入失败')
  } finally {
    importing.value = false
    target.value = ''
  }
}

const handleExportCommand = async (command: string) => {
  if (command === 'xlsx' || command === 'json') {
    exporting.value = true
    try {
      const ext = command === 'json' ? 'json' : 'xlsx'
      const blob = await userApi.exportFile(command)
      downloadBlob(blob, `用户数据.${ext}`)
      ElMessage.success('导出成功')
    } catch { ElMessage.error('导出失败') }
    finally { exporting.value = false }
  } else if (command === 'template-xlsx') {
    try {
      const blob = await userApi.downloadTemplate('xlsx')
      downloadBlob(blob, '用户导入模板.xlsx')
      ElMessage.success('模板下载成功')
    } catch { ElMessage.error('模板下载失败') }
  }
}

const downloadBlob = (blob: Blob, filename: string) => {
  const url = window.URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  link.click()
  window.URL.revokeObjectURL(url)
}

onMounted(() => {
  fetchList()
  fetchGroups()
})
</script>

<template>
  <div class="user-container">
    <div class="search-card">
      <div class="card-header">
        <span>筛选条件</span>
      </div>
      <el-form :model="searchForm" inline style="margin-top: 12px">
        <el-form-item label="用户名">
          <el-input v-model="searchForm.username" placeholder="请输入用户名" clearable @keyup.enter="handleSearch" />
        </el-form-item>
        <el-form-item label="真实姓名">
          <el-input v-model="searchForm.realName" placeholder="请输入真实姓名" clearable @keyup.enter="handleSearch" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.status" placeholder="请选择状态" clearable>
            <el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="权限组">
          <el-select v-model="searchForm.groupId" placeholder="请选择权限组" clearable>
            <el-option v-for="item in groups" :key="item.id" :label="item.groupName" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
          <el-button @click="handleReset">重置</el-button>
          <el-button type="primary" :icon="Plus" @click="handleCreate">新增用户</el-button>
          <el-button :icon="Upload" :loading="importing" @click="handleImport">导入数据</el-button>
          <el-dropdown trigger="click" @command="handleExportCommand">
            <el-button :icon="Download" :loading="exporting">
              导出数据<el-icon class="el-icon--right"><ArrowDown /></el-icon>
            </el-button>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="xlsx">导出 Excel (.xlsx)</el-dropdown-item>
                <el-dropdown-item command="json">导出 JSON (.json)</el-dropdown-item>
                <el-dropdown-item command="template-xlsx" divided>下载导入模板 (.xlsx)</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </el-form-item>
      </el-form>
    </div>

    <div class="content-row">
      <div class="detail-panel">
        <el-card class="detail-card">
          <template #header>
            <div class="card-header">
              <span>{{ isNew ? '新增用户' : isEditing ? '编辑用户' : '用户详情' }}</span>
              <div v-if="isEditing" class="header-actions">
                <el-button type="primary" :loading="saving" @click="handleSave">保存</el-button>
                <el-button v-if="!isNew" type="danger" :loading="deleting" @click="handleDeleteCurrent">删除</el-button>
                <el-button @click="handleCancel">取消</el-button>
              </div>
            </div>
          </template>
          <div v-if="!isEditing" class="empty-detail">
            <el-icon class="empty-icon"><Document /></el-icon>
            <p>请选择一条记录或点击新增</p>
          </div>
          <el-form v-else ref="formRef" :model="form" :rules="rules" label-width="80px" class="detail-form">
            <el-form-item label="用户名" prop="username">
              <el-input v-model="form.username" placeholder="请输入用户名" />
            </el-form-item>
            <el-form-item label="密码">
              <el-input v-model="form.password" type="password" :placeholder="isNew ? '请输入密码' : '留空则不修改'" show-password />
            </el-form-item>
            <el-form-item label="姓名">
              <el-input v-model="form.realName" placeholder="请输入真实姓名" />
            </el-form-item>
            <el-form-item label="状态">
              <el-select v-model="form.status" style="width: 100%">
                <el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" />
              </el-select>
            </el-form-item>
            <el-form-item label="权限组">
              <el-select v-model="form.groupIds" multiple placeholder="请选择权限组" style="width: 100%">
                <el-option v-for="item in groups" :key="item.id" :label="item.groupName" :value="item.id" />
              </el-select>
            </el-form-item>
            <el-form-item label="备注">
              <el-input v-model="form.remark" type="textarea" :rows="3" placeholder="请输入备注信息" />
            </el-form-item>
          </el-form>
        </el-card>
      </div>

      <div class="list-panel">
        <el-card class="list-card">
          <template #header>
            <span>用户列表</span>
          </template>
          <el-table v-loading="loading" :data="list" border highlight-current-row
            @row-click="handleSelect" style="width: 100%">
            <el-table-column prop="username" label="用户名" min-width="160" />
            <el-table-column prop="remark" label="备注" min-width="200" show-overflow-tooltip />
          </el-table>
          <div class="pagination-wrap">
            <el-pagination
              v-model:current-page="currentPage" v-model:page-size="pageSize"
              :page-sizes="[10, 20, 50, 100]" :total="total"
              layout="total, sizes, prev, pager, next, jumper" small
              @size-change="handleSizeChange" @current-change="handleCurrentChange"
            />
          </div>
        </el-card>
      </div>
    </div>

    <input ref="fileInput" type="file" accept=".xlsx,.xls" style="display: none" @change="handleFileChange" />
  </div>
</template>

<style scoped>
.user-container {
  height: 100%;
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
}

.search-card {
  flex-shrink: 0;
  background: #fff;
  border-radius: 4px;
  padding: 12px 16px;
  box-shadow: 0 1px 4px rgba(0, 21, 41, 0.08);
}

.content-row {
  flex: 1;
  min-height: 0;
  display: flex;
  gap: 16px;
  margin-top: 16px;
}

.detail-panel {
  flex: 3;
  min-width: 380px;
  min-height: 0;
}

.detail-card {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.detail-card :deep(.el-card__body) {
  flex: 1;
  overflow-y: auto;
}

.list-panel {
  flex: 2;
  min-width: 0;
  min-height: 0;
}

.list-card {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.list-card :deep(.el-card__body) {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  padding-bottom: 0;
}

.list-card :deep(.el-table) {
  flex: 1;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.header-actions {
  display: flex;
  gap: 8px;
}

.empty-detail {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: #999;
  gap: 12px;
}

.empty-detail p {
  margin: 0;
  font-size: 14px;
}

.empty-icon {
  font-size: 48px;
  color: #c0c4cc;
}

.detail-form {
  max-width: 100%;
}

.list-card .pagination-wrap {
  padding: 4px 0 0 0;
  display: flex;
  justify-content: flex-end;
  flex-shrink: 0;
}
</style>
