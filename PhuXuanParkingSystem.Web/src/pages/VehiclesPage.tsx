import { useState, useRef, useMemo } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Car,
  Bike,
  Truck,
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
  FileSpreadsheet,
  User,
  Info,
  CheckCircle2,
  XCircle,
  Tag,
} from 'lucide-react'
import { apiClient } from '@/services/apiClient'
import type { PagedResult, Vehicle, VehicleType, Person } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'
import { exportToExcel, parseExcelFile, downloadExcelTemplate } from '@/lib/excelHelper'

export function cleanPlateNumber(plate?: string): string {
  if (!plate) return ''
  return plate.replace(/[^a-zA-Z0-9]/g, '').toUpperCase()
}

function getVehicleTypeLabel(type?: VehicleType | string) {
  switch (type) {
    case 'Car': return 'Ô tô'
    case 'Motorcycle': return 'Xe máy'
    case 'Truck': return 'Xe tải'
    case 'Bicycle': return 'Xe đạp'
    default: return 'Khác'
  }
}

function getVehicleTypeBadge(type?: VehicleType | string) {
  switch (type) {
    case 'Car':
      return (
        <Badge variant="outline" className="bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-950/40 dark:text-blue-300 dark:border-blue-800 text-[11px] px-2 py-0.5 font-medium gap-1">
          <Car className="h-3 w-3" /> Ô tô
        </Badge>
      )
    case 'Motorcycle':
      return (
        <Badge variant="outline" className="bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-950/40 dark:text-amber-300 dark:border-amber-800 text-[11px] px-2 py-0.5 font-medium gap-1">
          <Bike className="h-3 w-3" /> Xe máy
        </Badge>
      )
    case 'Truck':
      return (
        <Badge variant="outline" className="bg-purple-50 text-purple-700 border-purple-200 dark:bg-purple-950/40 dark:text-purple-300 dark:border-purple-800 text-[11px] px-2 py-0.5 font-medium gap-1">
          <Truck className="h-3 w-3" /> Xe tải
        </Badge>
      )
    default:
      return <Badge variant="secondary" className="text-[11px] px-2 py-0.5">{getVehicleTypeLabel(type)}</Badge>
  }
}

export function VehiclesPage() {
  const queryClient = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [search, setSearch] = useState('')
  const [typeFilter, setTypeFilter] = useState('')
  const [ownerFilter, setOwnerFilter] = useState('')
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [selectedIds, setSelectedIds] = useState<string[]>([])

  // Modal State
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [isEditOpen, setIsEditOpen] = useState(false)
  const [isDetailOpen, setIsDetailOpen] = useState(false)
  const [selectedVehicle, setSelectedVehicle] = useState<Vehicle | null>(null)
  const [deleteConfirm, setDeleteConfirm] = useState<{
    isOpen: boolean
    id?: string
    plate?: string
    isBatch?: boolean
  }>({ isOpen: false })

  // Form State
  const [formPlate, setFormPlate] = useState('')
  const [formType, setFormType] = useState<VehicleType>('Car')
  const [formOwnerPersonId, setFormOwnerPersonId] = useState('')
  const [formIsActive, setFormIsActive] = useState(true)

  // 1. Lấy danh sách Người / Nhân sự để đưa vào dropdown Select chọn Chủ xe
  const { data: peopleData } = useQuery({
    queryKey: ['people-all-lookup'],
    queryFn: async () => {
      const res = await apiClient.get<{ data: PagedResult<Person> }>('/people', {
        params: { pageSize: 1000 },
      })
      return res.data.data?.items || []
    },
    staleTime: 60000,
  })
  const peopleList = peopleData || []

  // Map nhanh Person theo Id
  const personMap = useMemo(() => {
    const map = new Map<string, Person>()
    peopleList.forEach((p) => map.set(p.id, p))
    return map
  }, [peopleList])

  // 2. Query danh sách phương tiện
  const { data, isLoading } = useQuery({
    queryKey: ['vehicles-list', search, typeFilter, pageNumber, pageSize],
    queryFn: async () => {
      const res = await apiClient.get<{ data: PagedResult<Vehicle> }>('/vehicles', {
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

  // Lọc client thêm theo ownerFilter (nếu có)
  const rawItems = data?.items || []
  const items = useMemo(() => {
    if (!ownerFilter) return rawItems
    if (ownerFilter === 'has_owner') return rawItems.filter((v) => !!v.ownerPersonId)
    if (ownerFilter === 'no_owner') return rawItems.filter((v) => !v.ownerPersonId)
    return rawItems
  }, [rawItems, ownerFilter])

  const totalItems = data?.totalCount || 0
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize))

  // ======================== Mutations ========================
  const createMutation = useMutation({
    mutationFn: async () => {
      await apiClient.post('/vehicles', {
        plateNumber: cleanPlateNumber(formPlate),
        type: formType,
        ownerPersonId: formOwnerPersonId || null,
        isActive: formIsActive,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicles-list'] })
      setIsCreateOpen(false)
      resetForm()
    },
    onError: (err: any) => {
      alert('Lỗi thêm phương tiện: ' + (err?.response?.data?.message || err.message))
    },
  })

  const updateMutation = useMutation({
    mutationFn: async () => {
      if (!selectedVehicle) return
      await apiClient.put(`/vehicles/${selectedVehicle.id}`, {
        plateNumber: cleanPlateNumber(formPlate),
        type: formType,
        ownerPersonId: formOwnerPersonId || null,
        isActive: formIsActive,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicles-list'] })
      setIsEditOpen(false)
      resetForm()
    },
    onError: (err: any) => {
      alert('Lỗi cập nhật: ' + (err?.response?.data?.message || err.message))
    },
  })

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/vehicles/${id}`)
    },
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: ['vehicles-list'] })
      setSelectedIds((prev) => prev.filter((item) => item !== id))
    },
  })

  const batchDeleteMutation = useMutation({
    mutationFn: async (ids: string[]) => {
      await apiClient.post('/vehicles/delete-batch', ids)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicles-list'] })
      setSelectedIds([])
    },
  })

  const batchImportMutation = useMutation({
    mutationFn: async (vehicles: Partial<Vehicle>[]) => {
      await apiClient.post('/vehicles/batch', vehicles)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicles-list'] })
      alert('Nhập danh sách phương tiện từ Excel thành công!')
    },
    onError: (err: any) => {
      alert('Lỗi nhập Excel: ' + (err?.response?.data?.message || err.message))
    },
  })

  // ======================== Helpers ========================
  const resetForm = () => {
    setFormPlate('')
    setFormType('Car')
    setFormOwnerPersonId('')
    setFormIsActive(true)
    setSelectedVehicle(null)
  }

  const openEditModal = (vehicle: Vehicle) => {
    setSelectedVehicle(vehicle)
    setFormPlate(vehicle.plateNumber || '')
    setFormType(vehicle.type || 'Car')
    setFormOwnerPersonId(vehicle.ownerPersonId || '')
    setFormIsActive(vehicle.isActive ?? true)
    setIsEditOpen(true)
  }

  // Selection
  const handleToggleSelect = (id: string) =>
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((i) => i !== id) : [...prev, id]
    )

  const handleSelectAll = () => {
    if (!items.length) return
    const ids = items.map((d) => d.id)
    const allSelected = ids.every((id) => selectedIds.includes(id))
    if (allSelected) {
      setSelectedIds((prev) => prev.filter((id) => !ids.includes(id)))
    } else {
      setSelectedIds((prev) => Array.from(new Set([...prev, ...ids])))
    }
  }

  const isAllSelected = items.length > 0 && items.every((d) => selectedIds.includes(d.id))

  // ======================== Excel Handlers ========================
  const handleExportExcel = () => {
    if (!items.length) { alert('Không có dữ liệu để xuất Excel.'); return }

    const exportData = items.map((v, i) => {
      const owner = v.ownerPersonId ? personMap.get(v.ownerPersonId) : null
      return {
        STT: (pageNumber - 1) * pageSize + i + 1,
        'Biển Số Xe': v.plateNumber,
        'Loại Phương Tiện': getVehicleTypeLabel(v.type),
        'Chủ Sở Hữu': owner ? owner.fullName : 'Xe vãng lai / Chưa gán',
        'Mã Chủ Xe': owner ? owner.code : '',
        'Số Điện Thoại': owner ? (owner.phoneNumber || '') : '',
        'Trạng Thái': v.isActive ? 'Đang hoạt động' : 'Tạm dừng',
        'Ngày Đăng Ký': v.createdAt ? new Date(v.createdAt).toLocaleString('vi-VN') : '',
      }
    })

    exportToExcel(exportData, `Danh_Sach_Phuong_Tien_${new Date().toISOString().slice(0, 10)}.xlsx`, 'PhuongTien')
  }

  const handleDownloadTemplate = () => {
    const template = [
      {
        'Biển Số Xe': '30A12345',
        'Loại Xe (Car/Motorcycle/Truck)': 'Car',
        'Mã Nhân Sự Chủ Xe (Nếu có)': 'NV-001',
      },
      {
        'Biển Số Xe': '29B99988',
        'Loại Xe (Car/Motorcycle/Truck)': 'Motorcycle',
        'Mã Nhân Sự Chủ Xe (Nếu có)': '',
      },
    ]
    downloadExcelTemplate(template, 'Mau_Nhap_Phuong_Tien.xlsx')
  }

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    try {
      const rawData = await parseExcelFile<any>(file)
      if (!rawData?.length) { alert('File Excel không có dữ liệu.'); return }

      // Map mã nhân sự sang PersonId
      const codeToIdMap = new Map<string, string>()
      peopleList.forEach((p) => {
        if (p.code) codeToIdMap.set(p.code.toLowerCase().trim(), p.id)
      })

      const formattedData: Partial<Vehicle>[] = rawData.map((row) => {
        const rawType = String(row['Loại Xe (Car/Motorcycle/Truck)'] || row['Loại Xe'] || row['type'] || '').toLowerCase()
        let vehicleType: VehicleType = 'Car'
        if (rawType.includes('motor') || rawType.includes('máy')) vehicleType = 'Motorcycle'
        else if (rawType.includes('truck') || rawType.includes('tải')) vehicleType = 'Truck'

        const ownerCode = String(row['Mã Nhân Sự Chủ Xe (Nếu có)'] || row['Mã Nhân Sự'] || row['ownerCode'] || '').toLowerCase().trim()
        const ownerPersonId = ownerCode ? (codeToIdMap.get(ownerCode) || null) : null

        return {
          plateNumber: String(row['Biển Số Xe'] || row['plateNumber'] || '').trim(),
          type: vehicleType,
          ownerPersonId: ownerPersonId || undefined,
          isActive: true,
        }
      }).filter((v) => v.plateNumber)

      if (!formattedData.length) { alert('Không tìm thấy bản ghi hợp lệ (Cần có cột Biển Số Xe).'); return }

      batchImportMutation.mutate(formattedData as Vehicle[])
    } catch (err: any) {
      alert('Lỗi đọc file Excel: ' + err.message)
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  // ======================== Form JSX ========================
  const renderFormFields = () => (
    <div className="space-y-3.5 py-2 text-xs">
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">Biển số xe *</label>
          <Input
            placeholder="VD: 30A12345"
            value={formPlate}
            onChange={(e) => setFormPlate(cleanPlateNumber(e.target.value))}
            className="text-xs font-mono font-bold tracking-wider"
          />
        </div>
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">Loại phương tiện</label>
          <select
            value={formType}
            onChange={(e) => setFormType(e.target.value as VehicleType)}
            className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
          >
            <option value="Car">🚗 Ô tô</option>
            <option value="Motorcycle">🏍️ Xe máy</option>
            <option value="Truck">🚚 Xe tải</option>
            <option value="Bicycle">🚲 Xe đạp</option>
            <option value="Other">Khác</option>
          </select>
        </div>
      </div>

      {/* Dropdown Select chọn Chủ sở hữu từ bảng Person */}
      <div className="space-y-1">
        <label className="font-semibold text-slate-700 dark:text-slate-300 flex items-center justify-between">
          <span className="flex items-center gap-1.5">
            <User className="h-3.5 w-3.5 text-blue-600" />
            Chủ sở hữu phương tiện (Tùy chọn)
          </span>
        </label>
        <select
          value={formOwnerPersonId}
          onChange={(e) => setFormOwnerPersonId(e.target.value)}
          className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
        >
          <option value="">-- Không gán chủ xe (Xe vãng lai / Tự do) --</option>
          {peopleList.map((p) => (
            <option key={p.id} value={p.id}>
              {p.fullName} ({p.code || 'Chưa có mã'}){p.departmentName ? ` - [Phòng ${p.departmentName}]` : p.contractorName ? ` - [Nhà thầu: ${p.contractorName}]` : p.companyName ? ` - [${p.companyName}]` : ''}
            </option>
          ))}
        </select>
        <p className="text-[11px] text-slate-400 italic">
          Chọn người sở hữu từ danh sách nhân sự hoặc để trống nếu là phương tiện vãng lai/chưa định danh.
        </p>
      </div>

      <div className="flex items-center gap-2 pt-1 border-t border-slate-100 dark:border-slate-800">
        <input
          type="checkbox"
          id="isVehicleActive"
          checked={formIsActive}
          onChange={(e) => setFormIsActive(e.target.checked)}
          className="rounded border-slate-300 text-blue-600 focus:ring-blue-500 cursor-pointer h-3.5 w-3.5"
        />
        <label htmlFor="isVehicleActive" className="text-slate-700 dark:text-slate-300 font-medium cursor-pointer">
          Đang hoạt động
        </label>
      </div>
    </div>
  )

  const detailOwner = selectedVehicle?.ownerPersonId ? personMap.get(selectedVehicle.ownerPersonId) : null

  return (
    <div className="space-y-6">
      {/* Header Bar */}
      <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
        <div className="max-w-2xl min-w-0">
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
            Quản Lý Phương Tiện
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-1 leading-relaxed">
            Danh sách các phương tiện đăng ký & gán liên kết với cán bộ nhân viên, đối tác
          </p>
        </div>

        {/* Action Buttons - Cố định 100% không bao giờ bị xê dịch */}
        <div className="flex items-center gap-2 shrink-0 flex-nowrap">
          <input type="file" ref={fileInputRef} onChange={handleFileChange} accept=".xlsx,.xls,.csv" className="hidden" />
          <Button variant="outline" size="sm" onClick={() => fileInputRef.current?.click()}
            className="gap-1.5 text-xs text-slate-700 dark:text-slate-300 border-slate-200 dark:border-slate-700 cursor-pointer shadow-xs whitespace-nowrap">
            <Upload className="h-3.5 w-3.5 text-emerald-600" /> Nhập Excel
          </Button>
          <Button variant="outline" size="sm" onClick={handleDownloadTemplate}
            className="gap-1.5 text-xs text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-700 cursor-pointer shadow-xs whitespace-nowrap" title="Tải file mẫu Excel">
            <FileSpreadsheet className="h-3.5 w-3.5 text-blue-500" /> Tải Mẫu
          </Button>
          <Button variant="outline" size="sm" onClick={handleExportExcel}
            className="gap-1.5 text-xs text-slate-700 dark:text-slate-300 border-slate-200 dark:border-slate-700 cursor-pointer shadow-xs whitespace-nowrap">
            <Download className="h-3.5 w-3.5 text-blue-600" /> Xuất Excel
          </Button>
          <Button
            size="sm"
            onClick={() => { resetForm(); setIsCreateOpen(true) }}
            className="gap-2 text-xs font-semibold bg-blue-600 hover:bg-blue-700 text-white cursor-pointer shadow-xs whitespace-nowrap"
          >
            <Plus className="h-4 w-4" /> Đăng Ký Xe Mới
          </Button>
        </div>
      </div>

      {/* Filter Bar */}
      <Card className="shadow-xs border-slate-200 dark:border-slate-800">
        <CardContent className="p-4 flex flex-col md:flex-row items-center justify-between gap-3">
          <div className="flex flex-1 items-center gap-3 w-full">
            <div className="relative flex-1 max-w-sm">
              <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
              <Input
                placeholder="Tìm kiếm theo biển số xe..."
                value={search}
                onChange={(e) => { setSearch(e.target.value); setPageNumber(1) }}
                className="pl-9 text-xs"
              />
            </div>
            <div className="w-44">
              <select
                value={typeFilter}
                onChange={(e) => { setTypeFilter(e.target.value); setPageNumber(1) }}
                className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
              >
                <option value="">-- Tất cả loại xe --</option>
                <option value="Car">🚗 Ô tô</option>
                <option value="Motorcycle">🏍️ Xe máy</option>
                <option value="Truck">🚚 Xe tải</option>
                <option value="Bicycle">🚲 Xe đạp</option>
              </select>
            </div>
            <div className="w-48">
              <select
                value={ownerFilter}
                onChange={(e) => setOwnerFilter(e.target.value)}
                className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
              >
                <option value="">-- Tất cả chủ sở hữu --</option>
                <option value="has_owner">👤 Đã gán chủ xe</option>
                <option value="no_owner">⚪ Xe vãng lai (Chưa gán)</option>
              </select>
            </div>
          </div>

          {(search || typeFilter || ownerFilter) && (
            <Button variant="outline" size="sm"
              onClick={() => { setSearch(''); setTypeFilter(''); setOwnerFilter(''); setPageNumber(1) }}
              className="text-xs gap-1.5 cursor-pointer">
              <RefreshCw className="h-3.5 w-3.5" /> Đặt lại
            </Button>
          )}
        </CardContent>
      </Card>

      {/* Table */}
      <Card className="shadow-xs overflow-hidden border-slate-200 dark:border-slate-800">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-slate-100/80 dark:bg-slate-800/60 text-slate-700 dark:text-slate-300 font-semibold border-b border-slate-200 dark:border-slate-800">
              <tr>
                <th className="p-3.5 pl-4 w-10">
                  <input type="checkbox" checked={isAllSelected} onChange={handleSelectAll}
                    className="rounded border-slate-300 dark:border-slate-700 text-blue-600 cursor-pointer" />
                </th>
                <th className="p-3.5">Biển Số Xe</th>
                <th className="p-3.5">Loại Phương Tiện</th>
                <th className="p-3.5">Chủ Sở Hữu (Person)</th>
                <th className="p-3.5">Trạng Thái</th>
                <th className="p-3.5">Ngày Đăng Ký</th>
                <th className="p-3.5 text-right pr-4">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {isLoading ? (
                <tr><td colSpan={7} className="p-8 text-center text-slate-400">Đang tải danh sách phương tiện...</td></tr>
              ) : items.length > 0 ? (
                items.map((vehicle) => {
                  const isSelected = selectedIds.includes(vehicle.id)
                  const owner = vehicle.ownerPersonId ? personMap.get(vehicle.ownerPersonId) : null

                  return (
                    <tr key={vehicle.id}
                      className={`hover:bg-slate-50 dark:hover:bg-slate-800/40 transition-colors ${isSelected ? 'bg-blue-50/50 dark:bg-blue-950/20' : ''}`}>
                      <td className="p-3.5 pl-4">
                        <input type="checkbox" checked={isSelected} onChange={() => handleToggleSelect(vehicle.id)}
                          className="rounded border-slate-300 dark:border-slate-700 text-blue-600 cursor-pointer" />
                      </td>
                      <td className="p-3.5 font-bold font-mono text-slate-900 dark:text-slate-100 text-sm tracking-wide">
                        {cleanPlateNumber(vehicle.plateNumber)}
                      </td>
                      <td className="p-3.5">
                        {getVehicleTypeBadge(vehicle.type)}
                      </td>
                      <td className="p-3.5">
                        {owner ? (
                          <div className="flex items-center gap-1.5">
                            <User className="h-3.5 w-3.5 text-blue-600 dark:text-blue-400 flex-shrink-0" />
                            <div>
                              <span className="font-bold text-slate-800 dark:text-slate-200">
                                {owner.fullName}
                              </span>
                              <span className="text-[11px] text-slate-400 ml-1 font-mono">
                                ({owner.code || '--'})
                              </span>
                              {owner.departmentName ? (
                                <span className="text-[10px] block text-blue-600 dark:text-blue-400 font-medium">
                                  {owner.departmentName} {owner.companyName ? `• ${owner.companyName}` : ''}
                                </span>
                              ) : owner.contractorName ? (
                                <span className="text-[10px] block text-amber-600 dark:text-amber-400 font-medium">
                                  Nhà thầu: {owner.contractorName}
                                </span>
                              ) : owner.companyName ? (
                                <span className="text-[10px] block text-slate-500 dark:text-slate-400">
                                  {owner.companyName}
                                </span>
                              ) : null}
                            </div>
                          </div>
                        ) : (
                          <span className="text-slate-400 dark:text-slate-500 italic text-[11px]">
                            Xe vãng lai / Chưa gán
                          </span>
                        )}
                      </td>
                      <td className="p-3.5">
                        {vehicle.isActive ? (
                          <span className="inline-flex items-center gap-1 text-emerald-600 dark:text-emerald-400 font-medium text-[11px]">
                            <CheckCircle2 className="h-3.5 w-3.5" /> Hoạt động
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1 text-slate-400 dark:text-slate-500 font-medium text-[11px]">
                            <XCircle className="h-3.5 w-3.5" /> Tạm dừng
                          </span>
                        )}
                      </td>
                      <td className="p-3.5 text-slate-500 dark:text-slate-400 font-mono text-[11px]">
                        {vehicle.createdAt ? new Date(vehicle.createdAt).toLocaleDateString('vi-VN') : '--'}
                      </td>
                      <td className="p-3.5 text-right pr-4">
                        <div className="flex items-center justify-end gap-1.5">
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => { setSelectedVehicle(vehicle); setIsDetailOpen(true) }}
                            className="h-7 px-2.5 text-blue-600 hover:text-blue-700 border-blue-200 hover:bg-blue-50 dark:text-blue-400 dark:border-blue-900/60 dark:hover:bg-blue-950/50 text-[11px] font-semibold cursor-pointer shadow-2xs"
                            title="Xem Chi Tiết Phương Tiện"
                          >
                            <FileText className="h-3.5 w-3.5 mr-1 text-blue-500" />
                            Chi tiết
                          </Button>
                          <Button size="sm" variant="outline" onClick={() => openEditModal(vehicle)}
                            className="h-7 w-7 p-0 text-slate-600 hover:text-slate-900 dark:text-slate-400 cursor-pointer" title="Chỉnh sửa">
                            <Edit className="h-3.5 w-3.5" />
                          </Button>
                          <Button size="sm" variant="ghost"
                            onClick={() => {
                              setDeleteConfirm({
                                isOpen: true,
                                id: vehicle.id,
                                plate: vehicle.plateNumber,
                                isBatch: false,
                              })
                            }}
                            className="h-7 w-7 p-0 text-slate-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-950/40 cursor-pointer" title="Xóa phương tiện">
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
                    Chưa có phương tiện nào trong danh sách
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        <div className="p-3.5 border-t border-slate-200 dark:border-slate-800 bg-slate-50/80 dark:bg-slate-900/60 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs">
          <div className="flex items-center gap-3">
            <span className="text-slate-500 dark:text-slate-400">
              Hiển thị{' '}
              <strong className="text-slate-800 dark:text-slate-200">
                {totalItems > 0 ? (pageNumber - 1) * pageSize + 1 : 0} - {Math.min(pageNumber * pageSize, totalItems)}
              </strong>{' '}
              trên{' '}
              <strong className="text-slate-800 dark:text-slate-200">{totalItems}</strong> phương tiện
            </span>
            <div className="flex items-center gap-1.5 pl-2 border-l border-slate-200 dark:border-slate-700">
              <span className="text-slate-400 text-[11px]">Dòng/trang:</span>
              <select
                value={pageSize}
                onChange={(e) => { setPageSize(Number(e.target.value)); setPageNumber(1) }}
                className="h-7 rounded border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
              >
                {[5, 10, 15, 25, 50].map((n) => <option key={n} value={n}>{n}</option>)}
              </select>
            </div>
          </div>

          <div className="flex items-center gap-1">
            <Button variant="outline" size="sm" disabled={pageNumber <= 1} onClick={() => setPageNumber(1)} className="h-7 w-7 p-0 cursor-pointer"><ChevronsLeft className="h-3.5 w-3.5" /></Button>
            <Button variant="outline" size="sm" disabled={pageNumber <= 1} onClick={() => setPageNumber((p) => p - 1)} className="h-7 w-7 p-0 cursor-pointer"><ChevronLeft className="h-3.5 w-3.5" /></Button>
            {Array.from({ length: totalPages }, (_, i) => i + 1)
              .filter((p) => totalPages <= 5 || Math.abs(p - pageNumber) <= 1 || p === 1 || p === totalPages)
              .map((p, idx, arr) => {
                const prev = arr[idx - 1]
                return (
                  <div key={p} className="flex items-center">
                    {prev && p - prev > 1 && <span className="px-1 text-slate-400">...</span>}
                    <Button
                      variant={pageNumber === p ? 'default' : 'outline'}
                      size="sm"
                      onClick={() => setPageNumber(p)}
                      className={`h-7 min-w-[28px] px-2 text-xs cursor-pointer ${pageNumber === p ? 'bg-blue-600 text-white font-bold' : 'text-slate-600 dark:text-slate-400'}`}
                    >{p}</Button>
                  </div>
                )
              })}
            <Button variant="outline" size="sm" disabled={pageNumber >= totalPages} onClick={() => setPageNumber((p) => p + 1)} className="h-7 w-7 p-0 cursor-pointer"><ChevronRight className="h-3.5 w-3.5" /></Button>
            <Button variant="outline" size="sm" disabled={pageNumber >= totalPages} onClick={() => setPageNumber(totalPages)} className="h-7 w-7 p-0 cursor-pointer"><ChevronsRight className="h-3.5 w-3.5" /></Button>
          </div>
        </div>
      </Card>

      {/* ===================================================================== */}
      {/* MODAL CHI TIẾT PHƯƠNG TIỆN */}
      {/* ===================================================================== */}
      <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
        <DialogContent className="max-w-xl max-h-[90vh] p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 border-slate-200 dark:border-slate-800 shadow-2xl">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800 pr-12">
            <DialogTitle className="flex items-center justify-between gap-2 text-base">
              <div className="flex items-center gap-2.5">
                <div className="h-9 w-9 rounded-lg bg-blue-100 dark:bg-blue-950/60 text-blue-600 dark:text-blue-400 flex items-center justify-center">
                  <Car className="h-5 w-5" />
                </div>
                <div>
                  <span className="font-bold font-mono tracking-wider text-lg text-slate-900 dark:text-white">
                    {selectedVehicle?.plateNumber || '--'}
                  </span>
                  <div className="mt-0.5">{selectedVehicle && getVehicleTypeBadge(selectedVehicle.type)}</div>
                </div>
              </div>

              <div className="mr-4">
                {selectedVehicle?.isActive ? (
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

          {selectedVehicle && (
            <div className="flex-1 overflow-y-auto p-5 space-y-3.5 text-xs">
              <div className="flex items-center gap-1.5 font-bold text-slate-800 dark:text-slate-200">
                <Info className="h-4 w-4 text-blue-600" />
                <span>THÔNG TIN ĐỊNH DANH & CHỦ PHƯƠNG TIỆN</span>
              </div>

              <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2.5">
                <div className="flex items-center gap-1.5 font-bold text-blue-600 dark:text-blue-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                  <User className="h-4 w-4" />
                  <span>Chủ Sở Hữu Xe</span>
                </div>
                {detailOwner ? (
                  <div className="grid grid-cols-2 gap-2.5 pt-0.5">
                    <div>
                      <span className="text-slate-400 block text-[11px]">Họ và tên:</span>
                      <span className="font-bold text-slate-900 dark:text-white text-sm">{detailOwner.fullName}</span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Mã định danh/NV:</span>
                      <span className="font-mono font-bold text-slate-800 dark:text-slate-200">{detailOwner.code || '--'}</span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Số điện thoại:</span>
                      <span className="font-mono text-slate-800 dark:text-slate-200">{detailOwner.phoneNumber || '--'}</span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Email:</span>
                      <span className="text-slate-800 dark:text-slate-200">{detailOwner.email || '--'}</span>
                    </div>
                    {detailOwner.companyName && (
                      <div className="col-span-2">
                        <span className="text-slate-400 block text-[11px]">Đơn vị công tác:</span>
                        <span className="font-medium text-slate-800 dark:text-slate-200">
                          {detailOwner.companyName} {detailOwner.departmentName ? `• ${detailOwner.departmentName}` : ''}
                        </span>
                      </div>
                    )}
                  </div>
                ) : (
                  <div className="py-2 text-slate-400 italic">
                    Xe vãng lai hoặc chưa được gán với chủ sở hữu cụ thể trong hệ thống.
                  </div>
                )}
              </div>

              <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2.5">
                <div className="flex items-center gap-1.5 font-bold text-emerald-600 dark:text-emerald-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                  <Tag className="h-4 w-4" />
                  <span>Thông Tin Kỹ Thuật & Hệ Thống</span>
                </div>
                <div className="grid grid-cols-2 gap-2.5 pt-0.5">
                  <div>
                    <span className="text-slate-400 block text-[11px]">Mã ID bản ghi:</span>
                    <span className="font-mono text-[11px] text-slate-600 dark:text-slate-400">{selectedVehicle.id}</span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[11px]">Ngày đăng ký:</span>
                    <span className="font-mono text-slate-700 dark:text-slate-300">
                      {selectedVehicle.createdAt ? new Date(selectedVehicle.createdAt).toLocaleString('vi-VN') : '--'}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          )}

          <DialogFooter className="p-4 pt-3 border-t border-slate-200 dark:border-slate-800 gap-2 bg-slate-50/50 dark:bg-slate-900/50">
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                if (selectedVehicle) {
                  const v = selectedVehicle
                  setIsDetailOpen(false)
                  openEditModal(v)
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

      {/* MODAL THÊM MỚI */}
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="max-w-lg sm:max-w-xl p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 shadow-2xl max-h-[90vh]">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800">
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Plus className="h-5 w-5 text-blue-600" />
              Đăng Ký Phương Tiện Mới
            </DialogTitle>
          </DialogHeader>
          <div className="flex-1 overflow-y-auto p-5">
            {renderFormFields()}
          </div>
          <DialogFooter className="p-4 pt-3 border-t border-slate-200 dark:border-slate-800 gap-2 bg-slate-50/50 dark:bg-slate-900/50">
            <Button variant="outline" size="sm" onClick={() => setIsCreateOpen(false)} className="text-xs cursor-pointer">Hủy</Button>
            <Button size="sm" disabled={!formPlate.trim() || createMutation.isPending}
              onClick={() => createMutation.mutate()}
              className="bg-blue-600 hover:bg-blue-700 text-white text-xs cursor-pointer">
              {createMutation.isPending ? 'Đang lưu...' : 'Lưu Phương Tiện'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* MODAL CHỈNH SỬA */}
      <Dialog open={isEditOpen} onOpenChange={setIsEditOpen}>
        <DialogContent className="max-w-lg sm:max-w-xl p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 shadow-2xl max-h-[90vh]">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800">
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Edit className="h-5 w-5 text-blue-600" />
              Chỉnh Sửa Phương Tiện
            </DialogTitle>
          </DialogHeader>
          <div className="flex-1 overflow-y-auto p-5">
            {renderFormFields()}
          </div>
          <DialogFooter className="p-4 pt-3 border-t border-slate-200 dark:border-slate-800 gap-2 bg-slate-50/50 dark:bg-slate-900/50">
            <Button variant="outline" size="sm" onClick={() => setIsEditOpen(false)} className="text-xs cursor-pointer">Hủy</Button>
            <Button size="sm" disabled={!formPlate.trim() || updateMutation.isPending}
              onClick={() => updateMutation.mutate()}
              className="bg-blue-600 hover:bg-blue-700 text-white text-xs cursor-pointer">
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
            <span>Đã chọn <strong className="text-blue-600 dark:text-blue-400 font-mono text-sm">{selectedIds.length}</strong> phương tiện</span>
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
        title={deleteConfirm.isBatch ? 'Xác Nhận Xóa Nhiều Phương Tiện' : 'Xác Nhận Xóa Phương Tiện'}
        description={
          deleteConfirm.isBatch ? (
            <span>
              Bạn có chắc chắn muốn xóa{' '}
              <strong className="text-red-600 dark:text-red-400 font-semibold">
                {selectedIds.length} phương tiện
              </strong>{' '}
              đã chọn? Dữ liệu sẽ được lưu trữ trong thùng rác hệ thống.
            </span>
          ) : (
            <span>
              Bạn có chắc chắn muốn xóa phương tiện biển số{' '}
              <strong className="text-blue-600 dark:text-blue-400 font-mono font-bold">
                [{deleteConfirm.plate}]
              </strong>
              ? Dữ liệu sẽ được chuyển vào thùng rác.
            </span>
          )
        }
        confirmText={deleteConfirm.isBatch ? `Xóa ${selectedIds.length} Xe` : 'Xác Nhận Xóa'}
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
