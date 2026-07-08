import api from '@/api'
import type { Notification } from '@/types/notification'
import type { PageResult } from '@/types/common'

export interface NotificationListParams {
  current?: number
  size?: number
  title?: string
  module?: string
  status?: string
}

export const notificationApi = {
  getList(params: NotificationListParams): Promise<{ data: PageResult<Notification> }> {
    return api.get('/notifications', { params })
  },

  getById(id: number): Promise<{ data: Notification }> {
    return api.get(`/notifications/${id}`)
  },

  create(data: any): Promise<{ data: Notification }> {
    return api.post('/notifications', data)
  },

  update(id: number, data: any): Promise<{ data: Notification }> {
    return api.put(`/notifications/${id}`, data)
  },

  delete(id: number): Promise<void> {
    return api.delete(`/notifications/${id}`)
  },

  getAnnouncements(): Promise<{ data: Notification[] }> {
    return api.get('/notifications/announcements')
  },

  getPinned(): Promise<{ data: Notification[] }> {
    return api.get('/notifications/pinned')
  },

  importFile(file: File): Promise<{ data: string }> {
    const formData = new FormData()
    formData.append('file', file)
    return api.post('/notifications/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  exportFile(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/notifications/export', { params: { format }, responseType: 'blob' })
  },

  downloadTemplate(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/notifications/template', { params: { format }, responseType: 'blob' })
  },
}
