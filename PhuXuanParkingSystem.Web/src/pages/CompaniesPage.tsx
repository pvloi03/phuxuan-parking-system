import { useState, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Building,
  Search,
  Plus,
  Trash2,
  Edit,
  Eye,
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
} from 'lucide-react'
import { apiClient } from '@/services/apiClient'
import type { PagedResult, Company } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
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

  // Form State
  const [formCode, setFormCode] = useState('')
  const [formName, setFormName] = useState('')
  const [formPhone, setFormPhone] = useState('')
  const [formEmail, setFormEmail] = useState('')
  const [formNote, setFormNote] = useState('')

  // Query Data
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
        code: formCode.trim(),
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
  })

  // Update Mutation
  const updateMutation = useMutation({
    mutationFn: async () => {
      if (!selectedCompany) return
      await apiClient.put(`/companies/${selectedCompany.id}`, {
        code: formCode.trim(),
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
    mutationFn: async (companies: Partial<Company>[]) => {
      await apiClient.post('/companies/batch', companies)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['companies-list'] })
      alert('Nhập danh sách công ty từ Excel thành công!')
    },
    onError: (err: any) => {
      alert('Lỗi nhập Excel: ' + (err?.response?.data?.message || err.message))
    },
  })

  const resetForm = () => {
    setFormCode('')
    setFormName('')
    setFormPhone('')
    setFormEmail('')
    setFormNote('')
    setSelectedCompany(null)
  }

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
    const currentPageIds = items.map((c) => c.id)
    const allSelected = currentPageIds.every((id) => selectedIds.includes(id))

    if (allSelected) {
      setSelectedIds((prev) => prev.filter((id) => !currentPageIds.includes(id)))
    } else {
      setSelectedIds((prev) => Array.from(new Set([...prev, ...currentPageIds])))
    }
  }

  const isAllSelected =
    items.length > 0 && items.every((c) => selectedIds.includes(c.id))

  // Excel Handlers
  const handleExportExcel = () => {
    if (!items.length) {
      alert('Không có dữ liệu để xuất Excel.')
      return
    }

    const exportData = items.map((c, index) => ({
      STT: (pageNumber - 1) * pageSize + index + 1,
      'Mã Công Ty': c.code,
      'Tên Công Ty / Doanh Nghiệp': c.name,
      'Số Điện Thoại': c.phoneNumber || '',
      'Email Liên Hệ': c.email || '',
      'Ghi Chú': c.note || '',
      'Trạng Thái': c.isActive ? 'Đang hoạt động' : 'Tạm dừng',
    }))

    exportToExcel(exportData, `Danh_Sach_Cong_Ty_${new Date().toISOString().slice(0, 10)}.xlsx`, 'CongTy')
  }

  const handleDownloadTemplate = () => {
    const template = [
      {
        'Mã Công Ty': 'CT-001',
        'Tên Công Ty': 'Công Ty Cổ Phần Công Nghệ HP',
        'Số Điện Thoại': '0243123456',
        'Email': 'contact@hptech.vn',
        'Ghi Chú': 'Doanh nghiệp thành viên',
      },
      {
        'Mã Công Ty': 'CT-002',
        'Tên Công Ty': 'Tập Đoàn Đầu Tư Phú Xuân',
        'Số Điện Thoại': '0289876543',
        'Email': 'info@phuxuancorp.vn',
        'Ghi Chú': 'Chủ đầu tư dự án',
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
        code: String(row['Mã Công Ty'] || row['code'] || '').trim(),
        name: String(row['Tên Công Ty'] || row['Tên Công Ty / Doanh Nghiệp'] || row['name'] || '').trim(),
        phoneNumber: String(row['Số Điện Thoại'] || row['phone'] || '').trim() || undefined,
        email: String(row['Email'] || row['Email Liên Hệ'] || row['email'] || '').trim() || undefined,
        note: String(row['Ghi Chú'] || row['note'] || '').trim() || undefined,
        isActive: true,
      })).filter((c) => c.name)

      if (formattedData.length === 0) {
        alert('Không tìm thấy bản ghi công ty hợp lệ (Cần có cột Tên Công Ty).')
        return
      }

      if (confirm(`Đã đọc ${formattedData.length} công ty từ file Excel. Bạn có muốn lưu vào hệ thống?`)) {
        batchImportMutation.mutate(formattedData)
      }
    } catch (err: any) {
      alert('Lỗi đọc file Excel: ' + err.message)
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  return (
    <div className="space-y-6">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
            Quản Lý Công Ty & Doanh Nghiệp
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
            Danh mục công ty thành viên, đơn vị chủ quản và doanh nghiệp thuê trụ sở
          </p>
        </div>

        {/* Action Buttons */}
        <div className="flex flex-wrap items-center gap-2">
          {selectedIds.length > 0 && (
            <Button
              size="sm"
              variant="destructive"
              onClick={() => {
                if (confirm(`Bạn có chắc chắn muốn xóa ${selectedIds.length} công ty đã chọn?`)) {
                  batchDeleteMutation.mutate(selectedIds)
                }
              }}
              disabled={batchDeleteMutation.isPending}
              className="gap-1.5 text-xs font-semibold shadow-xs cursor-pointer"
            >
              <Trash2 className="h-4 w-4" />
              Xóa {selectedIds.length} Đã Chọn
            </Button>
          )}

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
            className="gap-1.5 text-xs font-medium text-slate-700 dark:text-slate-300 border-slate-200 dark:border-slate-700 cursor-pointer shadow-xs"
          >
            <Upload className="h-3.5 w-3.5 text-emerald-600" />
            Nhập Excel
          </Button>

          {/* Download Template */}
          <Button
            variant="outline"
            size="sm"
            onClick={handleDownloadTemplate}
            className="gap-1.5 text-xs font-medium text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-700 cursor-pointer shadow-xs"
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
            className="gap-1.5 text-xs font-medium text-slate-700 dark:text-slate-300 border-slate-200 dark:border-slate-700 cursor-pointer shadow-xs"
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
            className="gap-2 text-xs font-semibold bg-blue-600 hover:bg-blue-700 text-white cursor-pointer shadow-xs"
          >
            <Plus className="h-4 w-4" />
            Thêm Công Ty Mới
          </Button>
        </div>
      </div>

      {/* Filter & Search Bar */}
      <Card className="shadow-xs border-slate-200 dark:border-slate-800">
        <CardContent className="p-4 flex flex-col md:flex-row items-center justify-between gap-3">
          <div className="relative flex-1 max-w-sm w-full">
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
                <th className="p-3.5">Mã Công Ty</th>
                <th className="p-3.5">Tên Công Ty / Doanh Nghiệp</th>
                <th className="p-3.5">Thông Tin Liên Hệ</th>
                <th className="p-3.5">Ghi Chú</th>
                <th className="p-3.5 text-right pr-4">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {isLoading ? (
                <tr>
                  <td colSpan={6} className="p-8 text-center text-slate-400">
                    Đang tải danh sách công ty...
                  </td>
                </tr>
              ) : items.length > 0 ? (
                items.map((company) => {
                  const isSelected = selectedIds.includes(company.id)
                  return (
                    <tr
                      key={company.id}
                      className={`hover:bg-slate-50 dark:hover:bg-slate-800/40 transition-colors ${
                        isSelected ? 'bg-blue-50/50 dark:bg-blue-950/20' : ''
                      }`}
                    >
                      <td className="p-3.5 pl-4">
                        <input
                          type="checkbox"
                          checked={isSelected}
                          onChange={() => handleToggleSelect(company.id)}
                          className="rounded border-slate-300 dark:border-slate-700 text-blue-600 focus:ring-blue-500 cursor-pointer"
                        />
                      </td>
                      <td className="p-3.5 font-mono font-semibold text-slate-800 dark:text-slate-200">
                        {company.code || '--'}
                      </td>
                      <td className="p-3.5 font-bold text-slate-900 dark:text-slate-100">
                        {company.name}
                      </td>
                      <td className="p-3.5 text-slate-500 space-y-0.5">
                        {company.phoneNumber && (
                          <div className="flex items-center gap-1">
                            <Phone className="h-3 w-3 text-slate-400" /> {company.phoneNumber}
                          </div>
                        )}
                        {company.email && (
                          <div className="flex items-center gap-1">
                            <Mail className="h-3 w-3 text-slate-400" /> {company.email}
                          </div>
                        )}
                        {!company.phoneNumber && !company.email && <span>--</span>}
                      </td>
                      <td className="p-3.5 text-slate-600 dark:text-slate-400 italic">
                        {company.note || '--'}
                      </td>
                      <td className="p-3.5 text-right pr-4">
                        <div className="flex items-center justify-end gap-1.5">
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => {
                              setSelectedCompany(company)
                              setIsDetailOpen(true)
                            }}
                            className="h-7 px-2 text-xs text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300 border-blue-200 dark:border-blue-900/60 bg-blue-50/50 dark:bg-blue-950/40 cursor-pointer"
                            title="Xem chi tiết"
                          >
                            <Eye className="h-3.5 w-3.5 mr-1" />
                            Chi tiết
                          </Button>

                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => openEditModal(company)}
                            className="h-7 w-7 p-0 text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-slate-100 cursor-pointer"
                            title="Chỉnh sửa"
                          >
                            <Edit className="h-3.5 w-3.5" />
                          </Button>

                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() => {
                              if (confirm(`Bạn có chắc muốn xóa công ty [${company.name}]?`)) {
                                deleteMutation.mutate(company.id)
                              }
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
                  <td colSpan={6} className="p-8 text-center text-slate-400 italic">
                    Chưa có công ty nào trong danh sách
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
              công ty
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

      {/* MODAL CHI TIẾT CÔNG TY */}
      <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
        <DialogContent className="max-w-md bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Building className="h-5 w-5 text-blue-600" />
              Chi Tiết Công Ty / Doanh Nghiệp
            </DialogTitle>
          </DialogHeader>
          {selectedCompany && (
            <div className="space-y-3 py-2 text-xs">
              <div className="p-3 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2">
                <div className="grid grid-cols-2 gap-2">
                  <div>
                    <span className="text-slate-400 block text-[11px]">Mã công ty:</span>
                    <span className="font-mono font-bold text-slate-900 dark:text-slate-100 text-sm">
                      {selectedCompany.code || '--'}
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[11px]">Tên công ty:</span>
                    <span className="font-bold text-slate-900 dark:text-slate-100 text-sm">
                      {selectedCompany.name}
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[11px]">Số điện thoại:</span>
                    <span className="font-medium text-slate-800 dark:text-slate-200">
                      {selectedCompany.phoneNumber || 'Chưa cập nhật'}
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[11px]">Email liên hệ:</span>
                    <span className="font-medium text-slate-800 dark:text-slate-200">
                      {selectedCompany.email || 'Chưa cập nhật'}
                    </span>
                  </div>
                  <div className="col-span-2">
                    <span className="text-slate-400 block text-[11px]">Ghi chú:</span>
                    <span className="text-slate-700 dark:text-slate-300 italic">
                      {selectedCompany.note || 'Không có ghi chú'}
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

      {/* MODAL THÊM CÔNG TY MỚI */}
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="max-w-md bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Building className="h-5 w-5 text-blue-600" />
              Thêm Công Ty Mới
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-3 py-2 text-xs">
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Mã công ty / Mã định danh
              </label>
              <Input
                placeholder="VD: CT-001"
                value={formCode}
                onChange={(e) => setFormCode(e.target.value)}
                className="text-xs"
              />
            </div>
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Tên công ty / Doanh nghiệp *
              </label>
              <Input
                placeholder="VD: Công Ty Cổ Phần Công Nghệ HP"
                value={formName}
                onChange={(e) => setFormName(e.target.value)}
                className="text-xs"
              />
            </div>
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Số điện thoại liên hệ
              </label>
              <Input
                placeholder="VD: 0243123456"
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
                placeholder="VD: contact@hptech.vn"
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
                placeholder="VD: Doanh nghiệp thuê tại Tòa A"
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
              {createMutation.isPending ? 'Đang lưu...' : 'Lưu Công Ty'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* MODAL CHỈNH SỬA CÔNG TY */}
      <Dialog open={isEditOpen} onOpenChange={setIsEditOpen}>
        <DialogContent className="max-w-md bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Edit className="h-5 w-5 text-blue-600" />
              Chỉnh Sửa Thông Tin Công Ty
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-3 py-2 text-xs">
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Mã công ty
              </label>
              <Input
                value={formCode}
                onChange={(e) => setFormCode(e.target.value)}
                className="text-xs"
              />
            </div>
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Tên công ty *
              </label>
              <Input
                value={formName}
                onChange={(e) => setFormName(e.target.value)}
                className="text-xs"
              />
            </div>
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Số điện thoại liên hệ
              </label>
              <Input
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
    </div>
  )
}
