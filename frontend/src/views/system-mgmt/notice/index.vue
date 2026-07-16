<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete, View } from '@element-plus/icons-vue'
import { noticeApi } from '@/api/system/menu'
import type { SysNotice } from '@/types/system'

const loading = ref(false)
const list = ref<SysNotice[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const open = ref(false)
const detailOpen = ref(false)
const title = ref('')
const ids = ref<number[]>([])

const searchForm = ref({ noticeTitle: '', noticeType: '', createBy: '' })
const form = reactive<SysNotice>({ noticeTitle: '', noticeType: '1', noticeContent: '', status: '0' })

const rules = {
  noticeTitle: [{ required: true, message: '公告标题不能为空', trigger: 'blur' }],
  noticeType: [{ required: true, message: '公告类型不能为空', trigger: 'change' }],
}

const fetchList = async () => {
  loading.value = true
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.noticeTitle) params.noticeTitle = searchForm.value.noticeTitle
    if (searchForm.value.noticeType) params.noticeType = searchForm.value.noticeType
    if (searchForm.value.createBy) params.createBy = searchForm.value.createBy
    const { data } = await noticeApi.list(params)
    list.value = data.records; total.value = data.total
  } catch { ElMessage.error('获取公告列表失败') }
  finally { loading.value = false }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => { searchForm.value = { noticeTitle: '', noticeType: '', createBy: '' }; handleSearch() }
const handleSelectionChange = (rows: any[]) => { ids.value = rows.map((r: any) => r.noticeId) }
const resetForm = () => { form.noticeId = undefined; form.noticeTitle = ''; form.noticeType = '1'; form.noticeContent = ''; form.status = '0' }
const handleAdd = () => { resetForm(); title.value = '添加公告'; open.value = true }

const handleUpdate = (row?: any) => {
  const id = row?.noticeId ?? ids.value[0]
  if (!id) return
  noticeApi.get(id).then(({ data }) => { Object.assign(form, data); title.value = '修改公告'; open.value = true })
}

const handleView = (row: any) => { Object.assign(form, row); detailOpen.value = true }

const handleSubmit = async () => {
  try {
    form.noticeId ? await noticeApi.update(form) : await noticeApi.add(form)
    ElMessage.success(form.noticeId ? '修改成功' : '新增成功')
    open.value = false; fetchList()
  } catch { ElMessage.error('操作失败') }
}

const handleDelete = async (row?: any) => {
  const noticeIds = row?.noticeId ? [row.noticeId] : ids.value
  if (!noticeIds.length) return
  try {
    await ElMessageBox.confirm('是否确认删除该公告?', '提示', { type: 'warning' })
    await noticeApi.remove(noticeIds)
    ElMessage.success('删除成功'); fetchList()
  } catch { /* cancelled */ }
}

onMounted(() => fetchList())
</script>

<template>
  <div class="notice-container">
    <el-card>
      <el-form :model="searchForm" inline class="search-form">
        <el-form-item label="公告标题"><el-input v-model="searchForm.noticeTitle" placeholder="请输入" clearable @keyup.enter="handleSearch" /></el-form-item>
        <el-form-item label="公告类型"><el-select v-model="searchForm.noticeType" placeholder="请选择" clearable style="width:120px"><el-option label="通知" value="1" /><el-option label="公告" value="2" /></el-select></el-form-item>
        <el-form-item label="创建者"><el-input v-model="searchForm.createBy" placeholder="请输入" clearable @keyup.enter="handleSearch" /></el-form-item>
        <el-form-item><el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button><el-button :icon="Refresh" @click="handleReset">重置</el-button></el-form-item>
      </el-form>
      <div class="toolbar">
        <el-button v-hasPermi="['system:notice:add']" type="primary" :icon="Plus" @click="handleAdd">新增</el-button>
        <el-button v-hasPermi="['system:notice:edit']" type="success" :icon="Edit" :disabled="ids.length !== 1" @click="handleUpdate()">修改</el-button>
        <el-button v-hasPermi="['system:notice:remove']" type="danger" :icon="Delete" :disabled="!ids.length" @click="handleDelete()">删除</el-button>
      </div>
      <el-table v-loading="loading" :data="list" border @selection-change="handleSelectionChange">
        <el-table-column type="selection" width="50" align="center" />
        <el-table-column prop="noticeId" label="公告编号" width="100" align="center" />
        <el-table-column prop="noticeTitle" label="公告标题" min-width="200" show-overflow-tooltip><template #default="{ row }"><a class="link" @click="handleView(row)">{{ row.noticeTitle }}</a></template></el-table-column>
        <el-table-column label="公告类型" width="90" align="center"><template #default="{ row }"><el-tag :type="row.noticeType === '1' ? 'info' : 'warning'">{{ row.noticeType === '1' ? '通知' : '公告' }}</el-tag></template></el-table-column>
        <el-table-column label="状态" width="80" align="center"><template #default="{ row }"><el-tag :type="row.status === '0' ? 'success' : 'danger'">{{ row.status === '0' ? '正常' : '关闭' }}</el-tag></template></el-table-column>
        <el-table-column prop="createBy" label="创建者" width="100" />
        <el-table-column prop="createTime" label="创建时间" min-width="160" />
        <el-table-column label="操作" width="120" align="center" fixed="right">
          <template #default="{ row }">
            <el-tooltip content="查看"><el-button link type="info" :icon="View" @click="handleView(row)" /></el-tooltip>
            <el-tooltip content="修改"><el-button v-hasPermi="['system:notice:edit']" link type="primary" :icon="Edit" @click="handleUpdate(row)" /></el-tooltip>
            <el-tooltip content="删除"><el-button v-hasPermi="['system:notice:remove']" link type="danger" :icon="Delete" @click="handleDelete(row)" /></el-tooltip>
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination"><el-pagination v-model:current-page="currentPage" v-model:page-size="pageSize" :page-sizes="[10, 20, 50, 100]" :total="total" layout="total, sizes, prev, pager, next, jumper" @size-change="fetchList" @current-change="fetchList" /></div>
    </el-card>

    <el-dialog :title="title" v-model="open" width="650px" append-to-body>
      <el-form :model="form" :rules="rules" label-width="80px">
        <el-form-item label="公告标题" prop="noticeTitle"><el-input v-model="form.noticeTitle" placeholder="请输入公告标题" /></el-form-item>
        <el-form-item label="公告类型" prop="noticeType"><el-radio-group v-model="form.noticeType"><el-radio value="1">通知</el-radio><el-radio value="2">公告</el-radio></el-radio-group></el-form-item>
        <el-form-item label="状态"><el-radio-group v-model="form.status"><el-radio value="0">正常</el-radio><el-radio value="1">关闭</el-radio></el-radio-group></el-form-item>
        <el-form-item label="内容"><el-input v-model="form.noticeContent" type="textarea" :rows="6" placeholder="请输入内容" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="open = false">取消</el-button><el-button type="primary" @click="handleSubmit">确定</el-button></template>
    </el-dialog>

    <el-dialog title="公告详情" v-model="detailOpen" width="650px" append-to-body>
      <el-descriptions :column="1" border>
        <el-descriptions-item label="标题">{{ form.noticeTitle }}</el-descriptions-item>
        <el-descriptions-item label="类型">{{ form.noticeType === '1' ? '通知' : '公告' }}</el-descriptions-item>
        <el-descriptions-item label="内容"><div v-html="form.noticeContent" style="white-space:pre-wrap" /></el-descriptions-item>
        <el-descriptions-item label="创建者">{{ form.createBy }}</el-descriptions-item>
        <el-descriptions-item label="创建时间">{{ form.createTime }}</el-descriptions-item>
      </el-descriptions>
    </el-dialog>
  </div>
</template>

<style scoped>
.notice-container { padding: 16px; }
.search-form { margin-bottom: 8px; }
.toolbar { display: flex; gap: 8px; margin-bottom: 12px; }
.pagination { margin-top: 16px; display: flex; justify-content: flex-end; }
.link { color: #409eff; cursor: pointer; }
.link:hover { text-decoration: underline; }
</style>