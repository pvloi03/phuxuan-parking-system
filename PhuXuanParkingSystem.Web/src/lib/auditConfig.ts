import type { AuditActionType } from '@/types'

export interface ActionBadgeConfig {
  label: string
  bg: string
  text: string
  border: string
}

export const actionTypeConfig: Record<AuditActionType, ActionBadgeConfig> = {
  Login: {
    label: 'Đăng Nhập',
    bg: 'bg-purple-50 dark:bg-purple-950/30',
    text: 'text-purple-700 dark:text-purple-400',
    border: 'border-purple-200 dark:border-purple-800',
  },
  Logout: {
    label: 'Đăng Xuất',
    bg: 'bg-slate-50 dark:bg-slate-900/40',
    text: 'text-slate-700 dark:text-slate-400',
    border: 'border-slate-200 dark:border-slate-800',
  },
  Create: {
    label: 'Tạo Mới',
    bg: 'bg-emerald-50 dark:bg-emerald-950/30',
    text: 'text-emerald-700 dark:text-emerald-400',
    border: 'border-emerald-200 dark:border-emerald-800',
  },
  Update: {
    label: 'Cập Nhật',
    bg: 'bg-blue-50 dark:bg-blue-950/30',
    text: 'text-blue-700 dark:text-blue-400',
    border: 'border-blue-200 dark:border-blue-800',
  },
  Delete: {
    label: 'Xóa Dữ Liệu',
    bg: 'bg-rose-50 dark:bg-rose-950/30',
    text: 'text-rose-700 dark:text-rose-400',
    border: 'border-rose-200 dark:border-rose-800',
  },
  ChangePassword: {
    label: 'Đổi Mật Khẩu',
    bg: 'bg-amber-50 dark:bg-amber-950/30',
    text: 'text-amber-700 dark:text-amber-400',
    border: 'border-amber-200 dark:border-amber-800',
  },
  ChangeRole: {
    label: 'Đổi Vai Trò',
    bg: 'bg-orange-50 dark:bg-orange-950/30',
    text: 'text-orange-700 dark:text-orange-400',
    border: 'border-orange-200 dark:border-orange-800',
  },
  LicenseUpdate: {
    label: 'Cập Nhật Bản Quyền',
    bg: 'bg-cyan-50 dark:bg-cyan-950/30',
    text: 'text-cyan-700 dark:text-cyan-400',
    border: 'border-cyan-200 dark:border-cyan-800',
  },
  Export: {
    label: 'Xuất Báo Cáo',
    bg: 'bg-indigo-50 dark:bg-indigo-950/30',
    text: 'text-indigo-700 dark:text-indigo-400',
    border: 'border-indigo-200 dark:border-indigo-800',
  },
  ManualOverride: {
    label: 'Can Thiệp Thủ Công',
    bg: 'bg-red-50 dark:bg-red-950/30',
    text: 'text-red-700 dark:text-red-400',
    border: 'border-red-200 dark:border-red-800',
  },
  PermanentDelete: {
    label: 'Xóa Vĩnh Viễn',
    bg: 'bg-rose-100 dark:bg-rose-950/60',
    text: 'text-rose-800 dark:text-rose-300 font-semibold',
    border: 'border-rose-300 dark:border-rose-800',
  },
  Restore: {
    label: 'Khôi Phục',
    bg: 'bg-teal-50 dark:bg-teal-950/30',
    text: 'text-teal-700 dark:text-teal-400',
    border: 'border-teal-200 dark:border-teal-800',
  },
}

export const targetEntityList = [
  'User',
  'Person',
  'Vehicle',
  'Department',
  'Company',
  'Contractor',
  'Lane',
  'Device',
  'ParkingSession',
  'LicenseInfo',
]
