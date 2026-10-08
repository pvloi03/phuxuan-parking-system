import { useAuthStore } from '@/stores/useAuthStore'
import type { UserRole } from '@/types'

export const usePermission = () => {
  const user = useAuthStore((s) => s.user)
  const roleStr = user?.role ? String(user.role).toLowerCase() : ''

  const isAdmin = roleStr === 'admin' || roleStr === '1'
  const isManager = roleStr === 'manager' || roleStr === '2'
  const isOperator = roleStr === 'operator' || roleStr === '3'
  const isSecurity = roleStr === 'security' || roleStr === '4'
  const isViewer = roleStr === 'viewer' || roleStr === '5'

  // Quyền thao tác các phân hệ
  const canManageUsers = isAdmin
  const canManageHardware = isAdmin // Quản lý Thiết bị & Làn
  const canManageOrg = isAdmin || isManager // Quản lý Công ty, Phòng ban, Nhà thầu, Nhân sự, Phương tiện
  const canCreateOrEdit = isAdmin || isManager
  const canDelete = isAdmin // Chỉ Admin mới được xóa bản ghi
  const canExport = isAdmin || isManager
  const canAccessRecycleBin = isAdmin || isManager
  const canHardDelete = isAdmin // Chỉ Admin mới được xóa vĩnh viễn trong thùng rác

  const hasRole = (allowedRoles: (UserRole | string | number)[]): boolean => {
    if (!roleStr) return false
    return allowedRoles.some((r) => {
      const allowedStr = String(r).toLowerCase()
      if (allowedStr === 'admin' && isAdmin) return true
      if (allowedStr === 'manager' && isManager) return true
      if (allowedStr === 'operator' && isOperator) return true
      if (allowedStr === 'security' && isSecurity) return true
      if (allowedStr === 'viewer' && isViewer) return true
      return roleStr === allowedStr
    })
  }

  return {
    user,
    role: user?.role,
    isAdmin,
    isManager,
    isOperator,
    isSecurity,
    isViewer,
    canManageUsers,
    canManageHardware,
    canManageOrg,
    canCreateOrEdit,
    canDelete,
    canExport,
    canAccessRecycleBin,
    canHardDelete,
    hasRole,
  }
}
