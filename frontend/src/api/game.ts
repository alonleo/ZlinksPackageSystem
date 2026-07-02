import api from '@/api'
import type { Game } from '@/types/game'
import type { PageResult } from '@/types/common'

export interface GameListParams {
  current?: number
  size?: number
  gameName?: string
  status?: string
}

export const gameApi = {
  getList(params: GameListParams): Promise<{ data: PageResult<Game> }> {
    return api.get('/games', { params })
  },

  getById(id: number): Promise<{ data: Game }> {
    return api.get(`/games/${id}`)
  },

  create(data: Partial<Game>): Promise<{ data: Game }> {
    return api.post('/games', data)
  },

  update(id: number, data: Partial<Game>): Promise<{ data: Game }> {
    return api.put(`/games/${id}`, data)
  },

  delete(id: number): Promise<void> {
    return api.delete(`/games/${id}`)
  },
}