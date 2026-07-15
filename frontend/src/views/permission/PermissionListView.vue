<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete, Upload, Download, ArrowDown, User as UserIcon } from '@element-plus/icons-vue'
import { permissionApi } from '@/api/permission'
import type { PermissionGroup } from '@/types/permission-group'
import type { User } from '@/types/user'

const loading = ref(false)
const exporting = ref(false)
const importing = ref(false)
const fileInput = ref<HTMLInputElement>()
const list = ref<PermissionGroup[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const open = ref(false)
const title = ref('')
const ids = ref<number[]>([])
const single = ref(true)
const multiple = ref(true)

// User association dialog
const userOpen = ref(false)
const users = ref<User[]>([])
const availableUsers = ref<User[]>([])
const selectedUserId = ref<number | null>(null)
const currentGroupId = ref<number | null>(null)
const currentGroupName = ref('')

const searchForm = ref({ groupName: '' })

const form = reactive({
  groupName: '',
  selectedModules: [] as string[],
  remark: '',
})

const rules = {
  groupName: [{ required: true, message: '请输入权限组名称', trigger: 'blur' }],
}

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

const prevModules = ref<string[]>([])

const fetchList = async () => {
  loading.value = true
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.groupName) params.groupName = searchForm.value.groupName
    const { data } = await permissionApi.getList(params)
    list.value = data.records; total.value = data.total
  } catch { ElMessage.error('获取权限组列表失败') }
  finally { loading.value = false }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => { searchForm.value = { groupName: '' }; handleSearch() }
const handleSelectionChange = (rows: any[]) => {
  ids.value = rows.map((r: any) => r.id).filter(Boolean) as number[]
  single.value = rows.length !== 1; multiple.value = !rows.length
}

const resetForm = () => {
  form.groupName = ''
  form.selectedModules = []
  form.remark = ''
  prevModules.value = []
}

const handleAdd = () => { resetForm(); title.value = '添加权限组'; open.value = true }
const handleUpdate = async (row?: any) => {
  const id = row?.id ?? ids.value[0]
  if (!id) return
  try {
    const { data } = await permissionApi.getById(id)
    form.groupName = data.groupName
    form.remark = data.remark || ''
    try {
      const config = JSON.parse(data.groupPermission)
      form.selectedModules = config.modules || []
    } catch { form.selectedModules = [] }
    prevModules.value = [...form.selectedModules]
    title.value = '修改权限组'; open.value = true
  } catch { ElMessage.error('获取权限详情失败') }
}

const handleModuleChange = (values: unknown[]) => {
  const strValues = values.map(String)
  const hadAll = prevModules.value.includes('all')
  const hasAll = strValues.includes('all')
  if (!hadAll && hasAll) form.selectedModules = moduleOptions.map(m => m.value)
  else if (hadAll && !hasAll) form.selectedModules = []
  else if (hasAll && strValues.length < moduleOptions.length) form.selectedModules = strValues.filter(v => v !== 'all')
  prevModules.value = [...form.selectedModules]
}

const handleSubmit = async () => {
  try {
    let modules = form.selectedModules
    if (modules.includes('all')) modules = ['all']
    const data = {
      groupName: form.groupName,
      groupPermission: JSON.stringify({ modules }),
      remark: form.remark,
    }
    if (open.value && data) {
      const id = ids.value[0]
      if (id) await permissionApi.update(id, data)
      else await permissionApi.create(data)
    }
    ElMessage.success(ids.value.length ? '修改成功' : '新增成功')
    open.value = false; fetchList()
  } catch { ElMessage.error('操作失败') }
}

const handleDelete = async (row?: any) => {
  const idsToDelete: number[] = row?.id ? [row.id] : ids.value
  if (!idsToDelete.length) return
  try {
    await ElMessageBox.confirm('是否确认删除选中的权限组?', '提示', { type: 'warning' })
    for (const id of idsToDelete) await permissionApi.delete(id)
    ElMessage.success('删除成功'); fetchList()
  } catch { /* cancelled */ }
}

const openUserDialog = async (row: any) => {
  currentGroupId.value = row.id
  currentGroupName.value = row.groupName
  userOpen.value = true
  selectedUserId.value = null
  await fetchGroupUsers()
}

const fetchGroupUsers = async () => {
  if (!currentGroupId.value) return
  try {
    const [linkedRes, availableRes] = await Promise.all([
      permissionApi.getUsers(currentGroupId.value),
      permissionApi.getAvailableUsers(currentGroupId.value),
    ])
    users.value = linkedRes.data
    availableUsers.value = availableRes.data
  } catch { /* ignore */ }
}

const handleAddUser = async () => {
  if (!currentGroupId.value || !selectedUserId.value) return
  try {
    await permissionApi.addUser(currentGroupId.value, selectedUserId.value)
    ElMessage.success('添加成功')
    selectedUserId.value = null
    await fetchGroupUsers()
    fetchList()
  } catch { ElMessage.error('添加失败') }
}

const handleRemoveUser = async (userId: number) => {
  if (!currentGroupId.value) return
  try {
    await ElMessageBox.confirm('确认要移除该用户吗?', '提示', { type: 'warning' })
    await permissionApi.removeUser(currentGroupId.value, userId)
    ElMessage.success('移除成功')
    await fetchGroupUsers()
    fetchList()
  } catch { /* cancelled */ }
}

const handleImport = () => { fileInput.value?.click() }
const handleFileChange = async (event: Event) => {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return
  importing.value = true
  try {
    const res = await permissionApi.importFile(file)
    ElMessage.success(res.data)
    fetchList()
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.message || '导入失败')
  } finally {
    importing.value = false
    target.value = ''
  }
}

const handleExportCommand = async (command: string) => {
  if (command !== 'xlsx' && command !== 'json' && command !== 'template-xlsx') return
  exporting.value = true
  try {
    const blob = command === 'template-xlsx'
      ? await permissionApi.downloadTemplate('xlsx')
      : await permissionApi.exportFile(command as 'xlsx' | 'json')
    const ext = command === 'json' ? 'json' : 'xlsx'
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = command === 'template-xlsx' ? `权限组导入模板.${ext}` : `权限组数据.${ext}`
    link.click()
    window.URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch { ElMessage.error('导出失败') }
  finally { exporting.value = false }
}

onMounted(() => fetchList())
</script>

<template>
  <div class="permission-container">
    <el-card>
      <el-form :model="searchForm" inline class="search-form">
        <el-form-item label="权限组名称">
          <el-input v-model="searchForm.groupName" placeholder="请输入权限组名称" clearable @keyup.enter="handleSearch" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
          <el-button :icon="Refresh" @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
      <div class="toolbar">
        <el-button type="primary" :icon="Plus" @click="handleAdd">新增</el-button>
        <el-button type="success" :icon="Edit" :disabled="single" @click="handleUpdate()">修改</el-button>
        <el-button type="danger" :icon="Delete" :disabled="multiple" @click="handleDelete()">删除</el-button>
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
      </div>
      <el-table v-loading="loading" :data="list" border @selection-change="handleSelectionChange">
        <el-table-column type="selection" width="50" align="center" />
        <el-table-column prop="id" label="编号" width="80" align="center" />
        <el-table-column prop="groupName" label="权限组名称" min-width="160" show-overflow-tooltip />
        <el-table-column prop="userCount" label="关联用户" width="100" align="center" />
        <el-table-column prop="remark" label="备注" min-width="200" show-overflow-tooltip />
        <el-table-column prop="createTime" label="创建时间" min-width="160" />
        <el-table-column label="操作" width="180" align="center" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" :icon="Edit" @click="handleUpdate(row)" />
            <el-button link type="info" :icon="UserIcon" @click="openUserDialog(row)">关联用户</el-button>
            <el-button link type="danger" :icon="Delete" @click="handleDelete(row)" />
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination"><el-pagination v-model:current-page="currentPage" v-model:page-size="pageSize" :page-sizes="[10, 20, 50, 100]" :total="total" layout="total, sizes, prev, pager, next, jumper" @size-change="fetchList" @current-change="fetchList" /></div>
    </el-card>

    <el-dialog :title="title" v-model="open" width="500px" append-to-body>
      <el-form :model="form" :rules="rules" label-width="100px">
        <el-form-item label="权限组名称" prop="groupName"><el-input v-model="form.groupName" /></el-form-item>
        <el-form-item label="权限配置">
          <el-checkbox-group v-model="form.selectedModules" @change="handleModuleChange">
            <el-checkbox v-for="item in moduleOptions" :key="item.value" :label="item.value" :value="item.value">
              {{ item.label }}
            </el-checkbox>
          </el-checkbox-group>
        </el-form-item>
        <el-form-item label="备注"><el-input v-model="form.remark" type="textarea" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="open = false">取消</el-button><el-button type="primary" @click="handleSubmit">确定</el-button></template>
    </el-dialog>

    <el-dialog :title="'关联用户 - ' + currentGroupName" v-model="userOpen" width="700px" append-to-body>
      <div class="add-user-bar" style="margin-bottom: 12px;">
        <el-select v-model="selectedUserId" placeholder="选择要添加的用户" filterable clearable style="width: 300px">
          <el-option v-for="u in availableUsers" :key="u.id" :label="`${u.username} (${u.realName})`" :value="u.id" />
        </el-select>
        <el-button type="primary" :disabled="!selectedUserId" @click="handleAddUser">添加</el-button>
      </div>
      <el-table :data="users" border>
        <el-table-column prop="id" label="用户ID" width="100" align="center" />
        <el-table-column prop="username" label="用户名" min-width="120" show-overflow-tooltip />
        <el-table-column prop="realName" label="姓名" min-width="100" />
        <el-table-column label="操作" width="100" align="center" fixed="right">
          <template #default="{ row }">
            <el-button link type="danger" :icon="Delete" @click="handleRemoveUser(row.id)">移除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>

    <input ref="fileInput" type="file" accept=".xlsx,.xls" style="display: none" @change="handleFileChange" />
  </div>
</template>

<style scoped>
.permission-container { padding: 16px; }
.search-form { margin-bottom: 8px; }
.toolbar { display: flex; gap: 8px; margin-bottom: 12px; }
.pagination { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>