import api from '@/api'
import type { SysJob, JobQuery } from '@/types/monitor/job'
import type { PageResult } from '@/types/common'

export const jobApi = {
  list(params: JobQuery): Promise<{ data: PageResult<SysJob> }> {
    return api.get('/monitor/job/list', { params })
  },
  get(jobId: number): Promise<{ data: SysJob }> {
    return api.get('/monitor/job/' + jobId)
  },
  add(data: SysJob): Promise<void> {
    return api.post('/monitor/job', data)
  },
  update(data: SysJob): Promise<void> {
    return api.put('/monitor/job', data)
  },
  remove(jobIds: number[]): Promise<void> {
    return api.delete('/monitor/job/' + jobIds.join(','))
  },
  changeStatus(jobId: number, status: string): Promise<void> {
    return api.put('/monitor/job/changeStatus', { jobId, status })
  },
  run(jobId: number, jobGroup: string): Promise<void> {
    return api.put('/monitor/job/run', { jobId, jobGroup })
  },
}