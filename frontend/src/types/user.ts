export interface User {
  id: number
  username: string
  realName: string
  status: string
  groupIds: number[]
  groupNames: string[]
  remark: string
  createTime: string
  updateTime: string
}

export interface UserInfoResponse {
  code: number
  message: string
  data: {
    user: User
    roles: string[]
    permissions: string[]
  }
}
