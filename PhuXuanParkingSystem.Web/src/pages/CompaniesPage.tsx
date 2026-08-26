import { useState, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Building,
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
  FileSpreadsheet,
  CheckCircle2,
  XCircle,
  FileBadge,
  Info,
} from 'lucide-react'
import { apiClient } from '@/services/apiClient'
import type { PagedResult, Company } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'
import { exportToExcel, parseExcelFile, downloadExcelTemplate } from '@/lib/excelHelper'

export function CompaniesPage() {
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
  const [selectedCompany, setSelectedCompany] = useState<Company | null>(null)
  const [deleteConfirm, setDeleteConfirm] = useState<{
    isOpen: boolean
    id?: string
    name?: string
    isBatch?: boolean
  }>({ isOpen: false })

  // Form State
  const [formCode, setFormCode] = useState('')
  const [formName, setFormName] = useState('')
  const [formPhone, setFormPhone] = useState('')
  const [formEmail, setFormEmail] = useState('')
  const [formNote, setFormNote] = useState('')

  // Query Companies
  const { data, isLoading } = useQuery({
    queryKey: ['companies-list', search, pageNumber, pageSize],
    queryFn: async () => {
      const res = await apiClient.get<{ data: PagedResult<Company> }>('/companies', {
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
      await apiClient.post('/companies', {
        code: formCode.trim().toUpperCase(),
        name: formName.trim(),
        phoneNumber: formPhone.trim() || undefined,
        email: formEmail.trim() || undefined,
        note: formNote.trim() || undefined,
        isActive: true,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['companies-list'] })
      setIsCreateOpen(false)
      resetForm()
    },
    onError: (err: any) => {
      alert('Lỗi thêm công ty: ' + (err?.response?.data?.message || err.message))
    },
  })

  // Update Mutation
  const updateMutation = useMutation({
    mutationFn: async () => {
      if (!selectedCompany) return
      await apiClient.put(`/companies/${selectedCompany.id}`, {
        code: formCode.trim().toUpperCase(),
        name: formName.trim(),
        phoneNumber: formPhone.trim() || undefined,
        email: formEmail.trim() || undefined,
        note: formNote.trim() || undefined,
        isActive: selectedCompany.isActive,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['companies-list'] })
      setIsEditOpen(false)
      resetForm()
    },
    onError: (err: any) => {
      alert('Lỗi cập nhật công ty: ' + (err?.response?.data?.message || err.message))
    },
  })

  // Delete Mutation
  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/companies/${id}`)
    },
    onSuccess: (_data, deletedId) => {
      queryClient.invalidateQueries({ queryKey: ['companies-list'] })
      setSelectedIds((prev) => prev.filter((item) => item !== deletedId))
    },
  })

  // Batch Delete Mutation
  const batchDeleteMutation = useMutation({
    mutationFn: async (ids: string[]) => {
      await apiClient.post('/companies/delete-batch', ids)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['companies-list'] })
      setSelectedIds([])
    },
  })

  // Batch Import Mutation
  const batchImportMutation = useMutation({
    mutationFn: async (companyList: Partial<Company>[]) => {
      await apiClient.post('/companies/batch-import', companyList)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['companies-list'] })
      alert('Nhập danh sách công ty thành công!')
    },
    onError: (err: any) => {
      alert('Lỗi nhập Excel: ' + (err?.response?.data?.message || err.message))
    },
  })

  // Reset Form
  const resetForm = () => {
    setFormCode('')
    setFormName('')
    setFormPhone('')
    setFormEmail('')
    setFormNote('')
    setSelectedCompany(null)
  }

  // Open Edit
  const openEditModal = (comp: Company) => {
    setSelectedCompany(comp)
    setFormCode(comp.code || '')
    setFormName(comp.name || '')
    setFormPhone(comp.phoneNumber || '')
    setFormEmail(comp.email || '')
    setFormNote(comp.note || '')
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
    const allIds = items.map((i) => i.id)
    const allSelected = allIds.every((id) => selectedIds.includes(id))

    if (allSelected) {
      setSelectedIds([])
    } else {
      setSelectedIds(allIds)
    }
  }

  const isAllSelected = items.length > 0 && items.every((i) => selectedIds.includes(i.id))

  // Excel Handlers
  const handleExportExcel = () => {
    if (!items.length) {
      alert('Không có dữ liệu công ty để xuất Excel.')
      return
    }

    const exportData = items.map((c, index) => ({
      STT: (pageNumber - 1) * pageSize + index + 1,
      'Mã Công Ty': c.code || '',
      'Tên Công Ty': c.name,
      'Số Điện Thoại': c.phoneNumber || '',
      'Email': c.email || '',
      'Ghi Chú': c.note || '',
      'Trạng Thái': c.isActive ? 'Đang hoạt động' : 'Tạm dừng',
    }))

    exportToExcel(exportData, `Danh_Sach_Cong_Ty_${new Date().toISOString().slice(0, 10)}.xlsx`, 'CongTy')
  }

  const handleDownloadTemplate = () => {
    const template = [
      {
        'Mã Công Ty': 'CT-HP',
        'Tên Công Ty': 'Công Ty Cổ Phần Công Nghệ HP',
        'Số Điện Thoại': '0243123456',
        'Email': 'contact@hptech.vn',
        'Ghi Chú': 'Chủ đầu tư quản lý tòa nhà',
      },
      {
        'Mã Công Ty': 'CT-PX',
        'Tên Công Ty': 'Công Ty TNHH Phú Xuân',
        'Số Điện Thoại': '0243987654',
        'Email': 'info@phuxuan.vn',
        'Ghi Chú': 'Đối tác vận hành hệ thống',
      },
    ]
    downloadExcelTemplate(template, 'Mau_Nhap_Cong_Ty.xlsx')
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

      const formattedData: Partial<Company>[] = rawData.map((row) => ({
        code: String(row['Mã Công Ty'] || row['code'] || '').trim().toUpperCase(),
        name: String(row['Tên Công Ty'] || row['name'] || '').trim(),
        phoneNumber: String(row['Số Điện Thoại'] || row['phone'] || '').trim() || undefined,
        email: String(row['Email'] || row['email'] || '').trim() || undefined,
        note: String(row['Ghi Chú'] || row['note'] || '').trim() || undefined,
        isActive: true,
      })).filter((c) => c.name)

      if (!formattedData.length) { alert('Không tìm thấy bản ghi công ty hợp lệ trong file Excel.'); return }

      batchImportMutation.mutate(formattedData as Company[])
    } catch (err: any) {
      alert('Lỗi đọc file Excel: ' + err.message)
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  // Render Compact Form Fields
  const renderFormFields = () => (
    <div className="space-y-3.5 py-1 text-xs">
      {/* Khối 1: Định danh */}
      <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2.5">
        <div className="flex items-center gap-1.5 font-bold text-blue-600 dark:text-blue-400">
          <Building className="h-4 w-4" />
          <span>Thông Tin Doanh Nghiệp</span>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div className="space-y-1">
            <label className="font-semibold text-slate-700 dark:text-slate-300">
              Mã công ty / Mã số thuế
            </label>
            <Input
              placeholder="VD: CT-001, MST010203"
              value={formCode}
              onChange={(e) => setFormCode(e.target.value.toUpperCase())}
              className="text-xs font-mono font-bold"
            />
          </div>
          <div className="space-y-1">
            <label className="font-semibold text-slate-700 dark:text-slate-300">
              Số điện thoại liên hệ
            </label>
            <Input
              placeholder="VD: 024.3123456"
              value={formPhone}
              onChange={(e) => setFormPhone(e.target.value)}
              className="text-xs"
            />
          </div>
          <div className="sm:col-span-2 space-y-1">
            <label className="font-semibold text-slate-700 dark:text-slate-300">
              Tên công ty / Doanh nghiệp *
            </label>
            <Input
              placeholder="VD: Công Ty Cổ Phần Công Nghệ & Thương Mại ABC"
              value={formName}
              onChange={(e) => setFormName(e.target.value)}
              className="text-xs font-medium"
            />
          </div>
        </div>
      </div>

      {/* Khối 2: Liên hệ & Ghi chú */}
      <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2.5">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div className="space-y-1">
            <label className="font-medium text-slate-600 dark:text-slate-400 text-[11px]">
              Email công ty
            </label>
            <Input
              placeholder="VD: info@company.vn"
              value={formEmail}
              onChange={(e) => setFormEmail(e.target.value)}
              className="text-xs"
            />
          </div>
          <div className="space-y-1">
            <label className="font-medium text-slate-600 dark:text-slate-400 text-[11px]">
              Ghi chú thêm
            </label>
            <Input
              placeholder="VD: Đối tác thuê văn phòng tầng 5"
              value={formNote}
              onChange={(e) => setFormNote(e.target.value)}
              className="text-xs"
            />
          </div>
        </div>
      </div>
    </div>
  )

  return (
    <div className="space-y-6">
      {/* Header Bar */}
      <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
        <div className="max-w-2xl min-w-0">
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
            Quản Lý Công Ty / Doanh Nghiệp
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-1 leading-relaxed">
            Danh mục các công ty, doanh nghiệp và đối tác thuê mặt bằng trong hệ thống
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
            Thêm Công Ty Mới
          </Button>
        </div>
      </div>

      {/* Filter & Search Bar */}
      <Card className="shadow-xs border-slate-200 dark:border-slate-800">
        <CardContent className="p-4 flex flex-col md:flex-row items-center justify-between gap-3">
          <div className="flex flex-1 items-center gap-3 w-full">
            <div className="relative flex-1 max-w-sm">
              <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
              <Input
                placeholder="Tìm theo mã hoặc tên công ty..."
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value)
                  setPageNumber(1)
                }}
                className="pl-9 text-xs"
              />
            </div>
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
              Đặt lại bộ lọc
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
                <th className="p-3.5">Mã Công Ty</th>
                <th className="p-3.5">Tên Doanh Nghiệp</th>
                <th className="p-3.5">Số Điện Thoại</th>
                <th className="p-3.5">Email</th>
                <th className="p-3.5">Ghi Chú</th>
                <th className="p-3.5 text-center">Trạng Thái</th>
                <th className="p-3.5 text-right pr-4">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {isLoading ? (
                <tr>
                  <td colSpan={8} className="p-8 text-center text-slate-400">
                    Đang tải danh sách công ty...
                  </td>
                </tr>
              ) : items.length > 0 ? (
                items.map((comp) => {
                  const isSelected = selectedIds.includes(comp.id)

                  return (
                    <tr
                      key={comp.id}
                      className={`hover:bg-slate-50 dark:hover:bg-slate-800/40 transition-colors ${
                        isSelected ? 'bg-blue-50/50 dark:bg-blue-950/20' : ''
                      }`}
                    >
                      <td className="p-3.5 pl-4">
                        <input
                          type="checkbox"
                          checked={isSelected}
                          onChange={() => handleToggleSelect(comp.id)}
                          className="rounded border-slate-300 dark:border-slate-700 text-blue-600 focus:ring-blue-500 cursor-pointer"
                        />
                      </td>
                      <td className="p-3.5 font-mono font-bold text-slate-900 dark:text-slate-100">
                        {comp.code || '--'}
                      </td>
                      <td className="p-3.5">
                        <div className="font-bold text-slate-900 dark:text-slate-100">
                          {comp.name}
                        </div>
                      </td>
                      <td className="p-3.5 text-slate-700 dark:text-slate-300">
                        {comp.phoneNumber ? (
                          <div className="flex items-center gap-1">
                            <Phone className="h-3 w-3 text-slate-400" />
                            <span>{comp.phoneNumber}</span>
                          </div>
                        ) : (
                          <span className="text-slate-400 italic text-[11px]">--</span>
                        )}
                      </td>
                      <td className="p-3.5 text-slate-700 dark:text-slate-300">
                        {comp.email ? (
                          <div className="flex items-center gap-1">
                            <Mail className="h-3 w-3 text-slate-400" />
                            <span>{comp.email}</span>
                          </div>
                        ) : (
                          <span className="text-slate-400 italic text-[11px]">--</span>
                        )}
                      </td>
                      <td className="p-3.5 text-slate-600 dark:text-slate-400">
                        {comp.note ? (
                          <span className="text-xs line-clamp-1" title={comp.note}>
                            {comp.note}
                          </span>
                        ) : (
                          <span className="text-slate-400 italic text-[11px]">--</span>
                        )}
                      </td>
                      <td className="p-3.5 text-center">
                        {comp.isActive ? (
                          <span className="inline-flex items-center gap-1 text-emerald-600 dark:text-emerald-400 font-medium text-[11px]">
                            <CheckCircle2 className="h-3.5 w-3.5" /> Hoạt động
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1 text-slate-400 dark:text-slate-500 font-medium text-[11px]">
                            <XCircle className="h-3.5 w-3.5" /> Tạm dừng
                          </span>
                        )}
                      </td>
                      <td className="p-3.5 text-right pr-4">
                        <div className="flex items-center justify-end gap-1.5">
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => {
                              setSelectedCompany(comp)
                              setIsDetailOpen(true)
                            }}
                            className="h-7 px-2.5 text-blue-600 hover:text-blue-700 border-blue-200 hover:bg-blue-50 dark:text-blue-400 dark:border-blue-900/60 dark:hover:bg-blue-950/50 text-[11px] font-semibold cursor-pointer shadow-2xs"
                            title="Xem Bảng Chi Tiết"
                          >
                            <FileText className="h-3.5 w-3.5 mr-1 text-blue-500" />
                            Chi tiết
                          </Button>

                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => openEditModal(comp)}
                            className="h-7 w-7 p-0 text-slate-600 hover:text-slate-900 dark:text-slate-400 cursor-pointer"
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
                                id: comp.id,
                                name: comp.name,
                                isBatch: false,
                              })
                            }}
                            className="h-7 w-7 p-0 text-slate-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-950/40 rounded-lg cursor-pointer"
                            title="Xóa công ty"
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
                  <td colSpan={8} className="p-8 text-center text-slate-400 italic">
                    Không tìm thấy công ty nào phù hợp
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination Bar */}
        <div className="p-3 border-t border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900/50 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs">
          <div className="flex items-center gap-2 text-slate-500 dark:text-slate-400">
            <span>Hiển thị</span>
            <select
              value={pageSize}
              onChange={(e) => {
                setPageSize(Number(e.target.value))
                setPageNumber(1)
              }}
              className="h-8 rounded border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 text-xs text-slate-800 dark:text-slate-200 cursor-pointer"
            >
              <option value={10}>10</option>
              <option value={20}>20</option>
              <option value={50}>50</option>
            </select>
            <span>/ trang • Tổng số <strong>{totalItems}</strong> doanh nghiệp</span>
          </div>

          <div className="flex items-center gap-1">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPageNumber(1)}
              disabled={pageNumber === 1}
              className="h-8 w-8 p-0 cursor-pointer"
            >
              <ChevronsLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
              disabled={pageNumber === 1}
              className="h-8 w-8 p-0 cursor-pointer"
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>

            <span className="px-3 text-xs font-semibold text-slate-700 dark:text-slate-300">
              Trang {pageNumber} / {totalPages}
            </span>

            <Button
              variant="outline"
              size="sm"
              onClick={() => setPageNumber((p) => Math.min(totalPages, p + 1))}
              disabled={pageNumber === totalPages}
              className="h-8 w-8 p-0 cursor-pointer"
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPageNumber(totalPages)}
              disabled={pageNumber === totalPages}
              className="h-8 w-8 p-0 cursor-pointer"
            >
              <ChevronsRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </Card>

      {/* ===================================================================== */}
      {/* MODAL CHI TIẾT CÔNG TY — CARD THÔNG SỐ CHUYÊN NGHIỆP */}
      {/* ===================================================================== */}
      <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
        <DialogContent className="max-w-xl max-h-[90vh] p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 shadow-2xl">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800 pr-8">
            <DialogTitle className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 text-base">
              <div className="flex items-center gap-2.5">
                <div className="h-9 w-9 rounded-lg bg-blue-100 dark:bg-blue-950/60 text-blue-600 dark:text-blue-400 flex items-center justify-center">
                  <Building className="h-5 w-5" />
                </div>
                <div>
                  <span className="font-bold text-slate-900 dark:text-white tracking-wide text-base">
                    {selectedCompany?.name || 'Chi Tiết Doanh Nghiệp'}
                  </span>
                  <span className="text-xs text-slate-500 dark:text-slate-400 ml-2 font-mono hidden sm:inline">
                    ({selectedCompany?.code || 'Chưa đặt mã'})
                  </span>
                </div>
              </div>

              <div className="flex items-center gap-1.5 mr-2">
                {selectedCompany?.isActive ? (
                  <Badge variant="outline" className="bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-300 dark:border-emerald-800 text-[11px] px-2 py-0.5 font-medium gap-1">
                    <CheckCircle2 className="h-3 w-3" /> Hoạt động
                  </Badge>
                ) : (
                  <Badge variant="outline" className="bg-slate-100 text-slate-600 border-slate-200 dark:bg-slate-800 dark:text-slate-400 text-[11px] px-2 py-0.5 font-medium gap-1">
                    <XCircle className="h-3 w-3" /> Tạm dừng
                  </Badge>
                )}
              </div>
            </DialogTitle>
          </DialogHeader>

          {selectedCompany && (
            <div className="flex-1 overflow-y-auto p-5 space-y-3.5 text-xs">
              <div className="flex items-center gap-1.5 font-bold text-slate-800 dark:text-slate-200">
                <Info className="h-4 w-4 text-blue-600" />
                <span>HỒ SƠ THÔNG TIN DOANH NGHIỆP</span>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                {/* Card 1: Định danh */}
                <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2">
                  <div className="flex items-center gap-1.5 font-bold text-blue-600 dark:text-blue-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                    <FileBadge className="h-4 w-4" />
                    <span>Định Danh Doanh Nghiệp</span>
                  </div>
                  <div className="space-y-1.5 pt-0.5">
                    <div>
                      <span className="text-slate-400 block text-[11px]">Mã công ty / MST:</span>
                      <span className="font-mono font-extrabold text-slate-900 dark:text-white text-sm">
                        {selectedCompany.code || 'Chưa đặt mã'}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Tên công ty:</span>
                      <span className="font-bold text-slate-900 dark:text-slate-100 text-sm">
                        {selectedCompany.name}
                      </span>
                    </div>
                  </div>
                </div>

                {/* Card 2: Liên hệ */}
                <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2">
                  <div className="flex items-center gap-1.5 font-bold text-emerald-600 dark:text-emerald-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                    <Phone className="h-4 w-4" />
                    <span>Kênh Liên Hệ</span>
                  </div>
                  <div className="space-y-1.5 pt-0.5">
                    <div>
                      <span className="text-slate-400 block text-[11px]">Số điện thoại:</span>
                      <span className="font-medium text-slate-700 dark:text-slate-300">
                        {selectedCompany.phoneNumber || 'Chưa cập nhật'}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Email liên hệ:</span>
                      <span className="font-medium text-slate-700 dark:text-slate-300">
                        {selectedCompany.email || 'Chưa cập nhật'}
                      </span>
                    </div>
                  </div>
                </div>

                {/* Card 3: Ghi chú */}
                <div className="col-span-1 md:col-span-2 p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-1.5">
                  <span className="text-slate-400 block text-[11px]">Mô tả & Ghi chú nghiệp vụ:</span>
                  <span className="text-slate-700 dark:text-slate-300 font-medium">
                    {selectedCompany.note || 'Không có ghi chú thêm cho công ty này.'}
                  </span>
                </div>
              </div>
            </div>
          )}

          <DialogFooter className="p-4 pt-3 border-t border-slate-200 dark:border-slate-800 gap-2 bg-slate-50/50 dark:bg-slate-900/50">
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                if (selectedCompany) {
                  const c = selectedCompany
                  setIsDetailOpen(false)
                  openEditModal(c)
                }
              }}
              className="text-xs cursor-pointer gap-1.5 text-slate-700 dark:text-slate-300"
            >
              <Edit className="h-3.5 w-3.5" /> Chỉnh Sửa
            </Button>
            <Button
              size="sm"
              onClick={() => setIsDetailOpen(false)}
              className="bg-blue-600 hover:bg-blue-700 text-white text-xs cursor-pointer"
            >
              Đóng
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* ===================================================================== */}
      {/* MODAL THÊM MỚI CÔNG TY */}
      {/* ===================================================================== */}
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="max-w-lg sm:max-w-xl p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 shadow-2xl max-h-[90vh]">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800">
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Plus className="h-5 w-5 text-blue-600" />
              Thêm Công Ty / Doanh Nghiệp Mới
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
              {createMutation.isPending ? 'Đang lưu...' : 'Lưu Công Ty'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* ===================================================================== */}
      {/* MODAL CHỈNH SỬA CÔNG TY */}
      {/* ===================================================================== */}
      <Dialog open={isEditOpen} onOpenChange={setIsEditOpen}>
        <DialogContent className="max-w-lg sm:max-w-xl p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 shadow-2xl max-h-[90vh]">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800">
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Edit className="h-5 w-5 text-blue-600" />
              Chỉnh Sửa Doanh Nghiệp
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
            <span>Đã chọn <strong className="text-blue-600 dark:text-blue-400 font-mono text-sm">{selectedIds.length}</strong> công ty</span>
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
        title={deleteConfirm.isBatch ? 'Xác Nhận Xóa Nhiều Công Ty' : 'Xác Nhận Xóa Công Ty'}
        description={
          deleteConfirm.isBatch ? (
            <span>
              Bạn có chắc chắn muốn xóa{' '}
              <strong className="text-red-600 dark:text-red-400 font-semibold">
                {selectedIds.length} công ty / doanh nghiệp
              </strong>{' '}
              đã chọn? Dữ liệu sẽ được lưu trữ trong thùng rác hệ thống.
            </span>
          ) : (
            <span>
              Bạn có chắc chắn muốn xóa công ty{' '}
              <strong className="text-slate-900 dark:text-slate-100 font-semibold">
                [{deleteConfirm.name}]
              </strong>
              ? Dữ liệu sẽ được chuyển vào thùng rác.
            </span>
          )
        }
        confirmText={deleteConfirm.isBatch ? `Xóa ${selectedIds.length} Công Ty` : 'Xác Nhận Xóa'}
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
