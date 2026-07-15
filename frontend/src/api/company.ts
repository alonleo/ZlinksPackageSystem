import api from '@/api'
import type { Company } from '@/types/company'
import type { PageResult } from '@/types/common'

export interface CompanyListParams {
  current?: number
  size?: number
  companyName?: string
}

export const companyApi = {
  getList(params: CompanyListParams): Promise<{ data: PageResult<Company> }> {
    return api.get('/companies', { params })
  },

  getById(id: number): Promise<{ data: Company }> {
    return api.get(`/companies/${id}`)
  },

  create(data: Partial<Company>): Promise<{ data: Company }> {
    return api.post('/companies', data)
  },

  update(id: number, data: Partial<Company>): Promise<{ data: Company }> {
    return api.put(`/companies/${id}`, data)
  },

  delete(id: number): Promise<void> {
    return api.delete(`/companies/${id}`)
  },

  importFile(file: File): Promise<{ data: string }> {
    const formData = new FormData()
    formData.append('file', file)
    return api.post('/companies/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  exportFile(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/companies/export', { params: { format }, responseType: 'blob' })
  },

  downloadTemplate(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/companies/template', { params: { format }, responseType: 'blob' })
  },

  getPlatforms(): Promise<{ data: { id: number; platformName: string }[] }> {
    return api.get('/companies/platforms')
  },
}
