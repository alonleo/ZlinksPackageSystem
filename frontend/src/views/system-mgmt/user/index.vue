<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete, Key } from '@element-plus/icons-vue'
import { userApi } from '@/api/system/user'
import { roleApi } from '@/api/system/role'
import type { SysUser } from '@/types/system'

const loading = ref(false)
const list = ref<SysUser[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const open = ref(false)
const title = ref('')
const ids = ref<number[]>([])
const single = ref(true)
const multiple = ref(true)

const roleList = ref<any[]>([])

const searchForm = ref({ userName: '', phonenumber: '', status: '' })
const form = reactive<SysUser>({
  userName: '', nickName: '', phonenumber: '', email: '',
  sex: '0', status: '0', roleIds: [], password: '',
})

const rules = {
  userName: [{ required: true, message: '用户名称不能为空', trigger: 'blur' }],
  nickName: [{ required: true, message: '用户昵称不能为空', trigger: 'blur' }],
}

const fetchList = async () => {
  loading.value = true
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.userName) params.userName = searchForm.value.userName
    if (searchForm.value.phonenumber) params.phonenumber = searchForm.value.phonenumber
    if (searchForm.value.status) params.status = searchForm.value.status
    const { data } = await userApi.list(params)
    list.value = data.records; total.value = data.total
  } catch { ElMessage.error('获取用户列表失败') }
  finally { loading.value = false }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => { searchForm.value = { userName: '', phonenumber: '', status: '' }; fetchList() }
const handleSelectionChange = (rows: any[]) => {
  ids.value = rows.map((r: any) => r.userId)
  single.value = rows.length !== 1; multiple.value = !rows.length
}

const resetForm = () => {
  form.userId = undefined; form.userName = ''; form.nickName = ''
  form.phonenumber = ''; form.email = ''; form.sex = '0'; form.status = '0'
  form.roleIds = []; form.password = ''
}

const handleAdd = () => { resetForm(); title.value = '添加用户'; open.value = true }

const handleUpdate = async (row?: any) => {
  const id = row?.userId ?? ids.value[0]
  if (!id) return
  try {
    const { data } = await userApi.get(id)
    const roleIds = data.roles?.map((r: any) => r.roleId) ?? []
    Object.assign(form, { ...data, roleIds })
    title.value = '修改用户'; open.value = true
  } catch { ElMessage.error('获取用户详情失败') }
}

const handleSubmit = async () => {
  try {
    form.userId ? await userApi.update(form) : await userApi.add(form)
    ElMessage.success(form.userId ? '修改成功' : '新增成功')
    open.value = false; fetchList()
  } catch { ElMessage.error('操作失败') }
}

const handleDelete = async (row?: any) => {
  const userIds = row?.userId ? [row.userId] : ids.value
  if (!userIds.length) return
  try {
    await ElMessageBox.confirm('是否确认删除选中的用户?', '提示', { type: 'warning' })
    await userApi.remove(userIds)
    ElMessage.success('删除成功'); fetchList()
  } catch { /* cancelled */ }
}

const handleResetPwd = async (row: any) => {
  try {
    const { value: pwd } = await ElMessageBox.prompt('请输入新密码', '重置密码', { inputType: 'password' })
    await userApi.resetPwd(row.userId, pwd!)
    ElMessage.success('密码重置成功')
  } catch { /* cancelled */ }
}

const handleStatusChange = async (row: any) => {
  const text = row.status === '0' ? '启用' : '停用'
  try {
    await ElMessageBox.confirm('确认要"' + text + '""' + row.userName + '"用户吗?', '提示', { type: 'warning' })
    await userApi.changeStatus(row.userId, row.status)
    ElMessage.success(text + '成功')
  } catch { row.status = row.status === '0' ? '1' : '0' }
}

const loadRoleList = async () => {
  try { const { data } = await roleApi.list({}); roleList.value = data.records }
  catch { /* ignore */ }
}

onMounted(async () => {
  await Promise.all([fetchList(), loadRoleList()])
})
</script>

<template>
  <div class="user-container">
    <el-card>
      <el-form :model="searchForm" inline class="search-form">
        <el-form-item label="用户名称"><el-input v-model="searchForm.userName" placeholder="请输入" clearable @keyup.enter="handleSearch" /></el-form-item>
        <el-form-item label="手机号码"><el-input v-model="searchForm.phonenumber" placeholder="请输入" clearable @keyup.enter="handleSearch" /></el-form-item>
        <el-form-item label="状态"><el-select v-model="searchForm.status" placeholder="请选择" clearable style="width:120px"><el-option label="正常" value="0" /><el-option label="停用" value="1" /></el-select></el-form-item>
        <el-form-item><el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button><el-button :icon="Refresh" @click="handleReset">重置</el-button></el-form-item>
      </el-form>
      <div class="toolbar">
        <el-button v-hasPermi="['system:user:add']" type="primary" :icon="Plus" @click="handleAdd">新增</el-button>
        <el-button v-hasPermi="['system:user:edit']" type="success" :icon="Edit" :disabled="single" @click="handleUpdate()">修改</el-button>
        <el-button v-hasPermi="['system:user:remove']" type="danger" :icon="Delete" :disabled="multiple" @click="handleDelete()">删除</el-button>
      </div>
      <el-table v-loading="loading" :data="list" border @selection-change="handleSelectionChange">
        <el-table-column type="selection" width="50" align="center" />
        <el-table-column prop="userId" label="用户编号" width="100" align="center" />
        <el-table-column prop="userName" label="用户名称" min-width="100" show-overflow-tooltip />
        <el-table-column prop="nickName" label="用户昵称" min-width="100" show-overflow-tooltip />
        <el-table-column prop="phonenumber" label="手机号码" min-width="120" />
        <el-table-column label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-switch v-model="row.status" active-value="0" inactive-value="1" @change="handleStatusChange(row)" />
          </template>
        </el-table-column>
        <el-table-column prop="createTime" label="创建时间" min-width="160" />
        <el-table-column label="操作" width="200" align="center" fixed="right">
          <template #default="{ row }">
            <el-button v-hasPermi="['system:user:edit']" link type="primary" :icon="Edit" @click="handleUpdate(row)">修改</el-button>
            <el-button v-hasPermi="['system:user:remove']" link type="danger" :icon="Delete" @click="handleDelete(row)">删除</el-button>
            <el-button v-hasPermi="['system:user:resetPwd']" link type="warning" :icon="Key" @click="handleResetPwd(row)">重置</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination"><el-pagination v-model:current-page="currentPage" v-model:page-size="pageSize" :page-sizes="[10, 20, 50, 100]" :total="total" layout="total, sizes, prev, pager, next, jumper" @size-change="fetchList" @current-change="fetchList" /></div>
    </el-card>

    <el-dialog :title="title" v-model="open" width="550px" append-to-body>
      <el-form :model="form" :rules="rules" label-width="80px">
        <el-row>
          <el-col :span="12"><el-form-item label="用户名称" prop="userName"><el-input v-model="form.userName" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="用户昵称" prop="nickName"><el-input v-model="form.nickName" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="手机号码"><el-input v-model="form.phonenumber" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="邮箱"><el-input v-model="form.email" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="性别"><el-select v-model="form.sex" style="width:100%"><el-option label="男" value="0" /><el-option label="女" value="1" /><el-option label="未知" value="2" /></el-select></el-form-item></el-col>
          <el-col v-if="!form.userId" :span="12"><el-form-item label="密码" prop="password"><el-input v-model="form.password" type="password" show-password /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="状态"><el-radio-group v-model="form.status"><el-radio value="0">正常</el-radio><el-radio value="1">停用</el-radio></el-radio-group></el-form-item></el-col>
          <el-col :span="24"><el-form-item label="角色"><el-select v-model="form.roleIds" multiple placeholder="请选择" style="width:100%"><el-option v-for="r in roleList" :key="r.roleId" :label="r.roleName" :value="r.roleId" /></el-select></el-form-item></el-col>
          <el-col :span="24"><el-form-item label="备注"><el-input v-model="form.remark" type="textarea" /></el-form-item></el-col>
        </el-row>
      </el-form>
      <template #footer><el-button @click="open = false">取消</el-button><el-button type="primary" @click="handleSubmit">确定</el-button></template>
    </el-dialog>
  </div>
</template>

<style scoped>
.user-container { padding: 16px; }
.search-form { margin-bottom: 8px; }
.toolbar { display: flex; gap: 8px; margin-bottom: 12px; }
.pagination { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>