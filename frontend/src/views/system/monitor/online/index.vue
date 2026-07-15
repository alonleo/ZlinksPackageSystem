<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Delete } from '@element-plus/icons-vue'
import { onlineApi } from '@/api/monitor/online'
import type { UserOnline } from '@/types/monitor/online'

const loading = ref(false)
const list = ref<UserOnline[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)

const searchForm = ref({ ipaddr: '', userName: '' })

const fetchList = async () => {
  loading.value = true
  try {
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.value.ipaddr) params.ipaddr = searchForm.value.ipaddr
    if (searchForm.value.userName) params.userName = searchForm.value.userName
    const { data } = await onlineApi.list(params)
    list.value = data.records
    total.value = data.total
  } catch {
    ElMessage.error('获取在线用户失败')
  } finally { loading.value = false }
}

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => { searchForm.value = { ipaddr: '', userName: '' }; handleSearch() }

const handleForceLogout = async (row: any) => {
  try {
    await ElMessageBox.confirm('确定要强制下线用户 [' + row.userName + '] 吗?', '提示', { type: 'warning' })
    await onlineApi.forceLogout(row.tokenId)
    ElMessage.success('下线成功')
    fetchList()
  } catch { /* cancelled */ }
}

onMounted(() => fetchList())
</script>

<template>
  <div class="online-container">
    <el-card>
      <el-form :model="searchForm" inline class="search-form">
        <el-form-item label="登录地址">
          <el-input v-model="searchForm.ipaddr" placeholder="请输入 IP 地址" clearable @keyup.enter="handleSearch" />
        </el-form-item>
        <el-form-item label="用户名称">
          <el-input v-model="searchForm.userName" placeholder="请输入用户名称" clearable @keyup.enter="handleSearch" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
          <el-button :icon="Refresh" @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
      <el-table v-loading="loading" :data="list" border>
        <el-table-column prop="tokenId" label="会话ID" min-width="180" show-overflow-tooltip />
        <el-table-column prop="userName" label="登录用户" min-width="120" />
        <el-table-column prop="ipaddr" label="主机" min-width="130" />
        <el-table-column prop="loginLocation" label="登录地点" min-width="120" show-overflow-tooltip />
        <el-table-column prop="browser" label="浏览器" min-width="100" show-overflow-tooltip />
        <el-table-column prop="os" label="操作系统" min-width="100" />
        <el-table-column prop="roleKey" label="角色" min-width="120" />
        <el-table-column prop="loginTime" label="登录时间" min-width="160" />
        <el-table-column label="操作" width="100" align="center" fixed="right">
          <template #default="{ row }">
            <el-button v-hasPermi="['monitor:online:forceLogout']" type="danger" link :icon="Delete" @click="handleForceLogout(row)">强退</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination-container">
        <el-pagination v-model:current-page="currentPage" v-model:page-size="pageSize" :page-sizes="[10, 20, 50, 100]" :total="total" layout="total, sizes, prev, pager, next, jumper" @size-change="fetchList" @current-change="fetchList" />
      </div>
    </el-card>
  </div>
</template>

<style scoped>
.online-container { padding: 16px; }
.search-form { margin-bottom: 16px; }
.pagination-container { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>