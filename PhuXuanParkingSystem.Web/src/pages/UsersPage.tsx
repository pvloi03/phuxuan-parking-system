import React, { useState, useEffect, useCallback } from 'react'
import {
  Users,
  UserPlus,
  Search,
  RefreshCw,
  Edit2,
  Trash2,
  Key,
  Shield,
  ShieldCheck,
  UserCheck,
  Lock,
  Unlock,
  Mail,
  Phone,
  Eye,
  EyeOff,
  FileText,
  X,
  CheckCircle2,
  XCircle,
} from 'lucide-react'
import { userService, type UserQueryParams } from '@/services/userService'
import type { User, UserRole, CreateUserPayload, UpdateUserPayload, ChangePasswordPayload } from '@/types'
import { getUserRoleLabel } from '@/types'
import { usePermission } from '@/hooks/usePermission'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'

const roleBadges: Record<string, { label: string; bg: string; text: string; border: string; icon: React.ComponentType<{ className?: string }> }> = {
  Admin: {
    label: 'Quản Trị Viên',
    bg: 'bg-purple-50 dark:bg-purple-950/40',
    text: 'text-purple-700 dark:text-purple-300',
    border: 'border-purple-200 dark:border-purple-800',
    icon: ShieldCheck,
  },
  Manager: {
    label: 'Quản Lý',
    bg: 'bg-blue-50 dark:bg-blue-950/40',
    text: 'text-blue-700 dark:text-blue-300',
    border: 'border-blue-200 dark:border-blue-800',
    icon: UserCheck,
  },
  Operator: {
    label: 'Vận Hành Làn',
    bg: 'bg-cyan-50 dark:bg-cyan-950/40',
    text: 'text-cyan-700 dark:text-cyan-300',
    border: 'border-cyan-200 dark:border-cyan-800',
    icon: Shield,
  },
  Security: {
    label: 'Bảo Vệ Trực Cổng',
    bg: 'bg-emerald-50 dark:bg-emerald-950/40',
    text: 'text-emerald-700 dark:text-emerald-300',
    border: 'border-emerald-200 dark:border-emerald-800',
    icon: Lock,
  },
  Viewer: {
    label: 'Người Xem',
    bg: 'bg-gray-50 dark:bg-gray-800',
    text: 'text-gray-700 dark:text-gray-300',
    border: 'border-gray-200 dark:border-gray-700',
    icon: Eye,
  },
}

export const UsersPage: React.FC = () => {
  const { isAdmin, user: currentUser } = usePermission()

  const [users, setUsers] = useState<User[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(1)
  const [pageNumber, setPageNumber] = useState(1)
  const pageSize = 10
  const [loading, setLoading] = useState(false)

  // Filters
  const [search, setSearch] = useState('')
  const [roleFilter, setRoleFilter] = useState<string>('')
  const [statusFilter, setStatusFilter] = useState<string>('')

  // Modals
  const [isAddEditOpen, setIsAddEditOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<User | null>(null)
  const [isPasswordModalOpen, setIsPasswordModalOpen] = useState(false)
  const [passwordUser, setPasswordUser] = useState<User | null>(null)
  const [isDetailModalOpen, setIsDetailModalOpen] = useState(false)
  const [detailUser, setDetailUser] = useState<User | null>(null)

  // Confirm delete dialog
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false)
  const [deletingUser, setDeletingUser] = useState<User | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)

  // Form error & success message
  const [modalError, setModalError] = useState<string | null>(null)
  const [feedbackMessage, setFeedbackMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null)

  // Form states
  const [formData, setFormData] = useState<{
    username: string
    password: string
    fullName: string
    email: string
    phoneNumber: string
    role: UserRole
    isActive: boolean
  }>({
    username: '',
    password: '',
    fullName: '',
    email: '',
    phoneNumber: '',
    role: 'Operator',
    isActive: true,
  })

  const [passwordData, setPasswordData] = useState<{
    oldPassword?: string
    newPassword: string
    confirmPassword: string
  }>({
    oldPassword: '',
    newPassword: '',
    confirmPassword: '',
  })

  // Password visibility & loading
  const [showOldPassword, setShowOldPassword] = useState(false)
  const [showNewPassword, setShowNewPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)
  const [isSavingPassword, setIsSavingPassword] = useState(false)

  const fetchUsers = useCallback(async () => {
    try {
      setLoading(true)
      const params: UserQueryParams = {
        pageNumber,
        pageSize,
        search: search.trim() || undefined,
        role: roleFilter || undefined,
        isActive: statusFilter === '' ? undefined : statusFilter === 'true',
      }
      const data = await userService.getUsers(params)
      setUsers(data.items || [])
      setTotalCount(data.totalCount || 0)
      setTotalPages(data.totalPages || 1)
    } catch (err: any) {
      console.error(err)
      setFeedbackMessage({
        type: 'error',
        text: err?.response?.data?.message || 'Không thể tải danh sách người dùng.',
      })
    } finally {
      setLoading(false)
    }
  }, [pageNumber, pageSize, search, roleFilter, statusFilter])

  useEffect(() => {
    fetchUsers()
  }, [fetchUsers])

  // Clear feedback message automatically after 4s
  useEffect(() => {
    if (feedbackMessage) {
      const timer = setTimeout(() => setFeedbackMessage(null), 4000)
      return () => clearTimeout(timer)
    }
  }, [feedbackMessage])

  // Stat calculations
  const adminCount = users.filter((u) => u.role === 'Admin').length
  const operatorCount = users.filter((u) => u.role === 'Operator' || u.role === 'Security').length
  const activeCount = users.filter((u) => u.isActive).length

  // Handlers
  const handleOpenAdd = () => {
    setEditingUser(null)
    setModalError(null)
    setFormData({
      username: '',
      password: '',
      fullName: '',
      email: '',
      phoneNumber: '',
      role: 'Operator',
      isActive: true,
    })
    setIsAddEditOpen(true)
  }

  const handleOpenEdit = (user: User) => {
    setEditingUser(user)
    setModalError(null)
    setFormData({
      username: user.username,
      password: '',
      fullName: user.fullName,
      email: user.email || '',
      phoneNumber: user.phoneNumber || '',
      role: user.role,
      isActive: user.isActive,
    })
    setIsAddEditOpen(true)
  }

  const handleOpenPasswordModal = (user: User) => {
    setPasswordUser(user)
    setModalError(null)
    setPasswordData({
      oldPassword: '',
      newPassword: '',
      confirmPassword: '',
    })
    setShowOldPassword(false)
    setShowNewPassword(false)
    setShowConfirmPassword(false)
    setIsSavingPassword(false)
    setIsPasswordModalOpen(true)
  }

  const handleOpenDetail = (user: User) => {
    setDetailUser(user)
    setIsDetailModalOpen(true)
  }

  const handleSaveUser = async (e: React.FormEvent) => {
    e.preventDefault()
    setModalError(null)

    if (!formData.fullName.trim()) {
      setModalError('Vui lòng nhập Họ và tên.')
      return
    }

    try {
      if (editingUser) {
        // Update
        const payload: UpdateUserPayload = {
          fullName: formData.fullName.trim(),
          email: formData.email.trim() || undefined,
          phoneNumber: formData.phoneNumber.trim() || undefined,
          role: formData.role,
          isActive: formData.isActive,
        }
        await userService.updateUser(editingUser.id, payload)
        setFeedbackMessage({ type: 'success', text: 'Cập nhật thông tin tài khoản thành công!' })
      } else {
        // Create
        if (!formData.username.trim() || !formData.password.trim()) {
          setModalError('Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.')
          return
        }
        if (formData.password.length < 6) {
          setModalError('Mật khẩu phải có tối thiểu 6 ký tự.')
          return
        }
        const payload: CreateUserPayload = {
          username: formData.username.trim(),
          password: formData.password,
          fullName: formData.fullName.trim(),
          email: formData.email.trim() || undefined,
          phoneNumber: formData.phoneNumber.trim() || undefined,
          role: formData.role,
          isActive: formData.isActive,
        }
        await userService.createUser(payload)
        setFeedbackMessage({ type: 'success', text: 'Tạo mới tài khoản người dùng thành công!' })
      }
      setIsAddEditOpen(false)
      fetchUsers()
    } catch (err: any) {
      setModalError(err?.response?.data?.message || 'Có lỗi xảy ra khi lưu tài khoản.')
    }
  }

  const handleSavePassword = async (e: React.FormEvent) => {
    e.preventDefault()
    setModalError(null)
    if (!passwordUser) return

    // Với tài khoản không phải Admin: bắt buộc nhập mật khẩu cũ
    if (!isAdmin && !passwordData.oldPassword?.trim()) {
      setModalError('Vui lòng nhập mật khẩu hiện tại.')
      return
    }

    if (!passwordData.newPassword || !passwordData.newPassword.trim()) {
      setModalError('Vui lòng nhập mật khẩu mới.')
      return
    }
    if (passwordData.newPassword.trim().length < 6) {
      setModalError('Mật khẩu mới phải có tối thiểu 6 ký tự.')
      return
    }
    if (passwordData.newPassword.trim() !== passwordData.confirmPassword.trim()) {
      setModalError('Mật khẩu xác nhận không khớp với mật khẩu mới.')
      return
    }

    try {
      setIsSavingPassword(true)
      const payload: ChangePasswordPayload = {
        oldPassword: passwordData.oldPassword?.trim() || undefined,
        newPassword: passwordData.newPassword.trim(),
      }
      await userService.changePassword(passwordUser.id, payload)
      setFeedbackMessage({ type: 'success', text: `Đổi mật khẩu cho tài khoản @${passwordUser.username} thành công!` })
      setIsPasswordModalOpen(false)
    } catch (err: any) {
      setModalError(err?.response?.data?.message || 'Đổi mật khẩu thất bại. Vui lòng kiểm tra lại.')
    } finally {
      setIsSavingPassword(false)
    }
  }

  const handleToggleStatus = async (user: User) => {
    if (user.id === currentUser?.id) {
      setFeedbackMessage({ type: 'error', text: 'Bạn không thể tự khóa tài khoản của chính mình.' })
      return
    }

    try {
      const updated = await userService.toggleStatus(user.id)
      setFeedbackMessage({
        type: 'success',
        text: updated.isActive ? 'Đã mở khóa tài khoản.' : 'Đã khóa tài khoản.',
      })
      setUsers((prev) => prev.map((u) => (u.id === user.id ? { ...u, isActive: updated.isActive } : u)))
    } catch (err: any) {
      setFeedbackMessage({
        type: 'error',
        text: err?.response?.data?.message || 'Không thể thay đổi trạng thái.',
      })
    }
  }

  const handleRequestDelete = (user: User) => {
    if (user.id === currentUser?.id) {
      setFeedbackMessage({ type: 'error', text: 'Bạn không thể tự xóa tài khoản của chính mình.' })
      return
    }
    setDeletingUser(user)
    setIsConfirmDeleteOpen(true)
  }

  const handleConfirmDelete = async () => {
    if (!deletingUser) return
    try {
      setIsDeleting(true)
      await userService.deleteUser(deletingUser.id)
      setFeedbackMessage({ type: 'success', text: 'Đã xóa tài khoản thành công (chuyển vào thùng rác)!' })
      setIsConfirmDeleteOpen(false)
      fetchUsers()
    } catch (err: any) {
      setFeedbackMessage({
        type: 'error',
        text: err?.response?.data?.message || 'Không thể xóa tài khoản.',
      })
    } finally {
      setIsDeleting(false)
    }
  }

  // Get initials for avatar
  const getInitials = (name: string) => {
    if (!name) return 'U'
    const parts = name.trim().split(/\s+/)
    if (parts.length === 1) return parts[0].substring(0, 2).toUpperCase()
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
  }

  return (
    <div className="p-6 space-y-6">
      {/* Toast Feedback Alert */}
      {feedbackMessage && (
        <div
          className={`fixed top-5 right-5 z-50 px-4 py-3 rounded-xl shadow-lg border flex items-center gap-3 animate-slide-in ${
            feedbackMessage.type === 'success'
              ? 'bg-emerald-50 text-emerald-800 border-emerald-200 dark:bg-emerald-950/80 dark:text-emerald-200 dark:border-emerald-800'
              : 'bg-rose-50 text-rose-800 border-rose-200 dark:bg-rose-950/80 dark:text-rose-200 dark:border-rose-800'
          }`}
        >
          {feedbackMessage.type === 'success' ? (
            <CheckCircle2 className="w-5 h-5 text-emerald-600 dark:text-emerald-400" />
          ) : (
            <XCircle className="w-5 h-5 text-rose-600 dark:text-rose-400" />
          )}
          <span className="text-sm font-medium">{feedbackMessage.text}</span>
          <button
            onClick={() => setFeedbackMessage(null)}
            className="text-gray-400 hover:text-gray-600 ml-2"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Header & Title */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
            <Users className="w-7 h-7 text-blue-600 dark:text-blue-400" />
            Quản Lý Tài Khoản & Phân Quyền
          </h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            Quản lý danh sách tài khoản người dùng, phân cấp vai trò RBAC và kiểm soát quyền truy cập hệ thống.
          </p>
        </div>

        {isAdmin && (
          <button
            onClick={handleOpenAdd}
            className="inline-flex items-center justify-center gap-2 px-4 py-2.5 bg-blue-600 hover:bg-blue-700 text-white text-sm font-semibold rounded-lg shadow-sm hover:shadow transition-all cursor-pointer"
          >
            <UserPlus className="w-4 h-4 text-white" />
            <span>Thêm Tài Khoản Mới</span>
          </button>
        )}
      </div>

      {/* Stats Summary Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white dark:bg-gray-800 p-4 rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm flex items-center justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">
              Tổng Tài Khoản
            </p>
            <p className="text-2xl font-bold text-gray-900 dark:text-white mt-1">{totalCount}</p>
          </div>
          <div className="w-12 h-12 bg-blue-50 dark:bg-blue-900/30 rounded-xl flex items-center justify-center text-blue-600 dark:text-blue-400">
            <Users className="w-6 h-6" />
          </div>
        </div>

        <div className="bg-white dark:bg-gray-800 p-4 rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm flex items-center justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">
              Quản Trị Viên (Admin)
            </p>
            <p className="text-2xl font-bold text-purple-600 dark:text-purple-400 mt-1">{adminCount}</p>
          </div>
          <div className="w-12 h-12 bg-purple-50 dark:bg-purple-900/30 rounded-xl flex items-center justify-center text-purple-600 dark:text-purple-400">
            <ShieldCheck className="w-6 h-6" />
          </div>
        </div>

        <div className="bg-white dark:bg-gray-800 p-4 rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm flex items-center justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">
              Vận Hành & Trực Làn
            </p>
            <p className="text-2xl font-bold text-cyan-600 dark:text-cyan-400 mt-1">{operatorCount}</p>
          </div>
          <div className="w-12 h-12 bg-cyan-50 dark:bg-cyan-900/30 rounded-xl flex items-center justify-center text-cyan-600 dark:text-cyan-400">
            <Shield className="w-6 h-6" />
          </div>
        </div>

        <div className="bg-white dark:bg-gray-800 p-4 rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm flex items-center justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">
              Đang Hoạt Động
            </p>
            <p className="text-2xl font-bold text-emerald-600 dark:text-emerald-400 mt-1">{activeCount}</p>
          </div>
          <div className="w-12 h-12 bg-emerald-50 dark:bg-emerald-900/30 rounded-xl flex items-center justify-center text-emerald-600 dark:text-emerald-400">
            <CheckCircle2 className="w-6 h-6" />
          </div>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="bg-white dark:bg-gray-800 p-4 rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm flex flex-col md:flex-row gap-3 items-stretch md:items-center justify-between">
        <div className="flex-1 relative">
          <Search className="w-4 h-4 text-gray-400 absolute left-3 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            placeholder="Tìm theo Tên đăng nhập, Họ tên, Email, SĐT..."
            value={search}
            onChange={(e) => {
              setSearch(e.target.value)
              setPageNumber(1)
            }}
            className="w-full pl-9 pr-4 py-2 text-sm bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-900 dark:text-white"
          />
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <select
            value={roleFilter}
            onChange={(e) => {
              setRoleFilter(e.target.value)
              setPageNumber(1)
            }}
            className="px-3 py-2 text-sm bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-700 dark:text-gray-300"
          >
            <option value="">Tất cả vai trò</option>
            <option value="Admin">👑 Quản Trị Viên (Admin)</option>
            <option value="Manager">👔 Quản Lý (Manager)</option>
            <option value="Operator">🛡️ Vận Hành Làn (Operator)</option>
            <option value="Security">👮 Bảo Vệ (Security)</option>
            <option value="Viewer">👁️ Người Xem (Viewer)</option>
          </select>

          <select
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value)
              setPageNumber(1)
            }}
            className="px-3 py-2 text-sm bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-700 dark:text-gray-300"
          >
            <option value="">Tất cả trạng thái</option>
            <option value="true">Đang hoạt động</option>
            <option value="false">Đã bị khóa</option>
          </select>

          <button
            onClick={() => fetchUsers()}
            title="Làm mới"
            className="p-2 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors cursor-pointer"
          >
            <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
          </button>
        </div>
      </div>

      {/* Users Data Table */}
      <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse text-sm">
            <thead>
              <tr className="bg-gray-50/80 dark:bg-gray-900/60 border-b border-gray-200 dark:border-gray-700 text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">
                <th className="py-3.5 px-4">Tài Khoản</th>
                <th className="py-3.5 px-4">Liên Hệ</th>
                <th className="py-3.5 px-4">Vai Trò</th>
                <th className="py-3.5 px-4">Trạng Thái</th>
                <th className="py-3.5 px-4 text-center">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
              {loading && users.length === 0 ? (
                <tr>
                  <td colSpan={5} className="py-12 text-center text-gray-500 dark:text-gray-400">
                    <RefreshCw className="w-6 h-6 animate-spin mx-auto mb-2 text-blue-500" />
                    Đang tải danh sách người dùng...
                  </td>
                </tr>
              ) : users.length === 0 ? (
                <tr>
                  <td colSpan={5} className="py-12 text-center text-gray-500 dark:text-gray-400">
                    <Users className="w-10 h-10 mx-auto mb-2 text-gray-300 dark:text-gray-600" />
                    Không tìm thấy tài khoản nào phù hợp.
                  </td>
                </tr>
              ) : (
                users.map((user) => {
                  const badge = roleBadges[user.role] || roleBadges.Viewer
                  const RoleIcon = badge.icon
                  const isCurrent = user.id === currentUser?.id

                  return (
                    <tr
                      key={user.id}
                      className="hover:bg-gray-50/60 dark:hover:bg-gray-750/50 transition-colors group"
                    >
                      {/* User Info - Chỉ hiện username */}
                      <td className="py-3.5 px-4">
                        <div className="flex items-center gap-2">
                          <span className="font-semibold text-gray-900 dark:text-white">
                            {user.username}
                          </span>
                          {isCurrent && (
                            <span className="text-[10px] bg-blue-100 dark:bg-blue-950/60 text-blue-700 dark:text-blue-300 font-bold px-1.5 py-0.5 rounded border border-blue-300 dark:border-blue-800">
                              Bạn
                            </span>
                          )}
                        </div>
                      </td>

                      {/* Contact */}
                      <td className="py-3.5 px-4">
                        <div className="space-y-0.5 text-xs text-gray-600 dark:text-gray-300">
                          {user.email ? (
                            <div className="flex items-center gap-1.5">
                              <Mail className="w-3.5 h-3.5 text-gray-400" />
                              <span>{user.email}</span>
                            </div>
                          ) : (
                            <span className="text-gray-400 italic">Chưa có email</span>
                          )}
                          {user.phoneNumber && (
                            <div className="flex items-center gap-1.5">
                              <Phone className="w-3.5 h-3.5 text-gray-400" />
                              <span>{user.phoneNumber}</span>
                            </div>
                          )}
                        </div>
                      </td>

                      {/* Role Badge */}
                      <td className="py-3.5 px-4">
                        <span
                          className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-semibold border ${badge.bg} ${badge.text} ${badge.border}`}
                        >
                          <RoleIcon className="w-3.5 h-3.5" />
                          {badge.label}
                        </span>
                      </td>

                      {/* Status Toggle */}
                      <td className="py-3.5 px-4">
                        {isAdmin && !isCurrent ? (
                          <button
                            onClick={() => handleToggleStatus(user)}
                            className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium transition-all cursor-pointer ${
                              user.isActive
                                ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-300 hover:bg-emerald-100 border border-emerald-200 dark:border-emerald-800'
                                : 'bg-rose-50 text-rose-700 dark:bg-rose-950/40 dark:text-rose-300 hover:bg-rose-100 border border-rose-200 dark:border-rose-800'
                            }`}
                            title="Bấm để chuyển trạng thái khóa / mở khóa"
                          >
                            {user.isActive ? (
                              <>
                                <Unlock className="w-3 h-3" />
                                <span>Hoạt động</span>
                              </>
                            ) : (
                              <>
                                <Lock className="w-3 h-3" />
                                <span>Đã khóa</span>
                              </>
                            )}
                          </button>
                        ) : (
                          <span
                            className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium ${
                              user.isActive
                                ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800'
                                : 'bg-rose-50 text-rose-700 dark:bg-rose-950/40 dark:text-rose-300 border border-rose-200 dark:border-rose-800'
                            }`}
                          >
                            {user.isActive ? <CheckCircle2 className="w-3 h-3" /> : <XCircle className="w-3 h-3" />}
                            {user.isActive ? 'Hoạt động' : 'Đã khóa'}
                          </span>
                        )}
                      </td>

                      {/* Actions */}
                      <td className="py-3.5 px-4">
                        <div className="flex items-center justify-center gap-1.5">
                          {/* Standard Detail Button */}
                          <button
                            onClick={() => handleOpenDetail(user)}
                            className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium text-blue-600 bg-blue-50 border border-blue-200 rounded-md hover:bg-blue-100 dark:text-blue-400 dark:bg-blue-950/40 dark:border-blue-900 dark:hover:bg-blue-900/50 transition-colors cursor-pointer"
                            title="Xem chi tiết tài khoản"
                          >
                            <FileText className="w-3.5 h-3.5" />
                            Chi tiết
                          </button>

                          {/* Change Password Button */}
                          {(isAdmin || isCurrent) && (
                            <button
                              onClick={() => handleOpenPasswordModal(user)}
                              className="p-1.5 text-amber-600 hover:bg-amber-50 dark:text-amber-400 dark:hover:bg-amber-950/40 rounded-md border border-amber-200 dark:border-amber-900 transition-colors cursor-pointer"
                              title="Đổi mật khẩu"
                            >
                              <Key className="w-3.5 h-3.5" />
                            </button>
                          )}

                          {/* Edit Button */}
                          {isAdmin && (
                            <button
                              onClick={() => handleOpenEdit(user)}
                              className="p-1.5 text-blue-600 hover:bg-blue-50 dark:text-blue-400 dark:hover:bg-blue-950/40 rounded-md border border-blue-200 dark:border-blue-900 transition-colors cursor-pointer"
                              title="Chỉnh sửa thông tin"
                            >
                              <Edit2 className="w-3.5 h-3.5" />
                            </button>
                          )}

                          {/* Delete Button */}
                          {isAdmin && !isCurrent && (
                            <button
                              onClick={() => handleRequestDelete(user)}
                              className="p-1.5 text-rose-600 hover:bg-rose-50 dark:text-rose-400 dark:hover:bg-rose-950/40 rounded-md border border-rose-200 dark:border-rose-900 transition-colors cursor-pointer"
                              title="Xóa tài khoản"
                            >
                              <Trash2 className="w-3.5 h-3.5" />
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  )
                })
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination Footer */}
        {totalCount > 0 && (
          <div className="py-3 px-4 bg-gray-50/80 dark:bg-gray-900/60 border-t border-gray-200 dark:border-gray-700 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-gray-500 dark:text-gray-400">
            <div>
              Hiển thị <span className="font-semibold text-gray-700 dark:text-gray-200">{users.length}</span> trên tổng số{' '}
              <span className="font-semibold text-gray-700 dark:text-gray-200">{totalCount}</span> tài khoản
            </div>

            <div className="flex items-center gap-2">
              <button
                disabled={pageNumber <= 1}
                onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
                className="px-2.5 py-1 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md font-medium disabled:opacity-40 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-700"
              >
                Trước
              </button>
              <span>
                Trang {pageNumber} / {totalPages}
              </span>
              <button
                disabled={pageNumber >= totalPages}
                onClick={() => setPageNumber((p) => Math.min(totalPages, p + 1))}
                className="px-2.5 py-1 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md font-medium disabled:opacity-40 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-700"
              >
                Sau
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Confirm Delete Dialog */}
      <ConfirmDialog
        open={isConfirmDeleteOpen}
        onOpenChange={setIsConfirmDeleteOpen}
        title={`Xác Nhận Xóa Tài Khoản [${deletingUser?.username}]`}
        description={
          <span>
            Bạn có chắc chắn muốn xóa tài khoản <strong>{deletingUser?.fullName}</strong> (@{deletingUser?.username})? Tài khoản sẽ được chuyển vào Thùng Rác hệ thống.
          </span>
        }
        confirmText="Xác Nhận Xóa"
        isLoading={isDeleting}
        onConfirm={handleConfirmDelete}
      />

      {/* ========================================================================= */}
      {/* MODAL: THÊM MỚI / CHỈNH SỬA TÀI KHOẢN */}
      {/* ========================================================================= */}
      {isAddEditOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4 animate-fade-in">
          <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-2xl border border-gray-200 dark:border-gray-700 w-full max-w-xl overflow-hidden">
            {/* Header */}
            <div className="px-6 py-3.5 border-b border-gray-100 dark:border-gray-700 flex items-center justify-between bg-gray-50/50 dark:bg-gray-800/50">
              <h3 className="font-bold text-base text-gray-900 dark:text-white flex items-center gap-2">
                {editingUser ? <Edit2 className="w-4 h-4 text-blue-500" /> : <UserPlus className="w-4 h-4 text-blue-500" />}
                {editingUser ? 'Chỉnh Sửa Tài Khoản Người Dùng' : 'Thêm Mới Tài Khoản Người Dùng'}
              </h3>
              <button
                onClick={() => setIsAddEditOpen(false)}
                className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 cursor-pointer"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {/* Error Banner */}
            {modalError && (
              <div className="mx-6 mt-3 p-2.5 bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-800 rounded-lg text-xs text-red-700 dark:text-red-300 flex items-center gap-2">
                <XCircle className="w-4 h-4 shrink-0" />
                <span>{modalError}</span>
              </div>
            )}

            {/* Form - 2 Columns Compact Grid */}
            <form onSubmit={handleSaveUser} className="p-6 space-y-3.5">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
                {/* Tên Đăng Nhập */}
                <div>
                  <label className="block text-[11px] font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider mb-1">
                    Tên Đăng Nhập <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    required
                    disabled={!!editingUser}
                    placeholder="VD: nguyenvan_a"
                    value={formData.username}
                    onChange={(e) => setFormData({ ...formData, username: e.target.value })}
                    className="w-full px-3 py-2 text-xs bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-900 dark:text-white disabled:bg-gray-100 dark:disabled:bg-gray-800 disabled:cursor-not-allowed"
                  />
                  {editingUser && (
                    <p className="text-[10px] text-gray-500 mt-0.5">Không thể sửa username.</p>
                  )}
                </div>

                {/* Mật Khẩu (Khi Thêm mới) */}
                {!editingUser && (
                  <div>
                    <label className="block text-[11px] font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider mb-1">
                      Mật Khẩu <span className="text-red-500">*</span>
                    </label>
                    <input
                      type="password"
                      required
                      placeholder="Tối thiểu 6 ký tự"
                      value={formData.password}
                      onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                      className="w-full px-3 py-2 text-xs bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-900 dark:text-white"
                    />
                  </div>
                )}

                {/* Họ Và Tên */}
                <div className={editingUser ? 'col-span-1' : ''}>
                  <label className="block text-[11px] font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider mb-1">
                    Họ Và Tên <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    required
                    placeholder="VD: Nguyễn Văn An"
                    value={formData.fullName}
                    onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                    className="w-full px-3 py-2 text-xs bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-900 dark:text-white"
                  />
                </div>

                {/* Email */}
                <div>
                  <label className="block text-[11px] font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider mb-1">
                    Email
                  </label>
                  <input
                    type="email"
                    placeholder="user@phuxuan.vn"
                    value={formData.email}
                    onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                    className="w-full px-3 py-2 text-xs bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-900 dark:text-white"
                  />
                </div>

                {/* Số Điện Thoại */}
                <div>
                  <label className="block text-[11px] font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider mb-1">
                    Số Điện Thoại
                  </label>
                  <input
                    type="text"
                    placeholder="0912345678"
                    value={formData.phoneNumber}
                    onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
                    className="w-full px-3 py-2 text-xs bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-900 dark:text-white"
                  />
                </div>

                {/* Vai Trò Phân Quyền */}
                <div>
                  <label className="block text-[11px] font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider mb-1">
                    Vai Trò Phân Quyền <span className="text-red-500">*</span>
                  </label>
                  <select
                    value={formData.role}
                    onChange={(e) => setFormData({ ...formData, role: e.target.value as UserRole })}
                    className="w-full px-3 py-2 text-xs bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-900 dark:text-white"
                  >
                    <option value="Admin">👑 Quản Trị Viên (Admin)</option>
                    <option value="Manager">👔 Quản Lý (Manager)</option>
                    <option value="Operator">🛡️ Vận Hành Làn (Operator)</option>
                    <option value="Security">👮 Bảo Vệ Trực Cổng (Security)</option>
                    <option value="Viewer">👁️ Người Xem (Viewer)</option>
                  </select>
                </div>

                {/* Trạng Thái */}
                <div>
                  <label className="block text-[11px] font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider mb-1">
                    Trạng Thái
                  </label>
                  <select
                    value={formData.isActive ? 'true' : 'false'}
                    onChange={(e) => setFormData({ ...formData, isActive: e.target.value === 'true' })}
                    className="w-full px-3 py-2 text-xs bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-900 dark:text-white"
                  >
                    <option value="true">Đang hoạt động</option>
                    <option value="false">Khóa tài khoản</option>
                  </select>
                </div>
              </div>

              {/* Actions */}
              <div className="pt-3 border-t border-gray-100 dark:border-gray-700 flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setIsAddEditOpen(false)}
                  className="px-3.5 py-1.5 text-xs font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors cursor-pointer"
                >
                  Hủy Bỏ
                </button>
                <button
                  type="submit"
                  className="px-4 py-1.5 bg-blue-600 hover:bg-blue-700 text-white text-xs font-semibold rounded-lg shadow-sm hover:shadow transition-all cursor-pointer"
                >
                  {editingUser ? 'Lưu Thay Đổi' : 'Tạo Tài Khoản'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ========================================================================= */}
      {/* MODAL: ĐỔI MẬT KHẨU */}
      {/* ========================================================================= */}
      {isPasswordModalOpen && passwordUser && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4 animate-fade-in">
          <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-2xl border border-gray-200 dark:border-gray-700 w-full max-w-md overflow-hidden">
            <div className="px-6 py-4 border-b border-gray-100 dark:border-gray-700 flex items-center justify-between bg-blue-50/40 dark:bg-blue-950/20">
              <h3 className="font-bold text-base text-gray-900 dark:text-white flex items-center gap-2">
                <Key className="w-5 h-5 text-blue-600 dark:text-blue-400" />
                Đổi Mật Khẩu Tài Khoản
              </h3>
              <button
                onClick={() => setIsPasswordModalOpen(false)}
                className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 cursor-pointer"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Error Banner */}
            {modalError && (
              <div className="mx-6 mt-4 p-3 bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-800 rounded-lg text-xs text-red-700 dark:text-red-300 flex items-center gap-2">
                <XCircle className="w-4 h-4 shrink-0" />
                <span>{modalError}</span>
              </div>
            )}

            <form onSubmit={handleSavePassword} className="p-6 space-y-4">
              <div className="p-3 bg-gray-50 dark:bg-gray-900 rounded-xl text-xs space-y-1.5 text-gray-600 dark:text-gray-400 border border-gray-100 dark:border-gray-800">
                <p>
                  Đổi mật khẩu cho: <strong className="text-gray-900 dark:text-white font-mono">@{passwordUser.username}</strong>
                </p>
                {isAdmin ? (
                  <p className="text-blue-600 dark:text-blue-400 font-medium">
                    ⚡ Quyền Quản Trị Viên: Bạn có thể thiết lập trực tiếp mật khẩu mới cho tài khoản.
                  </p>
                ) : (
                  <p className="text-amber-600 dark:text-amber-400 font-medium">
                    🔒 Vui lòng nhập đúng mật khẩu hiện tại để xác thực trước khi đổi mật khẩu mới.
                  </p>
                )}
              </div>

              {/* Mật khẩu hiện tại (Chỉ bắt buộc đối với người dùng không phải Admin) */}
              {!isAdmin && (
                <div>
                  <label className="block text-[11px] font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider mb-1">
                    Mật Khẩu Hiện Tại <span className="text-red-500">*</span>
                  </label>
                  <div className="relative">
                    <input
                      type={showOldPassword ? 'text' : 'password'}
                      required
                      placeholder="Nhập mật khẩu hiện tại"
                      value={passwordData.oldPassword}
                      onChange={(e) => setPasswordData({ ...passwordData, oldPassword: e.target.value })}
                      className="w-full pl-3.5 pr-10 py-2.5 text-xs bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-900 dark:text-white"
                    />
                    <button
                      type="button"
                      onClick={() => setShowOldPassword(!showOldPassword)}
                      className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 cursor-pointer"
                    >
                      {showOldPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                    </button>
                  </div>
                </div>
              )}

              {/* Mật khẩu mới */}
              <div>
                <label className="block text-[11px] font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider mb-1">
                  Mật Khẩu Mới <span className="text-red-500">*</span>
                </label>
                <div className="relative">
                  <input
                    type={showNewPassword ? 'text' : 'password'}
                    required
                    placeholder="Tối thiểu 6 ký tự"
                    value={passwordData.newPassword}
                    onChange={(e) => setPasswordData({ ...passwordData, newPassword: e.target.value })}
                    className="w-full pl-3.5 pr-10 py-2.5 text-xs bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-900 dark:text-white"
                  />
                  <button
                    type="button"
                    onClick={() => setShowNewPassword(!showNewPassword)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 cursor-pointer"
                  >
                    {showNewPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>
              </div>

              {/* Xác nhận mật khẩu mới */}
              <div>
                <label className="block text-[11px] font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider mb-1">
                  Xác Nhận Mật Khẩu Mới <span className="text-red-500">*</span>
                </label>
                <div className="relative">
                  <input
                    type={showConfirmPassword ? 'text' : 'password'}
                    required
                    placeholder="Nhập lại mật khẩu mới"
                    value={passwordData.confirmPassword}
                    onChange={(e) => setPasswordData({ ...passwordData, confirmPassword: e.target.value })}
                    className="w-full pl-3.5 pr-10 py-2.5 text-xs bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-900 dark:text-white"
                  />
                  <button
                    type="button"
                    onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 cursor-pointer"
                  >
                    {showConfirmPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>
              </div>

              {/* Action Buttons */}
              <div className="pt-3 border-t border-gray-100 dark:border-gray-700 flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setIsPasswordModalOpen(false)}
                  className="px-3.5 py-1.5 text-xs font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors cursor-pointer"
                >
                  Hủy Bỏ
                </button>
                <button
                  type="submit"
                  disabled={isSavingPassword}
                  className="inline-flex items-center gap-1.5 px-4 py-1.5 bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white text-xs font-semibold rounded-lg shadow-sm hover:shadow transition-all cursor-pointer"
                >
                  {isSavingPassword && <RefreshCw className="w-3.5 h-3.5 animate-spin" />}
                  <span>{isSavingPassword ? 'Đang Xử Lý...' : 'Xác Nhận Đổi'}</span>
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ========================================================================= */}
      {/* MODAL: CHI TIẾT TÀI KHOẢN */}
      {/* ========================================================================= */}
      {isDetailModalOpen && detailUser && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4 animate-fade-in">
          <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-2xl border border-gray-200 dark:border-gray-700 w-full max-w-lg overflow-hidden">
            <div className="px-6 py-4 border-b border-gray-100 dark:border-gray-700 flex items-center justify-between bg-blue-50/40 dark:bg-blue-950/20">
              <h3 className="font-bold text-lg text-gray-900 dark:text-white flex items-center gap-2">
                <FileText className="w-5 h-5 text-blue-500" />
                Hồ Sơ Tài Khoản Người Dùng
              </h3>
              <button
                onClick={() => setIsDetailModalOpen(false)}
                className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="p-6 space-y-6">
              <div className="flex items-center gap-4">
                <div
                  className={`w-16 h-16 rounded-2xl flex items-center justify-center font-bold text-xl shadow-md ${
                    detailUser.role === 'Admin'
                      ? 'bg-gradient-to-tr from-purple-600 to-indigo-500 text-white'
                      : detailUser.role === 'Manager'
                      ? 'bg-gradient-to-tr from-blue-600 to-cyan-500 text-white'
                      : 'bg-gradient-to-tr from-teal-600 to-emerald-500 text-white'
                  }`}
                >
                  {getInitials(detailUser.fullName)}
                </div>
                <div>
                  <h4 className="text-lg font-bold text-gray-900 dark:text-white">{detailUser.fullName}</h4>
                  <p className="text-sm text-gray-500 dark:text-gray-400 font-mono">@{detailUser.username}</p>
                  <div className="mt-1.5 flex items-center gap-2">
                    <span
                      className={`inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-semibold ${
                        roleBadges[detailUser.role]?.bg
                      } ${roleBadges[detailUser.role]?.text}`}
                    >
                      {getUserRoleLabel(detailUser.role)}
                    </span>
                    <span
                      className={`inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium ${
                        detailUser.isActive
                          ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-300'
                          : 'bg-rose-50 text-rose-700 dark:bg-rose-950/40 dark:text-rose-300'
                      }`}
                    >
                      {detailUser.isActive ? 'Đang hoạt động' : 'Đã khóa'}
                    </span>
                  </div>
                </div>
              </div>

              <div className="bg-gray-50 dark:bg-gray-900/50 rounded-xl p-4 space-y-3 text-sm border border-gray-100 dark:border-gray-700/60">
                <div className="flex justify-between items-center py-1 border-b border-gray-200/60 dark:border-gray-700/60">
                  <span className="text-gray-500 dark:text-gray-400">ID Người Dùng:</span>
                  <span className="font-mono text-xs text-gray-700 dark:text-gray-300">{detailUser.id}</span>
                </div>
                <div className="flex justify-between items-center py-1 border-b border-gray-200/60 dark:border-gray-700/60">
                  <span className="text-gray-500 dark:text-gray-400">Email:</span>
                  <span className="text-gray-800 dark:text-gray-200">{detailUser.email || '--'}</span>
                </div>
                <div className="flex justify-between items-center py-1 border-b border-gray-200/60 dark:border-gray-700/60">
                  <span className="text-gray-500 dark:text-gray-400">Số Điện Thoại:</span>
                  <span className="text-gray-800 dark:text-gray-200">{detailUser.phoneNumber || '--'}</span>
                </div>
                <div className="flex justify-between items-center py-1 border-b border-gray-200/60 dark:border-gray-700/60">
                  <span className="text-gray-500 dark:text-gray-400">Đăng Nhập Gần Nhất:</span>
                  <span className="text-gray-800 dark:text-gray-200">
                    {detailUser.lastLoginAt ? new Date(detailUser.lastLoginAt).toLocaleString('vi-VN') : 'Chưa đăng nhập'}
                  </span>
                </div>
                <div className="flex justify-between items-center py-1">
                  <span className="text-gray-500 dark:text-gray-400">Ngày Tạo Tài Khoản:</span>
                  <span className="text-gray-800 dark:text-gray-200">
                    {new Date(detailUser.createdAt).toLocaleString('vi-VN')}
                  </span>
                </div>
              </div>

              <div className="flex justify-end">
                <button
                  onClick={() => setIsDetailModalOpen(false)}
                  className="px-5 py-2 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 text-gray-800 dark:text-gray-200 text-sm font-medium rounded-lg transition-colors"
                >
                  Đóng
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
