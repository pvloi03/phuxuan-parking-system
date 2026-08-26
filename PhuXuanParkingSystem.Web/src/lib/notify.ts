import { toast } from 'sonner'

/**
 * Helper thông báo thống nhất cho toàn bộ Web Admin.
 * Dùng sonner toast đã được mount trong App.tsx - thay thế cho window.alert() thô sơ.
 */

export const notify = {
  /** Thông báo thành công (màu xanh) */
  success: (message: string, description?: string) => {
    toast.success(message, description ? { description } : undefined)
  },

  /** Thông báo lỗi (màu đỏ) - tự trích message từ axios error */
  error: (message: string, err?: unknown) => {
    const detail = extractErrorMessage(err)
    toast.error(message, detail ? { description: detail } : undefined)
  },

  /** Cảnh báo (màu cam) */
  warning: (message: string, description?: string) => {
    toast.warning(message, description ? { description } : undefined)
  },

  /** Thông tin (màu xanh dương) */
  info: (message: string, description?: string) => {
    toast.info(message, description ? { description } : undefined)
  },
}

/**
 * Trích thông điệp lỗi thân thiệt từ axios error response
 */
function extractErrorMessage(err: unknown): string | undefined {
  if (!err) return undefined

  const axiosErr = err as {
    response?: { data?: { message?: string; errors?: Record<string, string[]> } }
    message?: string
  }

  const msg = axiosErr.response?.data?.message
  if (msg) return msg

  const errors = axiosErr.response?.data?.errors
  if (errors) {
    const firstKey = Object.keys(errors)[0]
    if (firstKey && errors[firstKey]?.length) return errors[firstKey][0]
  }

  return axiosErr.message
}