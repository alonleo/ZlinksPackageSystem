import api from '@/api'
import type { SignFile } from '@/types/sign-file'
import type { Company } from '@/types/company'
import type { PageResult } from '@/types/common'

export interface SignFileListParams {
  current?: number
  size?: number
  companyId?: number
}

export const signFileApi = {
  getList(params: SignFileListParams): Promise<{ data: PageResult<SignFile> }> {
    return api.get('/sign-files', { params })
  },

  getById(id: number): Promise<{ data: SignFile }> {
    return api.get(`/sign-files/${id}`)
  },

  create(data: Partial<SignFile>): Promise<{ data: SignFile }> {
    return api.post('/sign-files', data)
  },

  update(id: number, data: Partial<SignFile>): Promise<{ data: SignFile }> {
    return api.put(`/sign-files/${id}`, data)
  },

  delete(id: number): Promise<void> {
    return api.delete(`/sign-files/${id}`)
  },

  getCompanies(): Promise<{ data: Company[] }> {
    return api.get('/sign-files/companies')
  },

  importFile(file: File): Promise<{ data: string }> {
    const formData = new FormData()
    formData.append('file', file)
    return api.post('/sign-files/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  exportFile(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/sign-files/export', { params: { format }, responseType: 'blob' })
  },

  downloadTemplate(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/sign-files/template', { params: { format }, responseType: 'blob' })
  },
}
