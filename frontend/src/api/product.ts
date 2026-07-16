import api from '@/api'
import type { Product } from '@/types/product'
import type { PageResult } from '@/types/common'

interface ListParams {
  current?: number
  size?: number
  copyrightId?: number
  gameId?: number
  companyId?: number
  platformId?: number
  batch?: string
  status?: string
}

export interface PlatformOption {
  id: number
  name: string
}

export interface ProductOptions {
  platforms: any[]
  batches: string[]
  statuses: string[]
}

export const productApi = {
  getList(params: ListParams): Promise<{ data: PageResult<Product> }> {
    return api.get('/products', { params })
  },

  getById(id: number): Promise<{ data: Product }> {
    return api.get(`/products/${id}`)
  },

  create(data: Partial<Product>): Promise<{ data: Product }> {
    return api.post('/products', data)
  },

  update(id: number, data: Partial<Product>): Promise<{ data: Product }> {
    return api.put(`/products/${id}`, data)
  },

  delete(id: number): Promise<void> {
    return api.delete(`/products/${id}`)
  },

  triggerPackage(id: number): Promise<{ data: string }> {
    return api.post(`/products/${id}/package`)
  },

  getCopyrights(): Promise<{ data: { id: number; copyrightName: string }[] }> {
    return api.get('/products/copyrights')
  },

  getGames(): Promise<{ data: { id: number; gameName: string }[] }> {
    return api.get('/products/games')
  },

  getCompanies(): Promise<{ data: { id: number; companyName: string }[] }> {
    return api.get('/products/companies')
  },

  getPlatforms(): Promise<{ data: { id: number; platformName: string }[] }> {
    return api.get('/products/platforms')
  },

  getOptions(): Promise<{ data: ProductOptions }> {
    return api.get('/products/options')
  },

  importFile(file: File): Promise<{ data: string }> {
    const formData = new FormData()
    formData.append('file', file)
    return api.post('/products/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  exportFile(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/products/export', { params: { format }, responseType: 'blob' })
  },

  downloadTemplate(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/products/template', { params: { format }, responseType: 'blob' })
  },

  getAll(): Promise<{ data: { id: number; packageName: string; gameName?: string }[] }> {
    return api.get('/products/all')
  },
}
