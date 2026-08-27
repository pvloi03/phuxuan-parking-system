import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Trash2,
  RotateCcw,
  Search,
  RefreshCw,
  AlertTriangle,
  Car,
  User,
  Handshake,
  Building2,
  Building,
  Camera,
  Route,
  History,
  X,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'
import { recycleBinService, type ItemKey } from '@/services/recycleBinService'
import type { RecycleBinItem } from '@/types'
import { notify } from '@/lib/notify'

export const RecycleBinPage = () => {
  const queryClient = useQueryClient()

  // State bộ lọc và phân trang
  const [selectedTab, setSelectedTab] = useState<string>('All')
  const [searchTerm, setSearchTerm] = useState<string>('')
  const [page, setPage] = useState<number>(1)
  const [pageSize, setPageSize] = useState<number>(10)

  // State chọn hàng loạt
  const [selectedItems, setSelectedItems] = useState<ItemKey[]>([])

  // State Confirm Dialogs
  const [singleActionModal, setSingleActionModal] = useState<{
    isOpen: boolean
    action: 'restore' | 'hard-delete'
    item?: RecycleBinItem
  }>({ isOpen: false, action: 'restore' })

  const [batchActionModal, setBatchActionModal] = useState<{
    isOpen: boolean
    action: 'restore' | 'hard-delete'
  }>({ isOpen: false, action: 'restore' })

  const [emptyTrashModal, setEmptyTrashModal] = useState<boolean>(false)

  // 1. Query lấy counts cho từng tab
  const { data: counts, refetch: refetchCounts } = useQuery({
    queryKey: ['recycle-bin-counts'],
    queryFn: () => recycleBinService.getCounts(),
  })

  // 2. Query lấy danh sách items
  const { data: pagedData, isLoading, refetch: refetchItems } = useQuery({
    queryKey: ['recycle-bin-items', selectedTab, searchTerm, page, pageSize],
    queryFn: () =>
      recycleBinService.getItems({
        itemType: selectedTab === 'All' ? undefined : selectedTab,
        search: searchTerm || undefined,
        pageNumber: page,
        pageSize,
      }),
  })

  const items = pagedData?.items || []
  const totalCount = pagedData?.totalCount || 0
  const totalPages = pagedData?.totalPages || 1

  // Handle refresh all
  const handleRefresh = () => {
    refetchCounts()
    refetchItems()
  }

  // --- MUTATIONS ---
  const restoreSingleMutation = useMutation({
    mutationFn: ({ itemType, id }: ItemKey) => recycleBinService.restoreItem(itemType, id),
    onSuccess: () => {
      handleRefresh()
      // Invalidate all related modules cache
      queryClient.invalidateQueries()
      setSingleActionModal({ isOpen: false, action: 'restore' })
      notify.success('Khôi phục bản ghi thành công!')
    },
    onError: (err: any) => {
      notify.error('Có lỗi xảy ra khi khôi phục bản ghi này.', err)
    },
  })

  const hardDeleteSingleMutation = useMutation({
    mutationFn: ({ itemType, id }: ItemKey) => recycleBinService.hardDeleteItem(itemType, id),
    onSuccess: () => {
      handleRefresh()
      queryClient.invalidateQueries()
      setSingleActionModal({ isOpen: false, action: 'hard-delete' })
      notify.success('Đã xóa vĩnh viễn bản ghi khỏi hệ thống.')
    },
    onError: (err: any) => {
      notify.error('Không thể xóa vĩnh viễn do ràng buộc quan hệ dữ liệu.', err)
    },
  })

  const restoreBatchMutation = useMutation({
    mutationFn: (batch: ItemKey[]) => recycleBinService.restoreBatch(batch),
    onSuccess: (data) => {
      const count = selectedItems.length
      handleRefresh()
      queryClient.invalidateQueries()
      setSelectedItems([])
      setBatchActionModal({ isOpen: false, action: 'restore' })
      if (data.errors && data.errors.length > 0) {
        notify.warning(`Đã khôi phục thành công nhưng có cảnh báo: ${data.errors.join('; ')}`)
      } else {
        notify.success(`Khôi phục thành công ${count} bản ghi!`)
      }
    },
    onError: (err: any) => {
      notify.error('Có lỗi xảy ra khi khôi phục hàng loạt.', err)
    },
  })

  const hardDeleteBatchMutation = useMutation({
    mutationFn: (batch: ItemKey[]) => recycleBinService.hardDeleteBatch(batch),
    onSuccess: (data) => {
      const count = selectedItems.length
      handleRefresh()
      queryClient.invalidateQueries()
      setSelectedItems([])
      setBatchActionModal({ isOpen: false, action: 'hard-delete' })
      if (data.errors && data.errors.length > 0) {
        notify.warning(`Đã xóa một số mục, còn lại không thể xóa do ràng buộc: ${data.errors.join('; ')}`)
      } else {
        notify.success(`Đã xóa vĩnh viễn ${count} bản ghi khỏi hệ thống.`)
      }
    },
    onError: (err: any) => {
      notify.error('Có lỗi xảy ra khi xóa hàng loạt.', err)
    },
  })

  const emptyTrashMutation = useMutation({
    mutationFn: (type?: string) => recycleBinService.emptyRecycleBin(type === 'All' ? undefined : type),
    onSuccess: () => {
      handleRefresh()
      queryClient.invalidateQueries()
      setSelectedItems([])
      setEmptyTrashModal(false)
      notify.success('Đã dọn sạch thùng rác thành công!')
    },
    onError: (err: any) => {
      notify.error('Có lỗi xảy ra khi dọn sạch thùng rác.', err)
    },
  })

  // --- SELECTION LOGIC ---
  const isSelected = (item: RecycleBinItem) =>
    selectedItems.some((s) => s.itemType === item.itemType && s.id === item.id)

  const toggleSelect = (item: RecycleBinItem) => {
    setSelectedItems((prev) =>
      isSelected(item)
        ? prev.filter((s) => !(s.itemType === item.itemType && s.id === item.id))
        : [...prev, { itemType: item.itemType, id: item.id }]
    )
  }

  const isAllSelected = items.length > 0 && items.every(isSelected)

  const toggleSelectAll = () => {
    if (isAllSelected) {
      const pageKeys = new Set(items.map((i) => `${i.itemType}_${i.id}`))
      setSelectedItems((prev) => prev.filter((s) => !pageKeys.has(`${s.itemType}_${s.id}`)))
    } else {
      const newItems = items
        .filter((i) => !isSelected(i))
        .map((i) => ({ itemType: i.itemType, id: i.id }))
      setSelectedItems((prev) => [...prev, ...newItems])
    }
  }

  // --- TAB DEFINITIONS ---
  const tabs = [
    { id: 'All', label: 'Tất cả', count: counts?.totalCount || 0, icon: Trash2 },
    { id: 'Vehicle', label: 'Phương tiện', count: counts?.vehicleCount || 0, icon: Car },
    { id: 'Person', label: 'Nhân sự', count: counts?.personCount || 0, icon: User },
    { id: 'Contractor', label: 'Đối tác / Nhà thầu', count: counts?.contractorCount || 0, icon: Handshake },
    { id: 'Department', label: 'Phòng ban', count: counts?.departmentCount || 0, icon: Building2 },
    { id: 'Company', label: 'Công ty', count: counts?.companyCount || 0, icon: Building },
    { id: 'Device', label: 'Thiết bị', count: counts?.deviceCount || 0, icon: Camera },
    { id: 'Lane', label: 'Làn kiểm soát', count: counts?.laneCount || 0, icon: Route },
    { id: 'ParkingSession', label: 'Lượt gửi xe', count: counts?.parkingSessionCount || 0, icon: History },
  ]

  // Helper render type badge
  const renderTypeBadge = (itemType: string) => {
    switch (itemType) {
      case 'Vehicle':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-medium bg-blue-50 text-blue-700 border border-blue-200 dark:bg-blue-950/40 dark:text-blue-300 dark:border-blue-800">
            <Car className="h-3 w-3 text-blue-500" />
            Phương tiện
          </span>
        )
      case 'Person':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-medium bg-emerald-50 text-emerald-700 border border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-300 dark:border-emerald-800">
            <User className="h-3 w-3 text-emerald-500" />
            Nhân sự
          </span>
        )
      case 'Contractor':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-medium bg-amber-50 text-amber-700 border border-amber-200 dark:bg-amber-950/40 dark:text-amber-300 dark:border-amber-800">
            <Handshake className="h-3 w-3 text-amber-500" />
            Đối tác
          </span>
        )
      case 'Department':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-medium bg-purple-50 text-purple-700 border border-purple-200 dark:bg-purple-950/40 dark:text-purple-300 dark:border-purple-800">
            <Building2 className="h-3 w-3 text-purple-500" />
            Phòng ban
          </span>
        )
      case 'Company':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-medium bg-indigo-50 text-indigo-700 border border-indigo-200 dark:bg-indigo-950/40 dark:text-indigo-300 dark:border-indigo-800">
            <Building className="h-3 w-3 text-indigo-500" />
            Công ty
          </span>
        )
      case 'Device':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-medium bg-cyan-50 text-cyan-700 border border-cyan-200 dark:bg-cyan-950/40 dark:text-cyan-300 dark:border-cyan-800">
            <Camera className="h-3 w-3 text-cyan-500" />
            Thiết bị
          </span>
        )
      case 'Lane':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-medium bg-rose-50 text-rose-700 border border-rose-200 dark:bg-rose-950/40 dark:text-rose-300 dark:border-rose-800">
            <Route className="h-3 w-3 text-rose-500" />
            Làn kiểm soát
          </span>
        )
      default:
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-medium bg-slate-50 text-slate-700 border border-slate-200 dark:bg-slate-900 dark:text-slate-300 dark:border-slate-800">
            <History className="h-3 w-3 text-slate-500" />
            Lượt gửi
          </span>
        )
    }
  }

  return (
    <div className="space-y-6 pb-24">
      {/* HEADER */}
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 bg-white dark:bg-slate-900 p-6 rounded-2xl border border-slate-200/80 dark:border-slate-800 shadow-sm">
        <div className="flex items-center gap-3">
          <div className="p-3 bg-rose-50 dark:bg-rose-950/50 rounded-xl border border-rose-200/60 dark:border-rose-900/60 text-rose-600 dark:text-rose-400 shadow-2xs">
            <Trash2 className="h-6 w-6" />
          </div>
          <div>
            <h1 className="text-xl font-bold tracking-tight text-slate-900 dark:text-slate-100 flex items-center gap-2.5">
              Thùng Rác Hệ Thống
              {counts && counts.totalCount > 0 && (
                <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-rose-100 text-rose-700 dark:bg-rose-950 dark:text-rose-300 border border-rose-300 dark:border-rose-800">
                  {counts.totalCount} mục
                </span>
              )}
            </h1>
            <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
              Quản lý, tìm kiếm, khôi phục hoặc xóa vĩnh viễn các bản ghi đã xóa mềm khỏi hệ thống
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2.5">
          <Button
            variant="outline"
            size="sm"
            onClick={handleRefresh}
            className="h-9 px-3 text-xs font-medium cursor-pointer border-slate-200 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800"
            title="Làm mới dữ liệu"
          >
            <RefreshCw className="h-3.5 w-3.5 mr-1.5 text-slate-500" />
            Làm mới
          </Button>

          {counts && counts.totalCount > 0 && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => setEmptyTrashModal(true)}
              className="h-9 px-3.5 text-xs font-semibold text-rose-600 hover:text-rose-700 border-rose-200 hover:bg-rose-50 dark:text-rose-400 dark:border-rose-900/60 dark:hover:bg-rose-950/50 cursor-pointer shadow-2xs"
              title="Xóa vĩnh viễn toàn bộ mục trong thùng rác"
            >
              <Trash2 className="h-3.5 w-3.5 mr-1.5 text-rose-500" />
              Dọn sạch thùng rác
            </Button>
          )}
        </div>
      </div>

      {/* TABS LỌC NHÓM THỰC THỂ */}
      <div className="flex items-center gap-2 overflow-x-auto pb-1 no-scrollbar">
        {tabs.map((t) => {
          const Icon = t.icon
          const isActive = selectedTab === t.id
          return (
            <button
              key={t.id}
              onClick={() => {
                setSelectedTab(t.id)
                setPage(1)
                setSelectedItems([])
              }}
              className={`flex items-center gap-2 px-3.5 py-2 rounded-lg text-xs font-semibold transition-all whitespace-nowrap cursor-pointer border ${
                isActive
                  ? 'bg-blue-600 text-white border-blue-600 shadow-xs'
                  : 'bg-white text-slate-600 border-slate-200 hover:bg-slate-50 hover:text-slate-900 dark:bg-[#0f172a] dark:text-slate-400 dark:border-[#1e2d3d] dark:hover:bg-[#1a2845] dark:hover:text-slate-200'
              }`}
            >
              <Icon className={`h-3.5 w-3.5 ${isActive ? 'text-white' : 'text-slate-400 dark:text-slate-500'}`} />
              <span>{t.label}</span>
              <span
                className={`px-1.5 py-0.5 rounded-full text-[10px] font-bold leading-none ${
                  isActive
                    ? 'bg-white/20 text-white'
                    : t.count > 0
                    ? 'bg-rose-50 text-rose-600 dark:bg-rose-950/60 dark:text-rose-300 border border-rose-200 dark:border-rose-900/60'
                    : 'bg-slate-100 text-slate-400 dark:bg-slate-800 dark:text-slate-500'
                }`}
              >
                {t.count}
              </span>
            </button>
          )
        })}
      </div>

      {/* FILTER & SEARCH */}
      <div className="flex flex-col sm:flex-row items-center justify-between gap-3 bg-white dark:bg-slate-900 p-4 rounded-xl border border-slate-200/80 dark:border-slate-800 shadow-2xs">
        <div className="relative w-full sm:w-96">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
          <Input
            value={searchTerm}
            onChange={(e) => {
              setSearchTerm(e.target.value)
              setPage(1)
            }}
            placeholder="Tìm theo mã, biển số, họ tên, phòng ban..."
            className="pl-9 h-9 text-xs bg-slate-50/50 dark:bg-slate-950/50 border-slate-200 dark:border-slate-800 rounded-lg focus-visible:ring-1"
          />
          {searchTerm && (
            <button
              onClick={() => {
                setSearchTerm('')
                setPage(1)
              }}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
            >
              <X className="h-3.5 w-3.5" />
            </button>
          )}
        </div>

        <div className="flex items-center gap-2 self-end sm:self-auto text-xs text-slate-500">
          <span>Hiển thị:</span>
          <select
            value={pageSize}
            onChange={(e) => {
              setPageSize(Number(e.target.value))
              setPage(1)
            }}
            className="h-8 px-2 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-md text-xs font-medium focus:outline-none"
          >
            <option value={10}>10 dòng</option>
            <option value={20}>20 dòng</option>
            <option value={50}>50 dòng</option>
            <option value={100}>100 dòng</option>
          </select>
        </div>
      </div>

      {/* TABLE */}
      <div className="bg-white dark:bg-slate-900 rounded-xl border border-slate-200/80 dark:border-slate-800 shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs border-collapse">
            <thead>
              <tr className="bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 text-slate-500 dark:text-slate-400 font-semibold select-none">
                <th className="p-3.5 pl-4 w-10 text-center">
                  <input
                    type="checkbox"
                    checked={isAllSelected}
                    onChange={toggleSelectAll}
                    className="rounded border-slate-300 text-blue-600 focus:ring-blue-500 cursor-pointer h-4 w-4"
                  />
                </th>
                <th className="p-3.5 w-36">Phân Loại</th>
                <th className="p-3.5 w-40">Mã / Biển Số</th>
                <th className="p-3.5 w-56">Tên / Tiêu Đề</th>
                <th className="p-3.5">Thông Tin Chi Tiết & Ràng Buộc</th>
                <th className="p-3.5 w-36">Thời Điểm Xóa</th>
                <th className="p-3.5 pr-4 w-44 text-right">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {isLoading ? (
                <tr>
                  <td colSpan={7} className="p-12 text-center text-slate-400">
                    <div className="flex flex-col items-center justify-center gap-2">
                      <RefreshCw className="h-6 w-6 animate-spin text-blue-500" />
                      <span>Đang tải dữ liệu thùng rác...</span>
                    </div>
                  </td>
                </tr>
              ) : items.length > 0 ? (
                items.map((item) => {
                  const selected = isSelected(item)
                  return (
                    <tr
                      key={`${item.itemType}_${item.id}`}
                      className={`hover:bg-slate-50/80 dark:hover:bg-slate-800/40 transition-colors ${
                        selected ? 'bg-blue-50/40 dark:bg-blue-950/20' : ''
                      }`}
                    >
                      <td className="p-3.5 pl-4 text-center">
                        <input
                          type="checkbox"
                          checked={selected}
                          onChange={() => toggleSelect(item)}
                          className="rounded border-slate-300 text-blue-600 focus:ring-blue-500 cursor-pointer h-4 w-4"
                        />
                      </td>
                      <td className="p-3.5 whitespace-nowrap">{renderTypeBadge(item.itemType)}</td>
                      <td className="p-3.5 font-mono font-bold text-slate-800 dark:text-slate-200">
                        {item.identifier || '--'}
                      </td>
                      <td className="p-3.5 font-medium text-slate-900 dark:text-slate-100">
                        {item.title}
                      </td>
                      <td className="p-3.5 text-slate-600 dark:text-slate-300">
                        <div>{item.description || '--'}</div>
                        {item.warningMessage && (
                          <div className="flex items-center gap-1.5 text-amber-600 dark:text-amber-400 text-[11px] font-medium mt-1 bg-amber-50 dark:bg-amber-950/40 px-2 py-0.5 rounded border border-amber-200/60 dark:border-amber-900/40 w-fit">
                            <AlertTriangle className="h-3 w-3 shrink-0" />
                            <span>{item.warningMessage}</span>
                          </div>
                        )}
                      </td>
                      <td className="p-3.5 text-slate-500 dark:text-slate-400 font-mono text-[11px] whitespace-nowrap">
                        {item.deletedAt ? new Date(item.deletedAt).toLocaleString('vi-VN') : '--'}
                      </td>
                      <td className="p-3.5 pr-4 text-right whitespace-nowrap">
                        <div className="flex items-center justify-end gap-1.5">
                          {/* Nút Khôi Phục */}
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => setSingleActionModal({ isOpen: true, action: 'restore', item })}
                            className="h-7 px-2.5 text-emerald-600 hover:text-emerald-700 border-emerald-200 hover:bg-emerald-50 dark:text-emerald-400 dark:border-emerald-900/60 dark:hover:bg-emerald-950/50 text-[11px] font-semibold cursor-pointer shadow-2xs"
                            title="Khôi phục bản ghi này về trạng thái hoạt động"
                          >
                            <RotateCcw className="h-3.5 w-3.5 mr-1 text-emerald-500" />
                            Khôi phục
                          </Button>

                          {/* Nút Xóa Vĩnh Viễn */}
                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() => setSingleActionModal({ isOpen: true, action: 'hard-delete', item })}
                            className="h-7 w-7 p-0 text-slate-400 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-950/50 rounded-lg cursor-pointer transition-colors"
                            title="Xóa vĩnh viễn khỏi CSDL (Không thể hoàn tác)"
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
                  <td colSpan={7} className="p-12 text-center text-slate-400 italic">
                    <div className="flex flex-col items-center justify-center gap-2">
                      <Trash2 className="h-8 w-8 text-slate-300 dark:text-slate-700 stroke-[1.5]" />
                      <p>Thùng rác đang trống. Không có bản ghi nào bị xóa mềm.</p>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {/* PAGINATION */}
        <div className="p-4 bg-slate-50/50 dark:bg-slate-800/30 border-t border-slate-200 dark:border-slate-800 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-slate-500">
          <div>
            Hiển thị{' '}
            <span className="font-semibold text-slate-700 dark:text-slate-300">
              {totalCount === 0 ? 0 : (page - 1) * pageSize + 1} - {Math.min(page * pageSize, totalCount)}
            </span>{' '}
            trong tổng số{' '}
            <span className="font-semibold text-slate-700 dark:text-slate-300">{totalCount}</span> mục
          </div>

          <div className="flex items-center gap-1">
            <Button
              size="sm"
              variant="outline"
              onClick={() => setPage(1)}
              disabled={page <= 1}
              className="h-8 w-8 p-0 cursor-pointer"
            >
              <ChevronsLeft className="h-3.5 w-3.5" />
            </Button>
            <Button
              size="sm"
              variant="outline"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page <= 1}
              className="h-8 w-8 p-0 cursor-pointer"
            >
              <ChevronLeft className="h-3.5 w-3.5" />
            </Button>

            <span className="px-3 py-1 font-semibold text-slate-700 dark:text-slate-300">
              Trang {page} / {totalPages}
            </span>

            <Button
              size="sm"
              variant="outline"
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page >= totalPages}
              className="h-8 w-8 p-0 cursor-pointer"
            >
              <ChevronRight className="h-3.5 w-3.5" />
            </Button>
            <Button
              size="sm"
              variant="outline"
              onClick={() => setPage(totalPages)}
              disabled={page >= totalPages}
              className="h-8 w-8 p-0 cursor-pointer"
            >
              <ChevronsRight className="h-3.5 w-3.5" />
            </Button>
          </div>
        </div>
      </div>

      {/* FLOATING BULK ACTION BAR (GÓC DƯỚI BÊN PHẢI) */}
      {selectedItems.length > 0 && (
        <div className="fixed bottom-6 right-6 z-50 flex items-center gap-3 bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900 px-4 py-3 rounded-2xl shadow-2xl border border-slate-700 dark:border-slate-300 animate-in fade-in slide-in-from-bottom-5 duration-200">
          <div className="flex items-center gap-2 pr-2 border-r border-slate-700 dark:border-slate-300 text-xs font-semibold">
            <span className="flex h-2 w-2 rounded-full bg-blue-500 animate-pulse" />
            <span>Đã chọn {selectedItems.length} mục</span>
          </div>

          <div className="flex items-center gap-2">
            <Button
              size="sm"
              onClick={() => setBatchActionModal({ isOpen: true, action: 'restore' })}
              className="h-8 px-3 text-xs font-semibold bg-emerald-600 hover:bg-emerald-700 text-white cursor-pointer shadow-sm rounded-lg"
            >
              <RotateCcw className="h-3.5 w-3.5 mr-1.5" />
              Khôi phục {selectedItems.length} mục
            </Button>

            <Button
              size="sm"
              onClick={() => setBatchActionModal({ isOpen: true, action: 'hard-delete' })}
              className="h-8 px-3 text-xs font-semibold bg-rose-600 hover:bg-rose-700 text-white cursor-pointer shadow-sm rounded-lg"
            >
              <Trash2 className="h-3.5 w-3.5 mr-1.5" />
              Xóa vĩnh viễn
            </Button>

            <Button
              size="sm"
              variant="ghost"
              onClick={() => setSelectedItems([])}
              className="h-8 w-8 p-0 text-slate-400 hover:text-white dark:hover:text-slate-900 cursor-pointer rounded-lg"
              title="Bỏ chọn tất cả"
            >
              <X className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}

      {/* CONFIRM MODAL: SINGLE ACTION */}
      <ConfirmDialog
        open={singleActionModal.isOpen}
        onOpenChange={(open) => setSingleActionModal((prev) => ({ ...prev, isOpen: open }))}
        title={
          singleActionModal.action === 'restore'
            ? 'Xác nhận khôi phục bản ghi'
            : 'CẢNH BÁO: Xóa vĩnh viễn bản ghi khỏi CSDL'
        }
        description={
          singleActionModal.action === 'restore' ? (
            <div>
              Bạn có chắc chắn muốn khôi phục {singleActionModal.item?.itemTypeLabel?.toLowerCase()}{' '}
              <strong className="text-slate-900 dark:text-white font-mono">
                {singleActionModal.item?.title || singleActionModal.item?.identifier}
              </strong>{' '}
              về trạng thái hoạt động bình thường không?
            </div>
          ) : (
            <div className="space-y-2 text-rose-600 dark:text-rose-400">
              <p>
                Thao tác này sẽ xóa vĩnh viễn{' '}
                <strong>
                  {singleActionModal.item?.itemTypeLabel} ({singleActionModal.item?.title})
                </strong>{' '}
                hoàn toàn khỏi cơ sở dữ liệu MongoDB và <u>KHÔNG THỂ HOÀN TÁC</u>.
              </p>
              <p className="text-xs text-slate-500 dark:text-slate-400">
                Hệ thống sẽ tự động kiểm tra tính toàn vẹn quan hệ (nếu có bản ghi con đang tham chiếu sẽ bị từ chối xóa để bảo vệ dữ liệu).
              </p>
            </div>
          )
        }
        confirmText={singleActionModal.action === 'restore' ? 'Khôi phục ngay' : 'Xác nhận XÓA VĨNH VIỄN'}
        variant={singleActionModal.action === 'restore' ? 'default' : 'destructive'}
        isLoading={
          singleActionModal.action === 'restore'
            ? restoreSingleMutation.isPending
            : hardDeleteSingleMutation.isPending
        }
        onConfirm={() => {
          if (!singleActionModal.item) return
          if (singleActionModal.action === 'restore') {
            restoreSingleMutation.mutate({
              itemType: singleActionModal.item.itemType,
              id: singleActionModal.item.id,
            })
          } else {
            hardDeleteSingleMutation.mutate({
              itemType: singleActionModal.item.itemType,
              id: singleActionModal.item.id,
            })
          }
        }}
      />

      {/* CONFIRM MODAL: BATCH ACTION */}
      <ConfirmDialog
        open={batchActionModal.isOpen}
        onOpenChange={(open) => setBatchActionModal((prev) => ({ ...prev, isOpen: open }))}
        title={
          batchActionModal.action === 'restore'
            ? `Khôi phục hàng loạt (${selectedItems.length} mục)`
            : `CẢNH BÁO: Xóa vĩnh viễn ${selectedItems.length} mục`
        }
        description={
          batchActionModal.action === 'restore' ? (
            <div>
              Bạn có chắc chắn muốn khôi phục đồng thời{' '}
              <strong className="text-slate-900 dark:text-white">{selectedItems.length} mục đã chọn</strong> về lại
              danh mục tương ứng không?
            </div>
          ) : (
            <div className="space-y-2 text-rose-600 dark:text-rose-400">
              <p>
                Thao tác này sẽ xóa vĩnh viễn <strong>{selectedItems.length} bản ghi đã chọn</strong> khỏi CSDL và không thể phục hồi.
              </p>
            </div>
          )
        }
        confirmText={batchActionModal.action === 'restore' ? 'Khôi phục tất cả' : 'Xóa vĩnh viễn tất cả'}
        variant={batchActionModal.action === 'restore' ? 'default' : 'destructive'}
        isLoading={
          batchActionModal.action === 'restore'
            ? restoreBatchMutation.isPending
            : hardDeleteBatchMutation.isPending
        }
        onConfirm={() => {
          if (batchActionModal.action === 'restore') {
            restoreBatchMutation.mutate(selectedItems)
          } else {
            hardDeleteBatchMutation.mutate(selectedItems)
          }
        }}
      />

      {/* CONFIRM MODAL: EMPTY TRASH */}
      <ConfirmDialog
        open={emptyTrashModal}
        onOpenChange={setEmptyTrashModal}
        title="DỌN SẠCH THÙNG RÁC HỆ THỐNG"
        description={
          <div className="space-y-2 text-rose-600 dark:text-rose-400">
            <p>
              Bạn có chắc chắn muốn dọn sạch toàn bộ thùng rác ({counts?.totalCount || 0} mục bị xóa mềm)?
            </p>
            <p className="text-xs text-slate-500 dark:text-slate-400">
              Tất cả các dữ liệu xe, nhân sự, đối tác, phòng ban... đang nằm trong thùng rác sẽ bị xóa hoàn toàn khỏi cơ sở dữ liệu.
            </p>
          </div>
        }
        confirmText="Xác nhận DỌN SẠCH"
        variant="destructive"
        isLoading={emptyTrashMutation.isPending}
        onConfirm={() => emptyTrashMutation.mutate(selectedTab)}
      />
    </div>
  )
}
export default RecycleBinPage
