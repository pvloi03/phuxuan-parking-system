import { useState, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Camera,
  Cpu,
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
  FileSpreadsheet,
  Wifi,
  WifiOff,
  Network,
  MapPin,
} from 'lucide-react'
import { apiClient } from '@/services/apiClient'
import type { PagedResult, Device, DeviceType } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { exportToExcel, parseExcelFile, downloadExcelTemplate } from '@/lib/excelHelper'

// =====================================================================
// Helper functions
// =====================================================================
function getDeviceTypeLabel(type?: DeviceType | string | number) {
  const v = String(type ?? '').toLowerCase()
  if (v === 'camera' || v === '1') return 'Camera IP'
  if (v === 'controller' || v === '2') return 'Bộ Điều Khiển'
  return String(type ?? 'Khác')
}

function getDeviceTypeBadge(type?: DeviceType | string | number) {
  const label = getDeviceTypeLabel(type)
  const v = String(type ?? '').toLowerCase()

  if (v === 'camera' || v === '1') {
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
  if (v === 'controller' || v === '2') {
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

// Default port theo loại thiết bị
function getDefaultPort(type: DeviceType): number {
  if (type === 'Camera') return 8000
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

  // Form State
  const [formCode, setFormCode] = useState('')
  const [formName, setFormName] = useState('')
  const [formType, setFormType] = useState<DeviceType>('Camera')
  const [formIp, setFormIp] = useState('')
  const [formPort, setFormPort] = useState(8000)
  const [formUser, setFormUser] = useState('')
  const [formPass, setFormPass] = useState('')
  const [formLaneId, setFormLaneId] = useState('')
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
        laneId: formLaneId.trim() || undefined,
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
        laneId: formLaneId.trim() || undefined,
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
    setFormType('Camera')
    setFormIp('')
    setFormPort(8000)
    setFormUser('')
    setFormPass('')
    setFormLaneId('')
    setFormNote('')
    setFormIsActive(true)
    setSelectedDevice(null)
  }

  const openEditModal = (device: Device) => {
    setSelectedDevice(device)
    setFormCode(device.code || '')
    setFormName(device.name || '')
    setFormType((device.type as DeviceType) || 'Camera')
    setFormIp(device.ipAddress || '')
    setFormPort(device.port || 8000)
    setFormUser(device.userName || '')
    setFormPass(device.password || '')
    setFormLaneId(device.laneId || '')
    setFormNote(device.note || '')
    setFormIsActive(device.isActive ?? true)
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
      'ID Làn': d.laneId || '',
      'Ghi Chú': d.note || '',
      'Trạng Thái': d.isActive ? 'Đang hoạt động' : 'Tạm dừng',
    }))

    exportToExcel(exportData, `Danh_Sach_Thiet_Bi_${new Date().toISOString().slice(0, 10)}.xlsx`, 'ThietBi')
  }

  const handleDownloadTemplate = () => {
    const template = [
      {
        'Mã Thiết Bị': 'CAM-LV-01',
        'Tên Thiết Bị': 'Camera Làn Vào Cổng Chính',
        'Loại Thiết Bị (Camera/Controller)': 'Camera',
        'Địa Chỉ IP': '192.168.1.101',
        'Port': '8000',
        'Tên Đăng Nhập': 'admin',
        'Mật Khẩu': 'Abc@12345',
        'ID Làn': 'LANE-IN-01',
        'Ghi Chú': 'Camera Hikvision DS-2CD2143G2-I',
      },
      {
        'Mã Thiết Bị': 'CTRL-LV-01',
        'Tên Thiết Bị': 'Controller Làn Vào Cổng Chính',
        'Loại Thiết Bị (Camera/Controller)': 'Controller',
        'Địa Chỉ IP': '192.168.1.201',
        'Port': '4370',
        'Tên Đăng Nhập': '',
        'Mật Khẩu': '',
        'ID Làn': 'LANE-IN-01',
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
        const rawType = String(row['Loại Thiết Bị (Camera/Controller)'] || row['type'] || '').toLowerCase()
        const deviceType: DeviceType = rawType.includes('controller') ? 'Controller' : 'Camera'
        const port = Number(row['Port'] || row['port'] || (deviceType === 'Camera' ? 8000 : 4370))

        return {
          code: String(row['Mã Thiết Bị'] || row['code'] || '').trim(),
          name: String(row['Tên Thiết Bị'] || row['name'] || '').trim(),
          type: deviceType,
          ipAddress: String(row['Địa Chỉ IP'] || row['ip'] || '').trim(),
          port,
          userName: String(row['Tên Đăng Nhập'] || row['userName'] || '').trim() || undefined,
          password: String(row['Mật Khẩu'] || row['password'] || '').trim() || undefined,
          laneId: String(row['ID Làn'] || row['laneId'] || '').trim() || undefined,
          note: String(row['Ghi Chú'] || row['note'] || '').trim() || undefined,
          isActive: true,
        }
      }).filter((d) => d.name)

      if (!formattedData.length) { alert('Không tìm thấy bản ghi hợp lệ (Cần có cột Tên Thiết Bị).'); return }

      if (confirm(`Đã đọc ${formattedData.length} thiết bị từ file Excel. Bạn có muốn lưu vào hệ thống?`)) {
        batchImportMutation.mutate(formattedData as Device[])
      }
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
          <Input placeholder="VD: CAM-LV-01" value={formCode} onChange={(e) => setFormCode(e.target.value)} className="text-xs" />
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
            <option value="Camera">📷 Camera IP</option>
            <option value="Controller">🖥️ Bộ Điều Khiển</option>
          </select>
        </div>
      </div>

      <div className="space-y-1">
        <label className="font-semibold text-slate-700 dark:text-slate-300">Tên mô tả thiết bị *</label>
        <Input placeholder="VD: Camera Làn Vào Cổng Chính" value={formName} onChange={(e) => setFormName(e.target.value)} className="text-xs" />
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">Địa chỉ IP *</label>
          <Input
            placeholder="VD: 192.168.1.101"
            value={formIp}
            onChange={(e) => setFormIp(e.target.value)}
            className="text-xs font-mono"
          />
        </div>
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">Port kết nối</label>
          <Input
            type="number"
            placeholder="8000"
            value={formPort}
            onChange={(e) => setFormPort(Number(e.target.value))}
            className="text-xs font-mono"
          />
        </div>
      </div>

      {formType === 'Camera' && (
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
        <label className="font-semibold text-slate-700 dark:text-slate-300">ID Làn kiểm soát (Lane ID)</label>
        <Input placeholder="VD: LANE-IN-01" value={formLaneId} onChange={(e) => setFormLaneId(e.target.value)} className="text-xs font-mono" />
      </div>

      <div className="space-y-1">
        <label className="font-semibold text-slate-700 dark:text-slate-300">Ghi chú kỹ thuật</label>
        <Input placeholder="VD: Hikvision DS-2CD2143G2-I, IR 40m" value={formNote} onChange={(e) => setFormNote(e.target.value)} className="text-xs" />
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
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
            Quản Lý Thiết Bị Phần Cứng
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
            Camera IP giám sát & Bộ điều khiển Barrier tại các làn kiểm soát ra vào
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {selectedIds.length > 0 && (
            <Button
              size="sm"
              variant="destructive"
              onClick={() => {
                if (confirm(`Xóa ${selectedIds.length} thiết bị đã chọn?`))
                  batchDeleteMutation.mutate(selectedIds)
              }}
              disabled={batchDeleteMutation.isPending}
              className="gap-1.5 text-xs font-semibold cursor-pointer"
            >
              <Trash2 className="h-4 w-4" />
              Xóa {selectedIds.length} Đã Chọn
            </Button>
          )}

          <input type="file" ref={fileInputRef} onChange={handleFileChange} accept=".xlsx,.xls,.csv" className="hidden" />
          <Button variant="outline" size="sm" onClick={() => fileInputRef.current?.click()}
            className="gap-1.5 text-xs text-slate-700 dark:text-slate-300 border-slate-200 dark:border-slate-700 cursor-pointer shadow-xs">
            <Upload className="h-3.5 w-3.5 text-emerald-600" /> Nhập Excel
          </Button>
          <Button variant="outline" size="sm" onClick={handleDownloadTemplate}
            className="gap-1.5 text-xs text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-700 cursor-pointer shadow-xs" title="Tải file mẫu Excel">
            <FileSpreadsheet className="h-3.5 w-3.5 text-blue-500" /> Tải Mẫu
          </Button>
          <Button variant="outline" size="sm" onClick={handleExportExcel}
            className="gap-1.5 text-xs text-slate-700 dark:text-slate-300 border-slate-200 dark:border-slate-700 cursor-pointer shadow-xs">
            <Download className="h-3.5 w-3.5 text-blue-600" /> Xuất Excel
          </Button>
          <Button
            size="sm"
            onClick={() => { resetForm(); setIsCreateOpen(true) }}
            className="gap-2 text-xs font-semibold bg-blue-600 hover:bg-blue-700 text-white cursor-pointer shadow-xs"
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
                placeholder="Tìm theo mã, tên hoặc địa chỉ IP..."
                value={search}
                onChange={(e) => { setSearch(e.target.value); setPageNumber(1) }}
                className="pl-9 text-xs"
              />
            </div>
            <div className="w-52">
              <select
                value={typeFilter}
                onChange={(e) => { setTypeFilter(e.target.value); setPageNumber(1) }}
                className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
              >
                <option value="">-- Tất cả thiết bị --</option>
                <option value="Camera">📷 Camera IP</option>
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
                <th className="p-3.5">Mã / Loại</th>
                <th className="p-3.5">Tên Thiết Bị</th>
                <th className="p-3.5">Địa Chỉ Mạng</th>
                <th className="p-3.5">ID Làn</th>
                <th className="p-3.5">Trạng Thái</th>
                <th className="p-3.5 text-right pr-4">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {isLoading ? (
                <tr><td colSpan={7} className="p-8 text-center text-slate-400">Đang tải danh sách thiết bị...</td></tr>
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
                      <td className="p-3.5">
                        <div className="space-y-1">
                          <div className="font-mono font-semibold text-slate-800 dark:text-slate-200 text-xs">
                            {device.code || '--'}
                          </div>
                          {getDeviceTypeBadge(device.type)}
                        </div>
                      </td>
                      <td className="p-3.5">
                        <div className="font-semibold text-slate-900 dark:text-slate-100">
                          {device.name}
                        </div>
                        {device.note && (
                          <div className="text-[11px] text-slate-400 dark:text-slate-500 mt-0.5 max-w-[240px] truncate" title={device.note}>
                            {device.note}
                          </div>
                        )}
                      </td>
                      <td className="p-3.5 font-mono text-slate-600 dark:text-slate-400">
                        <div className="flex items-center gap-1.5">
                          <Network className="h-3.5 w-3.5 text-slate-400 flex-shrink-0" />
                          <span className="font-medium">{device.ipAddress}:{device.port}</span>
                        </div>
                        {device.userName && (
                          <div className="text-[11px] text-slate-400 dark:text-slate-500 mt-0.5">
                            👤 {device.userName}
                          </div>
                        )}
                      </td>
                      <td className="p-3.5">
                        {device.laneId ? (
                          <div className="flex items-center gap-1">
                            <MapPin className="h-3.5 w-3.5 text-slate-400 flex-shrink-0" />
                            <Badge variant="secondary" className="font-mono text-[11px] px-2 py-0.5 bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300">
                              {device.laneId}
                            </Badge>
                          </div>
                        ) : (
                          <span className="text-slate-400 dark:text-slate-500 italic text-[11px]">Chưa gán</span>
                        )}
                      </td>
                      <td className="p-3.5">
                        {device.isActive ? (
                          <span className="inline-flex items-center gap-1 text-emerald-600 dark:text-emerald-400 font-medium text-[11px]">
                            <Wifi className="h-3 w-3" /> Đang dùng
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1 text-slate-400 dark:text-slate-500 font-medium text-[11px]">
                            <WifiOff className="h-3 w-3" /> Tạm dừng
                          </span>
                        )}
                      </td>
                      <td className="p-3.5 text-right pr-4">
                        <div className="flex items-center justify-end gap-1.5">
                          <Button size="sm" variant="outline"
                            onClick={() => { setSelectedDevice(device); setIsDetailOpen(true) }}
                            className="h-7 px-2 text-xs text-blue-600 hover:text-blue-700 dark:text-blue-400 border-blue-200 dark:border-blue-900/60 bg-blue-50/50 dark:bg-blue-950/40 cursor-pointer">
                            <Eye className="h-3.5 w-3.5 mr-1" /> Chi tiết
                          </Button>
                          <Button size="sm" variant="outline" onClick={() => openEditModal(device)}
                            className="h-7 w-7 p-0 text-slate-600 hover:text-slate-900 dark:text-slate-400 cursor-pointer" title="Chỉnh sửa">
                            <Edit className="h-3.5 w-3.5" />
                          </Button>
                          <Button size="sm" variant="ghost"
                            onClick={() => { if (confirm(`Xóa thiết bị [${device.name}]?`)) deleteMutation.mutate(device.id) }}
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
                  <td colSpan={7} className="p-8 text-center text-slate-400 italic">
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

      {/* MODAL CHI TIẾT */}
      <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
        <DialogContent className="max-w-md bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              {selectedDevice && String(selectedDevice.type).toLowerCase() === 'camera'
                ? <Camera className="h-5 w-5 text-violet-600" />
                : <Cpu className="h-5 w-5 text-amber-600" />}
              Chi Tiết Thiết Bị
            </DialogTitle>
          </DialogHeader>
          {selectedDevice && (
            <div className="space-y-3 py-2 text-xs">
              <div className="p-3 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2">
                <div className="grid grid-cols-2 gap-2">
                  <div><span className="text-slate-400 block text-[11px]">Mã thiết bị:</span><span className="font-mono font-bold text-slate-900 dark:text-slate-100">{selectedDevice.code || '--'}</span></div>
                  <div><span className="text-slate-400 block text-[11px]">Loại thiết bị:</span><div className="mt-1">{getDeviceTypeBadge(selectedDevice.type)}</div></div>
                  <div className="col-span-2"><span className="text-slate-400 block text-[11px]">Tên mô tả:</span><span className="font-bold text-slate-900 dark:text-slate-100">{selectedDevice.name}</span></div>
                  <div><span className="text-slate-400 block text-[11px]">Địa chỉ IP:</span><span className="font-mono text-slate-800 dark:text-slate-200">{selectedDevice.ipAddress}</span></div>
                  <div><span className="text-slate-400 block text-[11px]">Port kết nối:</span><span className="font-mono text-slate-800 dark:text-slate-200">{selectedDevice.port}</span></div>
                  {selectedDevice.userName && <div><span className="text-slate-400 block text-[11px]">Tên đăng nhập:</span><span className="font-mono text-slate-800 dark:text-slate-200">{selectedDevice.userName}</span></div>}
                  <div><span className="text-slate-400 block text-[11px]">ID Làn kiểm soát:</span>
                    {selectedDevice.laneId ? (
                      <span className="font-mono font-medium text-slate-800 dark:text-slate-200">{selectedDevice.laneId}</span>
                    ) : (
                      <span className="text-slate-400 italic">Chưa gán</span>
                    )}
                  </div>
                  {selectedDevice.note && <div className="col-span-2"><span className="text-slate-400 block text-[11px]">Ghi chú kỹ thuật:</span><span className="italic text-slate-600 dark:text-slate-400">{selectedDevice.note}</span></div>}
                  <div><span className="text-slate-400 block text-[11px]">Trạng thái:</span>
                    {selectedDevice.isActive
                      ? <span className="text-emerald-600 dark:text-emerald-400 font-medium">✅ Đang hoạt động</span>
                      : <span className="text-slate-400 font-medium">⏸ Tạm dừng</span>}
                  </div>
                </div>
              </div>
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" size="sm" onClick={() => setIsDetailOpen(false)} className="text-xs cursor-pointer">Đóng</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* MODAL THÊM MỚI */}
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="max-w-lg bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Plus className="h-5 w-5 text-blue-600" />
              Thêm Thiết Bị Mới
            </DialogTitle>
          </DialogHeader>
          {renderFormFields()}
          <DialogFooter className="gap-2">
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
        <DialogContent className="max-w-lg bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Edit className="h-5 w-5 text-blue-600" />
              Chỉnh Sửa Thiết Bị
            </DialogTitle>
          </DialogHeader>
          {renderFormFields()}
          <DialogFooter className="gap-2">
            <Button variant="outline" size="sm" onClick={() => setIsEditOpen(false)} className="text-xs cursor-pointer">Hủy</Button>
            <Button size="sm" disabled={!formName.trim() || !formIp.trim() || updateMutation.isPending}
              onClick={() => updateMutation.mutate()}
              className="bg-blue-600 hover:bg-blue-700 text-white text-xs cursor-pointer">
              {updateMutation.isPending ? 'Đang cập nhật...' : 'Cập Nhật'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
