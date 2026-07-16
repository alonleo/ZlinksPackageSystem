<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete, Upload, Download, ArrowDown } from '@element-plus/icons-vue'
import { gameApi } from '@/api/game'
import type { GameOptions } from '@/api/game'
import type { Game } from '@/types/game'

const loading = ref(false)
const exporting = ref(false)
const importing = ref(false)
const fileInput = ref<HTMLInputElement>()
const list = ref<Game[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const open = ref(false)
const title = ref('')
const ids = ref<number[]>([])
const single = ref(true)
const multiple = ref(true)

const options = ref<GameOptions>({ gameDirections: [], sources: [], managers: [], statuses: [] })

const searchForm = ref({
  gameName: '', gameDirection: '', source: '', manager: '', status: '', priority: undefined as number | undefined,
})

const form = reactive<Partial<Game>>({
  gameName: '', gameDirection: '', source: '', gitUrl: '',
  priority: undefined, tags: '', projectType: '', manager: '',
  whiteBranch: '', status: 'active', androidFolderName: '', remark: '',
})

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
    const { data } = await gameApi.getList(params)
    list.value = data.records; total.value = data.total
  } catch { ElMessage.error('获取游戏列表失败') }
  finally { loading.value = false }
}

const fetchOptions = async () => {
  try { const { data } = await gameApi.getOptions(); options.value = data }
  catch { /* ignore */ }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => {
  searchForm.value = { gameName: '', gameDirection: '', source: '', manager: '', status: '', priority: undefined }
  handleSearch()
}
const handleSelectionChange = (rows: any[]) => {
  ids.value = rows.map((r: any) => r.id).filter(Boolean) as number[]
  single.value = rows.length !== 1; multiple.value = !rows.length
}

const resetForm = () => {
  form.id = undefined; form.gameName = ''; form.gameDirection = ''
  form.source = ''; form.gitUrl = ''; form.priority = undefined
  form.tags = ''; form.projectType = ''; form.manager = ''
  form.whiteBranch = ''; form.status = 'active'; form.androidFolderName = ''; form.remark = ''
}

const handleAdd = () => { resetForm(); title.value = '添加游戏'; open.value = true }
const handleUpdate = async (row?: any) => {
  const id = row?.id ?? ids.value[0]
  if (!id) return
  try {
    const { data } = await gameApi.getById(id)
    Object.assign(form, data)
    title.value = '修改游戏'; open.value = true
  } catch { ElMessage.error('获取游戏详情失败') }
}

const handleSubmit = async () => {
  try {
    form.id ? await gameApi.update(form.id, form) : await gameApi.create(form)
    ElMessage.success(form.id ? '修改成功' : '新增成功')
    open.value = false; fetchList()
  } catch { ElMessage.error('操作失败') }
}

const handleDelete = async (row?: any) => {
  const gameIds: number[] = row?.id ? [row.id] : ids.value
  if (!gameIds.length) return
  try {
    await ElMessageBox.confirm('是否确认删除选中的游戏?', '提示', { type: 'warning' })
    for (const id of gameIds) await gameApi.delete(id)
    ElMessage.success('删除成功'); fetchList()
  } catch { /* cancelled */ }
}

const directionLabels: Record<string, string> = { horizontal: '横屏', vertical: '竖屏' }
const statusLabels: Record<string, string> = { active: '进行中', completed: '已完成', paused: '已暂停', pending: '待处理' }
const priorityOptions = [
  { label: '1', value: 1 }, { label: '2', value: 2 }, { label: '3', value: 3 },
  { label: '4', value: 4 }, { label: '5', value: 5 },
]

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
      ? await gameApi.downloadTemplate('xlsx')
      : await gameApi.exportFile(command as 'xlsx' | 'json')
    const ext = command === 'json' ? 'json' : 'xlsx'
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = command === 'template-xlsx' ? `游戏导入模板.${ext}` : `游戏数据.${ext}`
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
  <div class="game-container">
    <el-card>
      <el-form :model="searchForm" inline class="search-form">
        <el-form-item label="游戏名称"><el-input v-model="searchForm.gameName" placeholder="请输入游戏名称" clearable @keyup.enter="handleSearch" /></el-form-item>
        <el-form-item label="游戏方向">
          <el-select v-model="searchForm.gameDirection" placeholder="请选择方向" clearable style="width:120px">
            <el-option v-for="d in options.gameDirections" :key="d" :label="directionLabels[d] || d" :value="d" />
          </el-select>
        </el-form-item>
        <el-form-item label="来源">
          <el-select v-model="searchForm.source" placeholder="请选择来源" clearable style="width:120px">
            <el-option v-for="s in options.sources" :key="s" :label="s" :value="s" />
          </el-select>
        </el-form-item>
        <el-form-item label="负责人">
          <el-select v-model="searchForm.manager" placeholder="请选择负责人" clearable style="width:120px">
            <el-option v-for="m in options.managers" :key="m" :label="m" :value="m" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.status" placeholder="请选择状态" clearable style="width:120px">
            <el-option v-for="s in options.statuses" :key="s" :label="statusLabels[s] || s" :value="s" />
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
        <el-table-column prop="gameName" label="游戏名称" min-width="160" show-overflow-tooltip />
        <el-table-column label="方向" width="80" align="center"><template #default="{ row }">{{ directionLabels[row.gameDirection || ''] || row.gameDirection }}</template></el-table-column>
        <el-table-column prop="source" label="来源" width="100" show-overflow-tooltip />
        <el-table-column prop="manager" label="负责人" width="100" show-overflow-tooltip />
        <el-table-column prop="priority" label="优先级" width="80" align="center" />
        <el-table-column label="状态" width="90" align="center"><template #default="{ row }">{{ statusLabels[row.status || ''] || row.status }}</template></el-table-column>
        <el-table-column prop="createTime" label="创建时间" min-width="160" />
        <el-table-column label="操作" width="120" align="center" fixed="right">
          <template #default="{ row }">
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
          <el-col :span="12"><el-form-item label="游戏名称" prop="gameName"><el-input v-model="form.gameName" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="游戏方向" prop="gameDirection">
            <el-select v-model="form.gameDirection" placeholder="请选择方向" style="width:100%">
              <el-option v-for="d in options.gameDirections" :key="d" :label="directionLabels[d] || d" :value="d" />
            </el-select>
          </el-form-item></el-col>
          <el-col :span="12"><el-form-item label="来源"><el-input v-model="form.source" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="Git地址"><el-input v-model="form.gitUrl" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="优先级">
            <el-select v-model="form.priority" placeholder="请选择" style="width:100%">
              <el-option v-for="p in priorityOptions" :key="p.value" :label="p.label" :value="p.value" />
            </el-select>
          </el-form-item></el-col>
          <el-col :span="12"><el-form-item label="负责人"><el-input v-model="form.manager" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="工程类型"><el-input v-model="form.projectType" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="白包分支"><el-input v-model="form.whiteBranch" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="安卓文件夹"><el-input v-model="form.androidFolderName" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="状态">
            <el-select v-model="form.status" style="width:100%">
              <el-option v-for="s in options.statuses" :key="s" :label="statusLabels[s] || s" :value="s" />
            </el-select>
          </el-form-item></el-col>
          <el-col :span="12"><el-form-item label="标签"><el-input v-model="form.tags" placeholder="逗号分隔" /></el-form-item></el-col>
          <el-col :span="24"><el-form-item label="备注"><el-input v-model="form.remark" type="textarea" /></el-form-item></el-col>
        </el-row>
      </el-form>
      <template #footer><el-button @click="open = false">取消</el-button><el-button type="primary" @click="handleSubmit">确定</el-button></template>
    </el-dialog>

    <input ref="fileInput" type="file" accept=".xlsx,.xls" style="display: none" @change="handleFileChange" />
  </div>
</template>

<style scoped>
.game-container { padding: 16px; }
.search-form { margin-bottom: 8px; }
.toolbar { display: flex; gap: 8px; margin-bottom: 12px; }
.pagination { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>