import { createRouter, createWebHistory } from 'vue-router'
import { useUserStore } from '@/stores/user'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/auth/LoginView.vue'),
      meta: { requiresAuth: false },
    },
    {
      path: '/',
      component: () => import('@/layouts/MainLayout.vue'),
      meta: { requiresAuth: true },
      children: [
        {
          path: '',
          name: 'home',
          component: () => import('@/views/home/HomeView.vue'),
          meta: { title: '首页' },
        },
        {
          path: 'games',
          name: 'games',
          component: () => import('@/views/game/GameListView.vue'),
          meta: { title: '游戏管理' },
        },
        {
          path: 'products',
          name: 'products',
          component: () => import('@/views/product/ProductListView.vue'),
          meta: { title: '产品管理' },
        },
        {
          path: 'tests',
          name: 'tests',
          component: () => import('@/views/test/TestListView.vue'),
          meta: { title: '测试管理' },
        },
        {
          path: 'users',
          name: 'users',
          component: () => import('@/views/user/UserListView.vue'),
          meta: { title: '用户管理' },
        },
        {
          path: 'permissions',
          name: 'permissions',
          component: () => import('@/views/permission/PermissionListView.vue'),
          meta: { title: '权限管理' },
        },
        {
          path: 'companies',
          name: 'companies',
          component: () => import('@/views/company/CompanyListView.vue'),
          meta: { title: '公司管理' },
        },
        {
          path: 'sign-files',
          name: 'sign-files',
          component: () => import('@/views/sign/SignFileListView.vue'),
          meta: { title: '签名管理' },
        },
        {
          path: 'copyrights',
          name: 'copyrights',
          component: () => import('@/views/copyright/CopyrightListView.vue'),
          meta: { title: '软著管理' },
        },
        {
          path: 'notifications',
          name: 'notifications',
          component: () => import('@/views/notification/NotificationListView.vue'),
          meta: { title: '通知管理' },
        },
        {
          path: 'operation-logs',
          name: 'operation-logs',
          component: () => import('@/views/log/LogListView.vue'),
          meta: { title: '日志管理' },
        },
      ],
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'not-found',
      component: () => import('@/views/NotFoundView.vue'),
    },
  ],
})

router.beforeEach((to, _from, next) => {
  const userStore = useUserStore()
  const requiresAuth = to.meta.requiresAuth !== false

  if (requiresAuth && !userStore.isAuthenticated) {
    next({ name: 'login', query: { redirect: to.fullPath } })
  } else if (to.name === 'login' && userStore.isAuthenticated) {
    next({ name: 'home' })
  } else {
    next()
  }
})

export default router