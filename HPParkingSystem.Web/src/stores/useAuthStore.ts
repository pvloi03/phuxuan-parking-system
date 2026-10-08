import { create } from 'zustand'
import type { LoginResponse, UserRole } from '@/types'

interface AuthState {
  token: string | null
  user: {
    id: string
    username: string
    fullName: string
    role: UserRole
  } | null
  isAuthenticated: boolean
  login: (data: LoginResponse) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>((set) => {
  const savedToken = localStorage.getItem('hpparking_token')
  const savedUser = localStorage.getItem('hpparking_user')
  let parsedUser = null

  try {
    if (savedUser) parsedUser = JSON.parse(savedUser)
  } catch {
    // Ignore error
  }

  return {
    token: savedToken,
    user: parsedUser,
    isAuthenticated: !!savedToken,
    login: (data: LoginResponse) => {
      localStorage.setItem('hpparking_token', data.token)
      const userInfo = {
        id: data.userId,
        username: data.username,
        fullName: data.fullName,
        role: data.role,
      }
      localStorage.setItem('hpparking_user', JSON.stringify(userInfo))
      set({
        token: data.token,
        user: userInfo,
        isAuthenticated: true,
      })
    },
    logout: () => {
      localStorage.removeItem('hpparking_token')
      localStorage.removeItem('hpparking_user')
      set({
        token: null,
        user: null,
        isAuthenticated: false,
      })
    },
  }
})
