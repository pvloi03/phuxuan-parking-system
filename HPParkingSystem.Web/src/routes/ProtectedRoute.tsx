import { Navigate, Outlet } from 'react-router-dom'
import { useAuthStore } from '@/stores/useAuthStore'
import type { UserRole } from '@/types'
import { ShieldAlert } from 'lucide-react'

interface ProtectedRouteProps {
  allowedRoles?: (UserRole | string | number)[]
}

export function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
  const token = useAuthStore((s) => s.token)
  const user = useAuthStore((s) => s.user)
  const isAuthenticated = !!token

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (allowedRoles && allowedRoles.length > 0) {
    const roleStr = user?.role ? String(user.role).toLowerCase() : ''
    const isAdmin = roleStr === 'admin' || roleStr === '1'
    const isManager = roleStr === 'manager' || roleStr === '2'
    const isOperator = roleStr === 'operator' || roleStr === '3'
    const isSecurity = roleStr === 'security' || roleStr === '4'
    const isViewer = roleStr === 'viewer' || roleStr === '5'

    const hasAccess = allowedRoles.some((r) => {
      const allowedStr = String(r).toLowerCase()
      if (allowedStr === 'admin' && isAdmin) return true
      if (allowedStr === 'manager' && isManager) return true
      if (allowedStr === 'operator' && isOperator) return true
      if (allowedStr === 'security' && isSecurity) return true
      if (allowedStr === 'viewer' && isViewer) return true
      return roleStr === allowedStr
    })

    if (!hasAccess) {
      return (
        <div className="min-h-[60vh] flex flex-col items-center justify-center p-6 text-center animate-fade-in">
          <div className="w-16 h-16 bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-800 rounded-2xl flex items-center justify-center text-red-600 dark:text-red-400 mb-4 shadow-sm">
            <ShieldAlert className="w-8 h-8" />
          </div>
          <h2 className="text-xl font-bold text-gray-900 dark:text-white">403 - Không Có Quyền Truy Cập</h2>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-2 max-w-md">
            Tài khoản của bạn không có đủ thẩm quyền để truy cập vào phân hệ này. Vui lòng liên hệ Quản Trị Viên hệ thống nếu bạn cần phân quyền.
          </p>
          <a
            href="/"
            className="mt-5 px-4 py-2 bg-primary-600 hover:bg-primary-700 text-white text-sm font-medium rounded-lg shadow transition-colors"
          >
            Quay Về Trang Chủ
          </a>
        </div>
      )
    }
  }

  return <Outlet />
}
