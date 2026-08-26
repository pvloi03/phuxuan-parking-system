import { useState, useEffect, useCallback } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Search,
  Download,
  Eye,
  Car,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  Layers,
  Clock,
  User,
  ArrowRightCircle,
  ArrowLeftCircle,
  Info,
  FileText,
  CheckCircle2,
  AlertCircle,
  Timer,
  Trash2,
} from 'lucide-react'
import { parkingService } from '@/services/parkingService'
import type { ImageStoragePathDto, ParkingSession, ParkingSessionStatus, VehicleType } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Card } from '@/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'

export function HistoryPage() {
  const queryClient = useQueryClient()
  const [plateNumber, setPlateNumber] = useState('')
  const [status, setStatus] = useState<string>('')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [selectedSession, setSelectedSession] = useState<ParkingSession | null>(null)
  const [activeSlide, setActiveSlide] = useState(0)
  const [activeTab, setActiveTab] = useState<'all' | 'slider' | 'details'>('all')
  const [isExporting, setIsExporting] = useState(false)
  const [selectedIds, setSelectedIds] = useState<string[]>([])
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [deleteConfirm, setDeleteConfirm] = useState<{
    isOpen: boolean
    id?: string
    plate?: string
    isBatch?: boolean
  }>({ isOpen: false })

  const { data, isLoading } = useQuery({
    queryKey: ['parking-history', plateNumber, status, page, pageSize],
    queryFn: () =>
      parkingService.getSessions({
        plateNumber: plateNumber || undefined,
        status: status ? (status as ParkingSessionStatus) : undefined,
        pageNumber: page,
        pageSize: pageSize,
      }),
  })

  // Mutation xóa 1 phiên
  const deleteMutation = useMutation({
    mutationFn: (id: string) => parkingService.deleteSession(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['parking-history'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard-metrics'] })
      setSelectedIds((prev) => prev.filter((i) => i !== deletingId))
      setDeletingId(null)
    },
    onError: () => {
      alert('Có lỗi xảy ra khi xóa bản ghi. Vui lòng thử lại.')
      setDeletingId(null)
    },
  })

  // Mutation xóa nhiều phiên
  const deleteBatchMutation = useMutation({
    mutationFn: (ids: string[]) => parkingService.deleteBatch(ids),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['parking-history'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard-metrics'] })
      setSelectedIds([])
    },
    onError: () => {
      alert('Có lỗi xảy ra khi xóa hàng loạt. Vui lòng thử lại.')
    },
  })



  const toggleSelectAll = () => {
    if (!data?.items) return
    if (selectedIds.length === data.items.length) {
      setSelectedIds([])
    } else {
      setSelectedIds(data.items.map((i) => i.id))
    }
  }

  const toggleSelectItem = (id: string) => {
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((i) => i !== id) : [...prev, id]
    )
  }

  const handleExport = async () => {
    setIsExporting(true)
    try {
      await parkingService.exportExcel({
        plateNumber: plateNumber || undefined,
        status: status ? (status as ParkingSessionStatus) : undefined,
      })
    } finally {
      setIsExporting(false)
    }
  }

  const getStatusBadge = (statusValue: ParkingSessionStatus | number | string) => {
    switch (statusValue) {
      case 'Active':
      case 1:
        return (
          <Badge variant="default" className="bg-blue-600/90 hover:bg-blue-600 text-white font-medium shadow-xs text-[11px] px-2.5 py-0.5">
            Đang trong bãi
          </Badge>
        )
      case 'Completed':
      case 2:
        return (
          <Badge variant="success" className="bg-emerald-600/90 hover:bg-emerald-600 text-white font-medium shadow-xs text-[11px] px-2.5 py-0.5">
            Đã hoàn thành
          </Badge>
        )
      case 'UnmatchedOut':
      case 3:
        return (
          <Badge variant="warning" className="bg-amber-600/90 hover:bg-amber-600 text-white font-medium shadow-xs text-[11px] px-2.5 py-0.5">
            Ra không có vào
          </Badge>
        )
      default:
        return <Badge variant="secondary">{statusValue}</Badge>
    }
  }

  const getVehicleTypeName = (type: VehicleType | number | string) => {
    if (type === 'Car' || type === 1) return 'Ô tô'
    if (type === 'Motorcycle' || type === 2) return 'Xe máy'
    if (type === 'Truck' || type === 3) return 'Xe tải'
    if (type === 'Bicycle' || type === 4) return 'Xe đạp'
    return 'Khác'
  }

  const getVehicleTypeBadge = (type: VehicleType | number | string) => {
    if (type === 'Car' || type === 1) {
      return (
        <Badge variant="outline" className="bg-indigo-50 text-indigo-700 border-indigo-200 dark:bg-indigo-950/40 dark:text-indigo-300 dark:border-indigo-800 font-semibold text-[11px] px-2.5 py-0.5 shadow-2xs">
          Ô tô
        </Badge>
      )
    }
    if (type === 'Motorcycle' || type === 2) {
      return (
        <Badge variant="outline" className="bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-950/40 dark:text-amber-300 dark:border-amber-800 font-semibold text-[11px] px-2.5 py-0.5 shadow-2xs">
          Xe máy
        </Badge>
      )
    }
    if (type === 'Truck' || type === 3) {
      return (
        <Badge variant="outline" className="bg-purple-50 text-purple-700 border-purple-200 dark:bg-purple-950/40 dark:text-purple-300 dark:border-purple-800 font-semibold text-[11px] px-2.5 py-0.5 shadow-2xs">
          Xe tải
        </Badge>
      )
    }
    if (type === 'Bicycle' || type === 4) {
      return (
        <Badge variant="outline" className="bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-300 dark:border-emerald-800 font-semibold text-[11px] px-2.5 py-0.5 shadow-2xs">
          Xe đạp
        </Badge>
      )
    }
    return <Badge variant="secondary" className="text-[11px] px-2 py-0.5">Khác</Badge>
  }

  const formatDuration = (inTimeStr?: string, outTimeStr?: string, statusVal?: string | number) => {
    if (!inTimeStr) return '--'
    const inTime = new Date(inTimeStr).getTime()
    const outTime = outTimeStr ? new Date(outTimeStr).getTime() : Date.now()
    const diffMs = outTime - inTime
    if (diffMs <= 0) return 'Dưới 1 phút'
    const totalMinutes = Math.floor(diffMs / 60000)
    const hours = Math.floor(totalMinutes / 60)
    const minutes = totalMinutes % 60
    const days = Math.floor(hours / 24)
    const remHours = hours % 24

    let text = ''
    if (days > 0) text += `${days} ngày `
    if (remHours > 0 || days > 0) text += `${remHours} giờ `
    text += `${minutes} phút`

    if (!outTimeStr && (statusVal === 'Active' || statusVal === 1)) {
      return `${text} (đang đỗ)`
    }
    return text
  }

  const formatImageUrl = (input?: string | ImageStoragePathDto | null) => {
    if (!input) return ''
    const rawPath = typeof input === 'object' ? input.path || '' : input
    if (!rawPath) return ''

    let normalized = rawPath.replace(/\\/g, '/')
    const capturesIdx = normalized.toLowerCase().indexOf('captures/')
    if (capturesIdx !== -1) {
      normalized = normalized.substring(capturesIdx + 'captures/'.length)
    }
    if (normalized.startsWith('/')) {
      normalized = normalized.substring(1)
    }

    return `/captures/${normalized}`
  }

  const hasImagePath = (input?: string | ImageStoragePathDto | null) => {
    if (!input) return false
    const rawPath = typeof input === 'object' ? input.path || '' : input
    return !!rawPath && rawPath.trim().length > 0
  }

  const getSessionSlides = (session: ParkingSession | null) => {
    if (!session) return []
    return [
      {
        id: 'in-overview',
        label: '1. Toàn Cảnh Lúc Vào',
        subtitle: `Làn: ${session.inLaneName || 'Làn Vào 1'} • Thời gian: ${session.inTime ? new Date(session.inTime).toLocaleString('vi-VN') : '--'}`,
        tag: 'VÀO',
        tagColor: 'bg-blue-600',
        url: formatImageUrl(session.inOverviewImagePath),
        hasImg: hasImagePath(session.inOverviewImagePath),
        fallbackText: 'Không có ảnh toàn cảnh vào',
      },
      {
        id: 'in-plate',
        label: '2. Cận Cảnh Biển Số Vào',
        subtitle: `Biển số nhận diện: ${session.plateNumber} • Thời gian: ${session.inTime ? new Date(session.inTime).toLocaleString('vi-VN') : '--'}`,
        tag: 'BIỂN SỐ VÀO',
        tagColor: 'bg-cyan-600',
        url: formatImageUrl(session.inPlateImagePath),
        hasImg: hasImagePath(session.inPlateImagePath),
        fallbackText: 'Không có ảnh biển số vào',
      },
      {
        id: 'out-overview',
        label: '3. Toàn Cảnh Lúc Ra',
        subtitle: `Làn: ${session.outLaneName || 'Làn Ra 1'} • Thời gian: ${session.outTime ? new Date(session.outTime).toLocaleString('vi-VN') : '--'}`,
        tag: 'RA',
        tagColor: 'bg-emerald-600',
        url: formatImageUrl(session.outOverviewImagePath),
        hasImg: hasImagePath(session.outOverviewImagePath),
        fallbackText: 'Chưa có ảnh toàn cảnh ra',
      },
      {
        id: 'out-plate',
        label: '4. Cận Cảnh Biển Số Ra',
        subtitle: `Biển số đối chiếu: ${session.plateNumber} • Thời gian: ${session.outTime ? new Date(session.outTime).toLocaleString('vi-VN') : '--'}`,
        tag: 'BIỂN SỐ RA',
        tagColor: 'bg-rose-600',
        url: formatImageUrl(session.outPlateImagePath),
        hasImg: hasImagePath(session.outPlateImagePath),
        fallbackText: 'Chưa có ảnh biển số ra',
      },
    ]
  }

  const slides = getSessionSlides(selectedSession)

  const handlePrevSlide = useCallback(() => {
    setActiveSlide((prev) => (prev > 0 ? prev - 1 : slides.length - 1))
  }, [slides.length])

  const handleNextSlide = useCallback(() => {
    setActiveSlide((prev) => (prev < slides.length - 1 ? prev + 1 : 0))
  }, [slides.length])

  // Keyboard navigation for image slider
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (!selectedSession) return
      if (e.key === 'ArrowLeft') handlePrevSlide()
      if (e.key === 'ArrowRight') handleNextSlide()
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [selectedSession, handlePrevSlide, handleNextSlide])

  // Helper tính danh sách số trang thông minh
  const getPaginationRange = (currentPage: number, totalPages: number) => {
    const delta = 1
    const range: (number | string)[] = []
    const rangeWithDots: (number | string)[] = []
    let l: number | undefined

    for (let i = 1; i <= totalPages; i++) {
      if (i === 1 || i === totalPages || (i >= currentPage - delta && i <= currentPage + delta)) {
        range.push(i)
      }
    }

    for (const i of range) {
      if (typeof i === 'number') {
        if (l !== undefined) {
          if (i - l === 2) {
            rangeWithDots.push(l + 1)
          } else if (i - l !== 1) {
            rangeWithDots.push('...')
          }
        }
        rangeWithDots.push(i)
        l = i
      }
    }

    return rangeWithDots
  }

  const isAllPageSelected =
    data?.items && data.items.length > 0 && selectedIds.length === data.items.length

  const totalPages = data?.totalPages || 1
  const totalCount = data?.totalCount || 0
  const startItem = totalCount === 0 ? 0 : (page - 1) * pageSize + 1
  const endItem = Math.min(page * pageSize, totalCount)

  return (
    <div className="space-y-6">
      {/* Header & Export Action */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
            Lịch Sử Xe Ra Vào
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
            Tra cứu, xem lại hình ảnh và quản lý dữ liệu xe vào/ra trên toàn hệ thống
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={handleExport}
            disabled={isExporting}
            className="gap-1.5 text-xs font-medium text-slate-700 dark:text-slate-300 border-slate-200 dark:border-slate-700 cursor-pointer shadow-xs"
          >
            <Download className="h-3.5 w-3.5 text-blue-600" />
            {isExporting ? 'Đang xuất Excel...' : 'Xuất Excel'}
          </Button>
        </div>
      </div>

      {/* Filters Bar */}
      <Card className="p-4 shadow-xs">
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div className="relative">
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
            <Input
              placeholder="Tìm theo biển số xe (VD: 30A-12345)..."
              value={plateNumber}
              onChange={(e) => {
                setPlateNumber(e.target.value)
                setPage(1)
              }}
              className="pl-9 text-xs"
            />
          </div>

          <div>
            <select
              value={status}
              onChange={(e) => {
                setStatus(e.target.value)
                setPage(1)
              }}
              className="w-full h-9 rounded-lg border border-slate-200 dark:border-slate-800 bg-transparent px-3 text-xs focus:outline-none focus:ring-2 focus:ring-blue-500 text-slate-800 dark:text-slate-200"
            >
              <option value="">-- Tất cả trạng thái --</option>
              <option value="Active">Đang trong bãi (Active)</option>
              <option value="Completed">Đã hoàn thành (Completed)</option>
              <option value="UnmatchedOut">Ra không có vào (Unmatched)</option>
            </select>
          </div>

          <div className="flex justify-end">
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                setPlateNumber('')
                setStatus('')
                setPage(1)
              }}
              className="text-xs h-9 cursor-pointer"
            >
              Đặt lại bộ lọc
            </Button>
          </div>
        </div>
      </Card>

      {/* History Data Table */}
      <Card className="shadow-xs overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-slate-100/80 dark:bg-slate-800/60 text-slate-700 dark:text-slate-300 font-semibold border-b border-slate-200 dark:border-slate-800">
              <tr>
                <th className="p-3.5 pl-4 w-10 text-center">
                  <input
                    type="checkbox"
                    checked={!!isAllPageSelected}
                    onChange={toggleSelectAll}
                    className="rounded border-slate-300 dark:border-slate-700 text-blue-600 focus:ring-blue-500 cursor-pointer h-4 w-4"
                    title="Chọn tất cả trên trang này"
                  />
                </th>
                <th className="p-3.5">Biển Số Xe</th>
                <th className="p-3.5">Chủ Xe</th>
                <th className="p-3.5 text-center">Loại Xe</th>
                <th className="p-3.5">Thời Gian Vào</th>
                <th className="p-3.5">Thời Gian Ra</th>
                <th className="p-3.5 text-center">Trạng Thái</th>
                <th className="p-3.5 text-center pr-4">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {isLoading ? (
                <tr>
                  <td colSpan={8} className="p-8 text-center text-slate-400">
                    Đang nạp dữ liệu lịch sử...
                  </td>
                </tr>
              ) : data?.items && data.items.length > 0 ? (
                data.items.map((session) => {
                  const isSelected = selectedIds.includes(session.id)
                  return (
                    <tr
                      key={session.id}
                      className={`hover:bg-slate-50 dark:hover:bg-slate-800/40 transition-colors ${
                        isSelected ? 'bg-blue-50/40 dark:bg-blue-950/20' : ''
                      }`}
                    >
                      <td className="p-3.5 pl-4 text-center">
                        <input
                          type="checkbox"
                          checked={isSelected}
                          onChange={() => toggleSelectItem(session.id)}
                          className="rounded border-slate-300 dark:border-slate-700 text-blue-600 focus:ring-blue-500 cursor-pointer h-4 w-4"
                        />
                      </td>
                      <td className="p-3.5 font-bold text-slate-900 dark:text-slate-100 font-mono">
                        {session.plateNumber}
                      </td>
                      <td className="p-3.5">
                        {session.personName ? (
                          <span
                            className="max-w-[120px] sm:max-w-[150px] truncate block font-medium text-slate-800 dark:text-slate-200 cursor-help"
                            title={session.personName}
                          >
                            {session.personName}
                          </span>
                        ) : (
                          <span className="text-slate-400 italic">Xe vãng lai</span>
                        )}
                      </td>
                      <td className="p-3.5 text-center">
                        {getVehicleTypeBadge(session.vehicleType)}
                      </td>
                      <td className="p-3.5 text-slate-600 dark:text-slate-400 font-mono">
                        {session.inTime ? new Date(session.inTime).toLocaleString('vi-VN') : '--'}
                      </td>
                      <td className="p-3.5 text-slate-600 dark:text-slate-400 font-mono">
                        {session.outTime ? new Date(session.outTime).toLocaleString('vi-VN') : '--'}
                      </td>
                      <td className="p-3.5 text-center">{getStatusBadge(session.status)}</td>
                      <td className="p-3.5 pr-4 text-center">
                        <div className="flex items-center justify-center gap-1.5">
                          {/* Nút Chi Tiết & Ảnh */}
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => {
                              setSelectedSession(session)
                              setActiveSlide(0)
                              setActiveTab('all')
                            }}
                            className="h-7 px-2.5 text-blue-600 hover:text-blue-700 border-blue-200 hover:bg-blue-50 dark:text-blue-400 dark:border-blue-900/60 dark:hover:bg-blue-950/50 text-[11px] font-semibold cursor-pointer shadow-2xs"
                            title="Xem Bảng Thông Số Chi Tiết & Slide Ảnh Camera"
                          >
                            <FileText className="h-3.5 w-3.5 mr-1 text-blue-500" />
                            Chi tiết
                          </Button>

                          {/* Nút Xóa */}
                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() => {
                              setDeleteConfirm({
                                isOpen: true,
                                id: session.id,
                                plate: session.plateNumber,
                                isBatch: false,
                              })
                            }}
                            disabled={deletingId === session.id}
                            className="h-7 w-7 p-0 text-slate-400 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-950/50 rounded-lg cursor-pointer transition-colors"
                            title="Xóa bản ghi này"
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
                    Không tìm thấy bản ghi lịch sử nào phù hợp
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {/* Full Enterprise Pagination Bar - Luôn Hiển Thị */}
        <div className="p-3.5 border-t border-slate-200 dark:border-slate-800 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-slate-500 dark:text-slate-400 bg-slate-50/50 dark:bg-slate-900/40">
          {/* Bên trái: Thông tin tổng số & Bộ chọn PageSize */}
          <div className="flex items-center gap-3">
            <span>
              Hiển thị <strong className="text-slate-700 dark:text-slate-300 font-mono">{startItem}</strong> -{' '}
              <strong className="text-slate-700 dark:text-slate-300 font-mono">{endItem}</strong> trên tổng số{' '}
              <strong className="text-slate-900 dark:text-slate-100 font-mono">{totalCount}</strong> bản ghi
            </span>

            <div className="flex items-center gap-1.5 border-l border-slate-200 dark:border-slate-800 pl-3">
              <span className="text-[11px] text-slate-400">Số dòng:</span>
              <select
                value={pageSize}
                onChange={(e) => {
                  setPageSize(Number(e.target.value))
                  setPage(1)
                }}
                className="h-7 px-2 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-xs text-slate-700 dark:text-slate-300 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
              >
                <option value={5}>5 / trang</option>
                <option value={10}>10 / trang</option>
                <option value={15}>15 / trang</option>
                <option value={25}>25 / trang</option>
                <option value={50}>50 / trang</option>
              </select>
            </div>
          </div>

          {/* Bên phải: Các nút điều hướng & danh sách số trang */}
          <div className="flex items-center gap-1">
            {/* Về trang đầu */}
            <Button
              variant="outline"
              size="sm"
              disabled={page <= 1}
              onClick={() => setPage(1)}
              className="h-7 w-7 p-0 cursor-pointer border-slate-200 dark:border-slate-700"
              title="Về trang đầu tiên (Trang 1)"
            >
              <ChevronsLeft className="h-3.5 w-3.5" />
            </Button>

            {/* Trang trước */}
            <Button
              variant="outline"
              size="sm"
              disabled={page <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              className="h-7 w-7 p-0 cursor-pointer border-slate-200 dark:border-slate-700"
              title="Trang trước"
            >
              <ChevronLeft className="h-3.5 w-3.5" />
            </Button>

            {/* Danh sách các số trang */}
            {getPaginationRange(page, totalPages).map((pNum, idx) => {
              if (pNum === '...') {
                return (
                  <span key={`dots-${idx}`} className="px-1.5 text-slate-400 font-mono">
                    ...
                  </span>
                )
              }
              const pageIndex = Number(pNum)
              const isActive = pageIndex === page
              return (
                <button
                  key={`page-${pageIndex}`}
                  type="button"
                  onClick={() => setPage(pageIndex)}
                  className={`h-7 min-w-[28px] px-1.5 rounded-md font-mono text-xs font-semibold transition-all cursor-pointer ${
                    isActive
                      ? 'bg-blue-600 text-white shadow-xs'
                      : 'border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700'
                  }`}
                >
                  {pageIndex}
                </button>
              )
            })}

            {/* Trang sau */}
            <Button
              variant="outline"
              size="sm"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              className="h-7 w-7 p-0 cursor-pointer border-slate-200 dark:border-slate-700"
              title="Trang tiếp theo"
            >
              <ChevronRight className="h-3.5 w-3.5" />
            </Button>

            {/* Đến trang cuối */}
            <Button
              variant="outline"
              size="sm"
              disabled={page >= totalPages}
              onClick={() => setPage(totalPages)}
              className="h-7 w-7 p-0 cursor-pointer border-slate-200 dark:border-slate-700"
              title={`Đến trang cuối cùng (Trang ${totalPages})`}
            >
              <ChevronsRight className="h-3.5 w-3.5" />
            </Button>
          </div>
        </div>
      </Card>

      {/* Interactive Image Slide & Comprehensive Details Dialog */}
      <Dialog open={!!selectedSession} onOpenChange={() => setSelectedSession(null)}>
        <DialogContent className="max-w-3xl max-h-[90vh] p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 border-slate-200 dark:border-slate-800 shadow-2xl">
          <DialogHeader className="p-4 sm:p-5 pb-3 border-b border-slate-200 dark:border-slate-800 pr-16">
            <DialogTitle className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-sm sm:text-base">
              <div className="flex items-center gap-2 min-w-0">
                <div className="h-7 w-7 rounded-lg bg-blue-50 dark:bg-blue-600/20 text-blue-600 dark:text-blue-400 flex items-center justify-center shrink-0">
                  <Car className="h-4 w-4" />
                </div>
                <div className="truncate">
                  <span className="font-bold text-slate-900 dark:text-white tracking-wide">
                    Chi Tiết Lượt Xe: {selectedSession?.plateNumber}
                  </span>
                  <span className="text-xs text-slate-500 dark:text-slate-400 ml-2 font-normal hidden sm:inline">
                    ({selectedSession?.personName || 'Khách vãng lai'} • {selectedSession ? getVehicleTypeName(selectedSession.vehicleType) : ''})
                  </span>
                </div>
              </div>

              {/* Header Badges: Status & Duration — Dịch sang trái cách xa nút Close */}
              <div className="flex items-center gap-1.5 shrink-0 mr-8 sm:mr-12">
                {selectedSession && getStatusBadge(selectedSession.status)}
                {selectedSession?.inTime && (
                  <Badge variant="outline" className="text-slate-600 dark:text-slate-300 border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800/80 text-[11px] px-2 py-0.5 font-mono flex items-center gap-1">
                    <Timer className="h-3 w-3 text-blue-500" />
                    {formatDuration(selectedSession.inTime, selectedSession.outTime, selectedSession.status)}
                  </Badge>
                )}
              </div>
            </DialogTitle>

            {/* View Switcher Tabs */}
            <div className="flex items-center gap-1 mt-2.5 pt-2 border-t border-slate-100 dark:border-slate-800/60">
              <button
                type="button"
                onClick={() => setActiveTab('all')}
                className={`text-xs px-3 py-1 rounded-md font-medium transition-colors cursor-pointer flex items-center gap-1.5 ${
                  activeTab === 'all'
                    ? 'bg-blue-600 text-white shadow-xs'
                    : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800'
                }`}
              >
                <Layers className="h-3.5 w-3.5" />
                Tổng Quan (Ảnh & Thông Tin)
              </button>
              <button
                type="button"
                onClick={() => setActiveTab('slider')}
                className={`text-xs px-3 py-1 rounded-md font-medium transition-colors cursor-pointer flex items-center gap-1.5 ${
                  activeTab === 'slider'
                    ? 'bg-blue-600 text-white shadow-xs'
                    : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800'
                }`}
              >
                <Eye className="h-3.5 w-3.5" />
                Slide Ảnh ({slides.length})
              </button>
              <button
                type="button"
                onClick={() => setActiveTab('details')}
                className={`text-xs px-3 py-1 rounded-md font-medium transition-colors cursor-pointer flex items-center gap-1.5 ${
                  activeTab === 'details'
                    ? 'bg-blue-600 text-white shadow-xs'
                    : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800'
                }`}
              >
                <FileText className="h-3.5 w-3.5" />
                Bảng Thông Số Chi Tiết
              </button>
            </div>
          </DialogHeader>

          {selectedSession && (
            <div className="flex-1 overflow-y-auto p-4 sm:p-5 space-y-4">
              {/* SECTION 1: IMAGE SLIDER */}
              {(activeTab === 'all' || activeTab === 'slider') && slides.length > 0 && (
                <div className="space-y-3">
                  {/* Main Hero Slide Stage - Compact 280px */}
                  <div className="relative w-full h-[240px] sm:h-[280px] bg-slate-100 dark:bg-slate-950 rounded-xl overflow-hidden flex items-center justify-center border border-slate-200 dark:border-slate-800 group shadow-inner">
                    {slides[activeSlide]?.hasImg ? (
                      <img
                        key={slides[activeSlide].id}
                        src={slides[activeSlide].url}
                        alt={slides[activeSlide].label}
                        className="w-full h-full object-contain transition-all duration-300 animate-in fade-in zoom-in-95"
                        onError={(e) => {
                          (e.target as any).src = 'https://placehold.co/600x400/1e293b/ffffff?text=Image+Unavailable'
                        }}
                      />
                    ) : (
                      <div className="flex flex-col items-center justify-center text-slate-400 dark:text-slate-500 gap-1.5">
                        <Layers className="h-7 w-7 opacity-40" />
                        <span className="text-xs">{slides[activeSlide]?.fallbackText}</span>
                      </div>
                    )}

                    {/* Top Info Banner Overlay */}
                    <div className="absolute top-2.5 left-2.5 right-2.5 flex items-center justify-between pointer-events-none">
                      <div className="flex items-center gap-1.5 bg-white/95 dark:bg-slate-900/90 backdrop-blur-md px-2.5 py-1 rounded-md border border-slate-200/90 dark:border-slate-700/60 shadow-md">
                        <span className={`text-[9px] font-extrabold text-white px-1.5 py-0.2 rounded ${slides[activeSlide].tagColor}`}>
                          {slides[activeSlide].tag}
                        </span>
                        <span className="text-[11px] font-semibold text-slate-800 dark:text-slate-100">
                          {slides[activeSlide].label}
                        </span>
                      </div>

                      <div className="bg-white/95 dark:bg-slate-900/90 backdrop-blur-md px-2.5 py-1 rounded-md border border-slate-200/90 dark:border-slate-700/60 text-[11px] text-slate-600 dark:text-slate-300 shadow-md font-mono hidden sm:block">
                        {slides[activeSlide].subtitle}
                      </div>
                    </div>

                    {/* Left / Right Carousel Navigation Buttons */}
                    <button
                      type="button"
                      onClick={handlePrevSlide}
                      className="absolute left-2.5 top-1/2 -translate-y-1/2 h-8 w-8 rounded-full bg-white/90 dark:bg-slate-900/80 hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-800 dark:text-white flex items-center justify-center border border-slate-200 dark:border-slate-700/70 shadow-lg transition-all opacity-80 group-hover:opacity-100 hover:scale-110 cursor-pointer"
                      title="Ảnh trước (Phím mũi tên Trái)"
                    >
                      <ChevronLeft className="h-4 w-4" />
                    </button>

                    <button
                      type="button"
                      onClick={handleNextSlide}
                      className="absolute right-2.5 top-1/2 -translate-y-1/2 h-8 w-8 rounded-full bg-white/90 dark:bg-slate-900/80 hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-800 dark:text-white flex items-center justify-center border border-slate-200 dark:border-slate-700/70 shadow-lg transition-all opacity-80 group-hover:opacity-100 hover:scale-110 cursor-pointer"
                      title="Ảnh tiếp theo (Phím mũi tên Phải)"
                    >
                      <ChevronRight className="h-4 w-4" />
                    </button>

                    {/* Dot Indicators */}
                    <div className="absolute bottom-2.5 left-1/2 -translate-x-1/2 flex items-center gap-1.5 bg-white/80 dark:bg-slate-900/70 backdrop-blur-sm px-2 py-0.5 rounded-full border border-slate-200 dark:border-slate-700/50">
                      {slides.map((_, idx) => (
                        <button
                          key={idx}
                          type="button"
                          onClick={() => setActiveSlide(idx)}
                          className={`h-1.5 rounded-full transition-all cursor-pointer ${
                            activeSlide === idx ? 'w-4 bg-blue-600 dark:bg-blue-500' : 'w-1.5 bg-slate-300 dark:bg-slate-600 hover:bg-slate-400'
                          }`}
                          title={`Đến ảnh ${idx + 1}`}
                        />
                      ))}
                    </div>
                  </div>

                  {/* Bottom Thumbnail Strip - Compact */}
                  <div className="grid grid-cols-4 gap-2">
                    {slides.map((slide, idx) => (
                      <button
                        key={slide.id}
                        type="button"
                        onClick={() => setActiveSlide(idx)}
                        className={`relative rounded-lg overflow-hidden border p-1 transition-all cursor-pointer text-left bg-slate-50 dark:bg-slate-950/60 ${
                          activeSlide === idx
                            ? 'border-blue-600 dark:border-blue-500 ring-2 ring-blue-500/30 bg-blue-50/50 dark:bg-slate-800/80 shadow-md'
                            : 'border-slate-200 dark:border-slate-800 hover:border-slate-300 dark:hover:border-slate-700 opacity-70 hover:opacity-100'
                        }`}
                      >
                        <div className="h-11 sm:h-12 w-full rounded bg-slate-200/60 dark:bg-slate-950 overflow-hidden flex items-center justify-center">
                          {slide.hasImg ? (
                            <img
                              src={slide.url}
                              alt={slide.label}
                              className="w-full h-full object-cover"
                              onError={(e) => {
                                (e.target as any).src = 'https://placehold.co/120x80/1e293b/ffffff?text=Thumb'
                              }}
                            />
                          ) : (
                            <span className="text-[9px] text-slate-400 dark:text-slate-500 text-center px-1">Chưa có</span>
                          )}
                        </div>
                        <div className="mt-1 flex items-center justify-between text-[10px] px-0.5">
                          <span className={`font-semibold truncate ${activeSlide === idx ? 'text-blue-600 dark:text-blue-400' : 'text-slate-500 dark:text-slate-400'}`}>
                            {slide.tag}
                          </span>
                          <span className="text-[9px] text-slate-400 dark:text-slate-500 font-mono">#{idx + 1}</span>
                        </div>
                      </button>
                    ))}
                  </div>
                </div>
              )}

              {/* SECTION 2: COMPREHENSIVE DETAILS GRID */}
              {(activeTab === 'all' || activeTab === 'details') && (
                <div className="space-y-3">
                  <div className="flex items-center gap-1.5 text-xs font-bold text-slate-800 dark:text-slate-200">
                    <Info className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                    <span>THÔNG TIN CHI TIẾT ĐẦY ĐỦ CỦA LƯỢT XE</span>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-xs">
                    {/* Card 1: Thông tin phương tiện & chủ xe */}
                    <div className="p-3 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2">
                      <div className="flex items-center gap-1.5 font-bold text-blue-600 dark:text-blue-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                        <Car className="h-4 w-4" />
                        <span>Phương Tiện & Chủ Xe</span>
                      </div>
                      <div className="grid grid-cols-2 gap-2 pt-0.5">
                        <div>
                          <span className="text-slate-400 block text-[11px]">Biển số xe:</span>
                          <span className="font-extrabold text-sm text-slate-900 dark:text-white font-mono">
                            {selectedSession.plateNumber}
                          </span>
                        </div>
                        <div>
                          <span className="text-slate-400 block text-[11px]">Loại phương tiện:</span>
                          <span className="font-semibold text-slate-800 dark:text-slate-200">
                            {getVehicleTypeName(selectedSession.vehicleType)}
                          </span>
                        </div>
                        <div>
                          <span className="text-slate-400 block text-[11px]">Họ tên chủ xe:</span>
                          <span className="font-semibold text-slate-800 dark:text-slate-200 flex items-center gap-1">
                            <User className="h-3 w-3 text-slate-400" />
                            {selectedSession.personName || 'Khách vãng lai'}
                          </span>
                        </div>
                        <div>
                          <span className="text-slate-400 block text-[11px]">Phân loại đối tượng:</span>
                          <span className="font-semibold text-blue-600 dark:text-blue-400">
                            {selectedSession.personType
                              ? (selectedSession.personType === 'Employee' ? 'Cán bộ / Nhân viên' :
                                 selectedSession.personType === 'Contractor' ? 'Đối tác / Nhà thầu' :
                                 selectedSession.personType === 'Visitor' ? 'Khách thăm' :
                                 selectedSession.personType === 'VIP' ? 'Khách VIP' : 'Khách vãng lai')
                              : (selectedSession.personName ? 'Xe đăng ký' : 'Khách vãng lai')}
                          </span>
                        </div>
                        {selectedSession.companyName && (
                          <div>
                            <span className="text-slate-400 block text-[11px]">Công ty / Đơn vị:</span>
                            <span className="font-medium text-slate-700 dark:text-slate-300">
                              {selectedSession.companyName}
                            </span>
                          </div>
                        )}
                        {selectedSession.departmentName && (
                          <div>
                            <span className="text-slate-400 block text-[11px]">Phòng ban:</span>
                            <span className="font-medium text-slate-700 dark:text-slate-300">
                              {selectedSession.departmentName}
                            </span>
                          </div>
                        )}
                      </div>
                    </div>

                    {/* Card 2: Thời lượng & Trạng thái */}
                    <div className="p-3 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2">
                      <div className="flex items-center gap-1.5 font-bold text-emerald-600 dark:text-emerald-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                        <Clock className="h-4 w-4" />
                        <span>Thời Lượng & Trạng Thái</span>
                      </div>
                      <div className="grid grid-cols-2 gap-2 pt-0.5">
                        <div>
                          <span className="text-slate-400 block text-[11px]">Trạng thái hệ thống:</span>
                          <div className="mt-0.5">{getStatusBadge(selectedSession.status)}</div>
                        </div>
                        <div>
                          <span className="text-slate-400 block text-[11px]">Tổng thời gian gửi:</span>
                          <span className="font-bold text-slate-900 dark:text-emerald-400">
                            {formatDuration(selectedSession.inTime, selectedSession.outTime, selectedSession.status)}
                          </span>
                        </div>
                        <div>
                          <span className="text-slate-400 block text-[11px]">Mã phiên (ID):</span>
                          <span className="font-mono text-[10px] text-slate-500 truncate block" title={selectedSession.id}>
                            {selectedSession.id}
                          </span>
                        </div>
                        <div>
                          <span className="text-slate-400 block text-[11px]">Ghi chú nghiệp vụ:</span>
                          <span className="text-slate-700 dark:text-slate-300 italic">
                            {selectedSession.note || 'Không có ghi chú'}
                          </span>
                        </div>
                      </div>
                    </div>

                    {/* Card 3: Chi tiết lượt vào (Check-In) */}
                    <div className="p-3 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2">
                      <div className="flex items-center gap-1.5 font-bold text-cyan-600 dark:text-cyan-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                        <ArrowRightCircle className="h-4 w-4 text-cyan-500" />
                        <span>Chi Tiết Lượt Vào (Check-In)</span>
                      </div>
                      <div className="space-y-1.5 pt-0.5">
                        <div className="flex items-center justify-between">
                          <span className="text-slate-400 text-[11px]">Thời gian vào chính xác:</span>
                          <span className="font-semibold text-slate-800 dark:text-slate-200 font-mono">
                            {selectedSession.inTime ? new Date(selectedSession.inTime).toLocaleString('vi-VN') : '--'}
                          </span>
                        </div>
                        <div className="flex items-center justify-between">
                          <span className="text-slate-400 text-[11px]">Làn kiểm soát vào:</span>
                          <span className="font-semibold text-slate-800 dark:text-slate-200">
                            {selectedSession.inLaneName || 'Làn Vào Số 1'}
                          </span>
                        </div>
                        <div className="flex items-center justify-between text-[11px]">
                          <span className="text-slate-400">Hình ảnh ghi nhận vào:</span>
                          <div className="flex items-center gap-1">
                            {hasImagePath(selectedSession.inOverviewImagePath) ? (
                              <span className="text-emerald-600 dark:text-emerald-400 flex items-center gap-0.5">
                                <CheckCircle2 className="h-3 w-3" /> Toàn cảnh
                              </span>
                            ) : (
                              <span className="text-slate-400 flex items-center gap-0.5">
                                <AlertCircle className="h-3 w-3" /> Thiếu toàn cảnh
                              </span>
                            )}
                            <span className="text-slate-300">•</span>
                            {hasImagePath(selectedSession.inPlateImagePath) ? (
                              <span className="text-emerald-600 dark:text-emerald-400 flex items-center gap-0.5">
                                <CheckCircle2 className="h-3 w-3" /> Biển số
                              </span>
                            ) : (
                              <span className="text-slate-400 flex items-center gap-0.5">
                                <AlertCircle className="h-3 w-3" /> Thiếu biển số
                              </span>
                            )}
                          </div>
                        </div>
                      </div>
                    </div>

                    {/* Card 4: Chi tiết lượt ra (Check-Out) */}
                    <div className="p-3 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-800/40 space-y-2">
                      <div className="flex items-center gap-1.5 font-bold text-rose-600 dark:text-rose-400 border-b border-slate-200 dark:border-slate-700/60 pb-1.5">
                        <ArrowLeftCircle className="h-4 w-4 text-rose-500" />
                        <span>Chi Tiết Lượt Ra (Check-Out)</span>
                      </div>
                      <div className="space-y-1.5 pt-0.5">
                        <div className="flex items-center justify-between">
                          <span className="text-slate-400 text-[11px]">Thời gian ra chính xác:</span>
                          <span className="font-semibold text-slate-800 dark:text-slate-200 font-mono">
                            {selectedSession.outTime ? (
                              new Date(selectedSession.outTime).toLocaleString('vi-VN')
                            ) : (
                              <span className="text-blue-500 italic">Đang gửi trong bãi</span>
                            )}
                          </span>
                        </div>
                        <div className="flex items-center justify-between">
                          <span className="text-slate-400 text-[11px]">Làn kiểm soát ra:</span>
                          <span className="font-semibold text-slate-800 dark:text-slate-200">
                            {selectedSession.outLaneName || (selectedSession.outTime ? 'Làn Ra Số 1' : '--')}
                          </span>
                        </div>
                        <div className="flex items-center justify-between text-[11px]">
                          <span className="text-slate-400">Hình ảnh ghi nhận ra:</span>
                          <div className="flex items-center gap-1">
                            {hasImagePath(selectedSession.outOverviewImagePath) ? (
                              <span className="text-emerald-600 dark:text-emerald-400 flex items-center gap-0.5">
                                <CheckCircle2 className="h-3 w-3" /> Toàn cảnh
                              </span>
                            ) : (
                              <span className="text-slate-400 flex items-center gap-0.5">
                                <AlertCircle className="h-3 w-3" /> Chưa có
                              </span>
                            )}
                            <span className="text-slate-300">•</span>
                            {hasImagePath(selectedSession.outPlateImagePath) ? (
                              <span className="text-emerald-600 dark:text-emerald-400 flex items-center gap-0.5">
                                <CheckCircle2 className="h-3 w-3" /> Biển số
                              </span>
                            ) : (
                              <span className="text-slate-400 flex items-center gap-0.5">
                                <AlertCircle className="h-3 w-3" /> Chưa có
                              </span>
                            )}
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </div>
          )}
        </DialogContent>
      </Dialog>

      {/* ===================================================================== */}
      {/* FLOATING BULK ACTION BAR — HIỆN/ẨN PHÍA DƯỚI BÊN PHẢI KHI CHỌN DÒNG */}
      {/* ===================================================================== */}
      {selectedIds.length > 0 && (
        <div className="fixed bottom-6 right-6 z-40 bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 border border-slate-200 dark:border-slate-800 shadow-2xl rounded-2xl px-4 py-2.5 flex items-center gap-3 animate-in slide-in-from-bottom-5 duration-200">
          <div className="flex items-center gap-1.5 text-xs font-semibold text-slate-700 dark:text-slate-300">
            <span className="h-2 w-2 rounded-full bg-blue-600 animate-pulse" />
            <span>
              Đã chọn <strong className="text-blue-600 dark:text-blue-400 font-mono text-sm">{selectedIds.length}</strong> bản ghi
            </span>
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
            disabled={deleteBatchMutation.isPending}
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
        title={deleteConfirm.isBatch ? 'Xác Nhận Xóa Nhiều Lượt Xe' : 'Xác Nhận Xóa Lượt Xe'}
        description={
          deleteConfirm.isBatch ? (
            <span>
              Bạn có chắc chắn muốn xóa{' '}
              <strong className="text-red-600 dark:text-red-400 font-semibold">
                {selectedIds.length} bản ghi lượt xe
              </strong>{' '}
              đã chọn? Dữ liệu sẽ được lưu trữ trong thùng rác hệ thống.
            </span>
          ) : (
            <span>
              Bạn có chắc chắn muốn xóa bản ghi lượt xe biển số{' '}
              <strong className="text-blue-600 dark:text-blue-400 font-mono font-bold">
                [{deleteConfirm.plate}]
              </strong>
              ? Dữ liệu sẽ được chuyển vào thùng rác.
            </span>
          )
        }
        confirmText={deleteConfirm.isBatch ? `Xóa ${selectedIds.length} Bản Ghi` : 'Xác Nhận Xóa'}
        isLoading={deleteMutation.isPending || deleteBatchMutation.isPending}
        onConfirm={() => {
          if (deleteConfirm.isBatch) {
            deleteBatchMutation.mutate(selectedIds, {
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
