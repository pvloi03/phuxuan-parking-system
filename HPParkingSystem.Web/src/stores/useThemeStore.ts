import { create } from 'zustand'

interface ThemeState {
  isDark: boolean
  toggleTheme: () => void
}

export const useThemeStore = create<ThemeState>((set) => {
  const isLogin = typeof window !== 'undefined' && window.location.pathname.startsWith('/login')
  const savedTheme = typeof localStorage !== 'undefined' ? localStorage.getItem('hpparking_theme') : null
  const initialDark = isLogin ? false : (savedTheme ? savedTheme === 'dark' : false)

  if (initialDark) {
    document.documentElement.classList.add('dark')
  } else {
    document.documentElement.classList.remove('dark')
  }

  return {
    isDark: initialDark,
    toggleTheme: () => {
      set((state) => {
        const nextDark = !state.isDark
        if (nextDark) {
          document.documentElement.classList.add('dark')
          localStorage.setItem('hpparking_theme', 'dark')
        } else {
          document.documentElement.classList.remove('dark')
          localStorage.setItem('hpparking_theme', 'light')
        }
        return { isDark: nextDark }
      })
    },
  }
})
