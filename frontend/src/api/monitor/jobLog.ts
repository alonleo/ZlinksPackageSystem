import api from '@/api'
import type { SysJobLog } from '@/types/monitor/job'
import type { PageResult } from '@/types/common'

export const jobLogApi = {
  list(params: Record<string, unknown>): Promise<{ data: PageResult<SysJobLog> }> {
    return api.get('/monitor/jobLog/list', { params })
  },
  remove(jobLogIds: number[]): Promise<void> {
    return api.delete('/monitor/jobLog/' + jobLogIds.join(','))
  },
  clean(): Promise<void> {
    return api.delete('/monitor/jobLog/clean')
  },
}