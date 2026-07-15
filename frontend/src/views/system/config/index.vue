<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete } from '@element-plus/icons-vue'
import { configApi } from '@/api/system/menu'
import type { SysConfig } from '@/types/system'

const loading = ref(false)
const list = ref<SysConfig[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const open = ref(false)
const title = ref('')
const ids = ref<number[]>([])

const searchForm = ref({ configName: '', configKey: '', configType: '' })
const form = reactive<SysConfig>({ configName: '', configKey: '', configValue: '', configType: 'Y' })

const rules = {
  configName: [{ required: true, message: '参数名称不能为空', trigger: 'blur' }],
  configKey: [{ required: true, message: '参数键名不能为空', trigger: 'blur' }],
  configValue: [{ required: true, message: '参数键值不能为空', trigger: 'blur' }],
}

const fetchList = async () => {
  loading.value = true
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.configName) params.configName = searchForm.value.configName
    if (searchForm.value.configKey) params.configKey = searchForm.value.configKey
    if (searchForm.value.configType) params.configType = searchForm.value.configType
    const { data } = await configApi.list(params)
    list.value = data.records; total.value = data.total
  } catch { ElMessage.error('获取参数列表失败') }
  finally { loading.value = false }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => { searchForm.value = { configName: '', configKey: '', configType: '' }; handleSearch() }
const handleSelectionChange = (rows: any[]) => { ids.value = rows.map((r: any) => r.configId) }
const resetForm = () => { form.configId = undefined; form.configName = ''; form.configKey = ''; form.configValue = ''; form.configType = 'Y' }
const handleAdd = () => { resetForm(); title.value = '添加参数'; open.value = true }
const handleUpdate = (row?: any) => {
  const id = row?.configId ?? ids.value[0]
  if (!id) return
  configApi.get(id).then(({ data }) => { Object.assign(form, data); title.value = '修改参数'; open.value = true })
}
const handleSubmit = async () => {
  try {
    form.configId ? await configApi.update(form) : await configApi.add(form)
    ElMessage.success(form.configId ? '修改成功' : '新增成功')
    open.value = false; fetchList()
  } catch { ElMessage.error('操作失败') }
}
const handleDelete = async (row?: any) => {
  const configIds = row?.configId ? [row.configId] : ids.value
  if (!configIds.length) return
  try {
    await ElMessageBox.confirm('是否确认删除该参数?', '提示', { type: 'warning' })
    await configApi.remove(configIds)
    ElMessage.success('删除成功'); fetchList()
  } catch { /* cancelled */ }
}
const handleRefreshCache = async () => {
  try { await configApi.refreshCache(); ElMessage.success('刷新缓存成功') }
  catch { ElMessage.error('刷新缓存失败') }
}

onMounted(() => fetchList())
</script>

<template>
  <div class="config-container">
    <el-card>
      <el-form :model="searchForm" inline class="search-form">
        <el-form-item label="参数名称"><el-input v-model="searchForm.configName" placeholder="请输入" clearable @keyup.enter="handleSearch" /></el-form-item>
        <el-form-item label="参数键名"><el-input v-model="searchForm.configKey" placeholder="请输入" clearable @keyup.enter="handleSearch" /></el-form-item>
        <el-form-item label="系统内置"><el-select v-model="searchForm.configType" placeholder="请选择" clearable style="width:120px"><el-option label="是" value="Y" /><el-option label="否" value="N" /></el-select></el-form-item>
        <el-form-item><el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button><el-button :icon="Refresh" @click="handleReset">重置</el-button></el-form-item>
      </el-form>
      <div class="toolbar">
        <el-button v-hasPermi="['system:config:add']" type="primary" :icon="Plus" @click="handleAdd">新增</el-button>
        <el-button v-hasPermi="['system:config:edit']" type="success" :icon="Edit" :disabled="ids.length !== 1" @click="handleUpdate()">修改</el-button>
        <el-button v-hasPermi="['system:config:remove']" type="danger" :icon="Delete" :disabled="!ids.length" @click="handleDelete()">删除</el-button>
        <el-button v-hasPermi="['system:config:edit']" type="warning" plain @click="handleRefreshCache">刷新缓存</el-button>
      </div>
      <el-table v-loading="loading" :data="list" border @selection-change="handleSelectionChange">
        <el-table-column type="selection" width="50" align="center" />
        <el-table-column prop="configId" label="参数编号" width="100" align="center" />
        <el-table-column prop="configName" label="参数名称" min-width="120" show-overflow-tooltip />
        <el-table-column prop="configKey" label="参数键名" min-width="140" show-overflow-tooltip />
        <el-table-column prop="configValue" label="参数键值" min-width="120" show-overflow-tooltip />
        <el-table-column label="系统内置" width="90" align="center"><template #default="{ row }"><el-tag :type="row.isBuiltin === '1' ? 'success' : 'info'">{{ row.isBuiltin === '1' ? '是' : '否' }}</el-tag></template></el-table-column>
        <el-table-column prop="createTime" label="创建时间" min-width="160" />
        <el-table-column label="操作" width="120" align="center" fixed="right">
          <template #default="{ row }">
            <el-button v-hasPermi="['system:config:edit']" link type="primary" :icon="Edit" @click="handleUpdate(row)" />
            <el-button v-hasPermi="['system:config:remove']" link type="danger" :icon="Delete" @click="handleDelete(row)" />
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination"><el-pagination v-model:current-page="currentPage" v-model:page-size="pageSize" :page-sizes="[10, 20, 50, 100]" :total="total" layout="total, sizes, prev, pager, next, jumper" @size-change="fetchList" @current-change="fetchList" /></div>
    </el-card>

    <el-dialog :title="title" v-model="open" width="550px" append-to-body>
      <el-form :model="form" :rules="rules" label-width="80px">
        <el-form-item label="参数名称" prop="configName"><el-input v-model="form.configName" placeholder="请输入参数名称" /></el-form-item>
        <el-form-item label="参数键名" prop="configKey"><el-input v-model="form.configKey" placeholder="请输入参数键名" /></el-form-item>
        <el-form-item label="参数键值" prop="configValue"><el-input v-model="form.configValue" placeholder="请输入参数键值" /></el-form-item>
        <el-form-item label="系统内置"><el-radio-group v-model="form.configType"><el-radio value="Y">是</el-radio><el-radio value="N">否</el-radio></el-radio-group></el-form-item>
        <el-form-item label="备注"><el-input v-model="form.remark" type="textarea" placeholder="请输入备注" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="open = false">取消</el-button><el-button type="primary" @click="handleSubmit">确定</el-button></template>
    </el-dialog>
  </div>
</template>

<style scoped>
.config-container { padding: 16px; }
.search-form { margin-bottom: 8px; }
.toolbar { display: flex; gap: 8px; margin-bottom: 12px; }
.pagination { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>