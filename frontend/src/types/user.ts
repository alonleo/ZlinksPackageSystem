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

export interface UserInfoModules {
  backend: string[]
  desktop: string[]
}

export interface UserInfo {
  user: {
    userId?: number
    userName: string
    nickName?: string
    avatar?: string
    [k: string]: unknown
  }
  roles: string[]
  permissions: string[]
  modules: UserInfoModules
}

export interface UserInfoResponse {
  code: number
  message: string
  data: {
    user: User
    roles: string[]
    permissions: string[]
    modules?: UserInfoModules
  }
}
