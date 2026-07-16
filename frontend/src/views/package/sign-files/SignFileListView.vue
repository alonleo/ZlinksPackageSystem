<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete, Upload, Download, ArrowDown } from '@element-plus/icons-vue'
import { signFileApi } from '@/api/sign-file'
import type { SignFile } from '@/types/sign-file'
import type { Company } from '@/types/company'

const loading = ref(false)
const exporting = ref(false)
const importing = ref(false)
const fileInput = ref<HTMLInputElement>()
const list = ref<SignFile[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const open = ref(false)
const title = ref('')
const ids = ref<number[]>([])
const single = ref(true)
const multiple = ref(true)

const companies = ref<Company[]>([])

const searchForm = ref({ companyId: undefined as number | undefined })

const form = reactive<Partial<SignFile>>({
  companyId: undefined, storeFile: '', storePassword: '', keyAlias: '', remark: '',
})

const rules = {
  companyId: [{ required: true, message: '请选择所属公司', trigger: 'change' }],
}

const fetchList = async () => {
  loading.value = true
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.companyId) params.companyId = searchForm.value.companyId
    const { data } = await signFileApi.getList(params)
    list.value = data.records; total.value = data.total
  } catch { ElMessage.error('获取签名文件列表失败') }
  finally { loading.value = false }
}

const fetchCompanies = async () => {
  try { const { data } = await signFileApi.getCompanies(); companies.value = data }
  catch { /* ignore */ }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => { searchForm.value = { companyId: undefined }; handleSearch() }
const handleSelectionChange = (rows: any[]) => {
  ids.value = rows.map((r: any) => r.id).filter(Boolean) as number[]
  single.value = rows.length !== 1; multiple.value = !rows.length
}

const resetForm = () => {
  form.id = undefined; form.companyId = undefined
  form.storeFile = ''; form.storePassword = ''; form.keyAlias = ''; form.remark = ''
}

const handleAdd = () => { resetForm(); title.value = '添加签名'; open.value = true }
const handleUpdate = async (row?: any) => {
  const id = row?.id ?? ids.value[0]
  if (!id) return
  try {
    const { data } = await signFileApi.getById(id)
    Object.assign(form, data)
    title.value = '修改签名'; open.value = true
  } catch { ElMessage.error('获取签名详情失败') }
}

const handleSubmit = async () => {
  try {
    form.id ? await signFileApi.update(form.id, form) : await signFileApi.create(form)
    ElMessage.success(form.id ? '修改成功' : '新增成功')
    open.value = false; fetchList()
  } catch { ElMessage.error('操作失败') }
}

const handleDelete = async (row?: any) => {
  const idsToDelete: number[] = row?.id ? [row.id] : ids.value
  if (!idsToDelete.length) return
  try {
    await ElMessageBox.confirm('是否确认删除选中的签名?', '提示', { type: 'warning' })
    for (const id of idsToDelete) await signFileApi.delete(id)
    ElMessage.success('删除成功'); fetchList()
  } catch { /* cancelled */ }
}

const handleImport = () => { fileInput.value?.click() }
const handleFileChange = async (event: Event) => {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return
  importing.value = true
  try {
    const res = await signFileApi.importFile(file)
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
      ? await signFileApi.downloadTemplate('xlsx')
      : await signFileApi.exportFile(command as 'xlsx' | 'json')
    const ext = command === 'json' ? 'json' : 'xlsx'
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = command === 'template-xlsx' ? `签名文件导入模板.${ext}` : `签名文件数据.${ext}`
    link.click()
    window.URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch { ElMessage.error('导出失败') }
  finally { exporting.value = false }
}

onMounted(async () => {
  await fetchList()
  await fetchCompanies()
})
</script>

<template>
  <div class="sign-container">
    <el-card>
      <el-form :model="searchForm" inline class="search-form">
        <el-form-item label="所属公司">
          <el-select v-model="searchForm.companyId" placeholder="请选择公司" clearable style="width:220px">
            <el-option v-for="c in companies" :key="c.id" :label="c.companyName" :value="c.id" />
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
        <el-table-column prop="companyName" label="所属公司" min-width="160" show-overflow-tooltip />
        <el-table-column prop="storeFile" label="文件路径" min-width="200" show-overflow-tooltip />
        <el-table-column prop="keyAlias" label="别名" min-width="120" show-overflow-tooltip />
        <el-table-column prop="remark" label="备注" min-width="160" show-overflow-tooltip />
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

    <el-dialog :title="title" v-model="open" width="550px" append-to-body>
      <el-form :model="form" :rules="rules" label-width="100px">
        <el-form-item label="所属公司" prop="companyId">
          <el-select v-model="form.companyId" placeholder="请选择公司" style="width:100%">
            <el-option v-for="c in companies" :key="c.id" :label="c.companyName" :value="c.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="文件路径"><el-input v-model="form.storeFile" placeholder="请输入文件路径" /></el-form-item>
        <el-form-item label="密码"><el-input v-model="form.storePassword" type="password" placeholder="请输入密码" show-password /></el-form-item>
        <el-form-item label="别名"><el-input v-model="form.keyAlias" placeholder="请输入签名别名" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="form.remark" type="textarea" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="open = false">取消</el-button><el-button type="primary" @click="handleSubmit">确定</el-button></template>
    </el-dialog>

    <input ref="fileInput" type="file" accept=".xlsx,.xls" style="display: none" @change="handleFileChange" />
  </div>
</template>

<style scoped>
.sign-container { padding: 16px; }
.search-form { margin-bottom: 8px; }
.toolbar { display: flex; gap: 8px; margin-bottom: 12px; }
.pagination { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>