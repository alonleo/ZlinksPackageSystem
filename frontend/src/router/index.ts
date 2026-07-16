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
          path: 'package/games',
          name: 'package-games',
          component: () => import('@/views/package/GameListView.vue'),
          meta: { title: '游戏管理' },
        },
        {
          path: 'package/products',
          name: 'package-products',
          component: () => import('@/views/package/ProductListView.vue'),
          meta: { title: '产品管理' },
        },
        {
          path: 'package/tests',
          name: 'package-tests',
          component: () => import('@/views/package/TestListView.vue'),
          meta: { title: '测试管理' },
        },
        {
          path: 'package/ad-params',
          name: 'package-ad-params',
          component: () => import('@/views/package/AdParamListView.vue'),
          meta: { title: '广告参数' },
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
          path: 'platforms',
          name: 'platforms',
          component: () => import('@/views/platform/PlatformListView.vue'),
          meta: { title: '平台管理' },
        },
        {
          path: 'notice',
          name: 'notice',
          component: () => import('@/views/system/notice/index.vue'),
          meta: { title: '通知管理' },
        },
        {
          path: 'operation-logs',
          name: 'operation-logs',
          component: () => import('@/views/log/LogListView.vue'),
          meta: { title: '操作日志' },
        },
        {
          path: 'system/user',
          name: 'system-user',
          component: () => import('@/views/system/user/index.vue'),
          meta: { title: '用户管理' },
        },
        {
          path: 'system/role',
          name: 'system-role',
          component: () => import('@/views/system/role/index.vue'),
          meta: { title: '角色管理' },
        },
        {
          path: 'system/menu',
          name: 'system-menu',
          component: () => import('@/views/system/menu/index.vue'),
          meta: { title: '菜单管理' },
        },
        {
          path: 'system/config',
          name: 'system-config',
          component: () => import('@/views/system/config/index.vue'),
          meta: { title: '参数设置' },
        },
        {
          path: 'system/monitor/server',
          name: 'monitor-server',
          component: () => import('@/views/system/monitor/server/index.vue'),
          meta: { title: '服务监控' },
        },
        {
          path: 'system/monitor/online',
          name: 'monitor-online',
          component: () => import('@/views/system/monitor/online/index.vue'),
          meta: { title: '在线用户' },
        },
        {
          path: 'system/monitor/job',
          name: 'monitor-job',
          component: () => import('@/views/system/monitor/job/index.vue'),
          meta: { title: '定时任务' },
        },
        {
          path: 'system/monitor/job/log/:jobId?',
          name: 'monitor-job-log',
          component: () => import('@/views/system/monitor/job/log.vue'),
          meta: { title: '调度日志' },
        },
        {
          path: 'system/monitor/druid',
          name: 'monitor-druid',
          component: () => import('@/views/system/monitor/druid/index.vue'),
          meta: { title: '数据监控' },
        },
        {
          path: 'system/monitor/cache',
          name: 'monitor-cache',
          component: () => import('@/views/system/monitor/cache/index.vue'),
          meta: { title: '缓存监控' },
        },
        {
          path: 'system/monitor/logininfor',
          name: 'monitor-logininfor',
          component: () => import('@/views/system/monitor/logininfor/index.vue'),
          meta: { title: '登录日志' },
        },
        {
          path: 'system/monitor/operlog',
          name: 'monitor-operlog',
          component: () => import('@/views/log/LogListView.vue'),
          meta: { title: '操作日志' },
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