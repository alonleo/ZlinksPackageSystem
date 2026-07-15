import api from '@/api'
import type { PageResult } from '@/types/common'
import type { SysMenu, SysConfig, SysNotice } from '@/types/system'

export const menuApi = {
  list(params?: Record<string, unknown>): Promise<{ data: SysMenu[] }> {
    return api.get('/system/menu/list', { params })
  },
  treeselect(): Promise<{ data: SysMenu[] }> {
    return api.get('/system/menu/treeselect')
  },
  roleMenuTreeselect(roleId: number): Promise<{ data: { checkedKeys: number[]; menus: SysMenu[] } }> {
    return api.get('/system/menu/roleMenuTreeselect/' + roleId)
  },
  get(menuId: number): Promise<{ data: SysMenu }> {
    return api.get('/system/menu/' + menuId)
  },
  add(data: SysMenu): Promise<void> {
    return api.post('/system/menu', data)
  },
  update(data: SysMenu): Promise<void> {
    return api.put('/system/menu', data)
  },
  remove(menuId: number): Promise<void> {
    return api.delete('/system/menu/' + menuId)
  },
}

export const configApi = {
  list(params?: Record<string, unknown>): Promise<{ data: PageResult<SysConfig> }> {
    return api.get('/system/config/list', { params })
  },
  get(configId: number): Promise<{ data: SysConfig }> {
    return api.get('/system/config/' + configId)
  },
  getByKey(key: string): Promise<{ data: SysConfig }> {
    return api.get('/system/config/' + key)
  },
  add(data: SysConfig): Promise<void> {
    return api.post('/system/config', data)
  },
  update(data: SysConfig): Promise<void> {
    return api.put('/system/config', data)
  },
  remove(configIds: number[]): Promise<void> {
    return api.delete('/system/config/' + configIds.join(','))
  },
  refreshCache(): Promise<void> {
    return api.put('/system/config/refreshCache')
  },
}

export const noticeApi = {
  list(params?: Record<string, unknown>): Promise<{ data: PageResult<SysNotice> }> {
    return api.get('/system/notice/list', { params })
  },
  get(noticeId: number): Promise<{ data: SysNotice }> {
    return api.get('/system/notice/' + noticeId)
  },
  add(data: SysNotice): Promise<void> {
    return api.post('/system/notice', data)
  },
  update(data: SysNotice): Promise<void> {
    return api.put('/system/notice', data)
  },
  remove(noticeIds: number[]): Promise<void> {
    return api.delete('/system/notice/' + noticeIds.join(','))
  },
}