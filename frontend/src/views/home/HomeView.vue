<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { noticeApi } from '@/api/system/menu'
import type { SysNotice } from '@/types/system'

const logs = ref([
  { id: 1, content: '用户 admin 登录了系统', time: '2024-01-15 10:30:00' },
  { id: 2, content: '游戏《王者荣耀》信息已更新', time: '2024-01-15 09:15:00' },
  { id: 3, content: '新增软著《游戏管理系统V1.0》', time: '2024-01-14 16:45:00' },
])

interface Announcement {
  id: number
  title: string
  content: string
  time: string
}
const announcements = ref<Announcement[]>([])

const fetchAnnouncements = async () => {
  try {
    const { data } = await noticeApi.list({ current: 1, size: 5 })
    announcements.value = data.records.map((n: SysNotice) => ({
      id: n.noticeId ?? 0,
      title: n.noticeTitle,
      content: n.noticeContent || '',
      time: n.createTime?.substring(0, 10) || '',
    }))
  } catch (error) {
    console.error('获取公告失败:', error)
  }
}

const quickLinks = ref([
  { id: 1, title: '首页', icon: 'HomeFilled', path: '/' },
  { id: 2, title: '打包管理', icon: 'Box', path: '/package/games' },
  { id: 3, title: '用户管理', icon: 'User', path: '/system-mgmt/users' },
  { id: 4, title: '系统监控', icon: 'Monitor', path: '/system/monitor/server' },
])

onMounted(() => fetchAnnouncements())
</script>

<template>
  <div class="home-container">
    <el-row :gutter="20">
      <el-col :span="16">
        <el-card class="box-card">
          <template #header>
            <div class="card-header">
              <span>最近日志</span>
            </div>
          </template>
          <div v-for="log in logs" :key="log.id" class="log-item">
            <div class="log-content">{{ log.content }}</div>
            <div class="log-time">{{ log.time }}</div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card class="box-card">
          <template #header>
            <div class="card-header">
              <span>公告通知</span>
            </div>
          </template>
          <div v-if="announcements.length === 0" class="announcement-item" style="color: #999">
            暂无公告
          </div>
          <div v-for="item in announcements" :key="item.id" class="announcement-item">
            <div class="announcement-title">{{ item.title }}</div>
            <div class="announcement-content">{{ item.content }}</div>
            <div class="announcement-time">{{ item.time }}</div>
          </div>
        </el-card>
      </el-col>
    </el-row>
    
    <el-card class="box-card" style="margin-top: 20px;">
      <template #header>
        <div class="card-header">
          <span>快捷访问</span>
        </div>
      </template>
      <el-row :gutter="20">
        <el-col :span="6" v-for="link in quickLinks" :key="link.id">
          <router-link :to="link.path" class="quick-link">
            <el-icon :size="40"><component :is="link.icon" /></el-icon>
            <span>{{ link.title }}</span>
          </router-link>
        </el-col>
      </el-row>
    </el-card>
  </div>
</template>

<style scoped>
.home-container { padding: 20px; }
.box-card { margin-bottom: 20px; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
.log-item { padding: 10px 0; border-bottom: 1px solid #eee; }
.log-item:last-child { border-bottom: none; }
.log-content { color: #333; margin-bottom: 5px; }
.log-time { color: #999; font-size: 12px; }
.announcement-item { padding: 10px 0; border-bottom: 1px solid #eee; }
.announcement-item:last-child { border-bottom: none; }
.announcement-title { font-weight: bold; color: #333; margin-bottom: 5px; }
.announcement-content { color: #666; font-size: 14px; margin-bottom: 5px; }
.announcement-time { color: #999; font-size: 12px; }
.quick-link { display: flex; flex-direction: column; align-items: center; padding: 20px; text-decoration: none; color: #666; border-radius: 8px; transition: all 0.3s; }
.quick-link:hover { background-color: #f5f7fa; color: #409eff; }
.quick-link span { margin-top: 10px; font-size: 14px; }
</style>
