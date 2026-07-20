export interface Copyright {
  id: number
  copyrightName: string
  copyrightOwner: string
  copyrightNumber: string
  remark: string
  createBy: string
  createTime: string
  updateBy: string
  updateTime: string
}

export interface CopyrightListParams {
  current?: number
  size?: number
  copyrightName?: string
}

export interface CopyrightRequest {
  id?: number
  copyrightName?: string
  copyrightOwner?: string
  copyrightNumber?: string
  remark?: string
}
