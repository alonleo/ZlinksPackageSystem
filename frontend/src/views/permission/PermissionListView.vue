<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { permissionApi } from '@/api/permission'
import type { PermissionGroup } from '@/types/permission-group'
import type { User } from '@/types/user'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Search, Upload, Download, Delete, Document, ArrowDown } from '@element-plus/icons-vue'

const loading = ref(false)
const saving = ref(false)
const deleting = ref(false)
const importing = ref(false)
const exporting = ref(false)
const formRef = ref()
const fileInput = ref<HTMLInputElement>()

const form = ref({
  groupName: '',
  selectedModules: [] as string[],
  remark: '',
})
const selectedId = ref<number | null>(null)
const isEditing = ref(false)
const prevModules = ref<string[]>([])

const users = ref<User[]>([])
const availableUsers = ref<User[]>([])
const selectedUserId = ref<number | null>(null)

const list = ref<PermissionGroup[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const searchForm = ref({ groupName: '' })

const isNew = computed(() => selectedId.value === null && isEditing.value)

const moduleOptions = [
  { label: '全部模块', value: 'all' },
  { label: '首页', value: 'home' },
  { label: '游戏管理', value: 'games' },
  { label: '产品管理', value: 'products' },
  { label: '测试管理', value: 'tests' },
  { label: '用户管理', value: 'users' },
  { label: '权限管理', value: 'permissions' },
  { label: '公司管理', value: 'companies' },
  { label: '软著管理', value: 'copyrights' },
  { label: '通知管理', value: 'notifications' },
]

const rules = {
  groupName: [{ required: true, message: '请输入权限组名称', trigger: 'blur' }],
}

const fetchList = async () => {
  loading.value = true
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.groupName) params.groupName = searchForm.value.groupName
    const response = await permissionApi.getList(params)
    list.value = response.data.records
    total.value = response.data.total
  } catch (error) {
    console.error('获取权限组列表失败:', error)
  } finally {
    loading.value = false
  }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => { searchForm.value = { groupName: '' }; handleSearch() }
const handleSizeChange = (val: number) => { pageSize.value = val; fetchList() }
const handleCurrentChange = (val: number) => { currentPage.value = val; fetchList() }

const handleCreate = () => {
  selectedId.value = null
  isEditing.value = true
  form.value = { groupName: '', selectedModules: [], remark: '' }
  prevModules.value = []
  users.value = []
  availableUsers.value = []
}

const handleSelect = async (row: PermissionGroup) => {
  selectedId.value = row.id
  isEditing.value = true
  form.value.groupName = row.groupName
  try {
    const config = JSON.parse(row.groupPermission)
    form.value.selectedModules = config.modules || []
  } catch {
    form.value.selectedModules = []
  }
  prevModules.value = [...form.value.selectedModules]
  form.value.remark = row.remark || ''
  await fetchUsers()
}

const fetchUsers = async () => {
  if (!selectedId.value) return
  try {
    const [usersRes, availableRes] = await Promise.all([
      permissionApi.getUsers(selectedId.value),
      permissionApi.getAvailableUsers(selectedId.value),
    ])
    users.value = usersRes.data
    availableUsers.value = availableRes.data
  } catch (error) {
    console.error('获取用户列表失败:', error)
  }
}

const handleModuleChange = (values: unknown[]) => {
  const strValues = values.map(String)
  const hadAll = prevModules.value.includes('all')
  const hasAll = strValues.includes('all')

  if (!hadAll && hasAll) {
    form.value.selectedModules = moduleOptions.map(m => m.value)
  } else if (hadAll && !hasAll) {
    form.value.selectedModules = []
  } else if (hasAll && strValues.length < moduleOptions.length) {
    form.value.selectedModules = strValues.filter(v => v !== 'all')
  }

  prevModules.value = [...form.value.selectedModules]
}

const getErrorMessage = (error: any): string => {
  return error?.response?.data?.message || error?.message || '操作失败'
}

const handleSave = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid: boolean) => {
    if (valid) {
      saving.value = true
      try {
        let modules = form.value.selectedModules
        if (modules.includes('all')) modules = ['all']
        const data = {
          groupName: form.value.groupName,
          groupPermission: JSON.stringify({ modules }),
          remark: form.value.remark,
        }
        if (isNew.value) {
          await permissionApi.create(data)
          ElMessage.success('创建成功')
        } else {
          await permissionApi.update(selectedId.value!, data)
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
  form.value = { groupName: '', selectedModules: [], remark: '' }
  prevModules.value = []
  users.value = []
  availableUsers.value = []
}

const handleDeleteCurrent = async () => {
  if (!selectedId.value) return
  try {
    await ElMessageBox.confirm(`确定要删除权限组"${form.value.groupName}"吗？`, '提示', {
      confirmButtonText: '确定', cancelButtonText: '取消', type: 'warning',
    })
    deleting.value = true
    await permissionApi.delete(selectedId.value)
    ElMessage.success('删除成功')
    handleCancel()
    fetchList()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('删除失败')
  } finally {
    deleting.value = false
  }
}

const handleAddUser = async () => {
  if (!selectedId.value || !selectedUserId.value) return
  try {
    await permissionApi.addUser(selectedId.value, selectedUserId.value)
    ElMessage.success('添加成功')
    selectedUserId.value = null
    await fetchUsers()
  } catch (error: any) {
    ElMessage.error(getErrorMessage(error))
  }
}

const handleRemoveUser = async (userId: number) => {
  if (!selectedId.value) return
  try {
    await permissionApi.removeUser(selectedId.value, userId)
    ElMessage.success('移除成功')
    await fetchUsers()
  } catch (error: any) {
    ElMessage.error(getErrorMessage(error))
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
    const res = await permissionApi.importFile(file)
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
      const blob = await permissionApi.exportFile(command)
      downloadBlob(blob, `权限组数据.${ext}`)
      ElMessage.success('导出成功')
    } catch { ElMessage.error('导出失败') }
    finally { exporting.value = false }
  } else if (command === 'template-xlsx') {
    try {
      const blob = await permissionApi.downloadTemplate('xlsx')
      downloadBlob(blob, '权限组导入模板.xlsx')
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

onMounted(() => fetchList())
</script>

<template>
  <div class="permission-container">
    <div class="search-card">
      <div class="card-header">
        <span>筛选条件</span>
      </div>
      <el-form :model="searchForm" inline style="margin-top: 12px">
        <el-form-item label="权限组名称">
          <el-input v-model="searchForm.groupName" placeholder="请输入权限组名称" clearable @keyup.enter="handleSearch" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
          <el-button @click="handleReset">重置</el-button>
          <el-button type="primary" :icon="Plus" @click="handleCreate">新增权限组</el-button>
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
              <span>{{ isNew ? '新增权限组' : isEditing ? '编辑权限组' : '权限详情' }}</span>
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
          <div v-else class="detail-scroll">
            <el-form ref="formRef" :model="form" :rules="rules" label-width="100px" class="detail-form">
              <el-form-item label="权限组名称" prop="groupName">
                <el-input v-model="form.groupName" placeholder="请输入权限组名称" />
              </el-form-item>
              <el-form-item label="权限配置">
                <el-checkbox-group v-model="form.selectedModules" @change="handleModuleChange">
                  <el-checkbox v-for="item in moduleOptions" :key="item.value" :label="item.value" :value="item.value">
                    {{ item.label }}
                  </el-checkbox>
                </el-checkbox-group>
              </el-form-item>
              <el-form-item label="备注">
                <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="请输入备注信息" />
              </el-form-item>
            </el-form>

            <div v-if="!isNew" class="user-section">
              <div class="section-title">关联用户 ({{ users.length }})</div>
              <div class="add-user-bar">
                <el-select v-model="selectedUserId" placeholder="选择要添加的用户" filterable clearable style="width: 220px">
                  <el-option v-for="u in availableUsers" :key="u.id" :label="`${u.username} (${u.realName})`" :value="u.id" />
                </el-select>
                <el-button type="primary" :disabled="!selectedUserId" @click="handleAddUser">添加</el-button>
              </div>
              <el-table :data="users" border size="small" style="width: 100%; margin-top: 8px">
                <el-table-column prop="username" label="用户名" min-width="100" />
                <el-table-column prop="realName" label="姓名" min-width="80" />
                <el-table-column label="操作" width="60" fixed="right">
                  <template #default="{ row }">
                    <el-button type="danger" link :icon="Delete" @click="handleRemoveUser(row.id)" />
                  </template>
                </el-table-column>
              </el-table>
            </div>
          </div>
        </el-card>
      </div>

      <div class="list-panel">
        <el-card class="list-card">
          <template #header>
            <span>权限组列表</span>
          </template>
          <el-table v-loading="loading" :data="list" border highlight-current-row
            @row-click="handleSelect" style="width: 100%">
            <el-table-column prop="groupName" label="权限组名称" min-width="160" />
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
.permission-container {
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

.detail-scroll {
  display: flex;
  flex-direction: column;
  gap: 12px;
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

.user-section {
  padding-top: 12px;
  border-top: 1px solid #ebeef5;
}

.section-title {
  font-size: 13px;
  font-weight: bold;
  margin-bottom: 8px;
  color: #303133;
}

.add-user-bar {
  display: flex;
  align-items: center;
  gap: 8px;
}

.list-card .pagination-wrap {
  padding: 4px 0 0 0;
  display: flex;
  justify-content: flex-end;
  flex-shrink: 0;
}
</style>
