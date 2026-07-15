import api from '@/api'
import type { CacheInfo } from '@/types/monitor/cache'

export const cacheApi = {
  getInfo(): Promise<{ data: CacheInfo }> {
    return api.get('/monitor/cache')
  },
  getNames(): Promise<{ data: string[] }> {
    return api.get('/monitor/cache/getNames')
  },
  getKeys(cacheName: string): Promise<{ data: string[] }> {
    return api.get('/monitor/cache/getKeys/' + encodeURIComponent(cacheName))
  },
  getValue(cacheName: string, cacheKey: string): Promise<{ data: { cacheName: string; cacheKey: string; cacheValue: unknown; remark: string } }> {
    return api.get('/monitor/cache/getValue/' + encodeURIComponent(cacheName) + '/' + encodeURIComponent(cacheKey))
  },
  clearCacheName(cacheName: string): Promise<void> {
    return api.delete('/monitor/cache/clearCacheName/' + encodeURIComponent(cacheName))
  },
  clearCacheKey(cacheKey: string): Promise<void> {
    return api.delete('/monitor/cache/clearCacheKey/' + encodeURIComponent(cacheKey))
  },
  clearCacheAll(): Promise<void> {
    return api.delete('/monitor/cache/clearCacheAll')
  },
}