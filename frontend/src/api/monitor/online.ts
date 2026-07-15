import api from '@/api'
import type { UserOnline, OnlineQuery } from '@/types/monitor/online'
import type { PageResult } from '@/types/common'

export const onlineApi = {
  list(params: OnlineQuery): Promise<{ data: PageResult<UserOnline> }> {
    return api.get('/monitor/online/list', { params })
  },
  forceLogout(tokenId: string): Promise<void> {
    return api.delete('/monitor/online/' + encodeURIComponent(tokenId))
  },
}