import api from '@/api'
import type { Copyright } from '@/types/copyright'
import type { PageResult } from '@/types/common'

export interface CopyrightListParams {
  current?: number
  size?: number
  copyrightName?: string
}

export const copyrightApi = {
  getList(params: CopyrightListParams): Promise<{ data: PageResult<Copyright> }> {
    return api.get('/copyrights', { params })
  },

  getById(id: number): Promise<{ data: Copyright }> {
    return api.get(`/copyrights/${id}`)
  },

  create(data: Partial<Copyright>): Promise<{ data: Copyright }> {
    return api.post('/copyrights', data)
  },

  update(id: number, data: Partial<Copyright>): Promise<{ data: Copyright }> {
    return api.put(`/copyrights/${id}`, data)
  },

  delete(id: number): Promise<void> {
    return api.delete(`/copyrights/${id}`)
  },

  importFile(file: File): Promise<{ data: string }> {
    const formData = new FormData()
    formData.append('file', file)
    return api.post('/copyrights/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  exportFile(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/copyrights/export', { params: { format }, responseType: 'blob' })
  },

  downloadTemplate(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/copyrights/template', { params: { format }, responseType: 'blob' })
  },
}
