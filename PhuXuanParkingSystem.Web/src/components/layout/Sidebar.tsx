import { useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import {
  LayoutDashboard,
  History,
  Car,
  Users,
  Building2,
  ShieldCheck,
  LogOut,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react'
import { useAuthStore } from '@/stores/useAuthStore'
import { cn } from '@/lib/utils'

interface NavItem {
  title: string
  href: string
  icon: React.ComponentType<{ className?: string }>
  roles?: string[]
}

const navItems: NavItem[] = [
  {
    title: 'Dashboard Thống Kê',
    href: '/',
    icon: LayoutDashboard,
  },
  {
    title: 'Lịch Sử Xe Ra Vào',
    href: '/history',
    icon: History,
  },
  {
    title: 'Quản Lý Phương Tiện',
    href: '/vehicles',
    icon: Car,
    roles: ['Admin', 'Manager', '1', '2'],
  },
  {
    title: 'Quản Lý Nhân Sự',
    href: '/people',
    icon: Users,
    roles: ['Admin', 'Manager', '1', '2'],
  },
  {
    title: 'Phòng Ban & Đơn Vị',
    href: '/departments',
    icon: Building2,
    roles: ['Admin', 'Manager', '1', '2'],
  },
]

export function Sidebar() {
  const location = useLocation()
  const { user, logout } = useAuthStore()
  const [isCollapsed, setIsCollapsed] = useState(false)

  const getRoleDisplayName = (role: any) => {
    if (role === 'Admin' || role === 1 || role === '1') return 'Quản Trị Viên'
    if (role === 'Manager' || role === 2 || role === '2') return 'Quản Lý'
    if (role === 'Operator' || role === 3 || role === '3') return 'Vận Hành'
    return 'Quản Trị Viên'
  }

  return (
    <aside
      className={cn(
        'border-r border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 flex flex-col h-screen sticky top-0 backdrop-blur-md transition-all duration-300 ease-in-out z-30',
        isCollapsed ? 'w-[72px]' : 'w-64'
      )}
    >
      {/* Brand Header */}
      <div
        className={cn(
          'border-b border-slate-200 dark:border-slate-800 flex items-center transition-all duration-300',
          isCollapsed ? 'p-3 flex-col justify-center gap-2' : 'p-4 justify-between gap-3'
        )}
      >
        <div className="flex items-center gap-3 min-w-0">
          <div className="h-10 w-10 rounded-xl bg-gradient-to-tr from-blue-600 to-indigo-600 flex items-center justify-center text-white shadow-md shadow-blue-500/20 shrink-0">
            <ShieldCheck className="h-6 w-6" />
          </div>
          {!isCollapsed && (
            <div className="min-w-0 animate-in fade-in duration-200">
              <h1 className="font-black text-base text-slate-900 dark:text-slate-50 tracking-tight flex items-center gap-1">
                <span>HP</span>
                <span className="text-blue-600 dark:text-blue-400">PARKING</span>
              </h1>
              <p className="text-[11px] text-slate-500 dark:text-slate-400 font-medium truncate">
                Smart Admin Portal
              </p>
            </div>
          )}
        </div>

        {/* Toggle Collapse Button */}
        <button
          type="button"
          onClick={() => setIsCollapsed(!isCollapsed)}
          title={isCollapsed ? 'Mở rộng sidebar' : 'Thu gọn sidebar'}
          className={cn(
            'p-1.5 rounded-lg text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800/80 transition-colors cursor-pointer shrink-0',
            isCollapsed && 'mt-1'
          )}
        >
          {isCollapsed ? (
            <ChevronRight className="h-4 w-4" />
          ) : (
            <ChevronLeft className="h-4 w-4" />
          )}
        </button>
      </div>

      {/* Navigation List */}
      <nav className="flex-1 p-2.5 space-y-1.5 overflow-y-auto">
        {navItems.map((item) => {
          if (
            item.roles &&
            user &&
            !item.roles.includes(String(user.role)) &&
            !item.roles.includes(user.role)
          ) {
            return null
          }

          const isActive =
            item.href === '/'
              ? location.pathname === '/'
              : location.pathname.startsWith(item.href)

          const Icon = item.icon

          return (
            <Link
              key={item.href}
              to={item.href}
              title={isCollapsed ? item.title : undefined}
              className={cn(
                'flex items-center rounded-xl text-sm font-medium transition-all duration-150 group relative',
                isCollapsed
                  ? 'justify-center p-2.5'
                  : 'gap-3 px-3.5 py-2.5',
                isActive
                  ? 'bg-blue-50 text-blue-700 dark:bg-blue-600/15 dark:text-blue-400 shadow-xs font-semibold'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100 dark:text-slate-400 dark:hover:text-slate-200 dark:hover:bg-slate-800/60'
              )}
            >
              <Icon
                className={cn(
                  'h-5 w-5 shrink-0 transition-transform group-hover:scale-105',
                  isActive
                    ? 'text-blue-600 dark:text-blue-400'
                    : 'text-slate-400 group-hover:text-slate-600 dark:group-hover:text-slate-300'
                )}
              />
              {!isCollapsed && (
                <span className="truncate animate-in fade-in duration-150">
                  {item.title}
                </span>
              )}
            </Link>
          )
        })}
      </nav>

      {/* User Profile & Logout Section */}
      <div className="p-3 border-t border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-950/40">
        {isCollapsed ? (
          <div className="flex flex-col items-center gap-2">
            <div
              className="h-9 w-9 rounded-full bg-gradient-to-tr from-blue-600 to-indigo-500 text-white font-bold flex items-center justify-center text-xs shadow-xs"
              title={`${user?.fullName || 'Quản Trị Viên'} (${getRoleDisplayName(user?.role)})`}
            >
              {user?.fullName?.charAt(0).toUpperCase() || 'Q'}
            </div>
            <button
              type="button"
              onClick={logout}
              title="Đăng xuất"
              className="p-2 rounded-lg text-slate-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-950/40 transition-colors cursor-pointer"
            >
              <LogOut className="h-4 w-4" />
            </button>
          </div>
        ) : (
          <div className="flex items-center justify-between gap-2">
            <div className="flex items-center gap-2.5 min-w-0">
              <div className="h-9 w-9 rounded-full bg-gradient-to-tr from-blue-600 to-indigo-500 text-white font-bold flex items-center justify-center text-xs shadow-xs shrink-0">
                {user?.fullName?.charAt(0).toUpperCase() || 'Q'}
              </div>
              <div className="min-w-0 space-y-0.5">
                <p
                  className="text-xs font-bold text-slate-800 dark:text-slate-100 truncate"
                  title={user?.fullName || 'Quản Trị Viên'}
                >
                  {user?.fullName || 'Quản Trị Viên'}
                </p>
                <div className="flex items-center">
                  <span className="inline-flex items-center text-[10px] font-semibold text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-950/80 px-1.5 py-0.2 rounded border border-blue-200/60 dark:border-blue-800/60">
                    {getRoleDisplayName(user?.role)}
                  </span>
                </div>
              </div>
            </div>
            <button
              type="button"
              onClick={logout}
              title="Đăng xuất"
              className="p-1.5 rounded-lg text-slate-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-950/40 transition-colors cursor-pointer shrink-0"
            >
              <LogOut className="h-4 w-4" />
            </button>
          </div>
        )}
      </div>
    </aside>
  )
}
