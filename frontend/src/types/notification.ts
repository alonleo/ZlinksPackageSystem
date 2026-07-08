export interface Notification {
  id: number
  title: string
  content: string
  module: string
  targetId: number
  targetType: string
  senderId: number
  senderName: string
  receiverIds: string
  receiverNames: string
  receiverType: string
  isPinned: number
  status: string
  createTime: string
  updateTime: string
}
