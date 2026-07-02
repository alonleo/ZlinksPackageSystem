<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
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
  SwitchButton,
} from '@element-plus/icons-vue'

const router = useRouter()
const userStore = useUserStore()
const isCollapse = ref(false)

const menuItems = [
  { index: '/', icon: HomeFilled, title: '首页' },
  { index: '/games', icon: Monitor, title: '游戏管理' },
  { index: '/products', icon: Goods, title: '产品管理' },
  { index: '/tests', icon: Connection, title: '测试管理' },
  { index: '/users', icon: User, title: '用户管理' },
  { index: '/permissions', icon: Lock, title: '权限管理' },
  { index: '/companies', icon: OfficeBuilding, title: '公司管理' },
  { index: '/copyrights', icon: Document, title: '软著管理' },
  { index: '/notifications', icon: Bell, title: '通知管理' },
]

const handleMenuSelect = (index: string) => {
  router.push(index)
}

const handleLogout = () => {
  userStore.logout()
  router.push({ name: 'login' })
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

.main {
  background-color: #f0f2f5;
  padding: 20px;
}
</style>