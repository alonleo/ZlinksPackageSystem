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
        },
        {
          path: 'games',
          name: 'games',
          component: () => import('@/views/game/GameListView.vue'),
        },
        {
          path: 'games/:id',
          name: 'game-detail',
          component: () => import('@/views/game/GameDetailView.vue'),
        },
        {
          path: 'products',
          name: 'products',
          component: () => import('@/views/product/ProductListView.vue'),
        },
        {
          path: 'tests',
          name: 'tests',
          component: () => import('@/views/test/TestListView.vue'),
        },
        {
          path: 'users',
          name: 'users',
          component: () => import('@/views/user/UserListView.vue'),
        },
        {
          path: 'permissions',
          name: 'permissions',
          component: () => import('@/views/permission/PermissionListView.vue'),
        },
        {
          path: 'companies',
          name: 'companies',
          component: () => import('@/views/company/CompanyListView.vue'),
        },
        {
          path: 'copyrights',
          name: 'copyrights',
          component: () => import('@/views/copyright/CopyrightListView.vue'),
        },
        {
          path: 'notifications',
          name: 'notifications',
          component: () => import('@/views/notification/NotificationListView.vue'),
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