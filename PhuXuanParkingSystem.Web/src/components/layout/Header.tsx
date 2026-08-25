import { Sun, Moon, Radio } from 'lucide-react'
import { useThemeStore } from '@/stores/useThemeStore'
import { Button } from '@/components/ui/button'

interface HeaderProps {
  title?: string
  subtitle?: string
}

export function Header({ title = 'Bảng Điều Khiển Quản Trị', subtitle }: HeaderProps) {
  const { isDark, toggleTheme } = useThemeStore()

  return (
    <header className="h-16 border-b border-slate-200 dark:border-slate-800 bg-white/80 dark:bg-slate-900/80 backdrop-blur-md px-6 flex items-center justify-between sticky top-0 z-40">
      <div>
        <h2 className="text-base font-bold text-slate-900 dark:text-slate-100 tracking-tight">
          {title}
        </h2>
        {subtitle && (
          <p className="text-xs text-slate-500 dark:text-slate-400 font-medium">
            {subtitle}
          </p>
        )}
      </div>

      <div className="flex items-center gap-3">
        {/* Realtime Live Status Badge */}
        <div className="hidden sm:flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-800/40">
          <Radio className="h-3.5 w-3.5 animate-pulse text-emerald-500" />
          <span>Realtime Online</span>
        </div>

        {/* Theme Toggle Button */}
        <Button
          variant="ghost"
          size="icon"
          onClick={toggleTheme}
          title={isDark ? 'Chuyển sang chế độ sáng' : 'Chuyển sang chế độ tối'}
          className="rounded-lg text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800"
        >
          {isDark ? <Sun className="h-4.5 w-4.5 text-amber-400" /> : <Moon className="h-4.5 w-4.5 text-slate-700" />}
        </Button>
      </div>
    </header>
  )
}
