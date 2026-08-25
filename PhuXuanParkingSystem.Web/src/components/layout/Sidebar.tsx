import { useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import {
  LayoutDashboard,
  History,
  Car,
  Users,
  Building2,
  Building,
  Handshake,
  Cpu,
  Trash2,
  ShieldCheck,
  LogOut,
  ChevronLeft,
  ChevronRight,
  ChevronDown,
} from 'lucide-react'
import { useAuthStore } from '@/stores/useAuthStore'
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
      title: 'Dashboard Thống Kê',
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
        {
          title: 'Công Ty & Doanh Nghiệp',
          href: '/companies',
          icon: Building,
        },
        {
          title: 'Phòng Ban Trực Thuộc',
          href: '/departments',
          icon: Building2,
        },
        {
          title: 'Đối Tác & Nhà Thầu',
          href: '/partners',
          icon: Handshake,
        },
        {
          title: 'Danh Sách Nhân Sự',
          href: '/people',
          icon: Users,
        },
      ],
    },
  },
  {
    type: 'single',
    item: {
      title: 'Quản Lý Phương Tiện',
      href: '/vehicles',
      icon: Car,
      roles: ['Admin', 'Manager', '1', '2'],
    },
  },
  {
    type: 'single',
    item: {
      title: 'Thiết Bị Phần Cứng',
      href: '/devices',
      icon: Cpu,
      roles: ['Admin', 'Manager', '1', '2'],
    },
  },
  {
    type: 'single',
    item: {
      title: 'Thùng Rác Hệ Thống',
      href: '/recycle-bin',
      icon: Trash2,
      roles: ['Admin', '1'],
    },
  },
]

export function Sidebar() {
  const location = useLocation()
  const { user, logout } = useAuthStore()
  const [isCollapsed, setIsCollapsed] = useState(false)
  const [openGroup, setOpenGroup] = useState<string | null>('Tổ Chức & Đơn Vị')

  const getRoleDisplayName = (role: any) => {
    if (role === 'Admin' || role === 1 || role === '1' || role === 'SuperAdmin') return 'Quản Trị Viên'
    if (role === 'Manager' || role === 2 || role === '2') return 'Quản Lý'
    if (role === 'Operator' || role === 3 || role === '3') return 'Nhân Viên Vận Hành'
    if (role === 'Security' || role === 4 || role === '4') return 'Bảo Vệ'
    if (role === 'Viewer' || role === 5 || role === '5') return 'Người Xem'
    return 'Quản Trị Viên'
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

  const displayName = user?.fullName || user?.username || 'Admin'
  const displayAvatarLetter = displayName.charAt(0).toUpperCase()

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
        {menuConfig.map((menu, index) => {
          if (menu.type === 'single') {
            const { item } = menu
            if (!isRoleAllowed(item.roles)) return null

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
                  isCollapsed ? 'justify-center p-2.5' : 'gap-3 px-3 py-2.5',
                  isActive
                    ? 'bg-blue-50 text-blue-700 dark:bg-blue-600/15 dark:text-blue-400 shadow-xs font-semibold'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100 dark:text-slate-400 dark:hover:text-slate-200 dark:hover:bg-slate-800/60'
                )}
              >
                <Icon
                  className={cn(
                    'h-4.5 w-4.5 shrink-0 transition-transform group-hover:scale-105',
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
          }

          // GROUP ACCORDION SUBMENU
          const { group } = menu
          if (!isRoleAllowed(group.roles)) return null

          const isGroupActive = group.children.some((c) =>
            location.pathname.startsWith(c.href)
          )
          const isOpen = openGroup === group.title || isGroupActive
          const GroupIcon = group.icon

          return (
            <div key={`group-${index}`} className="space-y-1">
              {/* Group Header Button */}
              <button
                type="button"
                onClick={() => toggleGroup(group.title)}
                title={isCollapsed ? group.title : undefined}
                className={cn(
                  'w-full flex items-center rounded-xl text-sm font-medium transition-all duration-150 group cursor-pointer',
                  isCollapsed ? 'justify-center p-2.5' : 'justify-between px-3 py-2.5',
                  isGroupActive
                    ? 'text-blue-700 dark:text-blue-400 font-semibold bg-blue-50/60 dark:bg-blue-950/30'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100 dark:text-slate-400 dark:hover:text-slate-200 dark:hover:bg-slate-800/60'
                )}
              >
                <div className="flex items-center gap-3 min-w-0">
                  <GroupIcon
                    className={cn(
                      'h-4.5 w-4.5 shrink-0',
                      isGroupActive ? 'text-blue-600 dark:text-blue-400' : 'text-slate-400'
                    )}
                  />
                  {!isCollapsed && (
                    <span className="truncate">{group.title}</span>
                  )}
                </div>

                {!isCollapsed && (
                  <ChevronDown
                    className={cn(
                      'h-4 w-4 text-slate-400 transition-transform duration-200',
                      isOpen && 'rotate-180 text-blue-600 dark:text-blue-400'
                    )}
                  />
                )}
              </button>

              {/* Children Submenu List (Tinh chỉnh thụt lề nhẹ nhàng) */}
              {(!isCollapsed && isOpen) && (
                <div className="pl-2.5 pr-1 space-y-1 animate-in slide-in-from-top-2 fade-in duration-200 border-l-2 border-slate-200 dark:border-slate-800 ml-3.5 my-1">
                  {group.children.map((child) => {
                    const isChildActive = location.pathname.startsWith(child.href)
                    const ChildIcon = child.icon

                    return (
                      <Link
                        key={child.href}
                        to={child.href}
                        className={cn(
                          'flex items-center gap-2.5 px-2.5 py-2 rounded-lg text-xs font-medium transition-colors',
                          isChildActive
                            ? 'bg-blue-600 text-white shadow-xs font-semibold'
                            : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100 dark:text-slate-400 dark:hover:text-slate-100 dark:hover:bg-slate-800/70'
                        )}
                      >
                        <ChildIcon
                          className={cn(
                            'h-3.5 w-3.5 shrink-0',
                            isChildActive ? 'text-white' : 'text-slate-400'
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
      </nav>

      {/* User Profile & Logout Section */}
      <div className="p-3 border-t border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-950/40">
        {isCollapsed ? (
          <div className="flex flex-col items-center gap-2">
            <div
              className="h-9 w-9 rounded-full bg-gradient-to-tr from-blue-600 to-indigo-500 text-white font-bold flex items-center justify-center text-xs shadow-xs"
              title={`${displayName} (${getRoleDisplayName(user?.role)})`}
            >
              {displayAvatarLetter}
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
                {displayAvatarLetter}
              </div>
              <div className="min-w-0 space-y-0.5">
                <p
                  className="text-xs font-bold text-slate-800 dark:text-slate-100 truncate"
                  title={displayName}
                >
                  {displayName}
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
