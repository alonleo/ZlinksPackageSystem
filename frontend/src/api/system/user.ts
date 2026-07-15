import api from '@/api'
import type { SysUser } from '@/types/system'
import type { PageResult } from '@/types/common'

export const userApi = {
  list(params: Record<string, unknown>): Promise<{ data: PageResult<SysUser> }> {
    return api.get('/system/user/list', { params })
  },
  get(userId: number): Promise<{ data: SysUser }> {
    return api.get('/system/user/' + userId)
  },
  profile(): Promise<{ data: SysUser }> {
    return api.get('/system/user/profile')
  },
  add(data: SysUser): Promise<void> {
    return api.post('/system/user', data)
  },
  update(data: SysUser): Promise<void> {
    return api.put('/system/user', data)
  },
  updateProfile(data: SysUser): Promise<void> {
    return api.put('/system/user/updateProfile', data)
  },
  remove(userIds: number[]): Promise<void> {
    return api.delete('/system/user/' + userIds.join(','))
  },
  resetPwd(userId: number, password: string): Promise<void> {
    return api.put('/system/user/resetPwd', { userId, password })
  },
  changeStatus(userId: number, status: string): Promise<void> {
    return api.put('/system/user/changeStatus', { userId, status })
  },
  authRole(userId: number, roleIds: number[]): Promise<void> {
    return api.put('/system/user/authRole/' + userId, roleIds)
  },
}