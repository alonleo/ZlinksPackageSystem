export interface UserOnline {
  tokenId: string
  userName: string
  ipaddr: string
  loginLocation: string
  browser: string
  os: string
  roleKey: string
  loginTime: string
}

export interface OnlineQuery {
  current?: number
  size?: number
  ipaddr?: string
  userName?: string
}