import { Navigate, Outlet } from 'react-router-dom'
import { useAuthStore } from '@/stores/useAuthStore'
import { usePermission } from '@/hooks/usePermission'
import type { UserRole } from '@/types'
import { ShieldAlert } from 'lucide-react'

interface ProtectedRouteProps {
  allowedRoles?: (UserRole | string | number)[]
}

export function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const { hasRole } = usePermission()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (allowedRoles && allowedRoles.length > 0 && !hasRole(allowedRoles)) {
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

  return <Outlet />
}
