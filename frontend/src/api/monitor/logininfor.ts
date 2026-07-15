import api from '@/api'
import type { Logininfor, LogininforQuery } from '@/types/monitor/logininfor'
import type { PageResult } from '@/types/common'

export const logininforApi = {
  list(params: LogininforQuery): Promise<{ data: PageResult<Logininfor> }> {
    return api.get('/monitor/logininfor/list', { params })
  },
  remove(infoIds: number[]): Promise<void> {
    return api.delete('/monitor/logininfor/' + infoIds.join(','))
  },
  clean(): Promise<void> {
    return api.delete('/monitor/logininfor/clean')
  },
  unlock(userName: string): Promise<void> {
    return api.put('/monitor/logininfor/unlock/' + encodeURIComponent(userName))
  },
}