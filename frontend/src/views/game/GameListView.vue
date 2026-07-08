<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { gameApi } from '@/api/game'
import type { GameOptions } from '@/api/game'
import type { Game } from '@/types/game'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Search, Upload, Download, Document, ArrowDown } from '@element-plus/icons-vue'

const loading = ref(false)
const saving = ref(false)
const deleting = ref(false)
const importing = ref(false)
const exporting = ref(false)
const formRef = ref()
const fileInput = ref<HTMLInputElement>()
const options = ref<GameOptions>({ gameDirections: [], sources: [], managers: [], statuses: [] })

const form = ref<Partial<Game>>({
  gameName: '',
  gameDirection: '',
  source: '',
  gitUrl: '',
  priority: undefined,
  tags: '',
  projectType: '',
  manager: '',
  whiteBranch: '',
  status: 'active',
  androidFolderName: '',
  remark: '',
})
const selectedId = ref<number | null>(null)
const isEditing = ref(false)

const list = ref<Game[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const searchForm = ref({
  gameName: '', gameDirection: '', source: '', manager: '', status: '',
  priority: undefined as number | undefined,
})

const isNew = computed(() => selectedId.value === null && isEditing.value)

const directionLabels: Record<string, string> = { horizontal: '横屏', vertical: '竖屏' }
const statusLabels: Record<string, string> = { active: '进行中', completed: '已完成', paused: '已暂停' }
const priorityOptions = [
  { label: '1', value: 1 }, { label: '2', value: 2 }, { label: '3', value: 3 },
  { label: '4', value: 4 }, { label: '5', value: 5 },
]

const rules = {
  gameName: [{ required: true, message: '请输入游戏名称', trigger: 'blur' }],
  gameDirection: [{ required: true, message: '请选择游戏方向', trigger: 'change' }],
}

const fetchList = async () => {
  loading.value = true
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.gameName) params.gameName = searchForm.value.gameName
    if (searchForm.value.gameDirection) params.gameDirection = searchForm.value.gameDirection
    if (searchForm.value.source) params.source = searchForm.value.source
    if (searchForm.value.manager) params.manager = searchForm.value.manager
    if (searchForm.value.status) params.status = searchForm.value.status
    if (searchForm.value.priority) params.priority = searchForm.value.priority
    const response = await gameApi.getList(params)
    list.value = response.data.records
    total.value = response.data.total
  } catch (error) {
    console.error('获取游戏列表失败:', error)
  } finally {
    loading.value = false
  }
}

const fetchOptions = async () => {
  try {
    const res = await gameApi.getOptions()
    options.value = res.data
  } catch (error) {
    console.error('获取筛选项失败:', error)
  }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => {
  searchForm.value = { gameName: '', gameDirection: '', source: '', manager: '', status: '', priority: undefined }
  handleSearch()
}
const handleSizeChange = (val: number) => { pageSize.value = val; fetchList() }
const handleCurrentChange = (val: number) => { currentPage.value = val; fetchList() }

const handleCreate = () => {
  selectedId.value = null
  isEditing.value = true
  form.value = { gameName: '', gameDirection: '', source: '', gitUrl: '', priority: undefined, tags: '', projectType: '', manager: '', whiteBranch: '', status: 'active', androidFolderName: '', remark: '' }
}

const handleSelect = async (row: Game) => {
  selectedId.value = row.id
  isEditing.value = true
  form.value = {
    gameName: row.gameName || '',
    gameDirection: row.gameDirection || '',
    source: row.source || '',
    gitUrl: row.gitUrl || '',
    priority: row.priority,
    tags: row.tags || '',
    projectType: row.projectType || '',
    manager: row.manager || '',
    whiteBranch: row.whiteBranch || '',
    status: row.status || 'active',
    androidFolderName: row.androidFolderName || '',
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
          await gameApi.create(form.value)
          ElMessage.success('创建成功')
        } else {
          await gameApi.update(selectedId.value!, form.value)
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
  form.value = { gameName: '', gameDirection: '', source: '', gitUrl: '', priority: undefined, tags: '', projectType: '', manager: '', whiteBranch: '', status: 'active', androidFolderName: '', remark: '' }
}

const handleDeleteCurrent = async () => {
  if (!selectedId.value) return
  try {
    await ElMessageBox.confirm('确定要删除这个游戏吗？', '提示', {
      confirmButtonText: '确定', cancelButtonText: '取消', type: 'warning',
    })
    deleting.value = true
    await gameApi.delete(selectedId.value)
    ElMessage.success('删除成功')
    handleCancel()
    fetchList()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('删除失败')
  } finally {
    deleting.value = false
  }
}



const handleImport = () => { fileInput.value?.click() }
const handleFileChange = async (event: Event) => {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return
  importing.value = true
  try {
    const res = await gameApi.importFile(file)
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
      const blob = await gameApi.exportFile(command)
      downloadBlob(blob, `游戏数据.${ext}`)
      ElMessage.success('导出成功')
    } catch { ElMessage.error('导出失败') }
    finally { exporting.value = false }
  } else if (command === 'template-xlsx') {
    try {
      const blob = await gameApi.downloadTemplate('xlsx')
      downloadBlob(blob, '游戏导入模板.xlsx')
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
  <div class="game-container">
    <div class="search-card">
      <div class="card-header">
        <span>筛选条件</span>
      </div>
      <el-form :model="searchForm" inline style="margin-top: 12px">
        <el-form-item label="游戏名称">
          <el-input v-model="searchForm.gameName" placeholder="请输入游戏名称" clearable @keyup.enter="handleSearch" />
        </el-form-item>
        <el-form-item label="游戏方向">
          <el-select v-model="searchForm.gameDirection" placeholder="请选择方向" clearable>
            <el-option v-for="d in options.gameDirections" :key="d" :label="directionLabels[d] || d" :value="d" />
          </el-select>
        </el-form-item>
        <el-form-item label="来源">
          <el-select v-model="searchForm.source" placeholder="请选择来源" clearable>
            <el-option v-for="s in options.sources" :key="s" :label="s" :value="s" />
          </el-select>
        </el-form-item>
        <el-form-item label="负责人">
          <el-select v-model="searchForm.manager" placeholder="请选择负责人" clearable>
            <el-option v-for="m in options.managers" :key="m" :label="m" :value="m" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.status" placeholder="请选择状态" clearable>
            <el-option v-for="s in options.statuses" :key="s" :label="statusLabels[s] || s" :value="s" />
          </el-select>
        </el-form-item>
        <el-form-item label="优先级">
          <el-select v-model="searchForm.priority" placeholder="请选择优先级" clearable>
            <el-option v-for="p in priorityOptions" :key="p.value" :label="p.label" :value="p.value" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
          <el-button @click="handleReset">重置</el-button>
          <el-button type="primary" :icon="Plus" @click="handleCreate">新增游戏</el-button>
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
              <span>{{ isNew ? '新增游戏' : isEditing ? '编辑游戏' : '游戏详情' }}</span>
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
          <el-form v-else ref="formRef" :model="form" :rules="rules" label-width="90px" class="detail-form">
            <el-form-item label="游戏名称" prop="gameName">
              <el-input v-model="form.gameName" placeholder="请输入游戏名称" />
            </el-form-item>
            <el-form-item label="游戏方向" prop="gameDirection">
              <el-select v-model="form.gameDirection" placeholder="请选择方向" style="width: 100%">
                <el-option v-for="d in options.gameDirections" :key="d" :label="directionLabels[d] || d" :value="d" />
              </el-select>
            </el-form-item>
            <el-form-item label="来源">
              <el-input v-model="form.source" placeholder="请输入来源" />
            </el-form-item>
            <el-form-item label="Git地址">
              <el-input v-model="form.gitUrl" placeholder="请输入Git地址" />
            </el-form-item>
            <el-form-item label="优先级">
              <el-select v-model="form.priority" placeholder="请选择优先级" style="width: 100%">
                <el-option v-for="p in priorityOptions" :key="p.value" :label="p.label" :value="p.value" />
              </el-select>
            </el-form-item>
            <el-form-item label="标签">
              <el-input v-model="form.tags" placeholder="请输入标签，逗号分隔" />
            </el-form-item>
            <el-form-item label="工程类型">
              <el-input v-model="form.projectType" placeholder="请输入工程类型" />
            </el-form-item>
            <el-form-item label="负责人">
              <el-input v-model="form.manager" placeholder="请输入负责人" />
            </el-form-item>
            <el-form-item label="白包分支">
              <el-input v-model="form.whiteBranch" placeholder="请输入白包分支" />
            </el-form-item>
            <el-form-item label="状态">
              <el-select v-model="form.status" style="width: 100%">
                <el-option v-for="s in options.statuses" :key="s" :label="statusLabels[s] || s" :value="s" />
              </el-select>
            </el-form-item>
            <el-form-item label="安卓文件夹">
              <el-input v-model="form.androidFolderName" placeholder="请输入安卓文件夹名称" />
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
            <span>游戏列表</span>
          </template>
          <el-table v-loading="loading" :data="list" border highlight-current-row
            @row-click="handleSelect" style="width: 100%">
            <el-table-column prop="gameName" label="游戏名称" min-width="160" />
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
.game-container {
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
