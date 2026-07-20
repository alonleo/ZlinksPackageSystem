import api from './index'
import type { Notification, NotificationListParams, NotificationRequest } from '@/types/notification'

export const notificationApi = {
  getList(params: NotificationListParams): Promise<{ data: { records: Notification[]; total: number; size: number; current: number; pages: number } }> {
    return api.get('/notifications', { params })
  },

  getById(id: number): Promise<{ data: Notification }> {
    return api.get(`/notifications/${id}`)
  },

  getPinned(): Promise<{ data: Notification[] }> {
    return api.get('/notifications/pinned')
  },

  getAnnouncements(): Promise<{ data: Notification[] }> {
    return api.get('/notifications/announcements')
  },

  create(data: NotificationRequest): Promise<{ data: Notification }> {
    return api.post('/notifications', data)
  },

  update(id: number, data: NotificationRequest): Promise<{ data: Notification }> {
    return api.put(`/notifications/${id}`, data)
  },

  delete(id: number): Promise<void> {
    return api.delete(`/notifications/${id}`)
  },

  importFile(file: File): Promise<{ data: string }> {
    const formData = new FormData()
    formData.append('file', file)
    return api.post('/notifications/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
  },

  exportFile(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/notifications/export', { params: { format }, responseType: 'blob' })
  },

  downloadTemplate(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/notifications/template', { params: { format }, responseType: 'blob' })
  }
}
