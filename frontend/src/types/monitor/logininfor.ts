export interface Logininfor {
  infoId: number
  userName: string
  ipaddr: string
  loginLocation: string
  browser: string
  os: string
  status: string
  msg: string
  loginTime: string
}

export interface LogininforQuery {
  current?: number
  size?: number
  userName?: string
  ipaddr?: string
  status?: string
}