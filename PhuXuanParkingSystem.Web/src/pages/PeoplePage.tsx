import { useState, useMemo, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Search,
  Plus,
  Trash2,
  Edit,
  FileText,
  Mail,
  Phone,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  RefreshCw,
  UserCheck,
  Download,
  Upload,
  FileSpreadsheet,
  Building2,
  HardHat,
  Shield,
  Layers,
  Info,
  CheckCircle2,
  XCircle,
} from 'lucide-react'
import { apiClient } from '@/services/apiClient'
import type { PagedResult, Person, PersonType, Department, Company, Contractor } from '@/types'
import { getPersonTypeLabel } from '@/types'
import { notify } from '@/lib/notify'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'
import { exportToExcel, parseExcelFile, downloadExcelTemplate } from '@/lib/excelHelper'

export function PeoplePage() {
  const queryClient = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [search, setSearch] = useState('')
  const [typeFilter, setTypeFilter] = useState<string>('')
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [selectedIds, setSelectedIds] = useState<string[]>([])

  // Modal states
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [isEditOpen, setIsEditOpen] = useState(false)
  const [isDetailOpen, setIsDetailOpen] = useState(false)
  const [selectedPerson, setSelectedPerson] = useState<Person | null>(null)
  const [deleteConfirm, setDeleteConfirm] = useState<{
    isOpen: boolean
    id?: string
    name?: string
    isBatch?: boolean
  }>({ isOpen: false })

  // Form states
  const [formCode, setFormCode] = useState('')
  const [formName, setFormName] = useState('')
  const [formType, setFormType] = useState<PersonType>('Employee')
  const [formPhone, setFormPhone] = useState('')
  const [formEmail, setFormEmail] = useState('')
  const [formDeptId, setFormDeptId] = useState('')
  const [formCompanyId, setFormCompanyId] = useState('')
  const [formContractorId, setFormContractorId] = useState('')
  const [formIsActive, setFormIsActive] = useState(true)

  // 1. Query Departments, Companies, Contractors for Lookups
  const { data: deptsData } = useQuery({
    queryKey: ['departments-all-lookup'],
    queryFn: async () => {
      const res = await apiClient.get<{ data: PagedResult<Department> }>('/departments', { params: { pageSize: 1000 } })
      return res.data.data?.items || []
    },
    staleTime: 60000,
  })
  const departments = deptsData || []

  const { data: compsData } = useQuery({
    queryKey: ['companies-all-lookup'],
    queryFn: async () => {
      const res = await apiClient.get<{ data: PagedResult<Company> }>('/companies', { params: { pageSize: 1000 } })
      return res.data.data?.items || []
    },
    staleTime: 60000,
  })
  const companies = compsData || []

  const { data: contrsData } = useQuery({
    queryKey: ['contractors-all-lookup'],
    queryFn: async () => {
      const res = await apiClient.get<{ data: PagedResult<Contractor> }>('/contractors', { params: { pageSize: 1000 } })
      return res.data.data?.items || []
    },
    staleTime: 60000,
  })
  const contractors = contrsData || []

  // Memoized maps for instant ID -> Name lookup
  const deptMap = useMemo(() => {
    const map = new Map<string, string>()
    departments.forEach((d) => map.set(d.id, d.name))
    return map
  }, [departments])

  const compMap = useMemo(() => {
    const map = new Map<string, string>()
    companies.forEach((c) => map.set(c.id, c.name))
    return map
  }, [companies])

  const contrMap = useMemo(() => {
    const map = new Map<string, string>()
    contractors.forEach((c) => map.set(c.id, c.name))
    return map
  }, [contractors])

  // 2. Query People List with Pagination & Filters
  const { data, isLoading } = useQuery({
    queryKey: ['people-list', search, typeFilter, pageNumber, pageSize],
    queryFn: async () => {
      const res = await apiClient.get<{ data: PagedResult<Person> }>('/people', {
        params: {
          search: search || undefined,
          type: typeFilter || undefined,
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

  // ======================== Mutations ========================
  const createMutation = useMutation({
    mutationFn: async () => {
      await apiClient.post('/people', {
        code: formCode.trim(),
        fullName: formName.trim(),
        type: formType,
        phoneNumber: formPhone.trim() || undefined,
        email: formEmail.trim() || undefined,
        departmentId: formDeptId || undefined,
        companyId: formCompanyId || undefined,
        contractorId: formContractorId || undefined,
        isActive: formIsActive,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['people-list'] })
      setIsCreateOpen(false)
      resetForm()
      notify.success('Thêm mới nhân sự thành công!')
    },
    onError: (err: any) => {
      notify.error('Thêm mới nhân sự thất bại', err)
    },
  })

  const updateMutation = useMutation({
    mutationFn: async () => {
      if (!selectedPerson) return
      await apiClient.put(`/people/${selectedPerson.id}`, {
        code: formCode.trim(),
        fullName: formName.trim(),
        type: formType,
        phoneNumber: formPhone.trim() || undefined,
        email: formEmail.trim() || undefined,
        departmentId: formDeptId || undefined,
        companyId: formCompanyId || undefined,
        contractorId: formContractorId || undefined,
        isActive: formIsActive,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['people-list'] })
      setIsEditOpen(false)
      resetForm()
      notify.success('Cập nhật thông tin nhân sự thành công!')
    },
    onError: (err: any) => {
      notify.error('Cập nhật nhân sự thất bại', err)
    },
  })

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/people/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['people-list'] })
      notify.success('Đã chuyển nhân sự vào thùng rác.')
    },
    onError: (err: any) => {
      notify.error('Xóa nhân sự thất bại', err)
    },
  })

  const batchDeleteMutation = useMutation({
    mutationFn: async (ids: string[]) => {
      await apiClient.post('/people/delete-batch', ids)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['people-list'] })
      notify.success(`Đã xóa ${selectedIds.length} nhân sự thành công.`)
      setSelectedIds([])
    },
    onError: (err: any) => {
      notify.error('Xóa nhiều nhân sự thất bại', err)
    },
  })

  const batchImportMutation = useMutation({
    mutationFn: async (people: Partial<Person>[]) => {
      await apiClient.post('/people/batch', people)
    },
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['people-list'] })
      notify.success(`Nhập thành công ${variables?.length || 0} nhân sự từ file Excel!`)
    },
    onError: (err: any) => {
      notify.error('Nhập file Excel thất bại', err)
    },
  })

  // Helpers
  const resetForm = () => {
    setFormCode('')
    setFormName('')
    setFormType('Employee')
    setFormPhone('')
    setFormEmail('')
    setFormDeptId('')
    setFormCompanyId('')
    setFormContractorId('')
    setFormIsActive(true)
    setSelectedPerson(null)
  }

  const openEditModal = (person: Person) => {
    setSelectedPerson(person)
    setFormCode(person.code || '')
    setFormName(person.fullName || '')
    setFormType(person.type || 'Employee')
    setFormPhone(person.phoneNumber || '')
    setFormEmail(person.email || '')
    setFormDeptId(person.departmentId || '')
    setFormCompanyId(person.companyId || '')
    setFormContractorId(person.contractorId || '')
    setFormIsActive(person.isActive ?? true)
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

  const isAllSelected = items.length > 0 && items.every((p) => selectedIds.includes(p.id))

  // Excel Handlers
  const handleExportExcel = () => {
    if (!items.length) {
      notify.warning('Không có dữ liệu nhân sự để xuất Excel.')
      return
    }

    const exportData = items.map((p, index) => {
      const deptName = p.departmentId ? deptMap.get(p.departmentId) || '' : ''
      const contrName = p.contractorId ? contrMap.get(p.contractorId) || '' : ''
      const compName = p.companyId ? compMap.get(p.companyId) || '' : ''

      return {
        STT: (pageNumber - 1) * pageSize + index + 1,
        'Mã Định Danh': p.code,
        'Họ Và Tên': p.fullName,
        'Phòng Ban': deptName,
        'Nhà Thầu': contrName,
        'Công Ty': compName,
        'Phân Loại Đối Tượng': getPersonTypeLabel(p.type),
        'Số Điện Thoại': p.phoneNumber || '',
        'Email Liên Hệ': p.email || '',
        'Trạng Thái': p.isActive ? 'Đang hoạt động' : 'Tạm dừng',
      }
    })

    exportToExcel(exportData, `Danh_Sach_Nhan_Su_${new Date().toISOString().slice(0, 10)}.xlsx`, 'NhanSu')
  }

  const handleDownloadTemplate = () => {
    const template = [
      {
        'Mã Định Danh': 'NS001',
        'Họ Và Tên': 'Nguyễn Văn A',
        'Phân Loại (Employee/Contractor/Visitor/VIP)': 'Employee',
        'Số Điện Thoại': '0901234567',
        'Email': 'nhansu.a@example.com',
      },
      {
        'Mã Định Danh': 'NS002',
        'Họ Và Tên': 'Trần Thị B',
        'Phân Loại (Employee/Contractor/Visitor/VIP)': 'Contractor',
        'Số Điện Thoại': '0912345678',
        'Email': 'nhansu.b@example.com',
      },
    ]
    downloadExcelTemplate(template, 'Mau_Nhap_Nhan_Su.xlsx', 'MauNhapNhanSu')
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

      const formattedData: Partial<Person>[] = rawData
        .map((row) => {
          const rawType = String(row['Phân Loại (Employee/Contractor/Visitor/VIP)'] || row['Phân Loại'] || row['type'] || 'Employee').trim()
          let personType: PersonType = 'Employee'
          if (rawType.toLowerCase().includes('contractor') || rawType.toLowerCase().includes('thầu')) personType = 'Contractor'
          else if (rawType.toLowerCase().includes('visitor') || rawType.toLowerCase().includes('khách')) personType = 'Visitor'
          else if (rawType.toLowerCase().includes('vip')) personType = 'VIP'

          return {
            code: String(row['Mã Định Danh'] || row['code'] || '').trim().toUpperCase(),
            fullName: String(row['Họ Và Tên'] || row['fullName'] || '').trim(),
            type: personType,
            phoneNumber: String(row['Số Điện Thoại'] || row['phoneNumber'] || '').trim() || undefined,
            email: String(row['Email'] || row['email'] || '').trim() || undefined,
            isActive: true,
          }
        })
        .filter((p) => p.fullName && p.code)

      if (!formattedData.length) {
        notify.warning('Không tìm thấy bản ghi nhân sự hợp lệ trong file Excel.')
        return
      }

      batchImportMutation.mutate(formattedData as Person[])
    } catch (err: any) {
      notify.error('Lỗi đọc file Excel', err)
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  // Badges
  const getPersonTypeBadge = (type: PersonType) => {
    const label = getPersonTypeLabel(type)
    switch (type) {
      case 'Employee':
        return <Badge variant="outline" className="bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-950/40 dark:text-blue-300 dark:border-blue-800 text-[11px] px-2 py-0.5 font-medium">{label}</Badge>
      case 'Contractor':
        return <Badge variant="outline" className="bg-purple-50 text-purple-700 border-purple-200 dark:bg-purple-950/40 dark:text-purple-300 dark:border-purple-800 text-[11px] px-2 py-0.5 font-medium">{label}</Badge>
      case 'VIP':
        return <Badge variant="outline" className="bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-950/40 dark:text-amber-300 dark:border-amber-800 text-[11px] px-2 py-0.5 font-medium">{label}</Badge>
      case 'Visitor':
        return <Badge variant="outline" className="bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-300 dark:border-emerald-800 text-[11px] px-2 py-0.5 font-medium">{label}</Badge>
      default:
        return <Badge variant="secondary" className="text-[11px] px-2 py-0.5">{label}</Badge>
    }
  }

  // Render Form Fields for Create / Edit
  const renderFormFields = () => (
    <div className="space-y-3 py-2 text-xs">
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">
            Mã định danh / Mã NV *
          </label>
          <Input
            placeholder="VD: NV-001 hoặc NT-001"
            value={formCode}
            onChange={(e) => setFormCode(e.target.value.toUpperCase())}
            className="text-xs font-mono font-bold"
          />
        </div>
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">
            Phân loại đối tượng
          </label>
          <select
            value={formType}
            onChange={(e) => setFormType(e.target.value as PersonType)}
            className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
          >
            <option value="Employee">👔 Cán bộ / Nhân viên</option>
            <option value="Contractor">👷 Đối tác / Nhà thầu</option>
            <option value="Visitor">👥 Khách thăm</option>
            <option value="VIP">⭐ Khách VIP / Ban Lãnh Đạo</option>
          </select>
        </div>
      </div>

      <div className="space-y-1">
        <label className="font-semibold text-slate-700 dark:text-slate-300">
          Họ và tên *
        </label>
        <Input
          placeholder="VD: Nguyễn Văn An"
          value={formName}
          onChange={(e) => setFormName(e.target.value)}
          className="text-xs"
        />
      </div>

      {formType === 'Employee' && (
        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-1">
            <label className="font-semibold text-slate-700 dark:text-slate-300">Phòng ban</label>
            <select
              value={formDeptId}
              onChange={(e) => setFormDeptId(e.target.value)}
              className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
            >
              <option value="">-- Chưa phân phòng ban --</option>
              {departments.map((d) => (
                <option key={d.id} value={d.id}>{d.name} ({d.code})</option>
              ))}
            </select>
          </div>
          <div className="space-y-1">
            <label className="font-semibold text-slate-700 dark:text-slate-300">Công ty trực thuộc</label>
            <select
              value={formCompanyId}
              onChange={(e) => setFormCompanyId(e.target.value)}
              className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
            >
              <option value="">-- Chưa phân công ty --</option>
              {companies.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>
        </div>
      )}

      {formType === 'Contractor' && (
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">Đơn vị Nhà thầu / Đối tác</label>
          <select
            value={formContractorId}
            onChange={(e) => setFormContractorId(e.target.value)}
            className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
          >
            <option value="">-- Chưa chọn nhà thầu --</option>
            {contractors.map((c) => (
              <option key={c.id} value={c.id}>{c.name} ({c.code})</option>
            ))}
          </select>
        </div>
      )}

      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">Số điện thoại</label>
          <Input
            placeholder="VD: 0912345678"
            value={formPhone}
            onChange={(e) => setFormPhone(e.target.value)}
            className="text-xs font-mono"
          />
        </div>
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">Email liên hệ</label>
          <Input
            placeholder="VD: an.nguyen@phuxuan.vn"
            value={formEmail}
            onChange={(e) => setFormEmail(e.target.value)}
            className="text-xs"
          />
        </div>
      </div>

      <div className="flex items-center gap-2 pt-1">
        <input
          type="checkbox"
          id="isPersonActive"
          checked={formIsActive}
          onChange={(e) => setFormIsActive(e.target.checked)}
          className="rounded border-slate-300 text-blue-600 focus:ring-blue-500 cursor-pointer h-3.5 w-3.5"
        />
        <label htmlFor="isPersonActive" className="text-slate-700 dark:text-slate-300 font-medium cursor-pointer">
          Đang hoạt động (Cho phép xác thực & lưu thông tin ra vào)
        </label>
      </div>
    </div>
  )

  return (
    <div className="space-y-6">
      {/* Header Bar */}
      <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
        <div className="max-w-2xl min-w-0">
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
            Quản Lý Nhân Sự
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-1 leading-relaxed">
            Danh sách cán bộ nhân viên, đối tác nhà thầu, khách thăm trong hệ thống
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
            onClick={() => { resetForm(); setIsCreateOpen(true) }}
            size="sm"
            className="gap-2 text-xs font-semibold bg-blue-600 hover:bg-blue-700 text-white cursor-pointer shadow-xs whitespace-nowrap"
          >
            <Plus className="h-4 w-4" />
            Thêm Nhân Sự Mới
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
                placeholder="Tìm theo tên, mã hoặc SĐT..."
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value)
                  setPageNumber(1)
                }}
                className="pl-9 text-xs"
              />
            </div>

            <div className="w-52">
              <select
                value={typeFilter}
                onChange={(e) => {
                  setTypeFilter(e.target.value)
                  setPageNumber(1)
                }}
                className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
              >
                <option value="">-- Tất cả phân loại --</option>
                <option value="Employee">👔 Cán bộ / Nhân viên</option>
                <option value="Contractor">👷 Đối tác / Nhà thầu</option>
                <option value="Visitor">👥 Khách thăm</option>
                <option value="VIP">⭐ Khách VIP / Ban Lãnh Đạo</option>
              </select>
            </div>
          </div>

          {(search || typeFilter) && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                setSearch('')
                setTypeFilter('')
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
                <th className="p-3.5">Mã Định Danh</th>
                <th className="p-3.5">Họ Và Tên</th>
                <th className="p-3.5">Phòng Ban / Nhà Thầu</th>
                <th className="p-3.5">Liên Hệ</th>
                <th className="p-3.5 text-right pr-4">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {isLoading ? (
                <tr>
                  <td colSpan={6} className="p-8 text-center text-slate-400">
                    Đang tải danh sách nhân sự...
                  </td>
                </tr>
              ) : items.length > 0 ? (
                items.map((person) => {
                  const isSelected = selectedIds.includes(person.id)
                  const deptName = person.departmentId ? deptMap.get(person.departmentId) : null
                  const contrName = person.contractorId ? contrMap.get(person.contractorId) : null
                  const compName = person.companyId ? compMap.get(person.companyId) : null

                  return (
                    <tr
                      key={person.id}
                      className={`hover:bg-slate-50 dark:hover:bg-slate-800/40 transition-colors ${
                        isSelected ? 'bg-blue-50/50 dark:bg-blue-950/20' : ''
                      }`}
                    >
                      <td className="p-3.5 pl-4">
                        <input
                          type="checkbox"
                          checked={isSelected}
                          onChange={() => handleToggleSelect(person.id)}
                          className="rounded border-slate-300 dark:border-slate-700 text-blue-600 focus:ring-blue-500 cursor-pointer"
                        />
                      </td>
                      <td className="p-3.5 font-mono font-semibold text-slate-800 dark:text-slate-200">
                        {person.code || '--'}
                      </td>
                      <td className="p-3.5 font-bold text-slate-900 dark:text-slate-100">
                        {person.fullName}
                      </td>
                      <td className="p-3.5">
                        {deptName ? (
                          <div>
                            <div className="flex items-center gap-1.5 font-semibold text-slate-800 dark:text-slate-200">
                              <Building2 className="h-3.5 w-3.5 text-blue-500 flex-shrink-0" />
                              <span>{deptName}</span>
                            </div>
                            {compName && (
                              <span className="text-[10px] text-slate-400 block ml-5">
                                {compName}
                              </span>
                            )}
                          </div>
                        ) : contrName ? (
                          <div className="flex items-center gap-1.5 font-semibold text-amber-700 dark:text-amber-300">
                            <HardHat className="h-3.5 w-3.5 text-amber-500 flex-shrink-0" />
                            <span>{contrName}</span>
                          </div>
                        ) : compName ? (
                          <div className="flex items-center gap-1.5 font-semibold text-slate-700 dark:text-slate-300">
                            <Building2 className="h-3.5 w-3.5 text-slate-400 flex-shrink-0" />
                            <span>{compName}</span>
                          </div>
                        ) : (
                          <span className="text-slate-400 italic text-[11px]">--</span>
                        )}
                      </td>
                      <td className="p-3.5 text-slate-500 space-y-0.5">
                        {person.phoneNumber && (
                          <div className="flex items-center gap-1">
                            <Phone className="h-3 w-3 text-slate-400" /> {person.phoneNumber}
                          </div>
                        )}
                        {person.email && (
                          <div className="flex items-center gap-1">
                            <Mail className="h-3 w-3 text-slate-400" /> {person.email}
                          </div>
                        )}
                        {!person.phoneNumber && !person.email && <span>--</span>}
                      </td>
                      <td className="p-3.5 text-right pr-4">
                        <div className="flex items-center justify-end gap-1.5">
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => {
                              setSelectedPerson(person)
                              setIsDetailOpen(true)
                            }}
                            className="h-7 px-2.5 text-blue-600 hover:text-blue-700 border-blue-200 hover:bg-blue-50 dark:text-blue-400 dark:border-blue-900/60 dark:hover:bg-blue-950/50 text-[11px] font-semibold cursor-pointer shadow-2xs"
                            title="Xem Bảng Chi Tiết Nhân Sự"
                          >
                            <FileText className="h-3.5 w-3.5 mr-1 text-blue-500" />
                            Chi tiết
                          </Button>

                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => openEditModal(person)}
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
                                id: person.id,
                                name: person.fullName,
                                isBatch: false,
                              })
                            }}
                            className="h-7 w-7 p-0 text-slate-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-950/40 cursor-pointer"
                            title="Xóa nhân sự"
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
                  <td colSpan={7} className="p-8 text-center text-slate-400 italic">
                    Không tìm thấy nhân sự nào phù hợp
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
              <strong className="text-slate-800 dark:text-slate-200">
                {totalItems > 0 ? (pageNumber - 1) * pageSize + 1 : 0} - {Math.min(pageNumber * pageSize, totalItems)}
              </strong>{' '}
              trên{' '}
              <strong className="text-slate-800 dark:text-slate-200">{totalItems}</strong> nhân sự
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
                {[5, 10, 15, 25, 50].map((num) => (
                  <option key={num} value={num}>{num}</option>
                ))}
              </select>
            </div>
          </div>

          {/* Pagination Navigation */}
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

            {/* Page indicator pills */}
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
                    {showEllipsis && (
                      <span className="px-1 text-slate-400 select-none">...</span>
                    )}
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

      {/* ===================================================================== */}
      {/* MODAL CHI TIẾT NHÂN SỰ — ĐỒNG BỘ THEO CARD THÔNG SỐ CHUYÊN NGHIỆP */}
      {/* ===================================================================== */}
      <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto p-4 sm:p-6 bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 border-slate-200 dark:border-slate-800 shadow-2xl">
          <DialogHeader className="border-b border-slate-200 dark:border-slate-800 pb-3 pr-8">
            <DialogTitle className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 text-base">
              <div className="flex items-center gap-2.5">
                <div className="h-9 w-9 rounded-lg bg-blue-100 dark:bg-blue-950/60 text-blue-600 dark:text-blue-400 flex items-center justify-center">
                  <UserCheck className="h-5 w-5" />
                </div>
                <div>
                  <span className="font-bold text-slate-900 dark:text-white tracking-wide text-base">
                    {selectedPerson?.fullName || 'Hồ Sơ Nhân Sự'}
                  </span>
                  <span className="text-xs text-slate-500 dark:text-slate-400 ml-2 font-mono hidden sm:inline">
                    ({selectedPerson?.code || 'Chưa đặt mã'})
                  </span>
                </div>
              </div>

              <div className="flex items-center gap-1.5 mr-2">
                {selectedPerson && getPersonTypeBadge(selectedPerson.type)}
                {selectedPerson?.isActive ? (
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

          {selectedPerson && (
            <div className="space-y-3.5 pt-2 text-xs">
              <div className="flex items-center gap-1.5 font-bold text-slate-800 dark:text-slate-200">
                <Info className="h-4 w-4 text-blue-600" />
                <span>THÔNG TIN ĐỊNH DANH & TỔ CHỨC CÔNG TÁC</span>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                {/* Card 1: Định danh nhân sự */}
                <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2.5">
                  <div className="flex items-center gap-1.5 font-bold text-blue-600 dark:text-blue-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                    <Layers className="h-4 w-4" />
                    <span>Định Danh Cá Nhân</span>
                  </div>
                  <div className="grid grid-cols-2 gap-2 pt-0.5">
                    <div>
                      <span className="text-slate-400 block text-[11px]">Mã định danh/NV:</span>
                      <span className="font-mono font-extrabold text-slate-900 dark:text-white text-sm">
                        {selectedPerson.code || '--'}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Phân loại đối tượng:</span>
                      <div className="mt-0.5">{getPersonTypeBadge(selectedPerson.type)}</div>
                    </div>
                    <div className="col-span-2">
                      <span className="text-slate-400 block text-[11px]">Họ và tên:</span>
                      <span className="font-bold text-slate-900 dark:text-slate-100 text-sm">
                        {selectedPerson.fullName}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Số điện thoại:</span>
                      <span className="font-mono font-medium text-slate-800 dark:text-slate-200">
                        {selectedPerson.phoneNumber || 'Chưa cập nhật'}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Email liên hệ:</span>
                      <span className="font-medium text-slate-800 dark:text-slate-200 truncate block" title={selectedPerson.email}>
                        {selectedPerson.email || 'Chưa cập nhật'}
                      </span>
                    </div>
                  </div>
                </div>

                {/* Card 2: Đơn vị công tác */}
                <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2.5">
                  <div className="flex items-center gap-1.5 font-bold text-emerald-600 dark:text-emerald-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                    <Building2 className="h-4 w-4" />
                    <span>Đơn Vị & Tổ Chức</span>
                  </div>
                  <div className="grid grid-cols-2 gap-2 pt-0.5">
                    <div className="col-span-2">
                      <span className="text-slate-400 block text-[11px]">Phòng ban / Bộ phận:</span>
                      <span className="font-semibold text-slate-900 dark:text-slate-100 text-sm">
                        {(selectedPerson.departmentId && deptMap.get(selectedPerson.departmentId)) || 'Chưa phân phòng ban'}
                      </span>
                    </div>
                    <div className="col-span-2">
                      <span className="text-slate-400 block text-[11px]">Công ty thành viên:</span>
                      <span className="font-medium text-slate-800 dark:text-slate-200">
                        {(selectedPerson.companyId && compMap.get(selectedPerson.companyId)) || 'Chưa phân công ty'}
                      </span>
                    </div>
                    {selectedPerson.contractorId && contrMap.has(selectedPerson.contractorId) && (
                      <div className="col-span-2">
                        <span className="text-slate-400 block text-[11px]">Đơn vị Nhà thầu / Đối tác:</span>
                        <span className="font-medium text-amber-700 dark:text-amber-300">
                          {contrMap.get(selectedPerson.contractorId)}
                        </span>
                      </div>
                    )}
                  </div>
                </div>

                {/* Card 3: Trạng thái & Nhật ký */}
                <div className="col-span-1 md:col-span-2 p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2.5">
                  <div className="flex items-center gap-1.5 font-bold text-amber-600 dark:text-amber-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                    <Shield className="h-4 w-4" />
                    <span>Trạng Thái & Hệ Thống</span>
                  </div>
                  <div className="grid grid-cols-2 sm:grid-cols-3 gap-2.5 pt-0.5">
                    <div>
                      <span className="text-slate-400 block text-[11px]">Trạng thái kích hoạt:</span>
                      <span className="font-semibold text-slate-800 dark:text-slate-200">
                        {selectedPerson.isActive ? '✅ Đang kích hoạt' : '⏸ Đang tạm dừng'}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Mã ID hệ thống:</span>
                      <span className="font-mono text-[10px] text-slate-500 truncate block" title={selectedPerson.id}>
                        {selectedPerson.id}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Ngày tạo:</span>
                      <span className="text-slate-600 dark:text-slate-400 font-mono text-[11px]">
                        {selectedPerson.createdAt ? new Date(selectedPerson.createdAt).toLocaleString('vi-VN') : '--'}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}

          <DialogFooter className="border-t border-slate-200 dark:border-slate-800 pt-3 gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                if (selectedPerson) {
                  const p = selectedPerson
                  setIsDetailOpen(false)
                  openEditModal(p)
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

      {/* MODAL THÊM MỚI NHÂN SỰ */}
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="max-w-lg sm:max-w-xl p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 shadow-2xl max-h-[90vh]">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800">
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Plus className="h-5 w-5 text-blue-600" />
              Thêm Nhân Sự Mới
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
              disabled={!formName.trim() || !formCode.trim() || createMutation.isPending}
              onClick={() => createMutation.mutate()}
              className="bg-blue-600 hover:bg-blue-700 text-white text-xs cursor-pointer"
            >
              {createMutation.isPending ? 'Đang lưu...' : 'Lưu Nhân Sự'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* MODAL CHỈNH SỬA NHÂN SỰ */}
      <Dialog open={isEditOpen} onOpenChange={setIsEditOpen}>
        <DialogContent className="max-w-lg sm:max-w-xl p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 shadow-2xl max-h-[90vh]">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800">
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Edit className="h-5 w-5 text-blue-600" />
              Chỉnh Sửa Nhân Sự
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
              disabled={!formName.trim() || !formCode.trim() || updateMutation.isPending}
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
            <span>Đã chọn <strong className="text-blue-600 dark:text-blue-400 font-mono text-sm">{selectedIds.length}</strong> nhân sự</span>
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
        title={deleteConfirm.isBatch ? 'Xác Nhận Xóa Nhiều Nhân Sự' : 'Xác Nhận Xóa Nhân Sự'}
        description={
          deleteConfirm.isBatch ? (
            <span>
              Bạn có chắc chắn muốn xóa{' '}
              <strong className="text-red-600 dark:text-red-400 font-semibold">
                {selectedIds.length} nhân sự
              </strong>{' '}
              đã chọn? Dữ liệu sẽ được lưu trữ trong thùng rác hệ thống.
            </span>
          ) : (
            <span>
              Bạn có chắc chắn muốn xóa nhân sự{' '}
              <strong className="text-slate-900 dark:text-slate-100 font-semibold">
                [{deleteConfirm.name}]
              </strong>
              ? Dữ liệu sẽ được chuyển vào thùng rác.
            </span>
          )
        }
        confirmText={deleteConfirm.isBatch ? `Xóa ${selectedIds.length} Mục` : 'Xác Nhận Xóa'}
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
