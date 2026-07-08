export interface OperationLog {
  id: number
  userId: number | null
  username: string
  module: string
  action: string
  target: string
  ipAddress: string
  createTime: string
}
