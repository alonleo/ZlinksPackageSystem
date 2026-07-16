import api from '@/api'
import type { HonorParam, VivoParam, HuaweiParam } from '@/types/ad-param'
import type { PageResult } from '@/types/common'

interface ListParams {
  current?: number
  size?: number
  productId?: number
  adParamStatus?: string
  listStatus?: string
}

function buildCrud<T>(path: string) {
  return {
    getList(params: ListParams): Promise<{ data: PageResult<T> }> {
      return api.get(path, { params })
    },
    getById(id: number): Promise<{ data: T }> {
      return api.get(`${path}/${id}`)
    },
    create(data: Partial<T>): Promise<{ data: T }> {
      return api.post(path, data)
    },
    update(id: number, data: Partial<T>): Promise<{ data: T }> {
      return api.put(`${path}/${id}`, data)
    },
    delete(id: number): Promise<void> {
      return api.delete(`${path}/${id}`)
    },
    importFile(file: File): Promise<{ data: string }> {
      const form = new FormData()
      form.append('file', file)
      return api.post(`${path}/import`, form, { headers: { 'Content-Type': 'multipart/form-data' } })
    },
    exportFile(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
      return api.get(`${path}/export`, { params: { format }, responseType: 'blob' })
    },
    downloadTemplate(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
      return api.get(`${path}/template`, { params: { format }, responseType: 'blob' })
    },
  }
}

export const honorApi = buildCrud<HonorParam>('/honor-params')
export const vivoApi = buildCrud<VivoParam>('/vivo-params')
export const huaweiApi = buildCrud<HuaweiParam>('/huawei-params')
