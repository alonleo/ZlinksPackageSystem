// 分页结果(后端 PageResult 实际字段)
export interface PageResult<T> {
  records: T[]
  total: number
  size: number
  current: number
  pages: number
}

// 后端统一响应包装:code/message/data
export interface Result<T> {
  code: number
  message: string
  data: T
}

// 分页接口的统一返回:Result<PageResult<T>>
export type PageResponse<T> = Result<PageResult<T>>

// 单实体接口的统一返回
export type ApiResponse<T> = Result<T>

// 错误码常量
export const ResultCode = {
  SUCCESS: 200,
  BAD_REQUEST: 400,
  UNAUTHORIZED: 401,
  FORBIDDEN: 403,
  NOT_FOUND: 404,
  SERVER_ERROR: 500
} as const

// 类型守卫:判断响应是否成功
export function isSuccess<T>(resp: Result<T>): boolean {
  return resp.code === ResultCode.SUCCESS
}
