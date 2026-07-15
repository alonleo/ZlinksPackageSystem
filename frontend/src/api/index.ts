import axios from 'axios'
import { ElMessage } from 'element-plus'
import { useUserStore } from '@/stores/user'
import router from '@/router'

const api = axios.create({
  baseURL: '/api',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
})

api.interceptors.request.use(
  (config) => {
    const userStore = useUserStore()
    if (userStore.token) {
      config.headers.Authorization = `Bearer ${userStore.token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

api.interceptors.response.use(
  (response) => {
    return response.data
  },
  (error) => {
    if (error.response) {
      const { status } = error.response
      const userStore = useUserStore()

      if (status === 401) {
        userStore.logout()
        router.push({ name: 'login' })
      } else if (status === 403) {
        ElMessage.error('您没有该操作权限')
      } else if (status >= 500) {
        ElMessage.error(error.response.data?.msg ?? '服务异常')
      }
    } else {
      ElMessage.error('网络异常,请稍后重试')
    }
    return Promise.reject(error)
  }
)

export default api