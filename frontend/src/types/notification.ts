export interface Notification {
  id: number
  title: string
  content: string
  module: string
  targetId: number | null
  targetType: string
  senderId: number | null
  senderName: string
  receiverIds: string
  receiverNames: string
  receiverType: string
  isPinned: number
  status: string
  createBy: string
  createTime: string
  updateBy: string
  updateTime: string
  remark: string
}

export interface NotificationListParams {
  current?: number
  size?: number
  title?: string
  module?: string
  status?: string
}

export interface NotificationRequest {
  title: string
  content: string
  module?: string
  targetId?: number
  targetType?: string
  receiverIds?: number[]
  receiverType?: string
  isPinned?: number
  status?: string
}
