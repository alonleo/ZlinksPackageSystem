export interface SysUser {
  userId?: number
  deptId?: number
  userName: string
  nickName: string
  email?: string
  phonenumber?: string
  sex?: string
  avatar?: string
  password?: string
  status: string
  loginIp?: string
  loginDate?: string
  dept?: { deptId: number; deptName: string }
  roles?: SysRole[]
  roleIds?: number[]
  postIds?: number[]
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface SysRole {
  roleId?: number
  roleName: string
  roleKey: string
  roleSort?: number
  dataScope?: string
  menuCheckStrictly?: boolean
  deptCheckStrictly?: boolean
  status: string
  delFlag?: string
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface SysMenu {
  menuId?: number
  menuName: string
  parentId: number
  orderNum: number
  path: string
  component: string
  query?: string
  isFrame: string
  isCache: string
  menuType: string
  visible: string
  status: string
  perms?: string
  icon: string
  children?: SysMenu[]
}

export interface SysConfig {
  configId?: number
  configName: string
  configKey: string
  configValue: string
  configType: string
  isBuiltin?: string
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface SysNotice {
  noticeId?: number
  noticeTitle: string
  noticeType: string
  noticeContent: string
  status: string
  createBy?: string
  createTime?: string
  updateBy?: string
  updateTime?: string
  remark?: string
}