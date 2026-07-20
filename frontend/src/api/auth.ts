import api from '@/api'
import type { User, UserInfoResponse } from '@/types/user'

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  code: number
  message: string
  data: string
}

export interface ChangePasswordRequest {
  oldPassword: string
  newPassword?: string
  newUsername?: string
}

export const authApi = {
  login(data: LoginRequest): Promise<LoginResponse> {
    return api.post('/auth/login', data)
  },

  getUserInfo(): Promise<UserInfoResponse> {
    return api.get('/getInfo')
  },

  register(user: Partial<User>): Promise<any> {
    return api.post('/auth/register', user)
  },

  changePassword(data: ChangePasswordRequest): Promise<any> {
    return api.post('/auth/change-password', data)
  },
}