<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { gameApi } from '@/api/game'
import type { Game } from '@/types/game'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Search, Edit, Delete, View } from '@element-plus/icons-vue'

const router = useRouter()
const loading = ref(false)
const gameList = ref<Game[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const searchForm = ref({
  gameName: '',
  status: '',
})

const statusOptions = [
  { label: '全部', value: '' },
  { label: '进行中', value: 'active' },
  { label: '已完成', value: 'completed' },
  { label: '已暂停', value: 'paused' },
]

const fetchGameList = async () => {
  loading.value = true
  try {
    const response = await gameApi.getList({
      current: currentPage.value,
      size: pageSize.value,
      ...searchForm.value,
    })
    gameList.value = response.data.records
    total.value = response.data.total
  } catch (error) {
    ElMessage.error('获取游戏列表失败')
  } finally {
    loading.value = false
  }
}

const handleSearch = () => {
  currentPage.value = 1
  fetchGameList()
}

const handleReset = () => {
  searchForm.value = { gameName: '', status: '' }
  handleSearch()
}

const handleSizeChange = (val: number) => {
  pageSize.value = val
  fetchGameList()
}

const handleCurrentChange = (val: number) => {
  currentPage.value = val
  fetchGameList()
}

const handleCreate = () => {
  router.push({ name: 'game-detail', params: { id: 'new' } })
}

const handleEdit = (id: number) => {
  router.push({ name: 'game-detail', params: { id } })
}

const handleDelete = async (id: number) => {
  try {
    await ElMessageBox.confirm('确定要删除这个游戏吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning',
    })
    await gameApi.delete(id)
    ElMessage.success('删除成功')
    fetchGameList()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

const getStatusLabel = (status: string) => {
  const option = statusOptions.find(item => item.value === status)
  return option ? option.label : status
}

const getStatusType = (status: string) => {
  switch (status) {
    case 'active':
      return 'success'
    case 'completed':
      return 'info'
    case 'paused':
      return 'warning'
    default:
      return ''
  }
}

onMounted(() => {
  fetchGameList()
})
</script>

<template>
  <div class="game-list-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <span>游戏管理</span>
          <el-button type="primary" :icon="Plus" @click="handleCreate">
            新增游戏
          </el-button>
        </div>
      </template>

      <el-form :model="searchForm" inline class="search-form">
        <el-form-item label="游戏名称">
          <el-input
            v-model="searchForm.gameName"
            placeholder="请输入游戏名称"
            clearable
            @keyup.enter="handleSearch"
          />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.status" placeholder="请选择状态" clearable>
            <el-option
              v-for="item in statusOptions"
              :key="item.value"
              :label="item.label"
              :value="item.value"
            />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="handleSearch">
            搜索
          </el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>

      <el-table
        v-loading="loading"
        :data="gameList"
        border
        style="width: 100%"
      >
        <el-table-column prop="id" label="ID" width="80" />
        <el-table-column prop="gameName" label="游戏名称" min-width="150" />
        <el-table-column prop="gameDirection" label="游戏方向" width="120" />
        <el-table-column prop="source" label="来源" width="120" />
        <el-table-column prop="manager" label="负责人" width="100" />
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)">
              {{ getStatusLabel(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="priority" label="优先级" width="80" />
        <el-table-column prop="createTime" label="创建时间" width="180" />
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link :icon="View" @click="handleEdit(row.id)">
              查看
            </el-button>
            <el-button type="primary" link :icon="Edit" @click="handleEdit(row.id)">
              编辑
            </el-button>
            <el-button type="danger" link :icon="Delete" @click="handleDelete(row.id)">
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-container">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
        />
      </div>
    </el-card>
  </div>
</template>

<style scoped>
.game-list-container {
  padding: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-form {
  margin-bottom: 20px;
}

.pagination-container {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>