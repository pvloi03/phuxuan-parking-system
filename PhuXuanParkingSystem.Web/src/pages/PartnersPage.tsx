import { useState, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Handshake,
  Search,
  Plus,
  Trash2,
  Edit,
  FileText,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  RefreshCw,
  Download,
  Upload,
  Phone,
  Mail,
  User,
  FileSpreadsheet,
} from 'lucide-react'
import { apiClient } from '@/services/apiClient'
import type { PagedResult, Contractor } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'
import { exportToExcel, parseExcelFile, downloadExcelTemplate } from '@/lib/excelHelper'

export function PartnersPage() {
  const queryClient = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [search, setSearch] = useState('')
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [selectedIds, setSelectedIds] = useState<string[]>([])

  // Modal State
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [isEditOpen, setIsEditOpen] = useState(false)
  const [isDetailOpen, setIsDetailOpen] = useState(false)
  const [selectedPartner, setSelectedPartner] = useState<Contractor | null>(null)
  const [deleteConfirm, setDeleteConfirm] = useState<{
    isOpen: boolean
    id?: string
    name?: string
    isBatch?: boolean
  }>({ isOpen: false })

  // Form State
  const [formCode, setFormCode] = useState('')
  const [formName, setFormName] = useState('')
  const [formContactPerson, setFormContactPerson] = useState('')
  const [formPhone, setFormPhone] = useState('')
  const [formEmail, setFormEmail] = useState('')
  const [formNote, setFormNote] = useState('')

  // Query Data
  const { data, isLoading } = useQuery({
    queryKey: ['partners-list', search, pageNumber, pageSize],
    queryFn: async () => {
      const res = await apiClient.get<{ data: PagedResult<Contractor> }>('/contractors', {
        params: {
          search: search || undefined,
          pageNumber,
          pageSize,
        },
      })
      return res.data.data
    },
  })

  const items = data?.items || []
  const totalItems = data?.totalCount || 0
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize))

  // Create Mutation
  const createMutation = useMutation({
    mutationFn: async () => {
      await apiClient.post('/contractors', {
        code: formCode.trim(),
        name: formName.trim(),
        contactPerson: formContactPerson.trim() || undefined,
        phoneNumber: formPhone.trim() || undefined,
        email: formEmail.trim() || undefined,
        note: formNote.trim() || undefined,
        isActive: true,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['partners-list'] })
      setIsCreateOpen(false)
      resetForm()
    },
  })

  // Update Mutation
  const updateMutation = useMutation({
    mutationFn: async () => {
      if (!selectedPartner) return
      await apiClient.put(`/contractors/${selectedPartner.id}`, {
        code: formCode.trim(),
        name: formName.trim(),
        contactPerson: formContactPerson.trim() || undefined,
        phoneNumber: formPhone.trim() || undefined,
        email: formEmail.trim() || undefined,
        note: formNote.trim() || undefined,
        isActive: selectedPartner.isActive,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['partners-list'] })
      setIsEditOpen(false)
      resetForm()
    },
  })

  // Delete Mutation
  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/contractors/${id}`)
    },
    onSuccess: (_data, deletedId) => {
      queryClient.invalidateQueries({ queryKey: ['partners-list'] })
      setSelectedIds((prev) => prev.filter((item) => item !== deletedId))
    },
  })

  // Batch Delete Mutation
  const batchDeleteMutation = useMutation({
    mutationFn: async (ids: string[]) => {
      await apiClient.post('/contractors/delete-batch', ids)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['partners-list'] })
      setSelectedIds([])
    },
  })

  // Batch Import Mutation
  const batchImportMutation = useMutation({
    mutationFn: async (contractors: Partial<Contractor>[]) => {
      await apiClient.post('/contractors/batch', contractors)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['partners-list'] })
      alert('Nhập danh sách đối tác từ Excel thành công!')
    },
    onError: (err: any) => {
      alert('Lỗi nhập Excel: ' + (err?.response?.data?.message || err.message))
    },
  })

  const resetForm = () => {
    setFormCode('')
    setFormName('')
    setFormContactPerson('')
    setFormPhone('')
    setFormEmail('')
    setFormNote('')
    setSelectedPartner(null)
  }

  const openEditModal = (partner: Contractor) => {
    setSelectedPartner(partner)
    setFormCode(partner.code || '')
    setFormName(partner.name || '')
    setFormContactPerson(partner.contactPerson || '')
    setFormPhone(partner.phoneNumber || '')
    setFormEmail(partner.email || '')
    setFormNote(partner.note || '')
    setIsEditOpen(true)
  }

  // Selection handlers
  const handleToggleSelect = (id: string) => {
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id]
    )
  }

  const handleSelectAll = () => {
    if (!items.length) return
    const currentPageIds = items.map((p) => p.id)
    const allSelected = currentPageIds.every((id) => selectedIds.includes(id))

    if (allSelected) {
      setSelectedIds((prev) => prev.filter((id) => !currentPageIds.includes(id)))
    } else {
      setSelectedIds((prev) => Array.from(new Set([...prev, ...currentPageIds])))
    }
  }

  const isAllSelected =
    items.length > 0 && items.every((p) => selectedIds.includes(p.id))

  // Excel Handlers
  const handleExportExcel = () => {
    if (!items.length) {
      alert('Không có dữ liệu để xuất Excel.')
      return
    }

    const exportData = items.map((p, index) => ({
      STT: (pageNumber - 1) * pageSize + index + 1,
      'Mã Đối Tác': p.code,
      'Tên Đối Tác / Nhà Thầu': p.name,
      'Người Đại Diện': p.contactPerson || '',
      'Số Điện Thoại': p.phoneNumber || '',
      'Email Liên Hệ': p.email || '',
      'Ghi Chú': p.note || '',
      'Trạng Thái': p.isActive ? 'Đang hoạt động' : 'Tạm dừng',
    }))

    exportToExcel(exportData, `Danh_Sach_Doi_Tac_${new Date().toISOString().slice(0, 10)}.xlsx`, 'DoiTac')
  }

  const handleDownloadTemplate = () => {
    const template = [
      {
        'Mã Đối Tác': 'DT-001',
        'Tên Đối Tác': 'Công Ty TNHH Xây Dựng & Dịch Vụ Thái Thụy',
        'Người Đại Diện': 'Vũ Đình Em',
        'Số Điện Thoại': '0945678901',
        'Email': 'em.vu@thaithuy.vn',
        'Ghi Chú': 'Nhà thầu vệ sinh công nghiệp',
      },
      {
        'Mã Đối Tác': 'DT-002',
        'Tên Đối Tác': 'Công Ty Cổ Phần Cơ Điện Hoàng Hà',
        'Người Đại Diện': 'Nguyễn Văn Minh',
        'Số Điện Thoại': '0988776655',
        'Email': 'minh.nguyen@hoanghapt.com',
        'Ghi Chú': 'Bảo trì hệ thống PCCC và thang máy',
      },
    ]
    downloadExcelTemplate(template, 'Mau_Nhap_Doi_Tac.xlsx')
  }

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    try {
      const rawData = await parseExcelFile<any>(file)
      if (!rawData || rawData.length === 0) {
        alert('File Excel không có dữ liệu.')
        return
      }

      const formattedData: Partial<Contractor>[] = rawData.map((row) => ({
        code: String(row['Mã Đối Tác'] || row['code'] || '').trim(),
        name: String(row['Tên Đối Tác'] || row['Tên Đối Tác / Nhà Thầu'] || row['name'] || '').trim(),
        contactPerson: String(row['Người Đại Diện'] || row['contactPerson'] || '').trim() || undefined,
        phoneNumber: String(row['Số Điện Thoại'] || row['phone'] || '').trim() || undefined,
        email: String(row['Email'] || row['email'] || '').trim() || undefined,
        note: String(row['Ghi Chú'] || row['note'] || '').trim() || undefined,
        isActive: true,
      })).filter((p) => p.name)

      if (!formattedData.length) { alert('Không tìm thấy bản ghi đối tác hợp lệ trong file Excel.'); return }

      batchImportMutation.mutate(formattedData as Contractor[])
    } catch (err: any) {
      alert('Lỗi đọc file Excel: ' + err.message)
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  const renderFormFields = () => (
    <div className="space-y-3.5 text-xs">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">Mã đối tác</label>
          <Input
            placeholder="Ví dụ: DT-001"
            value={formCode}
            onChange={(e) => setFormCode(e.target.value)}
            className="text-xs"
          />
        </div>
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">Tên đối tác / Nhà thầu *</label>
          <Input
            placeholder="Ví dụ: Công Ty Cơ Điện Hoàng Hà"
            value={formName}
            onChange={(e) => setFormName(e.target.value)}
            className="text-xs"
          />
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">Người đại diện liên hệ</label>
          <Input
            placeholder="Ví dụ: Nguyễn Văn Minh"
            value={formContactPerson}
            onChange={(e) => setFormContactPerson(e.target.value)}
            className="text-xs"
          />
        </div>
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">Số điện thoại</label>
          <Input
            placeholder="Ví dụ: 0988776655"
            value={formPhone}
            onChange={(e) => setFormPhone(e.target.value)}
            className="text-xs"
          />
        </div>
      </div>

      <div className="space-y-1">
        <label className="font-semibold text-slate-700 dark:text-slate-300">Email liên hệ</label>
        <Input
          placeholder="Ví dụ: minh.nguyen@hoanghapt.com"
          value={formEmail}
          onChange={(e) => setFormEmail(e.target.value)}
          className="text-xs"
        />
      </div>

      <div className="space-y-1">
        <label className="font-semibold text-slate-700 dark:text-slate-300">Ghi chú</label>
        <Input
          placeholder="Ví dụ: Đơn vị bảo trì hệ thống thang máy và PCCC"
          value={formNote}
          onChange={(e) => setFormNote(e.target.value)}
          className="text-xs"
        />
      </div>
    </div>
  )

  return (
    <div className="space-y-6">
      {/* Header Bar */}
      <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
        <div className="max-w-2xl min-w-0">
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
            Quản Lý Đối Tác & Nhà Thầu
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-1 leading-relaxed">
            Danh sách đơn vị thi công, nhà thầu bảo trì và đối tác liên kết
          </p>
        </div>

        {/* Action Buttons */}
        <div className="flex items-center gap-2 shrink-0 flex-nowrap">
          {/* Import Excel */}
          <input
            type="file"
            ref={fileInputRef}
            onChange={handleFileChange}
            accept=".xlsx, .xls, .csv"
            className="hidden"
          />
          <Button
            variant="outline"
            size="sm"
            onClick={() => fileInputRef.current?.click()}
            className="gap-1.5 text-xs font-medium text-slate-700 dark:text-slate-300 border-slate-200 dark:border-slate-700 cursor-pointer shadow-xs whitespace-nowrap"
          >
            <Upload className="h-3.5 w-3.5 text-emerald-600" />
            Nhập Excel
          </Button>

          {/* Download Template */}
          <Button
            variant="outline"
            size="sm"
            onClick={handleDownloadTemplate}
            className="gap-1.5 text-xs font-medium text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-700 cursor-pointer shadow-xs whitespace-nowrap"
            title="Tải file Excel mẫu để nhập liệu"
          >
            <FileSpreadsheet className="h-3.5 w-3.5 text-blue-500" />
            Tải Mẫu
          </Button>

          {/* Export Excel */}
          <Button
            variant="outline"
            size="sm"
            onClick={handleExportExcel}
            className="gap-1.5 text-xs font-medium text-slate-700 dark:text-slate-300 border-slate-200 dark:border-slate-700 cursor-pointer shadow-xs whitespace-nowrap"
          >
            <Download className="h-3.5 w-3.5 text-blue-600" />
            Xuất Excel
          </Button>

          {/* Create Button */}
          <Button
            onClick={() => {
              resetForm()
              setIsCreateOpen(true)
            }}
            size="sm"
            className="gap-2 text-xs font-semibold bg-blue-600 hover:bg-blue-700 text-white cursor-pointer shadow-xs whitespace-nowrap"
          >
            <Plus className="h-4 w-4" />
            Thêm Đối Tác Mới
          </Button>
        </div>
      </div>

      {/* Filter & Search Bar */}
      <Card className="shadow-xs border-slate-200 dark:border-slate-800">
        <CardContent className="p-4 flex flex-col md:flex-row items-center justify-between gap-3">
          <div className="relative flex-1 max-w-sm w-full">
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
            <Input
              placeholder="Tìm theo mã, tên đối tác hoặc người đại diện..."
              value={search}
              onChange={(e) => {
                setSearch(e.target.value)
                setPageNumber(1)
              }}
              className="pl-9 text-xs"
            />
          </div>

          {search && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                setSearch('')
                setPageNumber(1)
              }}
              className="text-xs text-slate-600 dark:text-slate-400 gap-1.5 cursor-pointer"
            >
              <RefreshCw className="h-3.5 w-3.5" />
              Đặt lại
            </Button>
          )}
        </CardContent>
      </Card>

      {/* Table Data */}
      <Card className="shadow-xs overflow-hidden border-slate-200 dark:border-slate-800">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-slate-100/80 dark:bg-slate-800/60 text-slate-700 dark:text-slate-300 font-semibold border-b border-slate-200 dark:border-slate-800">
              <tr>
                <th className="p-3.5 pl-4 w-10">
                  <input
                    type="checkbox"
                    checked={isAllSelected}
                    onChange={handleSelectAll}
                    className="rounded border-slate-300 dark:border-slate-700 text-blue-600 focus:ring-blue-500 cursor-pointer"
                  />
                </th>
                <th className="p-3.5">Mã Đối Tác</th>
                <th className="p-3.5">Tên Đối Tác / Nhà Thầu</th>
                <th className="p-3.5">Người Đại Diện</th>
                <th className="p-3.5">Thông Tin Liên Hệ</th>
                <th className="p-3.5 text-right pr-4">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {isLoading ? (
                <tr>
                  <td colSpan={6} className="p-8 text-center text-slate-400">
                    Đang tải danh sách đối tác...
                  </td>
                </tr>
              ) : items.length > 0 ? (
                items.map((partner) => {
                  const isSelected = selectedIds.includes(partner.id)
                  return (
                    <tr
                      key={partner.id}
                      className={`hover:bg-slate-50 dark:hover:bg-slate-800/40 transition-colors ${
                        isSelected ? 'bg-blue-50/50 dark:bg-blue-950/20' : ''
                      }`}
                    >
                      <td className="p-3.5 pl-4">
                        <input
                          type="checkbox"
                          checked={isSelected}
                          onChange={() => handleToggleSelect(partner.id)}
                          className="rounded border-slate-300 dark:border-slate-700 text-blue-600 focus:ring-blue-500 cursor-pointer"
                        />
                      </td>
                      <td className="p-3.5 font-mono font-semibold text-slate-800 dark:text-slate-200">
                        {partner.code || '--'}
                      </td>
                      <td className="p-3.5 font-bold text-slate-900 dark:text-slate-100">
                        {partner.name}
                      </td>
                      <td className="p-3.5">
                        {partner.contactPerson ? (
                          <span className="inline-flex items-center gap-1 font-medium text-slate-800 dark:text-slate-200">
                            <User className="h-3 w-3 text-slate-400" />
                            {partner.contactPerson}
                          </span>
                        ) : (
                          <span className="text-slate-400 italic">Chưa có</span>
                        )}
                      </td>
                      <td className="p-3.5 text-slate-500 space-y-0.5">
                        {partner.phoneNumber && (
                          <div className="flex items-center gap-1">
                            <Phone className="h-3 w-3 text-slate-400" /> {partner.phoneNumber}
                          </div>
                        )}
                        {partner.email && (
                          <div className="flex items-center gap-1">
                            <Mail className="h-3 w-3 text-slate-400" /> {partner.email}
                          </div>
                        )}
                        {!partner.phoneNumber && !partner.email && <span>--</span>}
                      </td>
                      <td className="p-3.5 text-right pr-4">
                        <div className="flex items-center justify-end gap-1.5">
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => {
                              setSelectedPartner(partner)
                              setIsDetailOpen(true)
                            }}
                            className="h-7 px-2.5 text-blue-600 hover:text-blue-700 border-blue-200 hover:bg-blue-50 dark:text-blue-400 dark:border-blue-900/60 dark:hover:bg-blue-950/50 text-[11px] font-semibold cursor-pointer shadow-2xs"
                            title="Xem Chi Tiết Đối Tác"
                          >
                            <FileText className="h-3.5 w-3.5 mr-1 text-blue-500" />
                            Chi tiết
                          </Button>

                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => openEditModal(partner)}
                            className="h-7 w-7 p-0 text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-slate-100 cursor-pointer"
                            title="Chỉnh sửa"
                          >
                            <Edit className="h-3.5 w-3.5" />
                          </Button>

                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() => {
                              setDeleteConfirm({
                                isOpen: true,
                                id: partner.id,
                                name: partner.name,
                                isBatch: false,
                              })
                            }}
                            className="h-7 w-7 p-0 text-slate-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-950/40 rounded-lg cursor-pointer"
                            title="Xóa đối tác"
                          >
                            <Trash2 className="h-3.5 w-3.5" />
                          </Button>
                        </div>
                      </td>
                    </tr>
                  )
                })
              ) : (
                <tr>
                  <td colSpan={6} className="p-8 text-center text-slate-400 italic">
                    Chưa có đối tác nào trong danh sách
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {/* FULL PAGINATION BAR */}
        <div className="p-3.5 border-t border-slate-200 dark:border-slate-800 bg-slate-50/80 dark:bg-slate-900/60 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs">
          {/* Summary & PageSize Selector */}
          <div className="flex items-center gap-3">
            <span className="text-slate-500 dark:text-slate-400">
              Hiển thị{' '}
              <strong className="font-semibold text-slate-800 dark:text-slate-200">
                {totalItems > 0 ? (pageNumber - 1) * pageSize + 1 : 0} -{' '}
                {Math.min(pageNumber * pageSize, totalItems)}
              </strong>{' '}
              trên tổng số{' '}
              <strong className="font-semibold text-slate-800 dark:text-slate-200">
                {totalItems}
              </strong>{' '}
              đối tác
            </span>

            <div className="flex items-center gap-1.5 pl-2 border-l border-slate-200 dark:border-slate-700">
              <span className="text-slate-400 text-[11px]">Dòng/trang:</span>
              <select
                value={pageSize}
                onChange={(e) => {
                  setPageSize(Number(e.target.value))
                  setPageNumber(1)
                }}
                className="h-7 rounded border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
              >
                <option value={5}>5</option>
                <option value={10}>10</option>
                <option value={15}>15</option>
                <option value={25}>25</option>
                <option value={50}>50</option>
              </select>
            </div>
          </div>

          {/* Navigation Buttons */}
          <div className="flex items-center gap-1">
            <Button
              variant="outline"
              size="sm"
              disabled={pageNumber <= 1}
              onClick={() => setPageNumber(1)}
              className="h-7 w-7 p-0 cursor-pointer"
              title="Trang đầu"
            >
              <ChevronsLeft className="h-3.5 w-3.5" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={pageNumber <= 1}
              onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
              className="h-7 w-7 p-0 cursor-pointer"
              title="Trang trước"
            >
              <ChevronLeft className="h-3.5 w-3.5" />
            </Button>

            {Array.from({ length: totalPages }, (_, i) => i + 1)
              .filter((p) => {
                if (totalPages <= 5) return true
                return Math.abs(p - pageNumber) <= 1 || p === 1 || p === totalPages
              })
              .map((p, idx, arr) => {
                const prev = arr[idx - 1]
                const showEllipsis = prev && p - prev > 1
                return (
                  <div key={p} className="flex items-center">
                    {showEllipsis && <span className="px-1 text-slate-400 select-none">...</span>}
                    <Button
                      variant={pageNumber === p ? 'default' : 'outline'}
                      size="sm"
                      onClick={() => setPageNumber(p)}
                      className={`h-7 min-w-[28px] px-2 text-xs cursor-pointer ${
                        pageNumber === p
                          ? 'bg-blue-600 text-white font-bold'
                          : 'text-slate-600 dark:text-slate-400'
                      }`}
                    >
                      {p}
                    </Button>
                  </div>
                )
              })}

            <Button
              variant="outline"
              size="sm"
              disabled={pageNumber >= totalPages}
              onClick={() => setPageNumber((p) => Math.min(totalPages, p + 1))}
              className="h-7 w-7 p-0 cursor-pointer"
              title="Trang sau"
            >
              <ChevronRight className="h-3.5 w-3.5" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={pageNumber >= totalPages}
              onClick={() => setPageNumber(totalPages)}
              className="h-7 w-7 p-0 cursor-pointer"
              title="Trang cuối"
            >
              <ChevronsRight className="h-3.5 w-3.5" />
            </Button>
          </div>
        </div>
      </Card>

      {/* MODAL CHI TIẾT ĐỐI TÁC */}
      <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
        <DialogContent className="max-w-md bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Handshake className="h-5 w-5 text-blue-600" />
              Chi Tiết Đối Tác / Nhà Thầu
            </DialogTitle>
          </DialogHeader>
          {selectedPartner && (
            <div className="space-y-3 py-2 text-xs">
              <div className="p-3 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2">
                <div className="grid grid-cols-2 gap-2">
                  <div>
                    <span className="text-slate-400 block text-[11px]">Mã đối tác:</span>
                    <span className="font-mono font-bold text-slate-900 dark:text-slate-100 text-sm">
                      {selectedPartner.code || '--'}
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[11px]">Tên đối tác:</span>
                    <span className="font-bold text-slate-900 dark:text-slate-100 text-sm">
                      {selectedPartner.name}
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[11px]">Người đại diện:</span>
                    <span className="font-medium text-slate-800 dark:text-slate-200">
                      {selectedPartner.contactPerson || 'Chưa cập nhật'}
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[11px]">Số điện thoại:</span>
                    <span className="font-medium text-slate-800 dark:text-slate-200">
                      {selectedPartner.phoneNumber || 'Chưa cập nhật'}
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[11px]">Email liên hệ:</span>
                    <span className="font-medium text-slate-800 dark:text-slate-200">
                      {selectedPartner.email || 'Chưa cập nhật'}
                    </span>
                  </div>
                  <div className="col-span-2">
                    <span className="text-slate-400 block text-[11px]">Ghi chú:</span>
                    <span className="text-slate-700 dark:text-slate-300 italic">
                      {selectedPartner.note || 'Không có ghi chú'}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          )}
          <DialogFooter>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsDetailOpen(false)}
              className="text-xs cursor-pointer"
            >
              Đóng
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* MODAL THÊM ĐỐI TÁC MỚI */}
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="max-w-md bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Handshake className="h-5 w-5 text-blue-600" />
              Thêm Đối Tác Mới
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-3 py-2 text-xs">
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Mã đối tác / Nhà thầu
              </label>
              <Input
                placeholder="VD: DT-001"
                value={formCode}
                onChange={(e) => setFormCode(e.target.value)}
                className="text-xs"
              />
            </div>
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Tên đối tác / Nhà thầu *
              </label>
              <Input
                placeholder="VD: Công Ty TNHH Xây Dựng Thái Thụy"
                value={formName}
                onChange={(e) => setFormName(e.target.value)}
                className="text-xs"
              />
            </div>
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Người đại diện liên hệ
              </label>
              <Input
                placeholder="VD: Vũ Đình Em"
                value={formContactPerson}
                onChange={(e) => setFormContactPerson(e.target.value)}
                className="text-xs"
              />
            </div>
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Số điện thoại
              </label>
              <Input
                placeholder="VD: 0945678901"
                value={formPhone}
                onChange={(e) => setFormPhone(e.target.value)}
                className="text-xs"
              />
            </div>
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Email liên hệ
              </label>
              <Input
                placeholder="VD: em.vu@thaithuy.vn"
                value={formEmail}
                onChange={(e) => setFormEmail(e.target.value)}
                className="text-xs"
              />
            </div>
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Ghi chú
              </label>
              <Input
                placeholder="VD: Đơn vị thi công cảnh quan"
                value={formNote}
                onChange={(e) => setFormNote(e.target.value)}
                className="text-xs"
              />
            </div>
          </div>
          <DialogFooter className="gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsCreateOpen(false)}
              className="text-xs cursor-pointer"
            >
              Hủy
            </Button>
            <Button
              size="sm"
              disabled={!formName.trim() || createMutation.isPending}
              onClick={() => createMutation.mutate()}
              className="bg-blue-600 hover:bg-blue-700 text-white text-xs cursor-pointer"
            >
              {createMutation.isPending ? 'Đang lưu...' : 'Lưu Đối Tác'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* MODAL THÊM MỚI ĐỐI TÁC */}
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="max-w-lg sm:max-w-xl p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 shadow-2xl max-h-[90vh]">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800">
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Plus className="h-5 w-5 text-blue-600" />
              Thêm Đối Tác / Nhà Thầu Mới
            </DialogTitle>
          </DialogHeader>
          <div className="flex-1 overflow-y-auto p-5">
            {renderFormFields()}
          </div>
          <DialogFooter className="p-4 pt-3 border-t border-slate-200 dark:border-slate-800 gap-2 bg-slate-50/50 dark:bg-slate-900/50">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsCreateOpen(false)}
              className="text-xs cursor-pointer"
            >
              Hủy
            </Button>
            <Button
              size="sm"
              disabled={!formName.trim() || createMutation.isPending}
              onClick={() => createMutation.mutate()}
              className="bg-blue-600 hover:bg-blue-700 text-white text-xs cursor-pointer"
            >
              {createMutation.isPending ? 'Đang lưu...' : 'Lưu Đối Tác'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* MODAL CHỈNH SỬA ĐỐI TÁC */}
      <Dialog open={isEditOpen} onOpenChange={setIsEditOpen}>
        <DialogContent className="max-w-lg sm:max-w-xl p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 shadow-2xl max-h-[90vh]">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800">
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Edit className="h-5 w-5 text-blue-600" />
              Chỉnh Sửa Đối Tác
            </DialogTitle>
          </DialogHeader>
          <div className="flex-1 overflow-y-auto p-5">
            {renderFormFields()}
          </div>
          <DialogFooter className="p-4 pt-3 border-t border-slate-200 dark:border-slate-800 gap-2 bg-slate-50/50 dark:bg-slate-900/50">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsEditOpen(false)}
              className="text-xs cursor-pointer"
            >
              Hủy
            </Button>
            <Button
              size="sm"
              disabled={!formName.trim() || updateMutation.isPending}
              onClick={() => updateMutation.mutate()}
              className="bg-blue-600 hover:bg-blue-700 text-white text-xs cursor-pointer"
            >
              {updateMutation.isPending ? 'Đang cập nhật...' : 'Cập Nhật'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* ===================================================================== */}
      {/* FLOATING BULK ACTION BAR — HIỆN/ẨN PHÍA DƯỚI BÊN PHẢI KHI CHỌN DÒNG */}
      {/* ===================================================================== */}
      {selectedIds.length > 0 && (
        <div className="fixed bottom-6 right-6 z-40 bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 border border-slate-200 dark:border-slate-800 shadow-2xl rounded-2xl px-4 py-2.5 flex items-center gap-3 animate-in slide-in-from-bottom-5 duration-200">
          <div className="flex items-center gap-1.5 text-xs font-semibold text-slate-700 dark:text-slate-300">
            <span className="h-2 w-2 rounded-full bg-blue-600 animate-pulse" />
            <span>Đã chọn <strong className="text-blue-600 dark:text-blue-400 font-mono text-sm">{selectedIds.length}</strong> đối tác</span>
          </div>

          <div className="h-4 w-px bg-slate-200 dark:bg-slate-800" />

          <Button
            variant="ghost"
            size="sm"
            onClick={() => setSelectedIds([])}
            className="h-8 px-2.5 text-xs text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200 cursor-pointer"
          >
            Hủy chọn
          </Button>

          <Button
            size="sm"
            variant="destructive"
            onClick={() => {
              setDeleteConfirm({
                isOpen: true,
                isBatch: true,
              })
            }}
            disabled={batchDeleteMutation.isPending}
            className="h-8 gap-1.5 text-xs font-bold shadow-md cursor-pointer bg-red-600 hover:bg-red-700 text-white"
          >
            <Trash2 className="h-3.5 w-3.5" />
            Xóa {selectedIds.length} Đã Chọn
          </Button>
        </div>
      )}

      {/* CONFIRM DELETE DIALOG HIỆN ĐẠI CHUYÊN NGHIỆP */}
      <ConfirmDialog
        open={deleteConfirm.isOpen}
        onOpenChange={(open) => setDeleteConfirm((prev) => ({ ...prev, isOpen: open }))}
        title={deleteConfirm.isBatch ? 'Xác Nhận Xóa Nhiều Đối Tác' : 'Xác Nhận Xóa Đối Tác'}
        description={
          deleteConfirm.isBatch ? (
            <span>
              Bạn có chắc chắn muốn xóa{' '}
              <strong className="text-red-600 dark:text-red-400 font-semibold">
                {selectedIds.length} đối tác
              </strong>{' '}
              đã chọn? Dữ liệu sẽ được lưu trữ trong thùng rác hệ thống.
            </span>
          ) : (
            <span>
              Bạn có chắc chắn muốn xóa đối tác{' '}
              <strong className="text-slate-900 dark:text-slate-100 font-semibold">
                [{deleteConfirm.name}]
              </strong>
              ? Dữ liệu sẽ được chuyển vào thùng rác.
            </span>
          )
        }
        confirmText={deleteConfirm.isBatch ? `Xóa ${selectedIds.length} Đối Tác` : 'Xác Nhận Xóa'}
        isLoading={deleteMutation.isPending || batchDeleteMutation.isPending}
        onConfirm={() => {
          if (deleteConfirm.isBatch) {
            batchDeleteMutation.mutate(selectedIds, {
              onSettled: () => setDeleteConfirm({ isOpen: false }),
            })
          } else if (deleteConfirm.id) {
            deleteMutation.mutate(deleteConfirm.id, {
              onSettled: () => setDeleteConfirm({ isOpen: false }),
            })
          }
        }}
      />
    </div>
  )
}
