import { useState, useMemo, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Building2,
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
  Info,
  CheckCircle2,
  XCircle,
  FileBadge,
} from 'lucide-react'
import { apiClient } from '@/services/apiClient'
import type { PagedResult, Department, Company } from '@/types'
import { notify } from '@/lib/notify'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'
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
  const [deleteConfirm, setDeleteConfirm] = useState<{
    isOpen: boolean
    id?: string
    name?: string
    isBatch?: boolean
  }>({ isOpen: false })

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
        code: formCode.trim().toUpperCase(),
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
      notify.success('Thêm mới phòng ban thành công!')
    },
    onError: (err: any) => {
      notify.error('Thêm mới phòng ban thất bại', err)
    },
  })

  // Update Mutation
  const updateMutation = useMutation({
    mutationFn: async () => {
      if (!selectedDept) return
      await apiClient.put(`/departments/${selectedDept.id}`, {
        code: formCode.trim().toUpperCase(),
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
      notify.success('Cập nhật thông tin phòng ban thành công!')
    },
    onError: (err: any) => {
      notify.error('Cập nhật phòng ban thất bại', err)
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
      notify.success('Đã chuyển phòng ban vào thùng rác.')
    },
    onError: (err: any) => {
      notify.error('Xóa phòng ban thất bại', err)
    },
  })

  // Batch Delete Mutation
  const batchDeleteMutation = useMutation({
    mutationFn: async (ids: string[]) => {
      await apiClient.post('/departments/delete-batch', ids)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['departments-list'] })
      notify.success(`Đã xóa ${selectedIds.length} phòng ban thành công.`)
      setSelectedIds([])
    },
    onError: (err: any) => {
      notify.error('Xóa nhiều phòng ban thất bại', err)
    },
  })

  // Batch Import Mutation - Gọi đúng endpoint /api/departments/batch
  const batchImportMutation = useMutation({
    mutationFn: async (deptList: Partial<Department>[]) => {
      await apiClient.post('/departments/batch', deptList)
    },
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['departments-list'] })
      notify.success(`Nhập thành công ${variables?.length || 0} phòng ban từ file Excel!`)
    },
    onError: (err: any) => {
      notify.error('Nhập file Excel thất bại', err)
    },
  })

  // Reset form
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

  // Open Edit
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
      notify.warning('Không có dữ liệu phòng ban để xuất Excel.')
      return
    }

    const exportData = items.map((d, index) => ({
      STT: (pageNumber - 1) * pageSize + index + 1,
      'Mã Phòng Ban': d.code || '',
      'Tên Phòng Ban': d.name,
      'Công Ty Trực Thuộc': d.companyId ? companyMap.get(d.companyId) || 'Chưa xác định' : 'Trực thuộc hệ thống',
      'Trưởng Phòng': d.managerName || '',
      'Số Điện Thoại': d.phoneNumber || '',
      'Email': d.email || '',
      'Ghi Chú': d.note || '',
      'Trạng Thái': d.isActive ? 'Đang hoạt động' : 'Tạm dừng',
    }))

    exportToExcel(exportData, `Danh_Sach_Phong_Ban_${new Date().toISOString().slice(0, 10)}.xlsx`, 'PhongBan')
  }

  const handleDownloadTemplate = () => {
    const template = [
      {
        'Mã Phòng Ban': 'PB001',
        'Tên Phòng Ban': 'Phòng Ban Mẫu A',
        'Trưởng Phòng': 'Nguyễn Văn A',
        'Số Điện Thoại': '0901234567',
        'Email': 'phongban.a@example.com',
        'Ghi Chú': 'Mô tả nhiệm vụ phòng ban 1',
      },
      {
        'Mã Phòng Ban': 'PB002',
        'Tên Phòng Ban': 'Phòng Ban Mẫu B',
        'Trưởng Phòng': 'Trần Thị B',
        'Số Điện Thoại': '0912345678',
        'Email': 'phongban.b@example.com',
        'Ghi Chú': 'Mô tả nhiệm vụ phòng ban 2',
      },
    ]
    downloadExcelTemplate(template, 'Mau_Nhap_Phong_Ban.xlsx', 'MauNhapPhongBan')
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

      const formattedData: Partial<Department>[] = rawData.map((row) => ({
        code: String(row['Mã Phòng Ban'] || row['code'] || '').trim().toUpperCase(),
        name: String(row['Tên Phòng Ban'] || row['name'] || '').trim(),
        managerName: String(row['Trưởng Phòng'] || row['manager'] || '').trim() || undefined,
        phoneNumber: String(row['Số Điện Thoại'] || row['phone'] || '').trim() || undefined,
        email: String(row['Email'] || row['email'] || '').trim() || undefined,
        note: String(row['Ghi Chú'] || row['note'] || '').trim() || undefined,
        isActive: true,
      })).filter((d) => d.name)

      if (!formattedData.length) {
        notify.warning('Không tìm thấy bản ghi phòng ban hợp lệ trong file Excel.')
        return
      }

      batchImportMutation.mutate(formattedData as Department[])
    } catch (err: any) {
      notify.error('Lỗi đọc file Excel', err)
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  // Render Compact Form Fields
  const renderFormFields = () => (
    <div className="space-y-3.5 py-1 text-xs">
      {/* Khối 1: Thông tin định danh */}
      <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2.5">
        <div className="flex items-center gap-1.5 font-bold text-blue-600 dark:text-blue-400">
          <Building2 className="h-4 w-4" />
          <span>Thông Tin Cơ Bản Phòng Ban</span>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div className="space-y-1">
            <label className="font-semibold text-slate-700 dark:text-slate-300">
              Mã phòng ban
            </label>
            <Input
              placeholder="VD: PB-KT, PB-HC"
              value={formCode}
              onChange={(e) => setFormCode(e.target.value.toUpperCase())}
              className="text-xs font-mono font-bold"
            />
          </div>
          <div className="space-y-1">
            <label className="font-semibold text-slate-700 dark:text-slate-300">
              Công ty trực thuộc
            </label>
            <select
              value={formCompanyId}
              onChange={(e) => setFormCompanyId(e.target.value)}
              className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-2.5 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
            >
              <option value="">-- Trực thuộc hệ thống (Chung) --</option>
              {companiesData?.map((comp) => (
                <option key={comp.id} value={comp.id}>
                  {comp.name}
                </option>
              ))}
            </select>
          </div>
          <div className="sm:col-span-2 space-y-1">
            <label className="font-semibold text-slate-700 dark:text-slate-300">
              Tên phòng ban *
            </label>
            <Input
              placeholder="VD: Phòng Kỹ Thuật & Công Nghệ"
              value={formName}
              onChange={(e) => setFormName(e.target.value)}
              className="text-xs font-medium"
            />
          </div>
        </div>
      </div>

      {/* Khối 2: Thông tin liên hệ */}
      <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2.5">
        <div className="flex items-center gap-1.5 font-bold text-slate-700 dark:text-slate-300">
          <Phone className="h-4 w-4 text-emerald-600" />
          <span>Thông Tin Liên Hệ Phòng Ban</span>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div className="space-y-1">
            <label className="font-medium text-slate-600 dark:text-slate-400 text-[11px]">
              Số điện thoại
            </label>
            <Input
              placeholder="VD: 0901234567"
              value={formPhone}
              onChange={(e) => setFormPhone(e.target.value)}
              className="text-xs"
            />
          </div>
          <div className="space-y-1">
            <label className="font-medium text-slate-600 dark:text-slate-400 text-[11px]">
              Email phòng ban
            </label>
            <Input
              placeholder="VD: kythuat@hptech.vn"
              value={formEmail}
              onChange={(e) => setFormEmail(e.target.value)}
              className="text-xs"
            />
          </div>
        </div>
      </div>

      {/* Khối 3: Ghi chú */}
      <div className="space-y-1">
        <label className="font-semibold text-slate-700 dark:text-slate-300">Ghi chú phòng ban</label>
        <Input
          placeholder="VD: Quản trị hạ tầng bãi đỗ xe và hệ thống CNTT"
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
            Quản Lý Phòng Ban Trực Thuộc
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-1 leading-relaxed">
            Cơ cấu tổ chức các phòng ban, trung tâm và bộ phận nghiệp vụ
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
            <div className="w-60">
              <select
                value={companyIdFilter}
                onChange={(e) => {
                  setCompanyIdFilter(e.target.value)
                  setPageNumber(1)
                }}
                className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
              >
                <option value="">-- Tất cả công ty trực thuộc --</option>
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
                <th className="p-3.5">Mã PB</th>
                <th className="p-3.5">Tên Phòng Ban</th>
                <th className="p-3.5">Liên Hệ</th>
                <th className="p-3.5 text-center">Trạng Thái</th>
                <th className="p-3.5 text-right pr-4">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {isLoading ? (
                <tr>
                  <td colSpan={6} className="p-8 text-center text-slate-400">
                    Đang tải dữ liệu phòng ban...
                  </td>
                </tr>
              ) : items.length > 0 ? (
                items.map((dept) => {
                  const isSelected = selectedIds.includes(dept.id)

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
                      <td className="p-3.5 font-mono font-bold text-slate-900 dark:text-slate-100">
                        {dept.code || '--'}
                      </td>
                      <td className="p-3.5">
                        <div className="font-bold text-slate-900 dark:text-slate-100">
                          {dept.name}
                        </div>
                        {dept.note && (
                          <div className="text-[11px] text-slate-400 line-clamp-1">
                            {dept.note}
                          </div>
                        )}
                      </td>
                      <td className="p-3.5">
                        <div className="space-y-0.5 text-[11px]">
                          {dept.phoneNumber && (
                            <div className="flex items-center gap-1 text-slate-600 dark:text-slate-400">
                              <Phone className="h-3 w-3 text-slate-400" />
                              <span>{dept.phoneNumber}</span>
                            </div>
                          )}
                          {dept.email && (
                            <div className="flex items-center gap-1 text-slate-600 dark:text-slate-400">
                              <Mail className="h-3 w-3 text-slate-400" />
                              <span>{dept.email}</span>
                            </div>
                          )}
                          {!dept.phoneNumber && !dept.email && (
                            <span className="text-slate-400 italic">Chưa có liên hệ</span>
                          )}
                        </div>
                      </td>
                      <td className="p-3.5 text-center">
                        {dept.isActive ? (
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
                              setSelectedDept(dept)
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
                            onClick={() => openEditModal(dept)}
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
                                id: dept.id,
                                name: dept.name,
                                isBatch: false,
                              })
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
                    Không tìm thấy phòng ban nào phù hợp
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
            <span>/ trang • Tổng số <strong>{totalItems}</strong> phòng ban</span>
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
      {/* MODAL CHI TIẾT PHÒNG BAN — CARD THÔNG SỐ CHUYÊN NGHIỆP */}
      {/* ===================================================================== */}
      <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
        <DialogContent className="max-w-xl max-h-[90vh] p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 shadow-2xl">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800 pr-8">
            <DialogTitle className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 text-base">
              <div className="flex items-center gap-2.5">
                <div className="h-9 w-9 rounded-lg bg-blue-100 dark:bg-blue-950/60 text-blue-600 dark:text-blue-400 flex items-center justify-center">
                  <Building2 className="h-5 w-5" />
                </div>
                <div>
                  <span className="font-bold text-slate-900 dark:text-white tracking-wide text-base">
                    {selectedDept?.name || 'Chi Tiết Phòng Ban'}
                  </span>
                  <span className="text-xs text-slate-500 dark:text-slate-400 ml-2 font-mono hidden sm:inline">
                    ({selectedDept?.code || 'Chưa đặt mã'})
                  </span>
                </div>
              </div>

              <div className="flex items-center gap-1.5 mr-2">
                {selectedDept?.isActive ? (
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

          {selectedDept && (
            <div className="flex-1 overflow-y-auto p-5 space-y-3.5 text-xs">
              <div className="flex items-center gap-1.5 font-bold text-slate-800 dark:text-slate-200">
                <Info className="h-4 w-4 text-blue-600" />
                <span>HỒ SƠ THÔNG TIN PHÒNG BAN</span>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                {/* Card 1: Định danh */}
                <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2">
                  <div className="flex items-center gap-1.5 font-bold text-blue-600 dark:text-blue-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                    <FileBadge className="h-4 w-4" />
                    <span>Định Danh Phòng Ban</span>
                  </div>
                  <div className="space-y-1.5 pt-0.5">
                    <div>
                      <span className="text-slate-400 block text-[11px]">Mã phòng ban:</span>
                      <span className="font-mono font-extrabold text-slate-900 dark:text-white text-sm">
                        {selectedDept.code || 'Chưa đặt mã'}
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
                      <span className="font-semibold text-slate-800 dark:text-slate-200">
                        {selectedDept.companyId ? companyMap.get(selectedDept.companyId) || 'Chưa xác định' : 'Trực thuộc hệ thống'}
                      </span>
                    </div>
                  </div>
                </div>

                {/* Card 2: Thông tin liên hệ */}
                <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2">
                  <div className="flex items-center gap-1.5 font-bold text-emerald-600 dark:text-emerald-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                    <Phone className="h-4 w-4" />
                    <span>Thông Tin Liên Hệ</span>
                  </div>
                  <div className="space-y-1.5 pt-0.5">
                    <div>
                      <span className="text-slate-400 block text-[11px]">Số điện thoại:</span>
                      <span className="font-medium text-slate-700 dark:text-slate-300">
                        {selectedDept.phoneNumber || 'Chưa cập nhật'}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Email liên hệ:</span>
                      <span className="font-medium text-slate-700 dark:text-slate-300">
                        {selectedDept.email || 'Chưa cập nhật'}
                      </span>
                    </div>
                  </div>
                </div>

                {/* Card 3: Ghi chú */}
                <div className="col-span-1 md:col-span-2 p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-1.5">
                  <span className="text-slate-400 block text-[11px]">Mô tả & Ghi chú nghiệp vụ:</span>
                  <span className="text-slate-700 dark:text-slate-300 font-medium">
                    {selectedDept.note || 'Không có ghi chú thêm cho phòng ban này.'}
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
                if (selectedDept) {
                  const d = selectedDept
                  setIsDetailOpen(false)
                  openEditModal(d)
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
      {/* MODAL THÊM MỚI PHÒNG BAN — CHUẨN GRID 2 CỘT GỌN GÀNG */}
      {/* ===================================================================== */}
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="max-w-lg sm:max-w-xl p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 shadow-2xl max-h-[90vh]">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800">
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Plus className="h-5 w-5 text-blue-600" />
              Thêm Phòng Ban Mới
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
              {createMutation.isPending ? 'Đang lưu...' : 'Lưu Phòng Ban'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* ===================================================================== */}
      {/* MODAL CHỈNH SỬA PHÒNG BAN */}
      {/* ===================================================================== */}
      <Dialog open={isEditOpen} onOpenChange={setIsEditOpen}>
        <DialogContent className="max-w-lg sm:max-w-xl p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 shadow-2xl max-h-[90vh]">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800">
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Edit className="h-5 w-5 text-blue-600" />
              Chỉnh Sửa Phòng Ban
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
            <span>Đã chọn <strong className="text-blue-600 dark:text-blue-400 font-mono text-sm">{selectedIds.length}</strong> phòng ban</span>
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
        title={deleteConfirm.isBatch ? 'Xác Nhận Xóa Nhiều Phòng Ban' : 'Xác Nhận Xóa Phòng Ban'}
        description={
          deleteConfirm.isBatch ? (
            <span>
              Bạn có chắc chắn muốn xóa{' '}
              <strong className="text-red-600 dark:text-red-400 font-semibold">
                {selectedIds.length} phòng ban
              </strong>{' '}
              đã chọn? Dữ liệu sẽ được lưu trữ trong thùng rác hệ thống.
            </span>
          ) : (
            <span>
              Bạn có chắc chắn muốn xóa phòng ban{' '}
              <strong className="text-slate-900 dark:text-slate-100 font-semibold">
                [{deleteConfirm.name}]
              </strong>
              ? Dữ liệu sẽ được chuyển vào thùng rác.
            </span>
          )
        }
        confirmText={deleteConfirm.isBatch ? `Xóa ${selectedIds.length} Phòng Ban` : 'Xác Nhận Xóa'}
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
