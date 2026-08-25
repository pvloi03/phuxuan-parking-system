import { useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  LayoutDashboard,
  History,
  Car,
  Users,
  Building2,
  Building,
  Handshake,
  Cpu,
  Route,
  Trash2,
  ShieldCheck,
  LogOut,
  ChevronLeft,
  ChevronRight,
  ChevronDown,
} from 'lucide-react'
import { useAuthStore } from '@/stores/useAuthStore'
import { recycleBinService } from '@/services/recycleBinService'
import { cn } from '@/lib/utils'

interface NavSubItem {
  title: string
  href: string
  icon: React.ComponentType<{ className?: string }>
}

interface NavGroup {
  title: string
  icon: React.ComponentType<{ className?: string }>
  children: NavSubItem[]
  roles?: string[]
}

interface NavSingleItem {
  title: string
  href: string
  icon: React.ComponentType<{ className?: string }>
  roles?: string[]
}

type MenuItem = { type: 'single'; item: NavSingleItem } | { type: 'group'; group: NavGroup }

const menuConfig: MenuItem[] = [
  {
    type: 'single',
    item: {
      title: 'Dashboard',
      href: '/',
      icon: LayoutDashboard,
    },
  },
  {
    type: 'single',
    item: {
      title: 'Lịch Sử Xe Ra Vào',
      href: '/history',
      icon: History,
    },
  },
  {
    type: 'group',
    group: {
      title: 'Tổ Chức & Đơn Vị',
      icon: Building2,
      roles: ['Admin', 'Manager', '1', '2'],
      children: [
        { title: 'Công Ty & Doanh Nghiệp', href: '/companies', icon: Building },
        { title: 'Phòng Ban', href: '/departments', icon: Building2 },
        { title: 'Đối Tác & Nhà Thầu', href: '/partners', icon: Handshake },
        { title: 'Danh Sách Nhân Sự', href: '/people', icon: Users },
      ],
    },
  },
  {
    type: 'single',
    item: { title: 'Quản Lý Phương Tiện', href: '/vehicles', icon: Car, roles: ['Admin', 'Manager', '1', '2'] },
  },
  {
    type: 'single',
    item: { title: 'Làn Kiểm Soát', href: '/lanes', icon: Route, roles: ['Admin', 'Manager', '1', '2'] },
  },
  {
    type: 'single',
    item: { title: 'Thiết Bị Phần Cứng', href: '/devices', icon: Cpu, roles: ['Admin', 'Manager', '1', '2'] },
  },
  {
    type: 'single',
    item: { title: 'Quản Lý Tài Khoản', href: '/users', icon: Users, roles: ['Admin', '1'] },
  },
  {
    type: 'single',
    item: { title: 'Thùng Rác', href: '/recycle-bin', icon: Trash2, roles: ['Admin', '1'] },
  },
]

export function Sidebar() {
  const location = useLocation()
  const { user, logout } = useAuthStore()
  const [isCollapsed, setIsCollapsed] = useState(false)
  const [openGroup, setOpenGroup] = useState<string | null>('Tổ Chức & Đơn Vị')

  const { data: trashCounts } = useQuery({
    queryKey: ['recycle-bin-counts'],
    queryFn: () => recycleBinService.getCounts(),
    refetchInterval: 30000,
  })

  const getRoleLabel = (role: any) => {
    if (role === 'Admin' || role === 1 || role === '1') return 'Admin'
    if (role === 'Manager' || role === 2 || role === '2') return 'Manager'
    if (role === 'Operator' || role === 3 || role === '3') return 'Operator'
    if (role === 'Security' || role === 4 || role === '4') return 'Security'
    return 'Viewer'
  }

  const isRoleAllowed = (roles?: string[]) => {
    if (!roles || !user) return true
    return roles.includes(String(user.role)) || roles.includes(user.role)
  }

  const toggleGroup = (groupTitle: string) => {
    if (isCollapsed) {
      setIsCollapsed(false)
      setOpenGroup(groupTitle)
    } else {
      setOpenGroup((prev) => (prev === groupTitle ? null : groupTitle))
    }
  }

  const displayName = user?.username || user?.fullName || 'admin'
  const avatarLetter = displayName.charAt(0).toUpperCase()

  return (
    <aside
      className={cn(
        'flex flex-col h-screen sticky top-0 z-30 transition-all duration-300 ease-in-out',
        'bg-white dark:bg-[#0c1220]',
        'border-r border-slate-200 dark:border-[#1e2d3d]',
        isCollapsed ? 'w-[68px]' : 'w-60'
      )}
    >
      {/* ── Brand ─────────────────────────────────────── */}
      <div
        className={cn(
          'flex items-center border-b border-slate-100 dark:border-[#1e2d3d] shrink-0 h-14',
          isCollapsed ? 'justify-center px-3' : 'px-4 justify-between'
        )}
      >
        <div className="flex items-center gap-2.5 min-w-0">
          <div className="h-8 w-8 rounded-lg bg-gradient-to-br from-blue-600 to-blue-500 flex items-center justify-center shadow-sm shrink-0">
            <ShieldCheck className="h-4.5 w-4.5 text-white" />
          </div>
          {!isCollapsed && (
            <div className="min-w-0 animate-fade-in">
              <p className="text-[13px] font-bold text-slate-900 dark:text-white tracking-tight leading-none">
                HP<span className="text-blue-600 dark:text-blue-400">PARKING</span>
              </p>
              <p className="text-[10px] text-slate-400 dark:text-slate-500 mt-0.5 font-medium">
                Admin Portal
              </p>
            </div>
          )}
        </div>

        {!isCollapsed && (
          <button
            type="button"
            onClick={() => setIsCollapsed(true)}
            className="p-1.5 rounded-md text-slate-400 hover:text-slate-600 dark:hover:text-slate-300 hover:bg-slate-100 dark:hover:bg-[#1e2d3d] transition-colors cursor-pointer shrink-0"
          >
            <ChevronLeft className="h-3.5 w-3.5" />
          </button>
        )}

        {isCollapsed && (
          <button
            type="button"
            onClick={() => setIsCollapsed(false)}
            className="p-1.5 rounded-md text-slate-400 hover:text-slate-600 dark:hover:text-slate-300 hover:bg-slate-100 dark:hover:bg-[#1e2d3d] transition-colors cursor-pointer"
          >
            <ChevronRight className="h-3.5 w-3.5" />
          </button>
        )}
      </div>

      {/* ── Navigation ────────────────────────────────── */}
      <nav className={cn('flex-1 overflow-y-auto py-3', isCollapsed ? 'px-2' : 'px-3')}>
        <div className="space-y-0.5">
          {menuConfig.map((menu, index) => {
            if (menu.type === 'single') {
              const { item } = menu
              if (!isRoleAllowed(item.roles)) return null
              const isActive =
                item.href === '/'
                  ? location.pathname === '/'
                  : location.pathname.startsWith(item.href)
              const Icon = item.icon
              const trashCount = item.href === '/recycle-bin' ? (trashCounts?.totalCount || 0) : 0

              return (
                <Link
                  key={item.href}
                  to={item.href}
                  title={isCollapsed ? item.title : undefined}
                  className={cn(
                    'flex items-center gap-2.5 rounded-lg text-[13px] font-medium transition-all duration-150 group relative',
                    isCollapsed ? 'justify-center p-2.5' : 'px-2.5 py-2',
                    isActive
                      ? 'bg-blue-600 text-white shadow-sm'
                      : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-100 dark:hover:bg-[#1a2845]'
                  )}
                >
                  <Icon
                    className={cn(
                      'h-4 w-4 shrink-0',
                      isActive ? 'text-white' : 'text-slate-400 dark:text-slate-500 group-hover:text-slate-600 dark:group-hover:text-slate-300'
                    )}
                  />
                  {!isCollapsed && (
                    <>
                      <span className="truncate flex-1">{item.title}</span>
                      {trashCount > 0 && (
                        <span className={cn(
                          'px-1.5 py-0.5 rounded-full text-[10px] font-bold leading-none',
                          isActive
                            ? 'bg-white/20 text-white'
                            : 'bg-rose-100 dark:bg-rose-950 text-rose-600 dark:text-rose-400'
                        )}>
                          {trashCount}
                        </span>
                      )}
                    </>
                  )}
                </Link>
              )
            }

            // GROUP
            const { group } = menu
            if (!isRoleAllowed(group.roles)) return null
            const isGroupActive = group.children.some((c) => location.pathname.startsWith(c.href))
            const isOpen = openGroup === group.title || isGroupActive
            const GroupIcon = group.icon

            return (
              <div key={`group-${index}`}>
                <button
                  type="button"
                  onClick={() => toggleGroup(group.title)}
                  title={isCollapsed ? group.title : undefined}
                  className={cn(
                    'w-full flex items-center gap-2.5 rounded-lg text-[13px] font-medium transition-all duration-150 group cursor-pointer',
                    isCollapsed ? 'justify-center p-2.5' : 'px-2.5 py-2 justify-between',
                    isGroupActive
                      ? 'text-blue-600 dark:text-blue-400'
                      : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-100 dark:hover:bg-[#1a2845]'
                  )}
                >
                  <div className="flex items-center gap-2.5 min-w-0">
                    <GroupIcon
                      className={cn(
                        'h-4 w-4 shrink-0',
                        isGroupActive ? 'text-blue-600 dark:text-blue-400' : 'text-slate-400 dark:text-slate-500'
                      )}
                    />
                    {!isCollapsed && <span className="truncate">{group.title}</span>}
                  </div>
                  {!isCollapsed && (
                    <ChevronDown
                      className={cn(
                        'h-3.5 w-3.5 text-slate-400 transition-transform duration-200 shrink-0',
                        isOpen && 'rotate-180 text-blue-500'
                      )}
                    />
                  )}
                </button>

                {/* Submenu */}
                {!isCollapsed && isOpen && (
                  <div className="mt-0.5 mb-1 ml-3.5 pl-2.5 border-l border-slate-200 dark:border-[#1e2d3d] space-y-0.5 animate-fade-in">
                    {group.children.map((child) => {
                      const isChildActive = location.pathname.startsWith(child.href)
                      const ChildIcon = child.icon
                      return (
                        <Link
                          key={child.href}
                          to={child.href}
                          className={cn(
                            'flex items-center gap-2 px-2 py-1.5 rounded-md text-[12.5px] font-medium transition-colors',
                            isChildActive
                              ? 'text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-950/40 font-semibold'
                              : 'text-slate-500 dark:text-slate-500 hover:text-slate-800 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-[#1a2845]'
                          )}
                        >
                          <ChildIcon
                            className={cn(
                              'h-3.5 w-3.5 shrink-0',
                              isChildActive ? 'text-blue-600 dark:text-blue-400' : 'text-slate-400'
                            )}
                          />
                          <span className="truncate">{child.title}</span>
                        </Link>
                      )
                    })}
                  </div>
                )}
              </div>
            )
          })}
        </div>
      </nav>

      {/* ── User Section ──────────────────────────────── */}
      <div
        className={cn(
          'border-t border-slate-100 dark:border-[#1e2d3d] shrink-0',
          isCollapsed ? 'p-2' : 'p-3'
        )}
      >
        {isCollapsed ? (
          <div className="flex flex-col items-center gap-2">
            <div
              title={`${displayName} · ${getRoleLabel(user?.role)}`}
              className="h-8 w-8 rounded-full bg-gradient-to-br from-blue-600 to-indigo-500 text-white text-xs font-bold flex items-center justify-center"
            >
              {avatarLetter}
            </div>
            <button
              type="button"
              onClick={logout}
              title="Đăng xuất"
              className="p-1.5 rounded-md text-slate-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-950/30 transition-colors cursor-pointer"
            >
              <LogOut className="h-3.5 w-3.5" />
            </button>
          </div>
        ) : (
          <div className="flex items-center justify-between gap-2">
            <div className="flex items-center gap-2.5 min-w-0">
              <div className="h-7 w-7 rounded-full bg-gradient-to-br from-blue-600 to-indigo-500 text-white text-xs font-bold flex items-center justify-center shrink-0">
                {avatarLetter}
              </div>
              <div className="min-w-0">
                <p className="text-[12.5px] font-semibold text-slate-800 dark:text-slate-100 truncate leading-tight">
                  {displayName}
                </p>
                <p className="text-[11px] text-slate-400 dark:text-slate-500 leading-tight">
                  {getRoleLabel(user?.role)}
                </p>
              </div>
            </div>
            <button
              type="button"
              onClick={logout}
              title="Đăng xuất"
              className="p-1.5 rounded-md text-slate-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-950/30 transition-colors cursor-pointer shrink-0"
            >
              <LogOut className="h-3.5 w-3.5" />
            </button>
          </div>
        )}
      </div>
    </aside>
  )
}
