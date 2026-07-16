<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete } from '@element-plus/icons-vue'
import { menuApi } from '@/api/system/menu'
import type { SysMenu } from '@/types/system'

const loading = ref(false)
const list = ref<SysMenu[]>([])
const open = ref(false)
const title = ref('')

const form = reactive<SysMenu>({
  menuName: '', parentId: 0, orderNum: 0, path: '', component: '',
  isFrame: '1', isCache: '0', menuType: 'M', visible: '0', status: '0', icon: '#',
})

const treeProps = { label: 'menuName', value: 'menuId', children: 'children' }
const rules = {
  menuName: [{ required: true, message: '菜单名称不能为空', trigger: 'blur' }],
  orderNum: [{ required: true, message: '显示顺序不能为空', trigger: 'blur' }],
  path: [{ required: true, message: '路由地址不能为空', trigger: 'blur' }],
}

const fetchList = async () => {
  loading.value = true
  try { const { data } = await menuApi.list(); list.value = data }
  catch { ElMessage.error('获取菜单列表失败') }
  finally { loading.value = false }
}

const resetForm = () => {
  form.menuId = undefined; form.menuName = ''; form.parentId = 0; form.orderNum = 0
  form.path = ''; form.component = ''; form.isFrame = '1'; form.isCache = '0'
  form.menuType = 'M'; form.visible = '0'; form.status = '0'; form.icon = '#'; form.perms = ''
}

const handleAdd = (row?: any) => {
  resetForm()
  form.parentId = row?.menuId ?? 0
  const depth = row ? 2 : 1
  form.menuType = depth === 2 ? 'C' : 'M'
  title.value = '添加菜单'; open.value = true
}

const handleUpdate = (row: any) => {
  menuApi.get(row.menuId).then(({ data }) => {
    Object.assign(form, data)
    title.value = '修改菜单'; open.value = true
  })
}

const handleSubmit = async () => {
  try {
    form.menuId ? await menuApi.update(form) : await menuApi.add(form)
    ElMessage.success(form.menuId ? '修改成功' : '新增成功')
    open.value = false; fetchList()
  } catch { ElMessage.error('操作失败') }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm('是否确认删除菜单 [' + row.menuName + ']?', '提示', { type: 'warning' })
    await menuApi.remove(row.menuId)
    ElMessage.success('删除成功'); fetchList()
  } catch { /* cancelled */ }
}

const getMenuType = (t: string): 'success' | 'warning' | 'info' | 'danger' => t === 'M' ? 'info' : t === 'C' ? 'success' : 'warning'

onMounted(() => fetchList())
</script>

<template>
  <div class="menu-container">
    <el-card>
      <div class="toolbar">
        <el-button v-hasPermi="['system:menu:add']" type="primary" :icon="Plus" @click="handleAdd()">新增</el-button>
      </div>
      <el-table v-loading="loading" :data="list" border row-key="menuId" default-expand-all :tree-props="{ children: 'children' }">
        <el-table-column prop="menuName" label="菜单名称" min-width="180" show-overflow-tooltip />
        <el-table-column prop="icon" label="图标" width="80" align="center"><template #default="{ row }">{{ row.icon || '#' }}</template></el-table-column>
        <el-table-column prop="orderNum" label="排序" width="70" align="center" />
        <el-table-column prop="path" label="路由地址" min-width="140" show-overflow-tooltip />
        <el-table-column prop="component" label="组件路径" min-width="140" show-overflow-tooltip />
        <el-table-column prop="perms" label="权限标识" min-width="160" show-overflow-tooltip />
        <el-table-column label="类型" width="80" align="center"><template #default="{ row }"><el-tag :type="getMenuType(row.menuType)">{{ row.menuType === 'M' ? '目录' : row.menuType === 'C' ? '菜单' : '按钮' }}</el-tag></template></el-table-column>
        <el-table-column label="状态" width="80" align="center"><template #default="{ row }"><el-tag :type="row.status === '0' ? 'success' : 'danger'">{{ row.status === '0' ? '正常' : '停用' }}</el-tag></template></el-table-column>
        <el-table-column label="操作" width="180" align="center" fixed="right">
          <template #default="{ row }">
            <el-button v-hasPermi="['system:menu:edit']" link type="primary" :icon="Edit" @click="handleUpdate(row)">修改</el-button>
            <el-button v-hasPermi="['system:menu:add']" link type="success" :icon="Plus" @click="handleAdd(row)">添加</el-button>
            <el-button v-hasPermi="['system:menu:remove']" link type="danger" :icon="Delete" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog :title="title" v-model="open" width="650px" append-to-body>
      <el-form :model="form" :rules="rules" label-width="80px">
        <el-row>
          <el-col :span="24">
            <el-form-item label="上级菜单">
              <el-tree-select v-model="form.parentId" :data="[{ menuId: 0, menuName: '主类目', children: list }]" :props="treeProps" check-strictly node-key="menuId" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="24"><el-form-item label="菜单类型"><el-radio-group v-model="form.menuType"><el-radio value="M">目录</el-radio><el-radio value="C">菜单</el-radio><el-radio value="F">按钮</el-radio></el-radio-group></el-form-item></el-col>
          <el-col :span="24"><el-form-item label="菜单名称" prop="menuName"><el-input v-model="form.menuName" /></el-form-item></el-col>
          <el-col v-if="form.menuType !== 'F'" :span="12"><el-form-item label="路由地址" prop="path"><el-input v-model="form.path" /></el-form-item></el-col>
          <el-col v-if="form.menuType === 'C'" :span="12"><el-form-item label="组件路径"><el-input v-model="form.component" /></el-form-item></el-col>
          <el-col v-if="form.menuType !== 'F'" :span="12"><el-form-item label="菜单图标"><el-input v-model="form.icon" /></el-form-item></el-col>
          <el-col v-if="form.menuType !== 'F'" :span="12"><el-form-item label="是否外链"><el-radio-group v-model="form.isFrame"><el-radio value="0">是</el-radio><el-radio value="1">否</el-radio></el-radio-group></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="显示排序"><el-input-number v-model="form.orderNum" :min="0" /></el-form-item></el-col>
          <el-col v-if="form.menuType === 'F'" :span="12"><el-form-item label="权限标识"><el-input v-model="form.perms" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="显示状态"><el-radio-group v-model="form.visible"><el-radio value="0">显示</el-radio><el-radio value="1">隐藏</el-radio></el-radio-group></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="菜单状态"><el-radio-group v-model="form.status"><el-radio value="0">正常</el-radio><el-radio value="1">停用</el-radio></el-radio-group></el-form-item></el-col>
        </el-row>
      </el-form>
      <template #footer><el-button @click="open = false">取消</el-button><el-button type="primary" @click="handleSubmit">确定</el-button></template>
    </el-dialog>
  </div>
</template>

<style scoped>
.menu-container { padding: 16px; }
.toolbar { margin-bottom: 12px; }
</style>