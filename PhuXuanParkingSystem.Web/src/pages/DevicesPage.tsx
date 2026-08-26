import { useState, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Camera,
  Cpu,
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
  Network,
  Info,
  Shield,
  Layers,
  CheckCircle2,
  XCircle,
} from 'lucide-react'
import { apiClient } from '@/services/apiClient'
import type { PagedResult, Device, DeviceType, DeviceStatus } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'
import { exportToExcel, parseExcelFile, downloadExcelTemplate } from '@/lib/excelHelper'

// =====================================================================
// Helper functions
// =====================================================================
function getDeviceTypeLabel(type?: DeviceType | string | number) {
  const v = String(type ?? '').toLowerCase()
  if (v === 'platecamera' || v === '1') return 'Camera Biển Số'
  if (v === 'overviewcamera' || v === '2') return 'Camera Toàn Cảnh'
  if (v === 'controller' || v === '3') return 'Bộ Điều Khiển'
  if (v === 'camera') return 'Camera IP'
  return String(type ?? 'Khác')
}

function getDeviceTypeBadge(type?: DeviceType | string | number) {
  const label = getDeviceTypeLabel(type)
  const v = String(type ?? '').toLowerCase()

  if (v === 'platecamera' || v === '1') {
    return (
      <Badge
        variant="outline"
        className="bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-950/40 dark:text-blue-300 dark:border-blue-800 text-[11px] px-2.5 py-0.5 font-medium gap-1"
      >
        <Camera className="h-3 w-3" />
        {label}
      </Badge>
    )
  }
  if (v === 'overviewcamera' || v === '2' || v === 'camera') {
    return (
      <Badge
        variant="outline"
        className="bg-violet-50 text-violet-700 border-violet-200 dark:bg-violet-950/40 dark:text-violet-300 dark:border-violet-800 text-[11px] px-2.5 py-0.5 font-medium gap-1"
      >
        <Camera className="h-3 w-3" />
        {label}
      </Badge>
    )
  }
  if (v === 'controller' || v === '3') {
    return (
      <Badge
        variant="outline"
        className="bg-amber-50 text-amber-800 border-amber-300 dark:bg-amber-950/40 dark:text-amber-300 dark:border-amber-800 text-[11px] px-2.5 py-0.5 font-medium gap-1"
      >
        <Cpu className="h-3 w-3" />
        {label}
      </Badge>
    )
  }
  return <Badge variant="secondary" className="text-[11px] px-2 py-0.5">{label}</Badge>
}

// Live status badge
function getDeviceLiveStatusBadge(status?: DeviceStatus | string) {
  const s = String(status || '').toLowerCase()
  if (s === 'connected' || s === '1') {
    return (
      <span className="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200 dark:bg-emerald-950/50 dark:text-emerald-300 dark:border-emerald-800">
        <span className="h-2 w-2 rounded-full bg-emerald-500 animate-pulse" />
        Đang kết nối
      </span>
    )
  }
  if (s === 'error' || s === '3') {
    return (
      <span className="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-[11px] font-semibold bg-rose-50 text-rose-700 border border-rose-200 dark:bg-rose-950/50 dark:text-rose-300 dark:border-rose-800">
        <span className="h-2 w-2 rounded-full bg-rose-500" />
        Lỗi kết nối
      </span>
    )
  }
  return (
    <span className="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600 border border-slate-200 dark:bg-slate-800 dark:text-slate-400 dark:border-slate-700">
      <span className="h-2 w-2 rounded-full bg-slate-400" />
      Mất kết nối
    </span>
  )
}

// Default port theo loại thiết bị
function getDefaultPort(type: DeviceType): number {
  if (type === 'PlateCamera') return 3000
  if (type === 'OverviewCamera') return 8000
  if (type === 'Controller') return 4370
  return 8000
}

// =====================================================================
// DevicesPage Component
// =====================================================================
export function DevicesPage() {
  const queryClient = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [search, setSearch] = useState('')
  const [typeFilter, setTypeFilter] = useState('')
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [selectedIds, setSelectedIds] = useState<string[]>([])

  // Modal State
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [isEditOpen, setIsEditOpen] = useState(false)
  const [isDetailOpen, setIsDetailOpen] = useState(false)
  const [selectedDevice, setSelectedDevice] = useState<Device | null>(null)
  const [showPassword, setShowPassword] = useState(false)
  const [deleteConfirm, setDeleteConfirm] = useState<{
    isOpen: boolean
    id?: string
    name?: string
    isBatch?: boolean
  }>({ isOpen: false })

  // Form State
  const [formCode, setFormCode] = useState('')
  const [formName, setFormName] = useState('')
  const [formType, setFormType] = useState<DeviceType>('PlateCamera')
  const [formIp, setFormIp] = useState('')
  const [formPort, setFormPort] = useState(3000)
  const [formUser, setFormUser] = useState('')
  const [formPass, setFormPass] = useState('')
  const [formNote, setFormNote] = useState('')
  const [formIsActive, setFormIsActive] = useState(true)

  // Query Devices
  const { data, isLoading } = useQuery({
    queryKey: ['devices-list', search, typeFilter, pageNumber, pageSize],
    queryFn: async () => {
      const res = await apiClient.get<{ data: PagedResult<Device> }>('/devices', {
        params: {
          search: search || undefined,
          type: typeFilter || undefined,
          pageNumber,
          pageSize,
        },
      })
      return res.data.data
    },
    refetchInterval: 15000, // Tự động cập nhật trạng thái kết nối từ WinForms mỗi 15 giây
  })

  const items = data?.items || []
  const totalItems = data?.totalCount || 0
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize))

  // ======================== Mutations ========================
  const createMutation = useMutation({
    mutationFn: async () => {
      await apiClient.post('/devices', {
        code: formCode.trim(),
        name: formName.trim(),
        type: formType,
        ipAddress: formIp.trim(),
        port: formPort,
        userName: formUser.trim() || undefined,
        password: formPass.trim() || undefined,
        note: formNote.trim() || undefined,
        isActive: formIsActive,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices-list'] })
      setIsCreateOpen(false)
      resetForm()
    },
  })

  const updateMutation = useMutation({
    mutationFn: async () => {
      if (!selectedDevice) return
      await apiClient.put(`/devices/${selectedDevice.id}`, {
        code: formCode.trim(),
        name: formName.trim(),
        type: formType,
        ipAddress: formIp.trim(),
        port: formPort,
        userName: formUser.trim() || undefined,
        password: formPass.trim() || undefined,
        note: formNote.trim() || undefined,
        isActive: formIsActive,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices-list'] })
      setIsEditOpen(false)
      resetForm()
    },
  })

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/devices/${id}`)
    },
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: ['devices-list'] })
      setSelectedIds((prev) => prev.filter((item) => item !== id))
    },
  })

  const batchDeleteMutation = useMutation({
    mutationFn: async (ids: string[]) => {
      await apiClient.post('/devices/delete-batch', ids)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices-list'] })
      setSelectedIds([])
    },
  })

  const batchImportMutation = useMutation({
    mutationFn: async (devices: Partial<Device>[]) => {
      await apiClient.post('/devices/batch', devices)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices-list'] })
      alert('Nhập danh sách thiết bị từ Excel thành công!')
    },
    onError: (err: any) => {
      alert('Lỗi nhập Excel: ' + (err?.response?.data?.message || err.message))
    },
  })

  // ======================== Helpers ========================
  const resetForm = () => {
    setFormCode('')
    setFormName('')
    setFormType('PlateCamera')
    setFormIp('')
    setFormPort(3000)
    setFormUser('')
    setFormPass('')
    setFormNote('')
    setFormIsActive(true)
    setSelectedDevice(null)
    setShowPassword(false)
  }

  const openEditModal = (device: Device) => {
    setSelectedDevice(device)
    setFormCode(device.code || '')
    setFormName(device.name || '')
    setFormType((device.type as DeviceType) || 'PlateCamera')
    setFormIp(device.ipAddress || '')
    setFormPort(device.port || 3000)
    setFormUser(device.userName || '')
    setFormPass(device.password || '')
    setFormNote(device.note || '')
    setFormIsActive(device.isActive ?? true)
    setShowPassword(false)
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

    const exportData = items.map((d, i) => ({
      STT: (pageNumber - 1) * pageSize + i + 1,
      'Mã Thiết Bị': d.code,
      'Tên Thiết Bị': d.name,
      'Loại Thiết Bị': getDeviceTypeLabel(d.type),
      'Địa Chỉ IP': d.ipAddress,
      'Port': d.port,
      'Tên Đăng Nhập': d.userName || '',
      'Ghi Chú': d.note || '',
      'Trạng Thái': d.isActive ? 'Đang hoạt động' : 'Tạm dừng',
    }))

    exportToExcel(exportData, `Danh_Sach_Thiet_Bi_${new Date().toISOString().slice(0, 10)}.xlsx`, 'ThietBi')
  }

  const handleDownloadTemplate = () => {
    const template = [
      {
        'Mã Thiết Bị': 'CAM-IN-PLT',
        'Tên Thiết Bị': 'Camera Biển Số Làn Vào (NST)',
        'Loại Thiết Bị (PlateCamera/OverviewCamera/Controller)': 'PlateCamera',
        'Địa Chỉ IP': '192.168.1.200',
        'Port': '3000',
        'Tên Đăng Nhập': 'admin',
        'Mật Khẩu': 'admin',
        'Ghi Chú': 'Camera nhận diện biển số NST LPR',
      },
      {
        'Mã Thiết Bị': 'CAM-IN-OVW',
        'Tên Thiết Bị': 'Camera Toàn Cảnh Làn Vào (Hikvision)',
        'Loại Thiết Bị (PlateCamera/OverviewCamera/Controller)': 'OverviewCamera',
        'Địa Chỉ IP': '192.168.1.61',
        'Port': '8000',
        'Tên Đăng Nhập': 'admin',
        'Mật Khẩu': 'Hoangphat130225',
        'Ghi Chú': 'Camera toàn cảnh Hikvision',
      },
      {
        'Mã Thiết Bị': 'CTRL-C3-200',
        'Tên Thiết Bị': 'Bộ Điều Khiển ZKTeco C3-200 (Radar & Barrier)',
        'Loại Thiết Bị (PlateCamera/OverviewCamera/Controller)': 'Controller',
        'Địa Chỉ IP': '192.168.1.202',
        'Port': '4370',
        'Tên Đăng Nhập': '',
        'Mật Khẩu': '',
        'Ghi Chú': 'ZKTeco C3-200 — Nhận tín hiệu radar, điều khiển Barrier',
      },
    ]
    downloadExcelTemplate(template, 'Mau_Nhap_Thiet_Bi.xlsx')
  }

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    try {
      const rawData = await parseExcelFile<any>(file)
      if (!rawData?.length) { alert('File Excel không có dữ liệu.'); return }

      const formattedData: Partial<Device>[] = rawData.map((row) => {
        const rawType = String(row['Loại Thiết Bị (PlateCamera/OverviewCamera/Controller)'] || row['type'] || '').toLowerCase()
        let deviceType: DeviceType = 'PlateCamera'
        if (rawType.includes('overview')) deviceType = 'OverviewCamera'
        else if (rawType.includes('controller')) deviceType = 'Controller'
        else if (rawType.includes('plate') || rawType.includes('camera')) deviceType = 'PlateCamera'

        const port = Number(row['Port'] || row['port'] || getDefaultPort(deviceType))

        return {
          code: String(row['Mã Thiết Bị'] || row['code'] || '').trim(),
          name: String(row['Tên Thiết Bị'] || row['name'] || '').trim(),
          type: deviceType,
          ipAddress: String(row['Địa Chỉ IP'] || row['ip'] || '').trim(),
          port,
          userName: String(row['Tên Đăng Nhập'] || row['userName'] || '').trim() || undefined,
          password: String(row['Mật Khẩu'] || row['password'] || '').trim() || undefined,
          note: String(row['Ghi Chú'] || row['note'] || '').trim() || undefined,
          isActive: true,
        }
      }).filter((d) => d.name)

      if (!formattedData.length) { alert('Không tìm thấy bản ghi thiết bị hợp lệ trong file Excel.'); return }

      batchImportMutation.mutate(formattedData as Device[])
    } catch (err: any) {
      alert('Lỗi đọc file Excel: ' + err.message)
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  // ======================== Form JSX ========================
  const renderFormFields = () => (
    <div className="space-y-3 py-2 text-xs">
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">Mã thiết bị</label>
          <Input placeholder="VD: CAM-IN-PLT" value={formCode} onChange={(e) => setFormCode(e.target.value)} className="text-xs" />
        </div>
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">Loại thiết bị</label>
          <select
            value={formType}
            onChange={(e) => {
              const t = e.target.value as DeviceType
              setFormType(t)
              setFormPort(getDefaultPort(t))
            }}
            className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
          >
            <option value="PlateCamera">📸 Camera Biển Số (PlateCamera)</option>
            <option value="OverviewCamera">📷 Camera Toàn Cảnh (OverviewCamera)</option>
            <option value="Controller">🖥️ Bộ Điều Khiển (Controller)</option>
          </select>
        </div>
      </div>

      <div className="space-y-1">
        <label className="font-semibold text-slate-700 dark:text-slate-300">Tên mô tả thiết bị *</label>
        <Input placeholder="VD: Camera Biển Số Làn Vào (NST)" value={formName} onChange={(e) => setFormName(e.target.value)} className="text-xs" />
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">Địa chỉ IP *</label>
          <Input
            placeholder="VD: 192.168.1.200"
            value={formIp}
            onChange={(e) => setFormIp(e.target.value)}
            className="text-xs font-mono"
          />
        </div>
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">Port kết nối</label>
          <Input
            type="number"
            placeholder={String(getDefaultPort(formType))}
            value={formPort}
            onChange={(e) => setFormPort(Number(e.target.value))}
            className="text-xs font-mono"
          />
        </div>
      </div>

      {formType !== 'Controller' && (
        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-1">
            <label className="font-semibold text-slate-700 dark:text-slate-300">Tên đăng nhập</label>
            <Input placeholder="VD: admin" value={formUser} onChange={(e) => setFormUser(e.target.value)} className="text-xs" />
          </div>
          <div className="space-y-1">
            <label className="font-semibold text-slate-700 dark:text-slate-300">Mật khẩu</label>
            <Input type="password" placeholder="••••••••" value={formPass} onChange={(e) => setFormPass(e.target.value)} className="text-xs" />
          </div>
        </div>
      )}

      <div className="space-y-1">
        <label className="font-semibold text-slate-700 dark:text-slate-300">Ghi chú kỹ thuật</label>
        <Input placeholder="VD: NST LPR Camera / Hikvision IR 40m" value={formNote} onChange={(e) => setFormNote(e.target.value)} className="text-xs" />
      </div>

      <div className="flex items-center gap-2 pt-1">
        <input
          type="checkbox"
          id="isActive"
          checked={formIsActive}
          onChange={(e) => setFormIsActive(e.target.checked)}
          className="rounded border-slate-300 text-blue-600 focus:ring-blue-500 cursor-pointer h-3.5 w-3.5"
        />
        <label htmlFor="isActive" className="text-slate-700 dark:text-slate-300 font-medium cursor-pointer">
          Đang hoạt động
        </label>
      </div>
    </div>
  )

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
        <div className="max-w-2xl min-w-0">
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
            Quản Lý Thiết Bị Phần Cứng
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-1 leading-relaxed">
            Camera nhận diện biển số, Camera toàn cảnh & Bộ điều khiển Access Control kiểm soát vào ra
          </p>
        </div>

        {/* Action Buttons */}
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
            <Plus className="h-4 w-4" /> Thêm Thiết Bị Mới
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
                placeholder="Tìm theo tên hoặc địa chỉ IP..."
                value={search}
                onChange={(e) => { setSearch(e.target.value); setPageNumber(1) }}
                className="pl-9 text-xs"
              />
            </div>
            <div className="w-56">
              <select
                value={typeFilter}
                onChange={(e) => { setTypeFilter(e.target.value); setPageNumber(1) }}
                className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
              >
                <option value="">-- Tất cả thiết bị --</option>
                <option value="PlateCamera">📸 Camera Biển Số</option>
                <option value="OverviewCamera">📷 Camera Toàn Cảnh</option>
                <option value="Controller">🖥️ Bộ Điều Khiển</option>
              </select>
            </div>
          </div>

          {(search || typeFilter) && (
            <Button variant="outline" size="sm"
              onClick={() => { setSearch(''); setTypeFilter(''); setPageNumber(1) }}
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
                <th className="p-3.5">Tên Thiết Bị</th>
                <th className="p-3.5">Loại Thiết Bị</th>
                <th className="p-3.5">Địa Chỉ IP</th>
                <th className="p-3.5">Trạng Thái</th>
                <th className="p-3.5 text-right pr-4">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {isLoading ? (
                <tr><td colSpan={6} className="p-8 text-center text-slate-400">Đang tải danh sách thiết bị...</td></tr>
              ) : items.length > 0 ? (
                items.map((device) => {
                  const isSelected = selectedIds.includes(device.id)
                  return (
                    <tr key={device.id}
                      className={`hover:bg-slate-50 dark:hover:bg-slate-800/40 transition-colors ${isSelected ? 'bg-blue-50/50 dark:bg-blue-950/20' : ''}`}>
                      <td className="p-3.5 pl-4">
                        <input type="checkbox" checked={isSelected} onChange={() => handleToggleSelect(device.id)}
                          className="rounded border-slate-300 dark:border-slate-700 text-blue-600 cursor-pointer" />
                      </td>
                      <td className="p-3.5 font-bold text-slate-900 dark:text-slate-100">
                        {device.name}
                      </td>
                      <td className="p-3.5">
                        {getDeviceTypeBadge(device.type)}
                      </td>
                      <td className="p-3.5 font-mono text-slate-700 dark:text-slate-300 font-medium">
                        <div className="flex items-center gap-1.5">
                          <Network className="h-3.5 w-3.5 text-slate-400 flex-shrink-0" />
                          <span>{device.ipAddress}</span>
                        </div>
                      </td>
                      <td className="p-3.5">
                        <div className="flex flex-col gap-1 items-start">
                          {getDeviceLiveStatusBadge(device.status)}
                          <span className="text-[10px] text-slate-400 dark:text-slate-500">
                            {device.isActive ? '• Cấu hình: Kích hoạt' : '• Cấu hình: Tạm dừng'}
                          </span>
                        </div>
                      </td>
                      <td className="p-3.5 text-right pr-4">
                        <div className="flex items-center justify-end gap-1.5">
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => { setSelectedDevice(device); setIsDetailOpen(true) }}
                            className="h-7 px-2.5 text-blue-600 hover:text-blue-700 border-blue-200 hover:bg-blue-50 dark:text-blue-400 dark:border-blue-900/60 dark:hover:bg-blue-950/50 text-[11px] font-semibold cursor-pointer shadow-2xs"
                            title="Xem Bảng Thông Số Chi Tiết Thiết Bị"
                          >
                            <FileText className="h-3.5 w-3.5 mr-1 text-blue-500" />
                            Chi tiết
                          </Button>
                          <Button size="sm" variant="outline" onClick={() => openEditModal(device)}
                            className="h-7 w-7 p-0 text-slate-600 hover:text-slate-900 dark:text-slate-400 cursor-pointer" title="Chỉnh sửa">
                            <Edit className="h-3.5 w-3.5" />
                          </Button>
                          <Button size="sm" variant="ghost"
                            onClick={() => {
                              setDeleteConfirm({
                                isOpen: true,
                                id: device.id,
                                name: device.name,
                                isBatch: false,
                              })
                            }}
                            className="h-7 w-7 p-0 text-slate-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-950/40 cursor-pointer" title="Xóa thiết bị">
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
                    Chưa có thiết bị nào trong danh sách
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
              <strong className="text-slate-800 dark:text-slate-200">{totalItems}</strong> thiết bị
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
      {/* MODAL CHI TIẾT THIẾT BỊ — PHONG CÁCH CHUYÊN NGHIỆP GIỐNG TRANG LỊCH SỬ */}
      {/* ===================================================================== */}
      <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
        <DialogContent className="max-w-2xl max-h-[90vh] p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 border-slate-200 dark:border-slate-800 shadow-2xl">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800 pr-8">
            <DialogTitle className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 text-sm sm:text-base">
              <div className="flex items-center gap-2.5">
                <div className={`h-8 w-8 rounded-lg flex items-center justify-center ${
                  selectedDevice && String(selectedDevice.type).toLowerCase().includes('camera')
                    ? 'bg-violet-100 text-violet-700 dark:bg-violet-950/60 dark:text-violet-300'
                    : 'bg-amber-100 text-amber-800 dark:bg-amber-950/60 dark:text-amber-300'
                }`}>
                  {selectedDevice && String(selectedDevice.type).toLowerCase().includes('camera')
                    ? <Camera className="h-4.5 w-4.5" />
                    : <Cpu className="h-4.5 w-4.5" />}
                </div>
                <div>
                  <span className="font-bold text-slate-900 dark:text-white tracking-wide text-base">
                    {selectedDevice?.name || 'Chi Tiết Thiết Bị'}
                  </span>
                  <span className="text-xs text-slate-500 dark:text-slate-400 ml-2 font-mono hidden sm:inline">
                    ({selectedDevice?.code || 'Chưa đặt mã'})
                  </span>
                </div>
              </div>

              {/* Header Badges: Status & Type */}
              <div className="flex items-center gap-1.5 mr-2">
                {selectedDevice && getDeviceTypeBadge(selectedDevice.type)}
                {selectedDevice?.isActive ? (
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

          {selectedDevice && (
            <div className="flex-1 overflow-y-auto p-5 space-y-3.5 text-xs">
              <div className="flex items-center gap-1.5 font-bold text-slate-800 dark:text-slate-200">
                <Info className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                <span>THÔNG SỐ KỸ THUẬT & CẤU HÌNH CHI TIẾT</span>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-xs">
                {/* Card 1: Định danh thiết bị */}
                <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2.5">
                  <div className="flex items-center gap-1.5 font-bold text-blue-600 dark:text-blue-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                    <Layers className="h-4 w-4" />
                    <span>Định Danh Thiết Bị</span>
                  </div>
                  <div className="grid grid-cols-2 gap-2 pt-0.5">
                    <div>
                      <span className="text-slate-400 block text-[11px]">Mã thiết bị:</span>
                      <span className="font-mono font-extrabold text-slate-900 dark:text-white text-sm">
                        {selectedDevice.code || '--'}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Phân loại phần cứng:</span>
                      <div className="mt-0.5">{getDeviceTypeBadge(selectedDevice.type)}</div>
                    </div>
                    <div className="col-span-2">
                      <span className="text-slate-400 block text-[11px]">Tên thiết bị:</span>
                      <span className="font-bold text-slate-900 dark:text-slate-100">
                        {selectedDevice.name}
                      </span>
                    </div>
                    <div className="col-span-2">
                      <span className="text-slate-400 block text-[11px]">Ghi chú kỹ thuật:</span>
                      <span className="text-slate-700 dark:text-slate-300 italic">
                        {selectedDevice.note || 'Không có ghi chú mô tả'}
                      </span>
                    </div>
                  </div>
                </div>

                {/* Card 2: Cấu hình mạng & Xác thực */}
                <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2.5">
                  <div className="flex items-center gap-1.5 font-bold text-emerald-600 dark:text-emerald-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                    <Network className="h-4 w-4" />
                    <span>Cấu Hình Mạng & Xác Thực</span>
                  </div>
                  <div className="grid grid-cols-2 gap-2 pt-0.5">
                    <div>
                      <span className="text-slate-400 block text-[11px]">Địa chỉ IP:</span>
                      <span className="font-mono font-bold text-slate-900 dark:text-emerald-400">
                        {selectedDevice.ipAddress}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Port kết nối:</span>
                      <span className="font-mono font-bold text-slate-900 dark:text-slate-100">
                        {selectedDevice.port}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Tài khoản đăng nhập:</span>
                      <span className="font-mono text-slate-800 dark:text-slate-200">
                        {selectedDevice.userName || '--'}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Mật khẩu kết nối:</span>
                      <div className="flex items-center gap-1.5 mt-0.5">
                        <span className="font-mono text-slate-800 dark:text-slate-200">
                          {selectedDevice.password
                            ? (showPassword ? selectedDevice.password : '••••••••')
                            : '--'}
                        </span>
                        {selectedDevice.password && (
                          <button
                            type="button"
                            onClick={() => setShowPassword(!showPassword)}
                            className="text-[10px] text-blue-600 hover:underline cursor-pointer"
                          >
                            {showPassword ? 'Ẩn' : 'Hiện'}
                          </button>
                        )}
                      </div>
                    </div>
                  </div>
                </div>

                {/* Card 3: Trạng thái Vận hành & Hệ thống */}
                <div className="col-span-1 md:col-span-2 p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2.5">
                  <div className="flex items-center gap-1.5 font-bold text-amber-600 dark:text-amber-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                    <Shield className="h-4 w-4" />
                    <span>Trạng Thái Vận Hành & Kết Nối Thực Tế</span>
                  </div>
                  <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 pt-0.5">
                    <div>
                      <span className="text-slate-400 block text-[11px]">Kết nối mạng thực tế:</span>
                      <div className="mt-1">{getDeviceLiveStatusBadge(selectedDevice.status)}</div>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Cấu hình kích hoạt:</span>
                      <span className="font-semibold text-slate-800 dark:text-slate-200 block mt-1">
                        {selectedDevice.isActive ? '✅ Đang kích hoạt' : '⏸ Đang tạm dừng'}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Phản hồi gần nhất (WinForms):</span>
                      <span className="text-slate-700 dark:text-slate-300 font-mono text-[11px] block mt-1">
                        {selectedDevice.lastHeartbeat
                          ? new Date(selectedDevice.lastHeartbeat).toLocaleString('vi-VN')
                          : 'Chưa có dữ liệu'}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Ngày tạo:</span>
                      <span className="text-slate-600 dark:text-slate-400 font-mono text-[11px] block mt-1">
                        {selectedDevice.createdAt ? new Date(selectedDevice.createdAt).toLocaleString('vi-VN') : '--'}
                      </span>
                    </div>
                    {selectedDevice.errorMessage && (
                      <div className="col-span-2 sm:col-span-4 mt-1 p-2 rounded-lg bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-900/60 text-rose-700 dark:text-rose-300 text-[11px]">
                        <strong>⚠️ Chi tiết lỗi kết nối:</strong> {selectedDevice.errorMessage}
                      </div>
                    )}
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
                if (selectedDevice) {
                  const dev = selectedDevice
                  setIsDetailOpen(false)
                  openEditModal(dev)
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
        <DialogContent className="max-w-lg p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 max-h-[90vh]">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800">
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Plus className="h-5 w-5 text-blue-600" />
              Thêm Thiết Bị Mới
            </DialogTitle>
          </DialogHeader>
          <div className="flex-1 overflow-y-auto p-5">
            {renderFormFields()}
          </div>
          <DialogFooter className="p-4 pt-3 border-t border-slate-200 dark:border-slate-800 gap-2 bg-slate-50/50 dark:bg-slate-900/50">
            <Button variant="outline" size="sm" onClick={() => setIsCreateOpen(false)} className="text-xs cursor-pointer">Hủy</Button>
            <Button size="sm" disabled={!formName.trim() || !formIp.trim() || createMutation.isPending}
              onClick={() => createMutation.mutate()}
              className="bg-blue-600 hover:bg-blue-700 text-white text-xs cursor-pointer">
              {createMutation.isPending ? 'Đang lưu...' : 'Lưu Thiết Bị'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* MODAL CHỈNH SỬA */}
      <Dialog open={isEditOpen} onOpenChange={setIsEditOpen}>
        <DialogContent className="max-w-lg p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 max-h-[90vh]">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800">
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Edit className="h-5 w-5 text-blue-600" />
              Chỉnh Sửa Thiết Bị
            </DialogTitle>
          </DialogHeader>
          <div className="flex-1 overflow-y-auto p-5">
            {renderFormFields()}
          </div>
          <DialogFooter className="p-4 pt-3 border-t border-slate-200 dark:border-slate-800 gap-2 bg-slate-50/50 dark:bg-slate-900/50">
            <Button variant="outline" size="sm" onClick={() => setIsEditOpen(false)} className="text-xs cursor-pointer">Hủy</Button>
            <Button size="sm" disabled={!formName.trim() || !formIp.trim() || updateMutation.isPending}
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
            <span>Đã chọn <strong className="text-blue-600 dark:text-blue-400 font-mono text-sm">{selectedIds.length}</strong> thiết bị</span>
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
        title={deleteConfirm.isBatch ? 'Xác Nhận Xóa Nhiều Thiết Bị' : 'Xác Nhận Xóa Thiết Bị'}
        description={
          deleteConfirm.isBatch ? (
            <span>
              Bạn có chắc chắn muốn xóa{' '}
              <strong className="text-red-600 dark:text-red-400 font-semibold">
                {selectedIds.length} thiết bị
              </strong>{' '}
              đã chọn? Dữ liệu sẽ được lưu trữ trong thùng rác hệ thống.
            </span>
          ) : (
            <span>
              Bạn có chắc chắn muốn xóa thiết bị{' '}
              <strong className="text-slate-900 dark:text-slate-100 font-semibold">
                [{deleteConfirm.name}]
              </strong>
              ? Dữ liệu sẽ được chuyển vào thùng rác.
            </span>
          )
        }
        confirmText={deleteConfirm.isBatch ? `Xóa ${selectedIds.length} Thiết Bị` : 'Xác Nhận Xóa'}
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
