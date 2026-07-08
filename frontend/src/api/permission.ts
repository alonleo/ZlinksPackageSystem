import api from '@/api'
import type { PermissionGroup } from '@/types/permission-group'
import type { User } from '@/types/user'
import type { PageResult } from '@/types/common'

export interface PermissionGroupListParams {
  current?: number
  size?: number
  groupName?: string
}

export const permissionApi = {
  getList(params: PermissionGroupListParams): Promise<{ data: PageResult<PermissionGroup> }> {
    return api.get('/permission-groups', { params })
  },

  getById(id: number): Promise<{ data: PermissionGroup }> {
    return api.get(`/permission-groups/${id}`)
  },

  create(data: Partial<PermissionGroup>): Promise<{ data: PermissionGroup }> {
    return api.post('/permission-groups', data)
  },

  update(id: number, data: Partial<PermissionGroup>): Promise<{ data: PermissionGroup }> {
    return api.put(`/permission-groups/${id}`, data)
  },

  delete(id: number): Promise<void> {
    return api.delete(`/permission-groups/${id}`)
  },

  getUsers(id: number): Promise<{ data: User[] }> {
    return api.get(`/permission-groups/${id}/users`)
  },

  getAvailableUsers(id: number): Promise<{ data: User[] }> {
    return api.get(`/permission-groups/${id}/available-users`)
  },

  addUser(id: number, userId: number): Promise<void> {
    return api.post(`/permission-groups/${id}/users/${userId}`)
  },

  removeUser(id: number, userId: number): Promise<void> {
    return api.delete(`/permission-groups/${id}/users/${userId}`)
  },

  importFile(file: File): Promise<{ data: string }> {
    const formData = new FormData()
    formData.append('file', file)
    return api.post('/permission-groups/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  exportFile(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/permission-groups/export', { params: { format }, responseType: 'blob' })
  },

  downloadTemplate(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/permission-groups/template', { params: { format }, responseType: 'blob' })
  },
}
