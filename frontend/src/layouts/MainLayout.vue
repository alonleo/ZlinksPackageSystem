<script setup lang="ts">
import { ref, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { useTabsStore } from '@/stores/tabs'
import {
  HomeFilled,
  Monitor,
  Goods,
  Connection,
  User,
  Lock,
  OfficeBuilding,
  Document,
  Bell,
  Key,
  SwitchButton,
  Fold,
  Expand,
  ArrowDown,
  Close,
  Tickets,
  Menu as IconMenu,
  Tools,
  Setting,
} from '@element-plus/icons-vue'
import { ElMessageBox } from 'element-plus'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()
const tabsStore = useTabsStore()
const isCollapse = ref(false)

watch(
  () => route.fullPath,
  () => {
    tabsStore.addTab(route)
  },
  { immediate: true },
)

const menuItems = [
  { index: '/', icon: HomeFilled, title: '首页' },
  { index: '/games', icon: Monitor, title: '游戏管理' },
  { index: '/products', icon: Goods, title: '产品管理' },
  { index: '/tests', icon: Connection, title: '测试管理' },
  { index: '/system/user', icon: User, title: '用户管理' },
  { index: '/system/role', icon: Lock, title: '角色管理' },
  { index: '/system/menu', icon: IconMenu, title: '菜单管理' },
  { index: '/system/config', icon: Tools, title: '参数设置' },
  { index: '/notice', icon: Bell, title: '通知管理' },
  { index: '/sign-files', icon: Key, title: '签名管理' },
  { index: '/copyrights', icon: Document, title: '软著管理' },
  { index: '/platforms', icon: Setting, title: '平台管理' },
  { index: '/companies', icon: OfficeBuilding, title: '公司管理' },
  { index: '/operation-logs', icon: Tickets, title: '操作日志' },
  { index: '/permissions', icon: Lock, title: '权限管理' },
]

const monitorItems = [
  { index: '/system/monitor/server', title: '服务监控' },
  { index: '/system/monitor/online', title: '在线用户' },
  { index: '/system/monitor/job', title: '定时任务' },
  { index: '/system/monitor/druid', title: '数据监控' },
  { index: '/system/monitor/cache', title: '缓存监控' },
  { index: '/system/monitor/logininfor', title: '登录日志' },
  { index: '/system/monitor/operlog', title: '操作日志' },
]

const handleMenuSelect = (index: string) => {
  router.push(index)
}

const handleLogout = () => {
  userStore.logout()
  router.push({ name: 'login' })
}

const handleTabClick = (path: string) => {
  tabsStore.activeTab = path
  router.push(path)
}

const handleTabRemove = (path: string) => {
  tabsStore.removeTab(path)
  if (tabsStore.activeTab) {
    router.push(tabsStore.activeTab)
  } else {
    router.push('/')
  }
}

const handleCloseAll = async () => {
  try {
    await ElMessageBox.confirm('确定要关闭所有标签页吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning',
    })
    tabsStore.closeAllTabs()
    router.push('/')
  } catch {
    // cancelled
  }
}
</script>

<template>
  <el-container class="layout-container">
    <el-aside :width="isCollapse ? '64px' : '200px'" class="aside">
      <div class="logo">
        <h1 v-if="!isCollapse">Zlinks</h1>
        <h1 v-else>Z</h1>
      </div>
      <el-menu
        :default-active="$route.path"
        :collapse="isCollapse"
        @select="handleMenuSelect"
        background-color="#304156"
        text-color="#bfcbd9"
        active-text-color="#409eff"
      >
        <el-menu-item
          v-for="item in menuItems"
          :key="item.index"
          :index="item.index"
        >
          <el-icon><component :is="item.icon" /></el-icon>
          <template #title>{{ item.title }}</template>
        </el-menu-item>
        <el-sub-menu index="monitor">
          <template #title>
            <el-icon><Monitor /></el-icon>
            <span>系统监控</span>
          </template>
          <el-menu-item
            v-for="item in monitorItems"
            :key="item.index"
            :index="item.index"
          >
            {{ item.title }}
          </el-menu-item>
        </el-sub-menu>
      </el-menu>
    </el-aside>
    <el-container>
      <el-header class="header">
        <div class="header-left">
          <el-icon
            class="collapse-btn"
            @click="isCollapse = !isCollapse"
          >
            <Fold v-if="!isCollapse" />
            <Expand v-else />
          </el-icon>
          <el-breadcrumb separator="/">
            <el-breadcrumb-item :to="{ path: '/' }">首页</el-breadcrumb-item>
            <el-breadcrumb-item v-if="$route.name !== 'home'">
              {{ $route.meta.title || $route.name }}
            </el-breadcrumb-item>
          </el-breadcrumb>
        </div>
        <div class="header-right">
          <el-dropdown @command="handleLogout">
            <span class="user-info">
              <el-icon><User /></el-icon>
              {{ userStore.currentUser?.realName || '用户' }}
              <el-tag
                v-for="name in userStore.currentUser?.groupNames"
                :key="name"
                size="small"
                effect="plain"
                class="role-tag"
              >
                {{ name }}
              </el-tag>
              <el-icon class="el-icon--right"><ArrowDown /></el-icon>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="logout">
                  <el-icon><SwitchButton /></el-icon>
                  退出登录
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>
      <div v-if="tabsStore.tabs.length > 0" class="tab-bar">
        <div class="tab-list">
          <div
            v-for="tab in tabsStore.tabs"
            :key="tab.path"
            class="tab-item"
            :class="{ active: tabsStore.activeTab === tab.path }"
            @click="handleTabClick(tab.path)"
          >
            <span class="tab-title">{{ tab.title }}</span>
            <el-icon
              class="tab-close"
              @click.stop="handleTabRemove(tab.path)"
            >
              <Close />
            </el-icon>
          </div>
        </div>
        <el-dropdown trigger="click" class="tab-actions">
          <span class="tab-dropdown-trigger">
            <el-icon><ArrowDown /></el-icon>
          </span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item @click="handleCloseAll">
                <el-icon><Close /></el-icon>
                关闭所有标签页
              </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
      <el-main class="main">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<style scoped>
.layout-container {
  height: 100vh;
}

.aside {
  background-color: #304156;
  transition: width 0.3s;
  overflow: hidden;
}

.logo {
  height: 60px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  background-color: #263445;
}

.logo h1 {
  margin: 0;
  font-size: 20px;
}

.header {
  background-color: #fff;
  box-shadow: 0 1px 4px rgba(0, 21, 41, 0.08);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 20px;
}

.collapse-btn {
  font-size: 20px;
  cursor: pointer;
  color: #666;
}

.collapse-btn:hover {
  color: #409eff;
}

.header-right {
  display: flex;
  align-items: center;
}

.user-info {
  display: flex;
  align-items: center;
  cursor: pointer;
  color: #666;
}

.user-info:hover {
  color: #409eff;
}

.role-tag {
  margin-left: 8px;
}

.main {
  background-color: #f0f2f5;
  padding: 20px;
}

.tab-bar {
  display: flex;
  align-items: center;
  background: #fff;
  border-bottom: 1px solid #e4e7ed;
  padding: 0 12px;
  height: 36px;
  flex-shrink: 0;
}

.tab-list {
  display: flex;
  align-items: center;
  flex: 1;
  overflow-x: auto;
  height: 100%;
  scrollbar-width: none;
}

.tab-list::-webkit-scrollbar {
  display: none;
}

.tab-item {
  display: flex;
  align-items: center;
  height: 100%;
  padding: 0 12px;
  font-size: 13px;
  color: #666;
  border-right: 1px solid #e4e7ed;
  cursor: pointer;
  white-space: nowrap;
  position: relative;
  transition: color 0.2s;
  flex-shrink: 0;
}

.tab-item:hover {
  color: #409eff;
}

.tab-item.active {
  color: #409eff;
  background: #f0f2f5;
}

.tab-title {
  max-width: 120px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.tab-close {
  margin-left: 6px;
  font-size: 12px;
  border-radius: 50%;
  padding: 2px;
  transition: background 0.2s;
}

.tab-close:hover {
  background: #c0c4cc;
  color: #fff;
}

.tab-actions {
  flex-shrink: 0;
  margin-left: 8px;
}

.tab-dropdown-trigger {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  cursor: pointer;
  color: #666;
  border-radius: 4px;
}

.tab-dropdown-trigger:hover {
  background: #f0f2f5;
  color: #409eff;
}
</style>