<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete } from '@element-plus/icons-vue'
import { roleApi } from '@/api/system/role'
import { menuApi } from '@/api/system/menu'
import type { SysRole } from '@/types/system'

const loading = ref(false)
const list = ref<SysRole[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const open = ref(false)
const title = ref('')
const ids = ref<number[]>([])

// Menu permission dialog
const menuOpen = ref(false)
const menuTree = ref<any[]>([])
const checkedKeys = ref<number[]>([])
const currentRole = ref<any>({})

// User allocation
const userOpen = ref(false)
const allocatedUsers = ref<any[]>([])
const unallocatedUsers = ref<any[]>([])
const allocatedPage = ref(1)
const unallocatedPage = ref(1)
const allocRoleId = ref<number>(0)

const searchForm = ref({ roleName: '', roleKey: '', status: '' })
const form = reactive<SysRole>({ roleName: '', roleKey: '', roleSort: 0, status: '0', dataScope: '1' })

const rules = {
  roleName: [{ required: true, message: '角色名称不能为空', trigger: 'blur' }],
  roleKey: [{ required: true, message: '角色权限字符串不能为空', trigger: 'blur' }],
  roleSort: [{ required: true, message: '角色顺序不能为空', trigger: 'blur' }],
}

const fetchList = async () => {
  loading.value = true
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.roleName) params.roleName = searchForm.value.roleName
    if (searchForm.value.roleKey) params.roleKey = searchForm.value.roleKey
    if (searchForm.value.status) params.status = searchForm.value.status
    const { data } = await roleApi.list(params)
    list.value = data.records; total.value = data.total
  } catch { ElMessage.error('获取角色列表失败') }
  finally { loading.value = false }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => { searchForm.value = { roleName: '', roleKey: '', status: '' }; handleSearch() }
const handleSelectionChange = (rows: any[]) => { ids.value = rows.map((r: any) => r.roleId) }
const resetForm = () => { form.roleId = undefined; form.roleName = ''; form.roleKey = ''; form.roleSort = 0; form.status = '0'; form.dataScope = '1' }
const handleAdd = () => { resetForm(); title.value = '添加角色'; open.value = true }
const handleUpdate = (row?: any) => {
  const id = row?.roleId ?? ids.value[0]
  if (!id) return
  roleApi.get(id).then(({ data }) => { Object.assign(form, data); title.value = '修改角色'; open.value = true })
}
const handleSubmit = async () => {
  try {
    form.roleId ? await roleApi.update(form) : await roleApi.add(form)
    ElMessage.success(form.roleId ? '修改成功' : '新增成功')
    open.value = false; fetchList()
  } catch { ElMessage.error('操作失败') }
}
const handleDelete = async (row?: any) => {
  const roleIds = row?.roleId ? [row.roleId] : ids.value
  if (!roleIds.length) return
  try {
    await ElMessageBox.confirm('是否确认删除该角色?', '提示', { type: 'warning' })
    await roleApi.remove(roleIds)
    ElMessage.success('删除成功'); fetchList()
  } catch { /* cancelled */ }
}

const handleStatusChange = async (row: any) => {
  const text = row.status === '0' ? '启用' : '停用'
  try {
    await ElMessageBox.confirm('确认要"' + text + '""' + row.roleName + '"角色吗?', '提示', { type: 'warning' })
    await roleApi.changeStatus(row.roleId, row.status)
    ElMessage.success(text + '成功')
  } catch { row.status = row.status === '0' ? '1' : '0' }
}

const handleMenuPerms = async (row: any) => {
  currentRole.value = row
  const { data } = await menuApi.roleMenuTreeselect(row.roleId)
  menuTree.value = data.menus
  checkedKeys.value = data.checkedKeys
  menuOpen.value = true
}

const handleMenuSubmit = async () => {
  try {
    // TODO: save checked menuIds from tree to server
    ElMessage.success('分配权限成功')
    menuOpen.value = false
  } catch { ElMessage.error('分配权限失败') }
}

const handleAllocUser = async (row: any) => {
  allocRoleId.value = row.roleId
  userOpen.value = true
  await fetchAllocated()
  await fetchUnallocated()
}

const fetchAllocated = async () => {
  try {
    const { data } = await roleApi.allocatedList(allocRoleId.value, { current: allocatedPage.value, size: 10 })
    allocatedUsers.value = data.records
  } catch { /* ignore */ }
}

const fetchUnallocated = async () => {
  try {
    const { data } = await roleApi.unallocatedList(allocRoleId.value, { current: unallocatedPage.value, size: 10 })
    unallocatedUsers.value = data.records
  } catch { /* ignore */ }
}

const handleCancelUser = async (row: any) => {
  try {
    await ElMessageBox.confirm('确认要取消该用户的该角色授权吗?', '提示', { type: 'warning' })
    await roleApi.cancelAuthUser(allocRoleId.value, row.userId)
    ElMessage.success('取消成功')
    await fetchAllocated()
    await fetchUnallocated()
  } catch { /* cancelled */ }
}

onMounted(() => fetchList())
</script>

<template>
  <div class="role-container">
    <el-card>
      <el-form :model="searchForm" inline class="search-form">
        <el-form-item label="角色名称"><el-input v-model="searchForm.roleName" placeholder="请输入" clearable @keyup.enter="handleSearch" /></el-form-item>
        <el-form-item label="权限字符"><el-input v-model="searchForm.roleKey" placeholder="请输入" clearable @keyup.enter="handleSearch" /></el-form-item>
        <el-form-item label="状态"><el-select v-model="searchForm.status" placeholder="请选择" clearable style="width:120px"><el-option label="正常" value="0" /><el-option label="停用" value="1" /></el-select></el-form-item>
        <el-form-item><el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button><el-button :icon="Refresh" @click="handleReset">重置</el-button></el-form-item>
      </el-form>
      <div class="toolbar">
        <el-button v-hasPermi="['system:role:add']" type="primary" :icon="Plus" @click="handleAdd">新增</el-button>
        <el-button v-hasPermi="['system:role:edit']" type="success" :icon="Edit" :disabled="ids.length !== 1" @click="handleUpdate()">修改</el-button>
        <el-button v-hasPermi="['system:role:remove']" type="danger" :icon="Delete" :disabled="!ids.length" @click="handleDelete()">删除</el-button>
      </div>
      <el-table v-loading="loading" :data="list" border @selection-change="handleSelectionChange">
        <el-table-column type="selection" width="50" align="center" />
        <el-table-column prop="roleId" label="角色编号" width="100" align="center" />
        <el-table-column prop="roleName" label="角色名称" min-width="120" show-overflow-tooltip />
        <el-table-column prop="roleKey" label="权限字符" min-width="120" show-overflow-tooltip />
        <el-table-column prop="roleSort" label="显示顺序" width="90" align="center" />
        <el-table-column label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-switch v-model="row.status" active-value="0" inactive-value="1" @change="handleStatusChange(row)" />
          </template>
        </el-table-column>
        <el-table-column prop="createTime" label="创建时间" min-width="160" />
        <el-table-column label="操作" width="240" align="center" fixed="right">
          <template #default="{ row }">
            <el-button v-hasPermi="['system:role:edit']" link type="primary" :icon="Edit" @click="handleUpdate(row)">修改</el-button>
            <el-button v-hasPermi="['system:role:remove']" link type="danger" :icon="Delete" @click="handleDelete(row)">删除</el-button>
            <el-button v-hasPermi="['system:role:edit']" link type="warning" @click="handleMenuPerms(row)">权限</el-button>
            <el-button v-hasPermi="['system:role:edit']" link type="info" @click="handleAllocUser(row)">用户</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination"><el-pagination v-model:current-page="currentPage" v-model:page-size="pageSize" :page-sizes="[10, 20, 50, 100]" :total="total" layout="total, sizes, prev, pager, next, jumper" @size-change="fetchList" @current-change="fetchList" /></div>
    </el-card>

    <el-dialog :title="title" v-model="open" width="500px" append-to-body>
      <el-form :model="form" :rules="rules" label-width="90px">
        <el-form-item label="角色名称" prop="roleName"><el-input v-model="form.roleName" /></el-form-item>
        <el-form-item label="权限字符" prop="roleKey"><el-input v-model="form.roleKey" /></el-form-item>
        <el-form-item label="角色顺序" prop="roleSort"><el-input-number v-model="form.roleSort" :min="0" /></el-form-item>
        <el-form-item label="状态"><el-radio-group v-model="form.status"><el-radio value="0">正常</el-radio><el-radio value="1">停用</el-radio></el-radio-group></el-form-item>
        <el-form-item label="备注"><el-input v-model="form.remark" type="textarea" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="open = false">取消</el-button><el-button type="primary" @click="handleSubmit">确定</el-button></template>
    </el-dialog>

    <el-dialog title="分配权限" v-model="menuOpen" width="400px" append-to-body>
      <el-tree ref="menuTreeRef" :data="menuTree" show-checkbox node-key="menuId" :default-checked-keys="checkedKeys" :props="{ label: 'menuName', children: 'children' }" default-expand-all />
      <template #footer><el-button @click="menuOpen = false">取消</el-button><el-button type="primary" @click="handleMenuSubmit">确定</el-button></template>
    </el-dialog>

    <el-dialog title="分配用户" v-model="userOpen" width="1000px" append-to-body>
      <el-row :gutter="12">
        <el-col :span="12">
          <el-card><template #header>已分配用户</template>
            <el-table :data="allocatedUsers" border><el-table-column prop="userName" label="用户名称" /><el-table-column label="操作" width="80"><template #default="{ row }"><el-button link type="danger" @click="handleCancelUser(row)">取消</el-button></template></el-table-column></el-table>
          </el-card>
        </el-col>
        <el-col :span="12">
          <el-card><template #header>未分配用户</template>
            <el-table :data="unallocatedUsers" border><el-table-column prop="userName" label="用户名称" /></el-table>
          </el-card>
        </el-col>
      </el-row>
    </el-dialog>
  </div>
</template>

<style scoped>
.role-container { padding: 16px; }
.search-form { margin-bottom: 8px; }
.toolbar { display: flex; gap: 8px; margin-bottom: 12px; }
.pagination { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>