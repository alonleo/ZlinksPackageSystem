import api from '@/api'
import type { SysRole } from '@/types/system'
import type { PageResult } from '@/types/common'

export const roleApi = {
  list(params: Record<string, unknown>): Promise<{ data: PageResult<SysRole> }> {
    return api.get('/system/role/list', { params })
  },
  get(roleId: number): Promise<{ data: SysRole }> {
    return api.get('/system/role/' + roleId)
  },
  optionselect(): Promise<{ data: SysRole[] }> {
    return api.get('/system/role/optionselect')
  },
  add(data: SysRole): Promise<void> {
    return api.post('/system/role', data)
  },
  update(data: SysRole): Promise<void> {
    return api.put('/system/role', data)
  },
  remove(roleIds: number[]): Promise<void> {
    return api.delete('/system/role/' + roleIds.join(','))
  },
  changeStatus(roleId: number, status: string): Promise<void> {
    return api.put('/system/role/changeStatus', { roleId, status })
  },
  dataScope(data: { roleId: number; deptIds: number[]; dataScope: string }): Promise<void> {
    return api.put('/system/role/dataScope', data)
  },
  allocatedList(roleId: number, params?: Record<string, unknown>): Promise<{ data: PageResult<SysRole> }> {
    return api.get('/system/role/authUser/allocatedList', { params: { ...params, roleId } })
  },
  unallocatedList(roleId: number, params?: Record<string, unknown>): Promise<{ data: PageResult<SysRole> }> {
    return api.get('/system/role/authUser/unallocatedList', { params: { ...params, roleId } })
  },
  cancelAuthUser(roleId: number, userId: number): Promise<void> {
    return api.put('/system/role/authUser/cancel', { roleId, userId })
  },
}