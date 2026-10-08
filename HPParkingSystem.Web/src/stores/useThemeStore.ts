import { create } from 'zustand'

interface ThemeState {
  isDark: boolean
  toggleTheme: () => void
}

export const useThemeStore = create<ThemeState>((set) => {
  const savedTheme = localStorage.getItem('hpparking_theme')
  const initialDark = savedTheme ? savedTheme === 'dark' : true

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
