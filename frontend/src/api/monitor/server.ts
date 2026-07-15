import api from '@/api'
import type { ServerInfo } from '@/types/server'

export const serverApi = {
  getInfo(): Promise<{ data: ServerInfo }> {
    return api.get('/monitor/server')
  },
}