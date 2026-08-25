import { useState, useMemo, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Search,
  Plus,
  Trash2,
  Edit,
  FileText,
  RefreshCw,
  Download,
  Upload,
  FileSpreadsheet,
  CheckCircle2,
  XCircle,
  ArrowDownToDot,
  ArrowUpFromDot,
  Camera,
  Cpu,
  Layers,
  Info,
  Route,
} from 'lucide-react'
import { apiClient } from '@/services/apiClient'
import type { Lane, Device } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'
import { exportToExcel, parseExcelFile, downloadExcelTemplate } from '@/lib/excelHelper'

export function LanesPage() {
  const queryClient = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [search, setSearch] = useState('')
  const [directionFilter, setDirectionFilter] = useState<string>('')
  const [selectedIds, setSelectedIds] = useState<string[]>([])

  // Modal states
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [isEditOpen, setIsEditOpen] = useState(false)
  const [isDetailOpen, setIsDetailOpen] = useState(false)
  const [selectedLane, setSelectedLane] = useState<Lane | null>(null)
  const [deleteConfirm, setDeleteConfirm] = useState<{
    isOpen: boolean
    id?: string
    name?: string
    isBatch?: boolean
  }>({ isOpen: false })

  // Form states
  const [formCode, setFormCode] = useState('')
  const [formName, setFormName] = useState('')
  const [formDirection, setFormDirection] = useState<'In' | 'Out'>('In')
  const [formDescription, setFormDescription] = useState('')
  const [formPlateCameraId, setFormPlateCameraId] = useState('')
  const [formOverviewCameraId, setFormOverviewCameraId] = useState('')
  const [formControllerId, setFormControllerId] = useState('')
  const [formAuxPort, setFormAuxPort] = useState<number>(1)
  const [formIsActive, setFormIsActive] = useState(true)

  // 1. Query danh sách thiết bị để gán vào làn
  const { data: devicesData } = useQuery({
    queryKey: ['devices-lookup-for-lanes'],
    queryFn: async () => {
      const res = await apiClient.get<{ data: { items: Device[] } }>('/devices', {
        params: { pageSize: 100 },
      })
      return res.data.data?.items || []
    },
    staleTime: 60000,
  })
  const devices = devicesData || []

  // Phân loại thiết bị theo từng loại
  const plateCameras = useMemo(
    () => devices.filter((d) => String(d.type) === 'PlateCamera' || String(d.type) === '1'),
    [devices]
  )
  const overviewCameras = useMemo(
    () => devices.filter((d) => String(d.type) === 'OverviewCamera' || String(d.type) === '2'),
    [devices]
  )
  const controllers = useMemo(
    () => devices.filter((d) => String(d.type) === 'Controller' || String(d.type) === '3'),
    [devices]
  )

  const deviceMap = useMemo(() => {
    const map = new Map<string, Device>()
    devices.forEach((d) => map.set(d.id, d))
    return map
  }, [devices])

  // 2. Query danh sách làn kiểm soát
  const { data: lanesData, isLoading } = useQuery({
    queryKey: ['lanes-list', search, directionFilter],
    queryFn: async () => {
      const res = await apiClient.get<{ data: Lane[] }>('/lanes', {
        params: {
          search: search || undefined,
          direction: directionFilter || undefined,
        },
      })
      return res.data.data || []
    },
  })
  const lanes = lanesData || []

  // Thống kê nhanh
  const stats = useMemo(() => {
    const total = lanes.length
    const inCount = lanes.filter((l) => l.direction === 'In' || l.direction === 1).length
    const outCount = lanes.filter((l) => l.direction === 'Out' || l.direction === 2).length
    const activeCount = lanes.filter((l) => l.isActive).length
    return { total, inCount, outCount, activeCount }
  }, [lanes])

  // ======================== Mutations ========================
  const createMutation = useMutation({
    mutationFn: async () => {
      await apiClient.post('/lanes', {
        code: formCode.trim().toUpperCase(),
        name: formName.trim(),
        direction: formDirection,
        description: formDescription.trim() || undefined,
        plateCameraDeviceId: formPlateCameraId || undefined,
        overviewCameraDeviceId: formOverviewCameraId || undefined,
        controllerDeviceId: formControllerId || undefined,
        triggerAuxPort: formAuxPort,
        isActive: formIsActive,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lanes-list'] })
      setIsCreateOpen(false)
      resetForm()
    },
    onError: (err: any) => {
      alert('Lỗi thêm làn kiểm soát: ' + (err?.response?.data?.message || err.message))
    },
  })

  const updateMutation = useMutation({
    mutationFn: async () => {
      if (!selectedLane) return
      await apiClient.put(`/lanes/${selectedLane.id}`, {
        code: formCode.trim().toUpperCase(),
        name: formName.trim(),
        direction: formDirection,
        description: formDescription.trim() || undefined,
        plateCameraDeviceId: formPlateCameraId || undefined,
        overviewCameraDeviceId: formOverviewCameraId || undefined,
        controllerDeviceId: formControllerId || undefined,
        triggerAuxPort: formAuxPort,
        isActive: formIsActive,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lanes-list'] })
      setIsEditOpen(false)
      resetForm()
    },
    onError: (err: any) => {
      alert('Lỗi cập nhật làn kiểm soát: ' + (err?.response?.data?.message || err.message))
    },
  })

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/lanes/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lanes-list'] })
    },
  })

  const batchDeleteMutation = useMutation({
    mutationFn: async (ids: string[]) => {
      await apiClient.post('/lanes/delete-batch', ids)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lanes-list'] })
      setSelectedIds([])
    },
  })

  // Helpers
  const resetForm = () => {
    setFormCode('')
    setFormName('')
    setFormDirection('In')
    setFormDescription('')
    setFormPlateCameraId('')
    setFormOverviewCameraId('')
    setFormControllerId('')
    setFormAuxPort(1)
    setFormIsActive(true)
    setSelectedLane(null)
  }

  const openEditModal = (lane: Lane) => {
    setSelectedLane(lane)
    setFormCode(lane.code || '')
    setFormName(lane.name || '')
    setFormDirection(lane.direction === 'Out' || lane.direction === 2 ? 'Out' : 'In')
    setFormDescription(lane.description || '')
    setFormPlateCameraId(lane.plateCameraDeviceId || '')
    setFormOverviewCameraId(lane.overviewCameraDeviceId || '')
    setFormControllerId(lane.controllerDeviceId || '')
    setFormAuxPort(lane.triggerAuxPort || 1)
    setFormIsActive(lane.isActive ?? true)
    setIsEditOpen(true)
  }

  // Selection handlers
  const handleToggleSelect = (id: string) => {
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id]
    )
  }

  const handleSelectAll = () => {
    if (!lanes.length) return
    const allIds = lanes.map((l) => l.id)
    const allSelected = allIds.every((id) => selectedIds.includes(id))

    if (allSelected) {
      setSelectedIds([])
    } else {
      setSelectedIds(allIds)
    }
  }

  const isAllSelected = lanes.length > 0 && lanes.every((l) => selectedIds.includes(l.id))

  // Excel Handlers
  const handleExportExcel = () => {
    if (!lanes.length) {
      alert('Không có dữ liệu làn để xuất Excel.')
      return
    }

    const exportData = lanes.map((l, index) => {
      const plCam = l.plateCameraDeviceId ? deviceMap.get(l.plateCameraDeviceId) : null
      const ovCam = l.overviewCameraDeviceId ? deviceMap.get(l.overviewCameraDeviceId) : null
      const ctrl = l.controllerDeviceId ? deviceMap.get(l.controllerDeviceId) : null

      return {
        STT: index + 1,
        'Mã Làn': l.code,
        'Tên Làn': l.name,
        'Chiều Làn': l.direction === 'In' || l.direction === 1 ? 'Làn Vào (In)' : 'Làn Ra (Out)',
        'Camera Biển Số': plCam ? `${plCam.name} (${plCam.ipAddress}:${plCam.port})` : 'Chưa gán',
        'Camera Toàn Cảnh': ovCam ? `${ovCam.name} (${ovCam.ipAddress}:${ovCam.port})` : 'Chưa gán',
        'Bộ Điều Khiển': ctrl ? `${ctrl.name} (${ctrl.ipAddress}) - Cổng Aux ${l.triggerAuxPort}` : 'Chưa gán',
        'Mô Tả': l.description || '',
        'Trạng Thái': l.isActive ? 'Đang hoạt động' : 'Tạm dừng',
      }
    })

    exportToExcel(exportData, `Danh_Sach_Lan_Kiem_Soat_${new Date().toISOString().slice(0, 10)}.xlsx`, 'LanKiemSoat')
  }

  const handleDownloadTemplate = () => {
    const template = [
      {
        'Mã Làn': 'L01',
        'Tên Làn': 'Làn Vào 1 (Cổng Chính)',
        'Chiều Làn (In/Out)': 'In',
        'Cổng Tín Hiệu Aux In (1/2)': '1',
        'Mô Tả': 'Làn kiểm soát xe vào cổng chính',
      },
      {
        'Mã Làn': 'L02',
        'Tên Làn': 'Làn Ra 1 (Cổng Chính)',
        'Chiều Làn (In/Out)': 'Out',
        'Cổng Tín Hiệu Aux In (1/2)': '2',
        'Mô Tả': 'Làn kiểm soát xe ra cổng chính',
      },
    ]
    downloadExcelTemplate(template, 'Mau_Nhap_Lan_Kiem_Soat.xlsx')
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

      alert(`Tính năng nhập Excel làn kiểm soát: Đã đọc được ${rawData.length} dòng dữ liệu.`)
    } catch (err: any) {
      alert('Lỗi đọc file Excel: ' + err.message)
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  // Render Form Fields
  const renderFormFields = () => (
    <div className="space-y-3.5 py-1 text-xs">
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">
            Mã làn kiểm soát *
          </label>
          <Input
            placeholder="VD: L01, L02"
            value={formCode}
            onChange={(e) => setFormCode(e.target.value.toUpperCase())}
            className="text-xs font-mono font-bold"
          />
        </div>
        <div className="space-y-1">
          <label className="font-semibold text-slate-700 dark:text-slate-300">
            Chiều kiểm soát *
          </label>
          <select
            value={formDirection}
            onChange={(e) => {
              const dir = e.target.value as 'In' | 'Out'
              setFormDirection(dir)
              setFormAuxPort(dir === 'In' ? 1 : 2)
            }}
            className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
          >
            <option value="In">📥 Làn Vào (Check-in)</option>
            <option value="Out">📤 Làn Ra (Check-out)</option>
          </select>
        </div>
      </div>

      <div className="space-y-1">
        <label className="font-semibold text-slate-700 dark:text-slate-300">
          Tên làn kiểm soát *
        </label>
        <Input
          placeholder="VD: Làn Vào Số 1 (Cổng Chính)"
          value={formName}
          onChange={(e) => setFormName(e.target.value)}
          className="text-xs font-medium"
        />
      </div>

      {/* Cấu hình Camera */}
      <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2.5">
        <div className="flex items-center gap-1.5 font-bold text-blue-600 dark:text-blue-400">
          <Camera className="h-4 w-4" />
          <span>Cấu Hình Camera Chụp Ảnh</span>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div className="space-y-1">
            <label className="font-medium text-slate-600 dark:text-slate-400 text-[11px]">
              📷 Camera Biển Số (Plate Camera)
            </label>
            <select
              value={formPlateCameraId}
              onChange={(e) => setFormPlateCameraId(e.target.value)}
              className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-2.5 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
            >
              <option value="">-- Chưa gán camera biển số --</option>
              {plateCameras.map((cam) => (
                <option key={cam.id} value={cam.id}>
                  {cam.name} ({cam.ipAddress}:{cam.port})
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-1">
            <label className="font-medium text-slate-600 dark:text-slate-400 text-[11px]">
              🌐 Camera Toàn Cảnh (Overview Camera)
            </label>
            <select
              value={formOverviewCameraId}
              onChange={(e) => setFormOverviewCameraId(e.target.value)}
              className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-2.5 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
            >
              <option value="">-- Chưa gán camera toàn cảnh --</option>
              {overviewCameras.map((cam) => (
                <option key={cam.id} value={cam.id}>
                  {cam.name} ({cam.ipAddress}:{cam.port})
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Cấu hình Controller Access Control */}
      <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2.5">
        <div className="flex items-center gap-1.5 font-bold text-amber-600 dark:text-amber-400">
          <Cpu className="h-4 w-4" />
          <span>Bộ Điều Khiển Access Control & Tín Hiệu Cảm Biến</span>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div className="space-y-1">
            <label className="font-medium text-slate-600 dark:text-slate-400 text-[11px]">
              🕹 Bộ Điều Khiển Access Control
            </label>
            <select
              value={formControllerId}
              onChange={(e) => setFormControllerId(e.target.value)}
              className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-2.5 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
            >
              <option value="">-- Chưa gán controller --</option>
              {controllers.map((ctrl) => (
                <option key={ctrl.id} value={ctrl.id}>
                  {ctrl.name} ({ctrl.ipAddress})
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-1">
            <label className="font-medium text-slate-600 dark:text-slate-400 text-[11px]">
              ⚡ Cổng Tín Hiệu Aux In (Sensor Trigger)
            </label>
            <select
              value={formAuxPort}
              onChange={(e) => setFormAuxPort(Number(e.target.value))}
              className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-2.5 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
            >
              <option value={1}>Cổng Aux In 1 (Mặc định Làn Vào)</option>
              <option value={2}>Cổng Aux In 2 (Mặc định Làn Ra)</option>
              <option value={3}>Cổng Aux In 3</option>
              <option value={4}>Cổng Aux In 4</option>
            </select>
          </div>
        </div>
      </div>

      <div className="space-y-1">
        <label className="font-semibold text-slate-700 dark:text-slate-300">Mô tả làn</label>
        <Input
          placeholder="VD: Làn kiểm soát xe máy / ô tô vào cổng số 1"
          value={formDescription}
          onChange={(e) => setFormDescription(e.target.value)}
          className="text-xs"
        />
      </div>

      <div className="flex items-center gap-2 pt-1">
        <input
          type="checkbox"
          id="isLaneActive"
          checked={formIsActive}
          onChange={(e) => setFormIsActive(e.target.checked)}
          className="rounded border-slate-300 text-blue-600 focus:ring-blue-500 cursor-pointer h-3.5 w-3.5"
        />
        <label htmlFor="isLaneActive" className="text-slate-700 dark:text-slate-300 font-medium cursor-pointer">
          Đang hoạt động (Cho phép tiếp nhận sự kiện ra/vào và xử lý tín hiệu cảm biến)
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
            Quản Lý Làn Kiểm Soát
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-1 leading-relaxed">
            Cấu hình phân bổ camera nhận diện biển số, camera toàn cảnh và bộ điều khiển kiểm soát vào ra (Access Control) theo từng làn
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
            Thêm Làn Mới
          </Button>
        </div>
      </div>

      {/* STATS CARDS */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <Card className="border-slate-200 dark:border-slate-800 shadow-xs">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                Tổng Số Làn
              </p>
              <p className="text-2xl font-black tracking-tight text-slate-900 dark:text-slate-100 mt-1">
                {stats.total}
              </p>
              <span className="text-[11px] text-slate-400">Làn kiểm soát cổng</span>
            </div>
            <div className="h-11 w-11 rounded-xl bg-blue-50 dark:bg-blue-950/60 text-blue-600 dark:text-blue-400 flex items-center justify-center">
              <Route className="h-5 w-5" />
            </div>
          </CardContent>
        </Card>

        <Card className="border-slate-200 dark:border-slate-800 shadow-xs">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wider text-emerald-600 dark:text-emerald-400">
                Làn Vào (In)
              </p>
              <p className="text-2xl font-black tracking-tight text-emerald-600 dark:text-emerald-400 mt-1">
                {stats.inCount}
              </p>
              <span className="text-[11px] text-slate-400">Cổng tiếp nhận check-in</span>
            </div>
            <div className="h-11 w-11 rounded-xl bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 flex items-center justify-center">
              <ArrowDownToDot className="h-5 w-5" />
            </div>
          </CardContent>
        </Card>

        <Card className="border-slate-200 dark:border-slate-800 shadow-xs">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wider text-purple-600 dark:text-purple-400">
                Làn Ra (Out)
              </p>
              <p className="text-2xl font-black tracking-tight text-purple-600 dark:text-purple-400 mt-1">
                {stats.outCount}
              </p>
              <span className="text-[11px] text-slate-400">Cổng kiểm soát check-out</span>
            </div>
            <div className="h-11 w-11 rounded-xl bg-purple-50 dark:bg-purple-950/60 text-purple-600 dark:text-purple-400 flex items-center justify-center">
              <ArrowUpFromDot className="h-5 w-5" />
            </div>
          </CardContent>
        </Card>

        <Card className="border-slate-200 dark:border-slate-800 shadow-xs">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wider text-blue-600 dark:text-blue-400">
                Đang Hoạt Động
              </p>
              <p className="text-2xl font-black tracking-tight text-slate-900 dark:text-slate-100 mt-1">
                {stats.activeCount}
              </p>
              <span className="text-[11px] text-emerald-600 font-medium">Sẵn sàng vận hành</span>
            </div>
            <div className="h-11 w-11 rounded-xl bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 flex items-center justify-center">
              <CheckCircle2 className="h-5 w-5" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filter & Search Bar */}
      <Card className="shadow-xs border-slate-200 dark:border-slate-800">
        <CardContent className="p-4 flex flex-col md:flex-row items-center justify-between gap-3">
          <div className="flex flex-1 items-center gap-3 w-full">
            <div className="relative flex-1 max-w-sm">
              <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
              <Input
                placeholder="Tìm theo tên làn, mã làn hoặc mô tả..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="pl-9 text-xs"
              />
            </div>

            <div className="w-48">
              <select
                value={directionFilter}
                onChange={(e) => setDirectionFilter(e.target.value)}
                className="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 text-xs text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
              >
                <option value="">-- Tất cả chiều làn --</option>
                <option value="In">📥 Làn Vào (Check-in)</option>
                <option value="Out">📤 Làn Ra (Check-out)</option>
              </select>
            </div>
          </div>

          {(search || directionFilter) && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                setSearch('')
                setDirectionFilter('')
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
                <th className="p-3.5">Mã Làn</th>
                <th className="p-3.5">Tên Làn Kiểm Soát</th>
                <th className="p-3.5 text-center">Chiều Làn</th>
                <th className="p-3.5">Mô Tả & Ghi Chú</th>
                <th className="p-3.5 text-center">Trạng Thái</th>
                <th className="p-3.5 text-right pr-4">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {isLoading ? (
                <tr>
                  <td colSpan={7} className="p-8 text-center text-slate-400">
                    Đang tải danh sách làn kiểm soát...
                  </td>
                </tr>
              ) : lanes.length > 0 ? (
                lanes.map((lane) => {
                  const isSelected = selectedIds.includes(lane.id)
                  const isIn = lane.direction === 'In' || lane.direction === 1

                  return (
                    <tr
                      key={lane.id}
                      className={`hover:bg-slate-50 dark:hover:bg-slate-800/40 transition-colors ${
                        isSelected ? 'bg-blue-50/50 dark:bg-blue-950/20' : ''
                      }`}
                    >
                      <td className="p-3.5 pl-4">
                        <input
                          type="checkbox"
                          checked={isSelected}
                          onChange={() => handleToggleSelect(lane.id)}
                          className="rounded border-slate-300 dark:border-slate-700 text-blue-600 focus:ring-blue-500 cursor-pointer"
                        />
                      </td>
                      <td className="p-3.5 font-mono font-bold text-slate-900 dark:text-slate-100 text-sm">
                        {lane.code}
                      </td>
                      <td className="p-3.5">
                        <div className="font-bold text-slate-900 dark:text-slate-100">
                          {lane.name}
                        </div>
                      </td>
                      <td className="p-3.5 text-center">
                        {isIn ? (
                          <Badge variant="outline" className="bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-300 dark:border-emerald-800 text-[11px] px-2 py-0.5 font-medium gap-1">
                            <ArrowDownToDot className="h-3 w-3" /> Làn Vào
                          </Badge>
                        ) : (
                          <Badge variant="outline" className="bg-purple-50 text-purple-700 border-purple-200 dark:bg-purple-950/40 dark:text-purple-300 dark:border-purple-800 text-[11px] px-2 py-0.5 font-medium gap-1">
                            <ArrowUpFromDot className="h-3 w-3" /> Làn Ra
                          </Badge>
                        )}
                      </td>
                      <td className="p-3.5 text-slate-600 dark:text-slate-400">
                        {lane.description ? (
                          <span className="text-xs line-clamp-1" title={lane.description}>
                            {lane.description}
                          </span>
                        ) : (
                          <span className="text-slate-400 italic text-[11px]">Chưa có mô tả</span>
                        )}
                      </td>
                      <td className="p-3.5 text-center">
                        {lane.isActive ? (
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
                              setSelectedLane(lane)
                              setIsDetailOpen(true)
                            }}
                            className="h-7 px-2.5 text-blue-600 hover:text-blue-700 border-blue-200 hover:bg-blue-50 dark:text-blue-400 dark:border-blue-900/60 dark:hover:bg-blue-950/50 text-[11px] font-semibold cursor-pointer shadow-2xs"
                            title="Xem Bảng Chi Tiết Làn"
                          >
                            <FileText className="h-3.5 w-3.5 mr-1 text-blue-500" />
                            Chi tiết
                          </Button>

                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => openEditModal(lane)}
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
                                id: lane.id,
                                name: lane.name,
                                isBatch: false,
                              })
                            }}
                            className="h-7 w-7 p-0 text-slate-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-950/40 cursor-pointer"
                            title="Xóa làn"
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
                    Không tìm thấy làn kiểm soát nào phù hợp
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>

      {/* ===================================================================== */}
      {/* MODAL CHI TIẾT LÀN KIỂM SOÁT — CARD THÔNG SỐ CHUYÊN NGHIỆP */}
      {/* ===================================================================== */}
      <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
        <DialogContent className="max-w-2xl max-h-[90vh] p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 border-slate-200 dark:border-slate-800 shadow-2xl">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800 pr-8">
            <DialogTitle className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 text-base">
              <div className="flex items-center gap-2.5">
                <div className="h-9 w-9 rounded-lg bg-blue-100 dark:bg-blue-950/60 text-blue-600 dark:text-blue-400 flex items-center justify-center">
                  <Route className="h-5 w-5" />
                </div>
                <div>
                  <span className="font-bold text-slate-900 dark:text-white tracking-wide text-base">
                    {selectedLane?.name || 'Chi Tiết Làn Kiểm Soát'}
                  </span>
                  <span className="text-xs text-slate-500 dark:text-slate-400 ml-2 font-mono hidden sm:inline">
                    ({selectedLane?.code || 'Chưa đặt mã'})
                  </span>
                </div>
              </div>

              <div className="flex items-center gap-1.5 mr-2">
                {selectedLane && (selectedLane.direction === 'In' || selectedLane.direction === 1) ? (
                  <Badge variant="outline" className="bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-300 dark:border-emerald-800 text-[11px] px-2 py-0.5 font-medium gap-1">
                    <ArrowDownToDot className="h-3 w-3" /> Làn Vào
                  </Badge>
                ) : (
                  <Badge variant="outline" className="bg-purple-50 text-purple-700 border-purple-200 dark:bg-purple-950/40 dark:text-purple-300 dark:border-purple-800 text-[11px] px-2 py-0.5 font-medium gap-1">
                    <ArrowUpFromDot className="h-3 w-3" /> Làn Ra
                  </Badge>
                )}
                {selectedLane?.isActive ? (
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

          {selectedLane && (
            <div className="flex-1 overflow-y-auto p-5 space-y-3.5 text-xs">
              <div className="flex items-center gap-1.5 font-bold text-slate-800 dark:text-slate-200">
                <Info className="h-4 w-4 text-blue-600" />
                <span>THÔNG SỐ KỸ THUẬT & PHÂN BỔ PHẦN CỨNG LÀN</span>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                {/* Card 1: Định danh làn */}
                <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2.5">
                  <div className="flex items-center gap-1.5 font-bold text-blue-600 dark:text-blue-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                    <Layers className="h-4 w-4" />
                    <span>Định Danh Làn Kiểm Soát</span>
                  </div>
                  <div className="grid grid-cols-2 gap-2 pt-0.5">
                    <div>
                      <span className="text-slate-400 block text-[11px]">Mã làn:</span>
                      <span className="font-mono font-extrabold text-slate-900 dark:text-white text-sm">
                        {selectedLane.code}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Chiều lưu thông:</span>
                      <span className="font-semibold text-slate-800 dark:text-slate-200">
                        {selectedLane.direction === 'In' || selectedLane.direction === 1 ? 'Làn Vào (Check-in)' : 'Làn Ra (Check-out)'}
                      </span>
                    </div>
                    <div className="col-span-2">
                      <span className="text-slate-400 block text-[11px]">Tên làn:</span>
                      <span className="font-bold text-slate-900 dark:text-slate-100 text-sm">
                        {selectedLane.name}
                      </span>
                    </div>
                    <div className="col-span-2">
                      <span className="text-slate-400 block text-[11px]">Mô tả làn:</span>
                      <span className="text-slate-700 dark:text-slate-300">
                        {selectedLane.description || 'Chưa cập nhật mô tả'}
                      </span>
                    </div>
                  </div>
                </div>

                {/* Card 2: Thiết bị camera gắn với làn */}
                <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2.5">
                  <div className="flex items-center gap-1.5 font-bold text-emerald-600 dark:text-emerald-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                    <Camera className="h-4 w-4" />
                    <span>Camera Nhận Diện & Toàn Cảnh</span>
                  </div>
                  <div className="space-y-2 pt-0.5">
                    <div>
                      <span className="text-slate-400 block text-[11px]">Camera nhận diện biển số (Plate):</span>
                      {selectedLane.plateCameraDeviceId && deviceMap.has(selectedLane.plateCameraDeviceId) ? (
                        <div className="font-semibold text-blue-600 dark:text-blue-400">
                          {deviceMap.get(selectedLane.plateCameraDeviceId)?.name} ({deviceMap.get(selectedLane.plateCameraDeviceId)?.ipAddress}:{deviceMap.get(selectedLane.plateCameraDeviceId)?.port})
                        </div>
                      ) : (
                        <span className="text-slate-400 italic">Chưa gán camera biển số</span>
                      )}
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Camera toàn cảnh (Overview):</span>
                      {selectedLane.overviewCameraDeviceId && deviceMap.has(selectedLane.overviewCameraDeviceId) ? (
                        <div className="font-semibold text-slate-800 dark:text-slate-200">
                          {deviceMap.get(selectedLane.overviewCameraDeviceId)?.name} ({deviceMap.get(selectedLane.overviewCameraDeviceId)?.ipAddress}:{deviceMap.get(selectedLane.overviewCameraDeviceId)?.port})
                        </div>
                      ) : (
                        <span className="text-slate-400 italic">Chưa gán camera toàn cảnh</span>
                      )}
                    </div>
                  </div>
                </div>

                {/* Card 3: Controller Access Control & Cảm Biến */}
                <div className="col-span-1 md:col-span-2 p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2.5">
                  <div className="flex items-center gap-1.5 font-bold text-amber-600 dark:text-amber-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                    <Cpu className="h-4 w-4" />
                    <span>Bộ Điều Khiển Access Control & Tín Hiệu Kích Hoạt</span>
                  </div>
                  <div className="grid grid-cols-2 sm:grid-cols-3 gap-2.5 pt-0.5">
                    <div>
                      <span className="text-slate-400 block text-[11px]">Bộ điều khiển Access Control:</span>
                      {selectedLane.controllerDeviceId && deviceMap.has(selectedLane.controllerDeviceId) ? (
                        <span className="font-semibold text-amber-700 dark:text-amber-300">
                          {deviceMap.get(selectedLane.controllerDeviceId)?.name} ({deviceMap.get(selectedLane.controllerDeviceId)?.ipAddress})
                        </span>
                      ) : (
                        <span className="text-slate-400 italic">Chưa gán controller</span>
                      )}
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Cổng tín hiệu cảm biến (Aux In):</span>
                      <span className="font-mono font-bold text-slate-900 dark:text-slate-100">
                        Cổng Aux In #{selectedLane.triggerAuxPort || 1}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400 block text-[11px]">Trạng thái kích hoạt:</span>
                      <span className="font-semibold text-slate-800 dark:text-slate-200">
                        {selectedLane.isActive ? '✅ Đang kích hoạt' : '⏸ Đang tạm dừng'}
                      </span>
                    </div>
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
                if (selectedLane) {
                  const l = selectedLane
                  setIsDetailOpen(false)
                  openEditModal(l)
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
      {/* MODAL THÊM MỚI LÀN */}
      {/* ===================================================================== */}
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="max-w-lg sm:max-w-xl p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 shadow-2xl max-h-[90vh]">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800">
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Plus className="h-5 w-5 text-blue-600" />
              Thêm Làn Kiểm Soát Mới
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
              {createMutation.isPending ? 'Đang lưu...' : 'Lưu Làn Kiểm Soát'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* ===================================================================== */}
      {/* MODAL CHỈNH SỬA LÀN */}
      {/* ===================================================================== */}
      <Dialog open={isEditOpen} onOpenChange={setIsEditOpen}>
        <DialogContent className="max-w-lg sm:max-w-xl p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 shadow-2xl max-h-[90vh]">
          <DialogHeader className="p-5 pb-3 border-b border-slate-200 dark:border-slate-800">
            <DialogTitle className="flex items-center gap-2 text-base font-bold text-slate-900 dark:text-slate-100">
              <Edit className="h-5 w-5 text-blue-600" />
              Chỉnh Sửa Làn Kiểm Soát
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
            <span>Đã chọn <strong className="text-blue-600 dark:text-blue-400 font-mono text-sm">{selectedIds.length}</strong> làn kiểm soát</span>
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
        title={deleteConfirm.isBatch ? 'Xác Nhận Xóa Nhiều Làn' : 'Xác Nhận Xóa Làn Kiểm Soát'}
        description={
          deleteConfirm.isBatch ? (
            <span>
              Bạn có chắc chắn muốn xóa{' '}
              <strong className="text-red-600 dark:text-red-400 font-semibold">
                {selectedIds.length} làn kiểm soát
              </strong>{' '}
              đã chọn? Dữ liệu sẽ được lưu trữ trong thùng rác hệ thống.
            </span>
          ) : (
            <span>
              Bạn có chắc chắn muốn xóa làn kiểm soát{' '}
              <strong className="text-slate-900 dark:text-slate-100 font-semibold">
                [{deleteConfirm.name}]
              </strong>
              ? Dữ liệu sẽ được chuyển vào thùng rác.
            </span>
          )
        }
        confirmText={deleteConfirm.isBatch ? `Xóa ${selectedIds.length} Làn` : 'Xác Nhận Xóa'}
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
