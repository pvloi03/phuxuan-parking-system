import { Sun, Moon, ShieldCheck, ShieldAlert, ShieldX } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { useThemeStore } from '@/stores/useThemeStore'
import { licenseService } from '@/services/licenseService'

interface HeaderProps {
  title?: string
  subtitle?: string
}

export function Header({ title = 'Bảng Điều Khiển', subtitle }: HeaderProps) {
  const { isDark, toggleTheme } = useThemeStore()

  const { data: license } = useQuery({
    queryKey: ['header-license-status'],
    queryFn: () => licenseService.getStatus(),
    refetchInterval: 60000,
  })

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

      <div className="flex items-center gap-3">
        {/* License Status Badge */}
        {license && (
          <Link
            to="/license"
            className={`hidden md:inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold transition border ${
              !license.isValid || license.isExpired
                ? 'bg-red-50 text-red-700 border-red-200 hover:bg-red-100 dark:bg-red-950/40 dark:text-red-400 dark:border-red-900/50'
                : !license.isPermanent && license.daysRemaining <= 15
                ? 'bg-amber-50 text-amber-700 border-amber-200 hover:bg-amber-100 dark:bg-amber-950/40 dark:text-amber-400 dark:border-amber-900/50'
                : 'bg-emerald-50 text-emerald-700 border-emerald-200 hover:bg-emerald-100 dark:bg-emerald-950/40 dark:text-emerald-400 dark:border-emerald-900/50'
            }`}
            title="Nhấp để xem chi tiết bản quyền và hạn mức"
          >
            {!license.isValid || license.isExpired ? (
              <>
                <ShieldX className="w-3.5 h-3.5 text-red-600" />
                <span>Hết Hạn Bản Quyền</span>
              </>
            ) : !license.isPermanent && license.daysRemaining <= 15 ? (
              <>
                <ShieldAlert className="w-3.5 h-3.5 text-amber-600" />
                <span>Bản Quyền: Còn {license.daysRemaining} ngày</span>
              </>
            ) : (
              <>
                <ShieldCheck className="w-3.5 h-3.5 text-emerald-600" />
                <span>{license.isPermanent ? 'Bản Quyền Vĩnh Viễn' : `Bản Quyền: Còn ${license.daysRemaining} ngày`}</span>
              </>
            )}
          </Link>
        )}

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
