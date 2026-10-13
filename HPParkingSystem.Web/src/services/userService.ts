import { apiClient } from './apiClient'
import type { User, CreateUserPayload, UpdateUserPayload, ChangePasswordPayload, UserPagedResult } from '../types'

export interface UserQueryParams {
  search?: string
  role?: string
  isActive?: boolean
  pageNumber?: number
  pageSize?: number
}

export const userService = {
  getUsers: async (params?: UserQueryParams): Promise<UserPagedResult> => {
    const res = await apiClient.get<{ success: boolean; data: UserPagedResult }>('/users', { params })
    return res.data.data
  },

  getUserById: async (id: string): Promise<User> => {
    const res = await apiClient.get<{ success: boolean; data: User }>(`/users/${id}`)
    return res.data.data
  },

  createUser: async (payload: CreateUserPayload): Promise<User> => {
    const res = await apiClient.post<{ success: boolean; data: User; message: string }>('/users', payload)
    return res.data.data
  },

  updateUser: async (id: string, payload: UpdateUserPayload): Promise<User> => {
    const res = await apiClient.put<{ success: boolean; data: User; message: string }>(`/users/${id}`, payload)
    return res.data.data
  },

  changePassword: async (id: string, payload: ChangePasswordPayload): Promise<{ success: boolean; message: string }> => {
    const res = await apiClient.put<{ success: boolean; message: string }>(`/users/${id}/password`, payload)
    return res.data
  },

  toggleStatus: async (id: string): Promise<User> => {
    const res = await apiClient.patch<{ success: boolean; data: User; message: string }>(`/users/${id}/toggle-status`)
    return res.data.data
  },

  deleteUser: async (id: string): Promise<{ success: boolean; message: string }> => {
    const res = await apiClient.delete<{ success: boolean; message: string }>(`/users/${id}`)
    return res.data
  }
}
