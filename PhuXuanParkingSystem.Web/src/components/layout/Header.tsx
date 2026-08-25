import { Sun, Moon } from 'lucide-react'
import { useThemeStore } from '@/stores/useThemeStore'

interface HeaderProps {
  title?: string
  subtitle?: string
}

export function Header({ title = 'Bảng Điều Khiển', subtitle }: HeaderProps) {
  const { isDark, toggleTheme } = useThemeStore()

  return (
    <header className="h-14 border-b border-slate-100 dark:border-[#1e2d3d] bg-white/95 dark:bg-[#0c1220]/95 backdrop-blur-sm px-6 flex items-center justify-between sticky top-0 z-40">
      <div>
        <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100 tracking-tight">
          {title}
        </h2>
        {subtitle && (
          <p className="text-xs text-slate-400 dark:text-slate-500 mt-0.5">
            {subtitle}
          </p>
        )}
      </div>

      <div className="flex items-center gap-2">
        {/* Live Indicator */}
        <div className="hidden sm:flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-medium bg-emerald-50 dark:bg-emerald-950/30 text-emerald-700 dark:text-emerald-400 border border-emerald-100 dark:border-emerald-900/50">
          <span className="relative flex h-1.5 w-1.5">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
            <span className="relative inline-flex rounded-full h-1.5 w-1.5 bg-emerald-500"></span>
          </span>
          <span>Realtime</span>
        </div>

        {/* Theme Toggle */}
        <button
          type="button"
          onClick={toggleTheme}
          title={isDark ? 'Chuyển sang chế độ sáng' : 'Chuyển sang chế độ tối'}
          className="p-1.5 rounded-lg text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-[#1e2d3d] transition-colors cursor-pointer"
        >
          {isDark
            ? <Sun className="h-4 w-4 text-amber-400" />
            : <Moon className="h-4 w-4" />
          }
        </button>
      </div>
    </header>
  )
}
