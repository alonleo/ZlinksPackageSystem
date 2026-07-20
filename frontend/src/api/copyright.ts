import api from './index'
import type { ApiResponse, PageResponse } from '@/types/common'
import type { Copyright, CopyrightListParams, CopyrightRequest } from '@/types/copyright'

export const copyrightApi = {
  getList(params: CopyrightListParams): Promise<PageResponse<Copyright>> {
    return api.get('/copyrights', { params })
  },

  getById(id: number): Promise<ApiResponse<Copyright>> {
    return api.get(`/copyrights/${id}`)
  },

  create(data: CopyrightRequest): Promise<ApiResponse<Copyright>> {
    return api.post('/copyrights', data)
  },

  update(id: number, data: CopyrightRequest): Promise<ApiResponse<Copyright>> {
    return api.put(`/copyrights/${id}`, data)
  },

  delete(id: number): Promise<ApiResponse<void>> {
    return api.delete(`/copyrights/${id}`)
  },

  importFile(file: File): Promise<ApiResponse<string>> {
    const formData = new FormData()
    formData.append('file', file)
    return api.post('/copyrights/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
  },

  exportFile(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/copyrights/export', { params: { format }, responseType: 'blob' })
  },

  downloadTemplate(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/copyrights/template', { params: { format }, responseType: 'blob' })
  }
}
