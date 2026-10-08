import { apiClient } from './apiClient'
import type { ApiResponse, LoginResponse, UserProfile } from '@/types'

export const authService = {
  login: async (username: string, password: string): Promise<LoginResponse> => {
    const res = await apiClient.post<ApiResponse<LoginResponse>>('/auth/login', {
      username,
      password,
    })
    return res.data.data
  },

  getMe: async (): Promise<UserProfile> => {
    const res = await apiClient.get<ApiResponse<UserProfile>>('/auth/me')
    return res.data.data
  },
}
