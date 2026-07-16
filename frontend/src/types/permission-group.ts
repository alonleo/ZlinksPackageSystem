export interface PermissionScope {
  id?: number
  groupId: number
  scope: 'backend' | 'desktop'
  modulesText?: string
  modules?: string[]
}

export interface PermissionGroup {
  id: number
  groupName: string
  groupPermission: string
  groupAccounts: string
  remark: string
  userCount: number
  createTime: string
  updateTime: string
  scopes?: PermissionScope[]
}

export const BACKEND_MODULE_OPTIONS: { label: string; value: string }[] = [
  { label: '首页', value: 'home' },
  { label: '系统管理', value: 'system-mgmt' },
  { label: '系统设置', value: 'system-settings' },
  { label: '打包管理', value: 'package' },
  { label: '系统监控', value: 'monitor' },
  { label: '全部', value: 'all' },
]

export const DESKTOP_MODULE_OPTIONS: { label: string; value: string }[] = [
  { label: '首页', value: 'home' },
  { label: '游戏管理', value: 'games' },
  { label: '产品管理', value: 'products' },
  { label: '参数管理', value: 'parameters' },
  { label: '测试管理', value: 'tests' },
  { label: '工具库', value: 'tool-library' },
  { label: '消息中心', value: 'notification' },
  { label: '设置', value: 'settings' },
  { label: '全部', value: 'all' },
]
