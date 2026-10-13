import { useRef } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/services/apiClient'
import { notify } from '@/lib/notify'
import {
  parseExcelFile,
  downloadCrossImportTemplate,
  parseCrossImportExcel,
} from '@/lib/excelHelper'
import type {
  ApiResponse,
  CrossImportResult,
  VehiclePersonCrossImportItem,
} from '@/types'

export interface UseCrossImportOptions {
  /** Phân hệ thực hiện nhập chéo: 'people' hoặc 'vehicles' */
  target: 'people' | 'vehicles'
  /** Tên file mẫu khi người dùng bấm tải mẫu (tùy chọn) */
  templateFileName?: string
  /** Callback bổ sung khi import thành công (tùy chọn) */
  onSuccess?: (data?: CrossImportResult) => void
}

/**
 * Hook dùng chung cho việc nhập file Excel liên thông chéo 2 chiều (Cross-Import)
 * giữa Nhân sự và Phương tiện.
 * Đóng gói toàn bộ quản lý input ref, validation, mutation, notification và cache invalidation.
 */
export const useCrossImport = ({
  target,
  templateFileName,
  onSuccess,
}: UseCrossImportOptions) => {
  const queryClient = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const defaultTemplate =
    target === 'people'
      ? 'Mau_Nhap_NhanSu_PhuongTien.xlsx'
      : 'Mau_Nhap_PhuongTien_NhanSu.xlsx'

  const mutation = useMutation({
    mutationFn: async (items: VehiclePersonCrossImportItem[]) => {
      const res = await apiClient.post<ApiResponse<CrossImportResult>>(
        `/${target}/cross-import`,
        items
      )
      return res.data.data
    },
    onSuccess: (data) => {
      // Tự động làm mới cache cả 2 bảng và lookup liên quan
      queryClient.invalidateQueries({ queryKey: ['people-list'] })
      queryClient.invalidateQueries({ queryKey: ['people-all-lookup'] })
      queryClient.invalidateQueries({ queryKey: ['vehicles-list'] })

      notify.success(
        data?.summaryMessage || 'Nhập dữ liệu từ file Excel thành công!'
      )
      if (data?.errors && data.errors.length > 0) {
        notify.warning(
          `Có ${data.errors.length} cảnh báo: ${data.errors.slice(0, 3).join('; ')}`
        )
      }
      onSuccess?.(data)
    },
    onError: (err: any) => {
      notify.error('Nhập file Excel thất bại', err)
    },
  })

  const handleDownloadTemplate = () => {
    downloadCrossImportTemplate(templateFileName || defaultTemplate)
  }

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    try {
      const rawData = await parseExcelFile<any>(file)
      if (!rawData || rawData.length === 0) {
        notify.warning('File Excel không có dữ liệu.')
        return
      }

      const formattedData = parseCrossImportExcel(rawData)

      if (!formattedData.length) {
        notify.warning(
          'Không tìm thấy bản ghi hợp lệ trong file Excel (Cần có Biển số xe hoặc Mã nhân sự / Họ tên).'
        )
        return
      }

      mutation.mutate(formattedData)
    } catch (err: any) {
      notify.error('Lỗi đọc file Excel', err)
    } finally {
      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }
    }
  }

  const triggerFileInput = () => {
    fileInputRef.current?.click()
  }

  return {
    fileInputRef,
    isImporting: mutation.isPending,
    mutation,
    handleDownloadTemplate,
    handleFileChange,
    triggerFileInput,
  }
}
