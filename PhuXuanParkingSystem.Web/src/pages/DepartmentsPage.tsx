import { useState, useMemo, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Building2,
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
  Building,
  User,
} from 'lucide-react'
import { apiClient } from '@/services/apiClient'
import type { PagedResult, Department, Company } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { exportToExcel, parseExcelFile, downloadExcelTemplate } from '@/lib/excelHelper'

export function DepartmentsPage() {
  const queryClient = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [search, setSearch] = useState('')
  const [companyIdFilter, setCompanyIdFilter] = useState('')
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [selectedIds, setSelectedIds] = useState<string[]>([])

  // Modal State
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [isEditOpen, setIsEditOpen] = useState(false)
  const [isDetailOpen, setIsDetailOpen] = useState(false)
  const [selectedDept, setSelectedDept] = useState<Department | null>(null)

  // Form State
  const [formCode, setFormCode] = useState('')
  const [formName, setFormName] = useState('')
  const [formCompanyId, setFormCompanyId] = useState('')
  const [formManagerName, setFormManagerName] = useState('')
  const [formPhone, setFormPhone] = useState('')
  const [formEmail, setFormEmail] = useState('')
  const [formNote, setFormNote] = useState('')

  // Query Companies for dropdown & mapping
  const { data: companiesData } = useQuery({
    queryKey: ['companies-all'],
    queryFn: async () => {
      const res = await apiClient.get<{ data: PagedResult<Company> }>('/companies', {
        params: { pageSize: 100 },
      })
      return res.data.data.items || []
    },
  })

  const companyMap = useMemo(() => {
    const map = new Map<string, string>()
    companiesData?.forEach((c) => map.set(c.id, c.name))
    return map
  }, [companiesData])

  // Query Departments
  const { data, isLoading } = useQuery({
    queryKey: ['departments-list', search, companyIdFilter, pageNumber, pageSize],
    queryFn: async () => {
      const res = await apiClient.get<{ data: PagedResult<Department> }>('/departments', {
        params: {
          search: search || undefined,
          companyId: companyIdFilter || undefined,
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
      await apiClient.post('/departments', {
        code: formCode.trim(),
        name: formName.trim(),
        companyId: formCompanyId || undefined,
        managerName: formManagerName.trim() || undefined,
        phoneNumber: formPhone.trim() || undefined,
        email: formEmail.trim() || undefined,
        note: formNote.trim() || undefined,
        isActive: true,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['departments-list'] })
      setIsCreateOpen(false)
      resetForm()
    },
  })

  // Update Mutation
  const updateMutation = useMutation({
    mutationFn: async () => {
      if (!selectedDept) return
      await apiClient.put(`/departments/${selectedDept.id}`, {
        code: formCode.trim(),
        name: formName.trim(),
        companyId: formCompanyId || undefined,
        managerName: formManagerName.trim() || undefined,
        phoneNumber: formPhone.trim() || undefined,
        email: formEmail.trim() || undefined,
        note: formNote.trim() || undefined,
        isActive: selectedDept.isActive,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['departments-list'] })
      setIsEditOpen(false)
      resetForm()
    },
  })

  // Delete Mutation
  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/departments/${id}`)
    },
    onSuccess: (_data, deletedId) => {
      queryClient.invalidateQueries({ queryKey: ['departments-list'] })
      setSelectedIds((prev) => prev.filter((item) => item !== deletedId))
    },
  })

  // Batch Delete Mutation
  const batchDeleteMutation = useMutation({
    mutationFn: async (ids: string[]) => {
      await apiClient.post('/departments/delete-batch', ids)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['departments-list'] })
      setSelectedIds([])
    },
  })

  // Batch Import Mutation
  const batchImportMutation = useMutation({
    mutationFn: async (departments: Partial<Department>[]) => {
      await apiClient.post('/departments/batch', departments)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['departments-list'] })
      alert('Nhập danh sách phòng ban từ Excel thành công!')
    },
    onError: (err: any) => {
      alert('Lỗi nhập Excel: ' + (err?.response?.data?.message || err.message))
    },
  })

  const resetForm = () => {
    setFormCode('')
    setFormName('')
    setFormCompanyId('')
    setFormManagerName('')
    setFormPhone('')
    setFormEmail('')
    setFormNote('')
    setSelectedDept(null)
  }

  const openEditModal = (dept: Department) => {
    setSelectedDept(dept)
    setFormCode(dept.code || '')
    setFormName(dept.name || '')
    setFormCompanyId(dept.companyId || '')
    setFormManagerName(dept.managerName || '')
    setFormPhone(dept.phoneNumber || '')
    setFormEmail(dept.email || '')
    setFormNote(dept.note || '')
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
    const currentPageIds = items.map((d) => d.id)
    const allSelected = currentPageIds.every((id) => selectedIds.includes(id))

    if (allSelected) {
      setSelectedIds((prev) => prev.filter((id) => !currentPageIds.includes(id)))
    } else {
      setSelectedIds((prev) => Array.from(new Set([...prev, ...currentPageIds])))
    }
  }

  const isAllSelected =
    items.length > 0 && items.every((d) => selectedIds.includes(d.id))

  // Excel Handlers
  const handleExportExcel = () => {
    if (!items.length) {
      alert('Không có dữ liệu để xuất Excel.')
      return
    }

    const exportData = items.map((d, index) => ({
      STT: (pageNumber - 1) * pageSize + index + 1,
      'Mã Phòng Ban': d.code,
      'Tên Phòng Ban': d.name,
      'Công Ty Trực Thuộc': (d.companyId && companyMap.get(d.companyId)) || d.companyId || 'Chưa gán',
      'Trưởng Phòng': d.managerName || '',
      'Số Điện Thoại': d.phoneNumber || '',
      'Email Liên Hệ': d.email || '',
      'Ghi Chú': d.note || '',
      'Trạng Thái': d.isActive ? 'Đang hoạt động' : 'Tạm dừng',
    }))

    exportToExcel(exportData, `Danh_Sach_Phong_Ban_${new Date().toISOString().slice(0, 10)}.xlsx`, 'PhongBan')
  }

  const handleDownloadTemplate = () => {
    const template = [
      {
        'Mã Phòng Ban': 'PB-KT',
        'Tên Phòng Ban': 'Phòng Kỹ Thuật & Công Nghệ',
        'Trưởng Phòng': 'Nguyễn Văn A',
        'Số Điện Thoại': '0901234567',
        'Email': 'kythuat@hptech.vn',
        'Ghi Chú': 'Bộ phận kỹ thuật tòa nhà',
      },
      {
        'Mã Phòng Ban': 'PB-HC',
        'Tên Phòng Ban': 'Phòng Hành Chính Nhân Sự',
        'Trưởng Phòng': 'Trần Thị B',
        'Số Điện Thoại': '0912345678',
        'Email': 'hr@hptech.vn',
        'Ghi Chú': 'Quản lý nhân sự',
      },
    ]
    downloadExcelTemplate(template, 'Mau_Nhap_Phong_Ban.xlsx')
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

      const formattedData: Partial<Department>[] = rawData.map((row) => ({
        code: String(row['Mã Phòng Ban'] || row['code'] || '').trim(),
        name: String(row['Tên Phòng Ban'] || row['name'] || '').trim(),
        managerName: String(row['Trưởng Phòng'] || row['manager'] || '').trim() || undefined,
        phoneNumber: String(row['Số Điện Thoại'] || row['phone'] || '').trim() || undefined,
        email: String(row['Email'] || row['email'] || '').trim() || undefined,
        note: String(row['Ghi Chú'] || row['note'] || '').trim() || undefined,
        isActive: true,
      })).filter((d) => d.name)

      if (formattedData.length === 0) {
        alert('Không tìm thấy bản ghi phòng ban hợp lệ (Cần có cột Tên Phòng Ban).')
        return
      }

      if (confirm(`Đã đọc ${formattedData.length} phòng ban từ file Excel. Bạn có muốn lưu vào hệ thống?`)) {
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
            Quản Lý Phòng Ban Trực Thuộc
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
            Cơ cấu tổ chức các phòng ban, trung tâm và bộ phận nghiệp vụ
          </p>
        </div>

        {/* Action Buttons */}
        <div className="flex flex-wrap items-center gap-2">
          {selectedIds.length > 0 && (
            <Button
              size="sm"
              variant="destructive"
              onClick={() => {
                if (confirm(`Bạn có chắc chắn muốn xóa ${selectedIds.length} phòng ban đã chọn?`)) {
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
            Thêm Phòng Ban Mới
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
                placeholder="Tìm theo mã hoặc tên phòng ban..."
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value)
                  setPageNumber(1)
                }}
                className="pl-9 text-xs"
              />
            </div>

            {/* Filter by Company */}
            <div className="w-56">
              <select
                value={companyIdFilter}
                onChange={(e) => {
                  setCompanyIdFilter(e.target.value)
                  setPageNumber(1)
                }}
                className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
              >
                <option value="">-- Tất cả công ty --</option>
                {companiesData?.map((comp) => (
                  <option key={comp.id} value={comp.id}>
                    {comp.name}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {(search || companyIdFilter) && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                setSearch('')
                setCompanyIdFilter('')
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
                <th className="p-3.5">Mã Phòng Ban</th>
                <th className="p-3.5">Tên Phòng Ban</th>
                <th className="p-3.5">Công Ty Trực Thuộc</th>
                <th className="p-3.5">Trưởng Phòng / Liên Hệ</th>
                <th className="p-3.5 text-right pr-4">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {isLoading ? (
                <tr>
                  <td colSpan={6} className="p-8 text-center text-slate-400">
                    Đang tải danh sách phòng ban...
                  </td>
                </tr>
              ) : items.length > 0 ? (
                items.map((dept) => {
                  const isSelected = selectedIds.includes(dept.id)
                  const companyName = (dept.companyId && companyMap.get(dept.companyId)) || 'Chưa gán'

                  return (
                    <tr
                      key={dept.id}
                      className={`hover:bg-slate-50 dark:hover:bg-slate-800/40 transition-colors ${
                        isSelected ? 'bg-blue-50/50 dark:bg-blue-950/20' : ''
                      }`}
                    >
                      <td className="p-3.5 pl-4">
                        <input
                          type="checkbox"
                          checked={isSelected}
                          onChange={() => handleToggleSelect(dept.id)}
                          className="rounded border-slate-300 dark:border-slate-700 text-blue-600 focus:ring-blue-500 cursor-pointer"
                        />
                      </td>
                      <td className="p-3.5 font-mono font-semibold text-slate-800 dark:text-slate-200">
                        {dept.code || '--'}
                      </td>
                      <td className="p-3.5 font-bold text-slate-900 dark:text-slate-100">
                        {dept.name}
                      </td>
                      <td className="p-3.5">
                        <span className="inline-flex items-center gap-1 font-medium text-slate-700 dark:text-slate-300">
                          <Building className="h-3 w-3 text-slate-400" />
                          {companyName}
                        </span>
                      </td>
                      <td className="p-3.5 text-slate-500 space-y-0.5">
                        {dept.managerName && (
                          <div className="flex items-center gap-1 font-medium text-slate-800 dark:text-slate-200">
                            <User className="h-3 w-3 text-slate-400" /> {dept.managerName}
                          </div>
                        )}
                        {dept.phoneNumber && (
                          <div className="flex items-center gap-1">
                            <Phone className="h-3 w-3 text-slate-400" /> {dept.phoneNumber}
                          </div>
                        )}
                        {dept.email && (
                          <div className="flex items-center gap-1">
                            <Mail className="h-3 w-3 text-slate-400" /> {dept.email}
                          </div>
                        )}
                        {!dept.managerName && !dept.phoneNumber && !dept.email && <span>--</span>}
                      </td>
                      <td className="p-3.5 text-right pr-4">
                        <div className="flex items-center justify-end gap-1.5">
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => {
                              setSelectedDept(dept)
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
                            onClick={() => openEditModal(dept)}
                            className="h-7 w-7 p-0 text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-slate-100 cursor-pointer"
                            title="Chỉnh sửa"
                          >
                            <Edit className="h-3.5 w-3.5" />
                          </Button>

                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() => {
                              if (confirm(`Bạn có chắc muốn xóa phòng ban [${dept.name}]?`)) {
                                deleteMutation.mutate(dept.id)
                              }
                            }}
                            className="h-7 w-7 p-0 text-slate-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-950/40 rounded-lg cursor-pointer"
                            title="Xóa phòng ban"
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
                    Chưa có phòng ban nào trong danh sách
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
              phòng ban
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

      {/* MODAL CHI TIẾT PHÒNG BAN */}
      <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
        <DialogContent className="max-w-md bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Building2 className="h-5 w-5 text-blue-600" />
              Chi Tiết Phòng Ban
            </DialogTitle>
          </DialogHeader>
          {selectedDept && (
            <div className="space-y-3 py-2 text-xs">
              <div className="p-3 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2">
                <div className="grid grid-cols-2 gap-2">
                  <div>
                    <span className="text-slate-400 block text-[11px]">Mã phòng ban:</span>
                    <span className="font-mono font-bold text-slate-900 dark:text-slate-100 text-sm">
                      {selectedDept.code || '--'}
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[11px]">Tên phòng ban:</span>
                    <span className="font-bold text-slate-900 dark:text-slate-100 text-sm">
                      {selectedDept.name}
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[11px]">Công ty trực thuộc:</span>
                    <span className="font-medium text-blue-600 dark:text-blue-400">
                      {(selectedDept.companyId && companyMap.get(selectedDept.companyId)) || 'Chưa gán'}
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[11px]">Trưởng phòng:</span>
                    <span className="font-medium text-slate-800 dark:text-slate-200">
                      {selectedDept.managerName || 'Chưa bổ nhiệm'}
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[11px]">Số điện thoại:</span>
                    <span className="font-medium text-slate-800 dark:text-slate-200">
                      {selectedDept.phoneNumber || 'Chưa cập nhật'}
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[11px]">Email liên hệ:</span>
                    <span className="font-medium text-slate-800 dark:text-slate-200">
                      {selectedDept.email || 'Chưa cập nhật'}
                    </span>
                  </div>
                  <div className="col-span-2">
                    <span className="text-slate-400 block text-[11px]">Ghi chú:</span>
                    <span className="text-slate-700 dark:text-slate-300 italic">
                      {selectedDept.note || 'Không có ghi chú'}
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

      {/* MODAL THÊM PHÒNG BAN MỚI */}
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="max-w-md bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Building2 className="h-5 w-5 text-blue-600" />
              Thêm Phòng Ban Mới
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-3 py-2 text-xs">
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Mã phòng ban
              </label>
              <Input
                placeholder="VD: PB-KT"
                value={formCode}
                onChange={(e) => setFormCode(e.target.value)}
                className="text-xs"
              />
            </div>
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Tên phòng ban *
              </label>
              <Input
                placeholder="VD: Phòng Kỹ Thuật & Công Nghệ"
                value={formName}
                onChange={(e) => setFormName(e.target.value)}
                className="text-xs"
              />
            </div>
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Công ty trực thuộc
              </label>
              <select
                value={formCompanyId}
                onChange={(e) => setFormCompanyId(e.target.value)}
                className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
              >
                <option value="">-- Chọn công ty trực thuộc --</option>
                {companiesData?.map((comp) => (
                  <option key={comp.id} value={comp.id}>
                    {comp.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Trưởng phòng / Người phụ trách
              </label>
              <Input
                placeholder="VD: Nguyễn Văn A"
                value={formManagerName}
                onChange={(e) => setFormManagerName(e.target.value)}
                className="text-xs"
              />
            </div>
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Số điện thoại liên hệ
              </label>
              <Input
                placeholder="VD: 0901234567"
                value={formPhone}
                onChange={(e) => setFormPhone(e.target.value)}
                className="text-xs"
              />
            </div>
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Email phòng ban
              </label>
              <Input
                placeholder="VD: kythuat@hptech.vn"
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
                placeholder="VD: Quản trị hệ thống bãi đỗ xe"
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
              {createMutation.isPending ? 'Đang lưu...' : 'Lưu Phòng Ban'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* MODAL CHỈNH SỬA PHÒNG BAN */}
      <Dialog open={isEditOpen} onOpenChange={setIsEditOpen}>
        <DialogContent className="max-w-md bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Edit className="h-5 w-5 text-blue-600" />
              Chỉnh Sửa Phòng Ban
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-3 py-2 text-xs">
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Mã phòng ban
              </label>
              <Input
                value={formCode}
                onChange={(e) => setFormCode(e.target.value)}
                className="text-xs"
              />
            </div>
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Tên phòng ban *
              </label>
              <Input
                value={formName}
                onChange={(e) => setFormName(e.target.value)}
                className="text-xs"
              />
            </div>
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Công ty trực thuộc
              </label>
              <select
                value={formCompanyId}
                onChange={(e) => setFormCompanyId(e.target.value)}
                className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
              >
                <option value="">-- Chọn công ty trực thuộc --</option>
                {companiesData?.map((comp) => (
                  <option key={comp.id} value={comp.id}>
                    {comp.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 dark:text-slate-300">
                Trưởng phòng
              </label>
              <Input
                value={formManagerName}
                onChange={(e) => setFormManagerName(e.target.value)}
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
                Email phòng ban
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
