import api from '@/api'
import type { User } from '@/types/user'

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  code: number
  message: string
  data: string
}

export interface UserInfoResponse {
  code: number
  message: string
  data: User
}

export const authApi = {
  login(data: LoginRequest): Promise<LoginResponse> {
    return api.post('/auth/login', data)
  },

  getUserInfo(): Promise<UserInfoResponse> {
    return api.get('/auth/info')
  },

  register(user: Partial<User>): Promise<any> {
    return api.post('/auth/register', user)
  },
}