<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { copyrightApi } from '@/api/copyright'
import type { Copyright } from '@/types/copyright'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Search, Upload, Download, Document, ArrowDown } from '@element-plus/icons-vue'

const loading = ref(false)
const saving = ref(false)
const deleting = ref(false)
const importing = ref(false)
const exporting = ref(false)
const formRef = ref()
const fileInput = ref<HTMLInputElement>()

const form = ref<Partial<Copyright>>({
  copyrightName: '',
  copyrightOwner: '',
  copyrightNumber: '',
  remark: '',
})
const selectedId = ref<number | null>(null)
const isEditing = ref(false)

const list = ref<Copyright[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const searchForm = ref({ copyrightName: '' })

const isNew = computed(() => selectedId.value === null && isEditing.value)

const rules = {
  copyrightName: [{ required: true, message: '请输入软著名称', trigger: 'blur' }],
}

const fetchList = async () => {
  loading.value = true
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.copyrightName) params.copyrightName = searchForm.value.copyrightName
    const response = await copyrightApi.getList(params)
    list.value = response.data.records
    total.value = response.data.total
  } catch (error) {
    console.error('获取软著列表失败:', error)
  } finally {
    loading.value = false
  }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => { searchForm.value = { copyrightName: '' }; handleSearch() }
const handleSizeChange = (val: number) => { pageSize.value = val; fetchList() }
const handleCurrentChange = (val: number) => { currentPage.value = val; fetchList() }

const handleCreate = () => {
  selectedId.value = null
  isEditing.value = true
  form.value = { copyrightName: '', copyrightOwner: '', copyrightNumber: '', remark: '' }
}

const handleSelect = async (row: Copyright) => {
  selectedId.value = row.id
  isEditing.value = true
  form.value = {
    copyrightName: row.copyrightName || '',
    copyrightOwner: row.copyrightOwner || '',
    copyrightNumber: row.copyrightNumber || '',
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
          await copyrightApi.create(form.value)
          ElMessage.success('创建成功')
        } else {
          await copyrightApi.update(selectedId.value!, form.value)
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
  form.value = { copyrightName: '', copyrightOwner: '', copyrightNumber: '', remark: '' }
}

const handleDeleteCurrent = async () => {
  if (!selectedId.value) return
  try {
    await ElMessageBox.confirm(`确定要删除软著"${form.value.copyrightName}"吗？`, '提示', {
      confirmButtonText: '确定', cancelButtonText: '取消', type: 'warning',
    })
    deleting.value = true
    await copyrightApi.delete(selectedId.value)
    ElMessage.success('删除成功')
    handleCancel()
    fetchList()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('删除失败')
  } finally {
    deleting.value = false
  }
}

const handleImport = () => {
  fileInput.value?.click()
}

const handleFileChange = async (event: Event) => {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return

  importing.value = true
  try {
    const res = await copyrightApi.importFile(file)
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
      const blob = await copyrightApi.exportFile(command)
      downloadBlob(blob, `软著数据.${ext}`)
      ElMessage.success('导出成功')
    } catch { ElMessage.error('导出失败') }
    finally { exporting.value = false }
  } else if (command === 'template-xlsx') {
    try {
      const blob = await copyrightApi.downloadTemplate('xlsx')
      downloadBlob(blob, '软著导入模板.xlsx')
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

onMounted(() => fetchList())
</script>

<template>
  <div class="copyright-container">
    <div class="search-card">
      <div class="card-header">
        <span>筛选条件</span>
      </div>
      <el-form :model="searchForm" inline style="margin-top: 12px">
        <el-form-item label="软著名称">
          <el-input v-model="searchForm.copyrightName" placeholder="请输入软著名称" clearable @keyup.enter="handleSearch" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
          <el-button @click="handleReset">重置</el-button>
          <el-button type="primary" :icon="Plus" @click="handleCreate">新增软著</el-button>
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
              <span>{{ isNew ? '新增软著' : isEditing ? '编辑软著' : '软著详情' }}</span>
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
          <el-form v-else ref="formRef" :model="form" :rules="rules" label-width="80px" class="detail-form">
            <el-form-item label="软著名称" prop="copyrightName">
              <el-input v-model="form.copyrightName" placeholder="请输入软著名称" />
            </el-form-item>
            <el-form-item label="著作权人">
              <el-input v-model="form.copyrightOwner" placeholder="请输入著作权人" />
            </el-form-item>
            <el-form-item label="软著号">
              <el-input v-model="form.copyrightNumber" placeholder="请输入软著号" />
            </el-form-item>
            <el-form-item label="备注">
              <el-input v-model="form.remark" type="textarea" :rows="3" placeholder="请输入备注信息" />
            </el-form-item>
          </el-form>
        </el-card>
      </div>

      <div class="list-panel">
        <el-card class="list-card">
          <template #header>
            <span>软著列表</span>
          </template>
          <el-table v-loading="loading" :data="list" border highlight-current-row
            @row-click="handleSelect" style="width: 100%">
            <el-table-column prop="copyrightName" label="软著名称" min-width="160" />
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
.copyright-container {
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
