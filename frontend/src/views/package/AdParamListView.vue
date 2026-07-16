<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete, Download, ArrowDown, Upload } from '@element-plus/icons-vue'
import { honorApi, vivoApi, huaweiApi } from '@/api/ad-param'
import { productApi } from '@/api/product'
import type { HonorParam, VivoParam, HuaweiParam } from '@/types/ad-param'

type Platform = 'honor' | 'vivo' | 'huawei'
type AnyParam = HonorParam | VivoParam | HuaweiParam

const activeTab = ref<Platform>('honor')
const loading = ref(false)
const exporting = ref(false)
const importing = ref(false)
const fileInput = ref<HTMLInputElement>()
const list = ref<AnyParam[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const open = ref(false)
const title = ref('')
const ids = ref<number[]>([])
const single = ref(true)
const multiple = ref(true)
const products = ref<{ id: number; packageName: string }[]>([])
const searchForm = reactive({ productId: undefined as number | undefined, adParamStatus: '', listStatus: '' })

const form = reactive<Record<string, any>>({})

const rules = {
  productId: [{ required: true, message: '请选择产品', trigger: 'change' }],
}

const adStatusOptions = [
  { label: '待配置', value: 'pending' },
  { label: '已配置', value: 'active' },
  { label: '已下线', value: 'inactive' },
]
const listStatusOptions = [
  { label: '上架', value: 'listed' },
  { label: '下架', value: 'unlisted' },
  { label: '暂停', value: 'paused' },
]

const tabLabels = [
  { value: 'honor', label: '荣耀参数' },
  { value: 'vivo', label: 'VIVO参数' },
  { value: 'huawei', label: '华为参数' },
]

const columnsByTab: Record<Platform, { key: string; label: string }[]> = {
  honor: [
    { key: 'productId', label: '产品ID' },
    { key: 'packageName', label: '包名' },
    { key: 'appId', label: 'AppId' },
    { key: 'mediaId', label: 'MediaId' },
    { key: 'tdAppId', label: 'TDAppId' },
    { key: 'adParamStatus', label: '广告参数' },
    { key: 'listStatus', label: '上架状态' },
    { key: 'operator', label: '操作人' },
  ],
  vivo: [
    { key: 'productId', label: '产品ID' },
    { key: 'appId', label: 'AppId' },
    { key: 'contractStatus', label: '合同状态' },
    { key: 'mediaId', label: 'MediaId' },
    { key: 'tdAppId', label: 'TDAppId' },
    { key: 'adParamStatus', label: '广告参数' },
    { key: 'listStatus', label: '上架状态' },
    { key: 'operator', label: '操作人' },
  ],
  huawei: [
    { key: 'productId', label: '产品ID' },
    { key: 'packageName', label: '包名' },
    { key: 'appId', label: 'AppId' },
    { key: 'tdAppId', label: 'TDAppId' },
    { key: 'adParamStatus', label: '广告参数' },
    { key: 'listStatus', label: '上架状态' },
    { key: 'operator', label: '操作人' },
  ],
}

const statusLabels: Record<string, string> = { pending: '待配置', active: '已配置', inactive: '已下线' }
const listLabels: Record<string, string> = { listed: '上架', unlisted: '下架', paused: '暂停' }

function currentApi() {
  const map: Record<Platform, typeof honorApi> = { honor: honorApi, vivo: vivoApi, huawei: huaweiApi } as any
  return map[activeTab.value]
}

const fetchList = async () => {
  loading.value = true
  try {
    const api = currentApi()
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.productId) params.productId = searchForm.productId
    if (searchForm.adParamStatus) params.adParamStatus = searchForm.adParamStatus
    if (searchForm.listStatus) params.listStatus = searchForm.listStatus
    const { data } = await api.getList(params)
    list.value = data.records as AnyParam[]
    total.value = data.total
  } catch { ElMessage.error('获取参数列表失败') }
  finally { loading.value = false }
}

const fetchProducts = async () => {
  try {
    const { data } = await productApi.getAll()
    products.value = data.map((p: any) => ({ id: p.id, packageName: p.packageName }))
  } catch { /* ignore */ }
}

const handleTabChange = () => {
  currentPage.value = 1
  fetchList()
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => { searchForm.productId = undefined; searchForm.adParamStatus = ''; searchForm.listStatus = ''; handleSearch() }

const handleSelectionChange = (rows: AnyParam[]) => {
  ids.value = rows.map((r: any) => r.id).filter(Boolean) as number[]
  single.value = rows.length !== 1
  multiple.value = !rows.length
}

const resetForm = () => {
  Object.keys(form).forEach((k) => delete form[k])
}

const handleAdd = () => {
  resetForm()
  const label = tabLabels.find(t => t.value === activeTab.value)?.label || ''
  title.value = `新增${label}`
  open.value = true
}

const handleUpdate = async (row?: any) => {
  const id = (row?.id ?? ids.value[0]) as number
  if (!id) return
  try {
    const api = currentApi()
    const { data } = await api.getById(id)
    Object.keys(form).forEach((k) => delete form[k])
    Object.assign(form, data)
    const label = tabLabels.find(t => t.value === activeTab.value)?.label || ''
    title.value = `修改${label}`
    open.value = true
  } catch { ElMessage.error('获取参数详情失败') }
}

const handleSubmit = async () => {
  try {
    const api = currentApi()
    if (form.id) await api.update(form.id, form)
    else await api.create(form)
    ElMessage.success(form.id ? '修改成功' : '新增成功')
    open.value = false
    fetchList()
  } catch { ElMessage.error('操作失败') }
}

const handleDelete = async (row?: any) => {
  const list = row ? [row.id] : ids.value
  try {
    await ElMessageBox.confirm(`确认删除选中 ${list.length} 条？`, '提示', { type: 'warning' })
    const api = currentApi()
    for (const id of list) await api.delete(id)
    ElMessage.success('删除成功')
    fetchList()
  } catch { /* cancelled */ }
}

const handleExport = async (format: 'xlsx' | 'json') => {
  exporting.value = true
  try {
    const api = currentApi()
    const blob = await api.exportFile(format)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${activeTab.value}-params.${format}`
    a.click()
    URL.revokeObjectURL(url)
  } finally { exporting.value = false }
}

const handleDownloadTemplate = async (format: 'xlsx' | 'json') => {
  exporting.value = true
  try {
    const api = currentApi()
    const blob = await api.downloadTemplate(format)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${activeTab.value}-params-template.${format}`
    a.click()
    URL.revokeObjectURL(url)
  } finally { exporting.value = false }
}

const triggerImport = () => fileInput.value?.click()
const handleImport = async (e: Event) => {
  const target = e.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return
  importing.value = true
  try {
    const api = currentApi()
    const { data } = await api.importFile(file)
    ElMessage.success(data)
    fetchList()
  } finally {
    importing.value = false
    target.value = ''
  }
}

onMounted(() => { fetchProducts(); fetchList() })
</script>

<template>
  <div class="app-container">
    <el-tabs v-model="activeTab" @tab-change="handleTabChange">
      <el-tab-pane v-for="t in tabLabels" :key="t.value" :label="t.label" :name="t.value" />
    </el-tabs>

    <el-form :inline="true" :model="searchForm" class="search-form">
      <el-form-item label="产品">
        <el-select v-model="searchForm.productId" placeholder="全部" clearable filterable style="width:200px">
          <el-option v-for="p in products" :key="p.id" :label="`${p.id} - ${p.packageName}`" :value="p.id" />
        </el-select>
      </el-form-item>
      <el-form-item label="广告参数">
        <el-select v-model="searchForm.adParamStatus" placeholder="全部" clearable style="width:150px">
          <el-option v-for="o in adStatusOptions" :key="o.value" :label="o.label" :value="o.value" />
        </el-select>
      </el-form-item>
      <el-form-item label="上架状态">
        <el-select v-model="searchForm.listStatus" placeholder="全部" clearable style="width:150px">
          <el-option v-for="o in listStatusOptions" :key="o.value" :label="o.label" :value="o.value" />
        </el-select>
      </el-form-item>
      <el-form-item>
        <el-button :icon="Search" type="primary" @click="handleSearch">搜索</el-button>
        <el-button :icon="Refresh" @click="handleReset">重置</el-button>
      </el-form-item>
    </el-form>

    <div class="toolbar">
      <el-button :icon="Plus" type="primary" @click="handleAdd">新增</el-button>
      <el-button :icon="Edit" :disabled="single" @click="handleUpdate()">修改</el-button>
      <el-button :icon="Delete" :disabled="multiple" @click="handleDelete()">删除</el-button>
      <el-button :icon="Upload" :loading="importing" @click="triggerImport">导入</el-button>
      <input ref="fileInput" type="file" accept=".xlsx,.json" style="display:none" @change="handleImport" />
      <el-dropdown @command="(c: string) => handleExport(c as 'xlsx' | 'json')">
        <el-button :icon="Download" :loading="exporting">
          导出<el-icon class="el-icon--right"><ArrowDown /></el-icon>
        </el-button>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item command="xlsx">Excel</el-dropdown-item>
            <el-dropdown-item command="json">JSON</el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
      <el-dropdown @command="(c: string) => handleDownloadTemplate(c as 'xlsx' | 'json')">
        <el-button :icon="Download">
          模板<el-icon class="el-icon--right"><ArrowDown /></el-icon>
        </el-button>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item command="xlsx">Excel</el-dropdown-item>
            <el-dropdown-item command="json">JSON</el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </div>

    <el-table v-loading="loading" :data="list" border @selection-change="handleSelectionChange" style="width:100%">
      <el-table-column type="selection" width="50" />
      <el-table-column type="index" label="#" width="60" />
      <el-table-column v-for="c in columnsByTab[activeTab]" :key="c.key" :prop="c.key" :label="c.label" min-width="140">
        <template v-if="c.key === 'adParamStatus'" #default="{ row }">
          {{ statusLabels[row.adParamStatus] || row.adParamStatus }}
        </template>
        <template v-else-if="c.key === 'listStatus'" #default="{ row }">
          {{ listLabels[row.listStatus] || row.listStatus }}
        </template>
      </el-table-column>
      <el-table-column label="操作" width="180" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="handleUpdate(row)">编辑</el-button>
          <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-pagination
      v-model:current-page="currentPage"
      v-model:page-size="pageSize"
      :total="total"
      :page-sizes="[10, 20, 50, 100]"
      layout="total, sizes, prev, pager, next, jumper"
      style="margin-top:16px;justify-content:flex-end;display:flex"
      @current-change="fetchList"
      @size-change="fetchList"
    />

    <el-dialog v-model="open" :title="title" width="640px">
      <el-form :model="form" :rules="rules" label-width="100px">
        <el-form-item label="产品" prop="productId">
          <el-select v-model="form.productId" placeholder="选择产品" filterable style="width:100%">
            <el-option v-for="p in products" :key="p.id" :label="`${p.id} - ${p.packageName}`" :value="p.id" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="activeTab !== 'vivo'" label="包名">
          <el-input v-model="form.packageName" />
        </el-form-item>
        <el-form-item label="AppId">
          <el-input v-model="form.appId" />
        </el-form-item>
        <el-form-item v-if="activeTab === 'honor'" label="AppSecret">
          <el-input v-model="form.appSecret" type="password" show-password />
        </el-form-item>
        <el-form-item v-if="activeTab !== 'huawei'" label="MediaId">
          <el-input v-model="form.mediaId" />
        </el-form-item>
        <el-form-item v-if="activeTab === 'vivo'" label="合同状态">
          <el-input v-model="form.contractStatus" />
        </el-form-item>
        <el-form-item v-if="activeTab !== 'vivo'" label="AGConnect路径">
          <el-input v-model="form.agconnectPath" />
        </el-form-item>
        <el-form-item label="TDAppId">
          <el-input v-model="form.tdAppId" />
        </el-form-item>
        <el-form-item label="广告参数">
          <el-select v-model="form.adParamStatus" placeholder="选择状态" style="width:100%">
            <el-option v-for="o in adStatusOptions" :key="o.value" :label="o.label" :value="o.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="上架状态">
          <el-select v-model="form.listStatus" placeholder="选择状态" style="width:100%">
            <el-option v-for="o in listStatusOptions" :key="o.value" :label="o.label" :value="o.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="操作人">
          <el-input v-model="form.operator" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="form.remark" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="open = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.app-container { padding: 20px; }
.search-form { margin-top: 16px; }
.toolbar { margin-bottom: 16px; }
</style>
