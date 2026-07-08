import api from '@/api'
import type { OperationLog } from '@/types/operation-log'
import type { PageResult } from '@/types/common'

export interface LogListParams {
  current?: number
  size?: number
  username?: string
  module?: string
  action?: string
}

export const operationLogApi = {
  getList(params: LogListParams): Promise<{ data: PageResult<OperationLog> }> {
    return api.get('/operation-logs', { params })
  },

  exportFile(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/operation-logs/export', { params: { format }, responseType: 'blob' })
  },
}
