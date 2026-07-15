import api from '@/api'
import type { Platform } from '@/types/platform'
import type { PageResult } from '@/types/common'

export interface PlatformListParams {
  current?: number
  size?: number
  platformName?: string
  status?: string
}

export const platformApi = {
  getList(params: PlatformListParams): Promise<{ data: PageResult<Platform> }> {
    return api.get('/platforms', { params })
  },

  getById(id: number): Promise<{ data: Platform }> {
    return api.get(`/platforms/${id}`)
  },

  create(data: Partial<Platform>): Promise<{ data: Platform }> {
    return api.post('/platforms', data)
  },

  update(id: number, data: Partial<Platform>): Promise<{ data: Platform }> {
    return api.put(`/platforms/${id}`, data)
  },

  delete(id: number): Promise<void> {
    return api.delete(`/platforms/${id}`)
  },

  importFile(file: File): Promise<{ data: string }> {
    const formData = new FormData()
    formData.append('file', file)
    return api.post('/platforms/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  exportFile(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/platforms/export', { params: { format }, responseType: 'blob' })
  },

  downloadTemplate(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/platforms/template', { params: { format }, responseType: 'blob' })
  },
}
