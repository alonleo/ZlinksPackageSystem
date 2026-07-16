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
          path: 'system-mgmt/users',
          name: 'system-mgmt-users',
          component: () => import('@/views/system-mgmt/user/index.vue'),
          meta: { title: '用户管理' },
        },
        {
          path: 'system-mgmt/roles',
          name: 'system-mgmt-roles',
          component: () => import('@/views/system-mgmt/role/index.vue'),
          meta: { title: '角色管理' },
        },
        {
          path: 'system-mgmt/notice',
          name: 'system-mgmt-notice',
          component: () => import('@/views/system-mgmt/notice/index.vue'),
          meta: { title: '通知管理' },
        },
        {
          path: 'system-mgmt/permissions',
          name: 'system-mgmt-permissions',
          component: () => import('@/views/system-mgmt/permissions/PermissionListView.vue'),
          meta: { title: '权限管理' },
        },
        {
          path: 'system-settings/menus',
          name: 'system-settings-menus',
          component: () => import('@/views/system-settings/menus/index.vue'),
          meta: { title: '菜单管理' },
        },
        {
          path: 'system-settings/config',
          name: 'system-settings-config',
          component: () => import('@/views/system-settings/config/index.vue'),
          meta: { title: '参数设置' },
        },
        {
          path: 'package/platforms',
          name: 'package-platforms',
          component: () => import('@/views/package/platforms/PlatformListView.vue'),
          meta: { title: '平台管理' },
        },
        {
          path: 'package/sign-files',
          name: 'package-sign-files',
          component: () => import('@/views/package/sign-files/SignFileListView.vue'),
          meta: { title: '签名管理' },
        },
        {
          path: 'package/copyrights',
          name: 'package-copyrights',
          component: () => import('@/views/package/copyrights/CopyrightListView.vue'),
          meta: { title: '软著管理' },
        },
        {
          path: 'package/companies',
          name: 'package-companies',
          component: () => import('@/views/package/companies/CompanyListView.vue'),
          meta: { title: '公司管理' },
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