import api from '@/api'
import type { Game } from '@/types/game'
import type { PageResult } from '@/types/common'

export interface GameListParams {
  current?: number
  size?: number
  gameName?: string
  gameDirection?: string
  source?: string
  manager?: string
  status?: string
  priority?: number
}

export interface GameOptions {
  gameDirections: string[]
  sources: string[]
  managers: string[]
  statuses: string[]
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

  getOptions(): Promise<{ data: GameOptions }> {
    return api.get('/games/options')
  },

  importFile(file: File): Promise<{ data: string }> {
    const formData = new FormData()
    formData.append('file', file)
    return api.post('/games/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  exportFile(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/games/export', { params: { format }, responseType: 'blob' })
  },

  downloadTemplate(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
    return api.get('/games/template', { params: { format }, responseType: 'blob' })
  },
}
