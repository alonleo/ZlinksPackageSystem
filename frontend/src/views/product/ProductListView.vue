<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { productApi } from '@/api/product'
import type { ProductOptions } from '@/api/product'
import type { Product } from '@/types/product'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Search, Upload, Download, Document, Box, ArrowDown } from '@element-plus/icons-vue'

const loading = ref(false)
const saving = ref(false)
const deleting = ref(false)
const packing = ref(false)
const importing = ref(false)
const exporting = ref(false)
const formRef = ref()
const fileInput = ref<HTMLInputElement>()

const copyrights = ref<{ id: number; copyrightName: string }[]>([])
const games = ref<{ id: number; gameName: string }[]>([])
const companies = ref<{ id: number; companyName: string }[]>([])
const filterOptions = ref<ProductOptions>({ platforms: [], batches: [], statuses: [] })

const form = ref<Partial<Product>>({
  copyrightId: undefined,
  gameId: undefined,
  companyId: undefined,
  platform: '',
  packageName: '',
  sdkVersion: '',
  apkVersion: '',
  batch: '',
  packageMode: '',
  status: 'pending',
  remark: '',
})
const selectedId = ref<number | null>(null)
const isEditing = ref(false)

const list = ref<Product[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const searchForm = ref({
  copyrightId: undefined as number | undefined,
  gameId: undefined as number | undefined,
  companyId: undefined as number | undefined,
  platform: '',
  batch: '',
  status: '',
})

const isNew = computed(() => selectedId.value === null && isEditing.value)

const statusLabels: Record<string, string> = {
  pending: '待处理', processing: '处理中', completed: '已完成',
}

const fetchList = async () => {
  loading.value = true
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.copyrightId) params.copyrightId = searchForm.value.copyrightId
    if (searchForm.value.gameId) params.gameId = searchForm.value.gameId
    if (searchForm.value.companyId) params.companyId = searchForm.value.companyId
    if (searchForm.value.platform) params.platform = searchForm.value.platform
    if (searchForm.value.batch) params.batch = searchForm.value.batch
    if (searchForm.value.status) params.status = searchForm.value.status
    const response = await productApi.getList(params)
    list.value = response.data.records
    total.value = response.data.total
  } catch (error) {
    console.error('获取产品列表失败:', error)
  } finally {
    loading.value = false
  }
}

const fetchOptions = async () => {
  try {
    const [cRes, gRes, coRes, optRes] = await Promise.all([
      productApi.getCopyrights(), productApi.getGames(),
      productApi.getCompanies(), productApi.getOptions(),
    ])
    copyrights.value = cRes.data
    games.value = gRes.data
    companies.value = coRes.data
    filterOptions.value = optRes.data
  } catch (error) {
    console.error('获取下拉选项失败:', error)
  }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => {
  searchForm.value = { copyrightId: undefined, gameId: undefined, companyId: undefined, platform: '', batch: '', status: '' }
  handleSearch()
}
const handleSizeChange = (val: number) => { pageSize.value = val; fetchList() }
const handleCurrentChange = (val: number) => { currentPage.value = val; fetchList() }

const handleCreate = () => {
  selectedId.value = null
  isEditing.value = true
  form.value = { copyrightId: undefined, gameId: undefined, companyId: undefined, platform: '', packageName: '', sdkVersion: '', apkVersion: '', batch: '', packageMode: '', status: 'pending', remark: '' }
}

const handleSelect = async (row: Product) => {
  selectedId.value = row.id
  isEditing.value = true
  form.value = {
    copyrightId: row.copyrightId,
    gameId: row.gameId,
    companyId: row.companyId,
    platform: row.platform || '',
    packageName: row.packageName || '',
    sdkVersion: row.sdkVersion || '',
    apkVersion: row.apkVersion || '',
    batch: row.batch || '',
    packageMode: row.packageMode || '',
    status: row.status || 'pending',
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
      saving.value = true
      try {
        if (isNew.value) {
          await productApi.create(form.value)
          ElMessage.success('创建成功')
        } else {
          await productApi.update(selectedId.value!, form.value)
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
  form.value = { copyrightId: undefined, gameId: undefined, companyId: undefined, platform: '', packageName: '', sdkVersion: '', apkVersion: '', batch: '', packageMode: '', status: 'pending', remark: '' }
}

const handleDeleteCurrent = async () => {
  if (!selectedId.value) return
  try {
    await ElMessageBox.confirm('确定要删除该产品吗？', '提示', {
      confirmButtonText: '确定', cancelButtonText: '取消', type: 'warning',
    })
    deleting.value = true
    await productApi.delete(selectedId.value)
    ElMessage.success('删除成功')
    handleCancel()
    fetchList()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('删除失败')
  } finally {
    deleting.value = false
  }
}

const handlePackage = async () => {
  if (!selectedId.value) return
  try {
    await ElMessageBox.confirm('确定要对该产品进行打包吗？', '提示', {
      confirmButtonText: '确定', cancelButtonText: '取消', type: 'info',
    })
    packing.value = true
    await productApi.triggerPackage(selectedId.value)
    ElMessage.success('打包任务已提交')
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('打包失败')
  } finally {
    packing.value = false
  }
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
      const blob = await productApi.exportFile(command)
      downloadBlob(blob, `产品数据.${ext}`)
      ElMessage.success('导出成功')
    } catch { ElMessage.error('导出失败') }
    finally { exporting.value = false }
  } else if (command === 'template-xlsx') {
    try {
      const blob = await productApi.downloadTemplate('xlsx')
      downloadBlob(blob, '产品导入模板.xlsx')
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

onMounted(() => { fetchList(); fetchOptions() })
</script>

<template>
  <div class="product-container">
    <div class="search-card">
      <div class="card-header">
        <span>筛选条件</span>
      </div>
      <el-form :model="searchForm" inline style="margin-top: 12px">
        <el-form-item label="软著">
          <el-select v-model="searchForm.copyrightId" placeholder="请选择软著" clearable filterable>
            <el-option v-for="c in copyrights" :key="c.id" :label="c.copyrightName" :value="c.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="游戏">
          <el-select v-model="searchForm.gameId" placeholder="请选择游戏" clearable filterable>
            <el-option v-for="g in games" :key="g.id" :label="g.gameName" :value="g.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="公司">
          <el-select v-model="searchForm.companyId" placeholder="请选择公司" clearable filterable>
            <el-option v-for="c in companies" :key="c.id" :label="c.companyName" :value="c.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="平台">
          <el-select v-model="searchForm.platform" placeholder="请选择平台" clearable>
            <el-option v-for="p in filterOptions.platforms" :key="p" :label="p" :value="p" />
          </el-select>
        </el-form-item>
        <el-form-item label="批次">
          <el-select v-model="searchForm.batch" placeholder="请选择批次" clearable>
            <el-option v-for="b in filterOptions.batches" :key="b" :label="b" :value="b" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.status" placeholder="请选择状态" clearable>
            <el-option v-for="s in filterOptions.statuses" :key="s" :label="statusLabels[s] || s" :value="s" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
          <el-button @click="handleReset">重置</el-button>
          <el-button type="primary" :icon="Plus" @click="handleCreate">新增产品</el-button>
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
              <span>{{ isNew ? '新增产品' : isEditing ? '编辑产品' : '产品详情' }}</span>
              <div v-if="isEditing" class="header-actions">
                <el-button type="primary" :loading="saving" @click="handleSave">保存</el-button>
                <el-button v-if="!isNew" type="success" :loading="packing" :icon="Box" @click="handlePackage">打包</el-button>
                <el-button v-if="!isNew" type="danger" :loading="deleting" @click="handleDeleteCurrent">删除</el-button>
                <el-button @click="handleCancel">取消</el-button>
              </div>
            </div>
          </template>
          <div v-if="!isEditing" class="empty-detail">
            <el-icon class="empty-icon"><Document /></el-icon>
            <p>请选择一条记录或点击新增</p>
          </div>
          <el-form v-else ref="formRef" :model="form" label-width="80px" class="detail-form">
            <el-form-item label="软著" prop="copyrightId">
              <el-select v-model="form.copyrightId" placeholder="请选择软著" filterable style="width: 100%">
                <el-option v-for="c in copyrights" :key="c.id" :label="c.copyrightName" :value="c.id" />
              </el-select>
            </el-form-item>
            <el-form-item label="游戏">
              <el-select v-model="form.gameId" placeholder="请选择游戏" filterable style="width: 100%">
                <el-option v-for="g in games" :key="g.id" :label="g.gameName" :value="g.id" />
              </el-select>
            </el-form-item>
            <el-form-item label="公司">
              <el-select v-model="form.companyId" placeholder="请选择公司" filterable style="width: 100%">
                <el-option v-for="c in companies" :key="c.id" :label="c.companyName" :value="c.id" />
              </el-select>
            </el-form-item>
            <el-form-item label="平台">
              <el-select v-model="form.platform" placeholder="请选择平台" style="width: 100%">
                <el-option v-for="p in filterOptions.platforms" :key="p" :label="p" :value="p" />
              </el-select>
            </el-form-item>
            <el-form-item label="包名">
              <el-input v-model="form.packageName" placeholder="请输入包名" />
            </el-form-item>
            <el-form-item label="SDK版本">
              <el-input v-model="form.sdkVersion" placeholder="请输入SDK版本" />
            </el-form-item>
            <el-form-item label="APK版本">
              <el-input v-model="form.apkVersion" placeholder="请输入APK版本" />
            </el-form-item>
            <el-form-item label="批次">
              <el-input v-model="form.batch" placeholder="请输入批次" />
            </el-form-item>
            <el-form-item label="打包模式">
              <el-input v-model="form.packageMode" placeholder="请输入打包模式" />
            </el-form-item>
            <el-form-item label="状态">
              <el-select v-model="form.status" style="width: 100%">
                <el-option v-for="s in filterOptions.statuses" :key="s" :label="statusLabels[s] || s" :value="s" />
              </el-select>
            </el-form-item>
            <el-form-item label="备注">
              <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="请输入备注信息" />
            </el-form-item>
          </el-form>
        </el-card>
      </div>

      <div class="list-panel">
        <el-card class="list-card">
          <template #header>
            <span>产品列表</span>
          </template>
          <el-table v-loading="loading" :data="list" border highlight-current-row
            @row-click="handleSelect" style="width: 100%">
            <el-table-column prop="copyrightName" label="产品名称" min-width="160" />
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
.product-container {
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
