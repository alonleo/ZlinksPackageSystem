<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete, Box, Upload, Download, ArrowDown } from '@element-plus/icons-vue'
import { productApi } from '@/api/product'
import type { ProductOptions } from '@/api/product'
import type { Product } from '@/types/product'

interface NameId { id: number; copyrightName?: string; gameName?: string; companyName?: string; platformName?: string }

const loading = ref(false)
const exporting = ref(false)
const importing = ref(false)
const fileInput = ref<HTMLInputElement>()
const list = ref<Product[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const open = ref(false)
const title = ref('')
const ids = ref<number[]>([])
const single = ref(true)
const multiple = ref(true)

const copyrights = ref<NameId[]>([])
const games = ref<NameId[]>([])
const companies = ref<NameId[]>([])
const platforms = ref<NameId[]>([])
const filterOptions = ref<ProductOptions>({ platforms: [], batches: [], statuses: [] })

const searchForm = ref({
  copyrightId: undefined as number | undefined,
  gameId: undefined as number | undefined,
  companyId: undefined as number | undefined,
  platformId: undefined as number | undefined,
  batch: '', status: '',
})

const form = reactive<Partial<Product>>({
  copyrightId: undefined, gameId: undefined, companyId: undefined,
  platformId: undefined as number | undefined,
  packageName: '', sdkVersion: '', apkVersion: '',
  batch: '', packageMode: '', status: 'pending', remark: '',
})

const rules = {
  packageName: [{ required: true, message: '请输入包名', trigger: 'blur' }],
}

const fetchList = async () => {
  loading.value = true
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.copyrightId) params.copyrightId = searchForm.value.copyrightId
    if (searchForm.value.gameId) params.gameId = searchForm.value.gameId
    if (searchForm.value.companyId) params.companyId = searchForm.value.companyId
    if (searchForm.value.platformId) params.platformId = searchForm.value.platformId
    if (searchForm.value.batch) params.batch = searchForm.value.batch
    if (searchForm.value.status) params.status = searchForm.value.status
    const { data } = await productApi.getList(params)
    list.value = data.records; total.value = data.total
  } catch { ElMessage.error('获取产品列表失败') }
  finally { loading.value = false }
}

const fetchOptions = async () => {
  try {
    const [cRes, gRes, coRes, pRes, optRes] = await Promise.all([
      productApi.getCopyrights(), productApi.getGames(),
      productApi.getCompanies(), productApi.getPlatforms(),
      productApi.getOptions(),
    ])
    copyrights.value = cRes.data
    games.value = gRes.data
    companies.value = coRes.data
    platforms.value = pRes.data as NameId[]
    filterOptions.value = optRes.data
  } catch { /* ignore */ }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => {
  searchForm.value = { copyrightId: undefined, gameId: undefined, companyId: undefined, platformId: undefined, batch: '', status: '' }
  handleSearch()
}
const handleSelectionChange = (rows: any[]) => {
  ids.value = rows.map((r: any) => r.id).filter(Boolean) as number[]
  single.value = rows.length !== 1; multiple.value = !rows.length
}

const resetForm = () => {
  form.id = undefined; form.copyrightId = undefined; form.gameId = undefined
  form.companyId = undefined; form.platformId = undefined
  form.packageName = ''
  form.sdkVersion = ''; form.apkVersion = ''; form.batch = ''
  form.packageMode = ''; form.status = 'pending'; form.remark = ''
}

const handleAdd = () => { resetForm(); title.value = '添加产品'; open.value = true }
const handleUpdate = async (row?: any) => {
  const id = row?.id ?? ids.value[0]
  if (!id) return
  try {
    const { data } = await productApi.getById(id)
    Object.assign(form, data)
    title.value = '修改产品'; open.value = true
  } catch { ElMessage.error('获取产品详情失败') }
}

const handleSubmit = async () => {
  try {
    form.id ? await productApi.update(form.id, form) : await productApi.create(form)
    ElMessage.success(form.id ? '修改成功' : '新增成功')
    open.value = false; fetchList()
  } catch { ElMessage.error('操作失败') }
}

const handleDelete = async (row?: any) => {
  const productIds: number[] = row?.id ? [row.id] : ids.value
  if (!productIds.length) return
  try {
    await ElMessageBox.confirm('是否确认删除选中的产品?', '提示', { type: 'warning' })
    for (const id of productIds) await productApi.delete(id)
    ElMessage.success('删除成功'); fetchList()
  } catch { /* cancelled */ }
}

const handlePackage = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确认要打包产品"${row.copyrightName || row.packageName}"吗?`, '提示', { type: 'info' })
    await productApi.triggerPackage(row.id)
    ElMessage.success('打包任务已提交')
  } catch { /* cancelled */ }
}

const statusLabels: Record<string, string> = {
  pending: '待处理', processing: '处理中', completed: '已完成',
}

const handleImport = () => { fileInput.value?.click() }
const handleFileChange = async (event: Event) => {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return
  importing.value = true
  try {
    const res = await productApi.importFile(file)
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
      ? await productApi.downloadTemplate('xlsx')
      : await productApi.exportFile(command as 'xlsx' | 'json')
    const ext = command === 'json' ? 'json' : 'xlsx'
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = command === 'template-xlsx' ? `产品导入模板.${ext}` : `产品数据.${ext}`
    link.click()
    window.URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch { ElMessage.error('导出失败') }
  finally { exporting.value = false }
}

onMounted(async () => {
  await fetchList()
  await fetchOptions()
})
</script>

<template>
  <div class="product-container">
    <el-card>
      <el-form :model="searchForm" inline class="search-form">
        <el-form-item label="软著">
          <el-select v-model="searchForm.copyrightId" placeholder="请选择软著" clearable filterable style="width:160px">
            <el-option v-for="c in copyrights" :key="c.id" :label="c.copyrightName" :value="c.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="游戏">
          <el-select v-model="searchForm.gameId" placeholder="请选择游戏" clearable filterable style="width:160px">
            <el-option v-for="g in games" :key="g.id" :label="g.gameName" :value="g.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="公司">
          <el-select v-model="searchForm.companyId" placeholder="请选择公司" clearable filterable style="width:160px">
            <el-option v-for="c in companies" :key="c.id" :label="c.companyName" :value="c.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="平台">
          <el-select v-model="searchForm.platformId" placeholder="请选择" clearable style="width:120px">
            <el-option v-for="p in platforms" :key="p.id" :label="p.platformName" :value="p.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="批次">
          <el-select v-model="searchForm.batch" placeholder="请选择" clearable style="width:120px">
            <el-option v-for="b in filterOptions.batches" :key="b" :label="b" :value="b" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.status" placeholder="请选择" clearable style="width:120px">
            <el-option v-for="s in filterOptions.statuses" :key="s" :label="statusLabels[s] || s" :value="s" />
          </el-select>
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
        <el-table-column prop="copyrightName" label="产品名称" min-width="160" show-overflow-tooltip />
        <el-table-column prop="gameName" label="游戏" min-width="120" show-overflow-tooltip />
        <el-table-column prop="companyName" label="公司" min-width="120" show-overflow-tooltip />
        <el-table-column prop="platformName" label="平台" width="100" show-overflow-tooltip />
        <el-table-column prop="packageName" label="包名" min-width="120" show-overflow-tooltip />
        <el-table-column prop="batch" label="批次" width="100" show-overflow-tooltip />
        <el-table-column label="状态" width="90" align="center"><template #default="{ row }">{{ statusLabels[row.status || ''] || row.status }}</template></el-table-column>
        <el-table-column prop="createTime" label="创建时间" min-width="160" />
        <el-table-column label="操作" width="180" align="center" fixed="right">
          <template #default="{ row }">
            <el-tooltip content="打包"><el-button link type="success" :icon="Box" @click="handlePackage(row)" /></el-tooltip>
            <el-button link type="primary" :icon="Edit" @click="handleUpdate(row)" />
            <el-button link type="danger" :icon="Delete" @click="handleDelete(row)" />
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination"><el-pagination v-model:current-page="currentPage" v-model:page-size="pageSize" :page-sizes="[10, 20, 50, 100]" :total="total" layout="total, sizes, prev, pager, next, jumper" @size-change="fetchList" @current-change="fetchList" /></div>
    </el-card>

    <el-dialog :title="title" v-model="open" width="620px" append-to-body>
      <el-form :model="form" :rules="rules" label-width="100px">
        <el-row>
          <el-col :span="12"><el-form-item label="软著">
            <el-select v-model="form.copyrightId" placeholder="请选择软著" filterable style="width:100%">
              <el-option v-for="c in copyrights" :key="c.id" :label="c.copyrightName" :value="c.id" />
            </el-select>
          </el-form-item></el-col>
          <el-col :span="12"><el-form-item label="游戏">
            <el-select v-model="form.gameId" placeholder="请选择游戏" filterable style="width:100%">
              <el-option v-for="g in games" :key="g.id" :label="g.gameName" :value="g.id" />
            </el-select>
          </el-form-item></el-col>
          <el-col :span="12"><el-form-item label="公司">
            <el-select v-model="form.companyId" placeholder="请选择公司" filterable style="width:100%">
              <el-option v-for="c in companies" :key="c.id" :label="c.companyName" :value="c.id" />
            </el-select>
          </el-form-item></el-col>
          <el-col :span="12"><el-form-item label="平台">
            <el-select v-model="form.platformId" placeholder="请选择" style="width:100%">
              <el-option v-for="p in platforms" :key="p.id" :label="p.platformName" :value="p.id" />
            </el-select>
          </el-form-item></el-col>
          <el-col :span="12"><el-form-item label="包名" prop="packageName"><el-input v-model="form.packageName" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="SDK版本"><el-input v-model="form.sdkVersion" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="APK版本"><el-input v-model="form.apkVersion" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="批次"><el-input v-model="form.batch" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="打包模式"><el-input v-model="form.packageMode" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="状态">
            <el-select v-model="form.status" style="width:100%">
              <el-option v-for="s in filterOptions.statuses" :key="s" :label="statusLabels[s] || s" :value="s" />
            </el-select>
          </el-form-item></el-col>
          <el-col :span="24"><el-form-item label="备注"><el-input v-model="form.remark" type="textarea" /></el-form-item></el-col>
        </el-row>
      </el-form>
      <template #footer><el-button @click="open = false">取消</el-button><el-button type="primary" @click="handleSubmit">确定</el-button></template>
    </el-dialog>

    <input ref="fileInput" type="file" accept=".xlsx,.xls" style="display: none" @change="handleFileChange" />
  </div>
</template>

<style scoped>
.product-container { padding: 16px; }
.search-form { margin-bottom: 8px; }
.toolbar { display: flex; gap: 8px; margin-bottom: 12px; }
.pagination { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>