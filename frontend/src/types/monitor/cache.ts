export interface CacheInfo {
  info: Record<string, string>
  dbSize: number
  commandStats: Array<{ name: string; value: string }>
}