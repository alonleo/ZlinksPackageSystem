import axios, { AxiosError } from 'axios'
import { ElMessage } from 'element-plus'
import { useUserStore } from '@/stores/user'
import router from '@/router'
import type { Result } from '@/types/common'

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
  (error) => Promise.reject(error)
)

api.interceptors.response.use(
  (response) => response.data,
  (error: AxiosError<Result<unknown>>) => {
    if (error.response) {
      const { status, data } = error.response
      const userStore = useUserStore()

      if (status === 401) {
        userStore.logout()
        router.push({ name: 'login' })
      } else if (status === 403) {
        ElMessage.error('您没有该操作权限')
      } else if (status >= 500) {
        ElMessage.error((data as any)?.message ?? '服务异常')
      } else {
        // 4xx 业务错误,返回的 data 可能是 {code,message,data:{...field errors}}
        ElMessage.error((data as any)?.message ?? `请求失败 (${status})`)
      }
    } else {
      ElMessage.error('网络异常,请稍后重试')
    }
    return Promise.reject(error)
  }
)

export default api
