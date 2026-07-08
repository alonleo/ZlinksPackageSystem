import api from '@/api'
import type { User } from '@/types/user'
import type { PageResult } from '@/types/common'
import type { PermissionGroup } from '@/types/permission-group'

export interface UserListParams {
  current?: number
  size?: number
  username?: string
  realName?: string
  status?: string
  groupId?: number
}

export const userApi = {
  getList(params: UserListParams): Promise<{ data: PageResult<User> }> {
    return api.get('/users', { params })
  },

  getById(id: number): Promise<{ data: User }> {
    return api.get(`/users/${id}`)
  },

  create(data: Partial<User> & { password?: string }): Promise<{ data: User }> {
    return api.post('/users', data)
  },

  update(id: number, data: Partial<User> & { password?: string }): Promise<{ data: User }> {
    return api.put(`/users/${id}`, data)
  },

  delete(id: number): Promise<void> {
    return api.delete(`/users/${id}`)
  },

  getGroups(): Promise<{ data: PermissionGroup[] }> {
    return api.get('/users/groups')
  },

  importFile(file: File): Promise<{ data: string }> {
    const formData = new FormData()
    formData.append('file', file)
    return api.post('/users/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  exportFile(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/users/export', { params: { format }, responseType: 'blob' })
  },

  downloadTemplate(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/users/template', { params: { format }, responseType: 'blob' })
  },
}
