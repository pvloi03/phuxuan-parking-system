import { useState, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  ShieldAlert,
  Search,
  RefreshCw,
  Eye,
  CheckCircle2,
  XCircle,
  ChevronLeft,
  ChevronRight,
  Activity,
  UserCheck,
  AlertOctagon,
  Download,
} from 'lucide-react'
import { auditLogService } from '@/services/auditLogService'
import type { AuditLogQueryParams } from '@/services/auditLogService'
import type { AuditLog, AuditActionType } from '@/types'
import { AuditLogDetailDrawer } from '@/components/audit/AuditLogDetailDrawer'
import { actionTypeConfig, targetEntityList } from '@/lib/auditConfig'
import { cn } from '@/lib/utils'

const toEndOfDayIso = (d: string) => {
  const date = new Date(d)
  date.setHours(23, 59, 59, 999)
  return date.toISOString()
}

export function AuditLogsPage() {
  const [search, setSearch] = useState('')
  const [selectedAction, setSelectedAction] = useState<string>('')
  const [selectedEntity, setSelectedEntity] = useState<string>('')
  const [fromDate, setFromDate] = useState<string>('')
  const [toDate, setToDate] = useState<string>('')
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(15)
  const [isExporting, setIsExporting] = useState(false)

  const [selectedLog, setSelectedLog] = useState<AuditLog | null>(null)
  const [isDrawerOpen, setIsDrawerOpen] = useState(false)

  const queryParams: AuditLogQueryParams = useMemo(() => ({
    pageNumber,
    pageSize,
    search: search.trim() || undefined,
    actionType: selectedAction || undefined,
    targetEntity: selectedEntity || undefined,
    fromDate: fromDate ? new Date(fromDate).toISOString() : undefined,
    toDate: toDate ? toEndOfDayIso(toDate) : undefined,
  }), [search, selectedAction, selectedEntity, fromDate, toDate, pageNumber, pageSize])

  const { data, isLoading, isFetching, isError, error, refetch } = useQuery({
    queryKey: ['audit-logs', queryParams],
    queryFn: () => auditLogService.getLogs(queryParams),
  })

  const logs = data?.items || []
  const totalCount = data?.totalCount || 0
  const totalPages = data?.totalPages || 1

  const handleOpenDetail = (log: AuditLog) => {
    setSelectedLog(log)
    setIsDrawerOpen(true)
  }

  const handleResetFilters = () => {
    setSearch('')
    setSelectedAction('')
    setSelectedEntity('')
    setFromDate('')
    setToDate('')
    setPageNumber(1)
  }

  const handleExport = async () => {
    try {
      setIsExporting(true)
      await auditLogService.exportLogs(queryParams)
    } catch (err) {
      console.error('Export failed:', err)
    } finally {
      setIsExporting(false)
    }
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100 flex items-center gap-2.5">
            <ShieldAlert className="w-7 h-7 text-indigo-600 dark:text-indigo-400" />
            Nhật Ký Kiểm Toán (Audit Logs)
          </h1>
          <p className="text-sm text-slate-500 dark:text-slate-400 mt-1">
            Theo dõi, giám sát toàn bộ hoạt động quản trị, bảo mật và thay đổi dữ liệu trên Web Admin
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={handleExport}
            disabled={isExporting}
            className="flex items-center gap-2 px-3.5 py-2 text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 rounded-lg shadow-xs transition-colors"
          >
            <Download className={cn('w-4 h-4', isExporting && 'animate-bounce')} />
            {isExporting ? 'Đang Xuất...' : 'Xuất Excel'}
          </button>

          <button
            onClick={() => refetch()}
            disabled={isFetching}
            className="flex items-center gap-2 px-3.5 py-2 text-sm font-medium text-slate-700 dark:text-slate-300 bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-700 hover:bg-slate-50 dark:hover:bg-slate-750 rounded-lg shadow-xs transition-colors"
          >
            <RefreshCw className={cn('w-4 h-4', isFetching && 'animate-spin text-indigo-600')} />
            Làm Mới
          </button>
        </div>
      </div>

      {/* Metrics Overview */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="p-4 bg-white dark:bg-slate-800/80 rounded-xl border border-slate-200 dark:border-slate-700/80 shadow-xs flex items-center gap-4">
          <div className="p-3 rounded-xl bg-indigo-50 dark:bg-indigo-950/50 text-indigo-600 dark:text-indigo-400">
            <Activity className="w-6 h-6" />
          </div>
          <div>
            <div className="text-xs font-medium text-slate-500 dark:text-slate-400">Tổng Số Bản Ghi</div>
            <div className="text-xl font-bold text-slate-900 dark:text-slate-100 mt-0.5">{totalCount}</div>
          </div>
        </div>

        <div className="p-4 bg-white dark:bg-slate-800/80 rounded-xl border border-slate-200 dark:border-slate-700/80 shadow-xs flex items-center gap-4">
          <div className="p-3 rounded-xl bg-emerald-50 dark:bg-emerald-950/50 text-emerald-600 dark:text-emerald-400">
            <UserCheck className="w-6 h-6" />
          </div>
          <div>
            <div className="text-xs font-medium text-slate-500 dark:text-slate-400">Trang Hiện Tại</div>
            <div className="text-xl font-bold text-slate-900 dark:text-slate-100 mt-0.5">
              {pageNumber} / {totalPages}
            </div>
          </div>
        </div>

        <div className="p-4 bg-white dark:bg-slate-800/80 rounded-xl border border-slate-200 dark:border-slate-700/80 shadow-xs flex items-center gap-4">
          <div className="p-3 rounded-xl bg-amber-50 dark:bg-amber-950/50 text-amber-600 dark:text-amber-400">
            <AlertOctagon className="w-6 h-6" />
          </div>
          <div>
            <div className="text-xs font-medium text-slate-500 dark:text-slate-400">Lưu Trữ Tự Động (TTL)</div>
            <div className="text-sm font-semibold text-slate-900 dark:text-slate-100 mt-0.5">12 Tháng (365 Ngày)</div>
          </div>
        </div>
      </div>

      {/* Filter Bar */}
      <div className="p-4 bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 shadow-xs space-y-3">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3">
          {/* Search */}
          <div className="relative">
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-3" />
            <input
              type="text"
              placeholder="Tìm người dùng, thực thể, lý do..."
              value={search}
              onChange={(e) => {
                setSearch(e.target.value)
                setPageNumber(1)
              }}
              className="w-full pl-9 pr-3 py-2 text-sm bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-lg focus:outline-hidden focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 text-slate-900 dark:text-slate-100 placeholder-slate-400"
            />
          </div>

          {/* Action Filter */}
          <div>
            <select
              value={selectedAction}
              onChange={(e) => {
                setSelectedAction(e.target.value)
                setPageNumber(1)
              }}
              className="w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-lg focus:outline-hidden focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 text-slate-900 dark:text-slate-100"
            >
              <option value="">Tất cả loại hành động</option>
              {Object.keys(actionTypeConfig).map((act) => (
                <option key={act} value={act}>
                  {actionTypeConfig[act as AuditActionType]?.label || act}
                </option>
              ))}
            </select>
          </div>

          {/* Entity Filter */}
          <div>
            <select
              value={selectedEntity}
              onChange={(e) => {
                setSelectedEntity(e.target.value)
                setPageNumber(1)
              }}
              className="w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-lg focus:outline-hidden focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 text-slate-900 dark:text-slate-100"
            >
              <option value="">Tất cả thực thể</option>
              {targetEntityList.map((ent) => (
                <option key={ent} value={ent}>
                  {ent}
                </option>
              ))}
            </select>
          </div>

          {/* From Date */}
          <div>
            <input
              type="date"
              value={fromDate}
              onChange={(e) => {
                setFromDate(e.target.value)
                setPageNumber(1)
              }}
              className="w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-lg focus:outline-hidden focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 text-slate-900 dark:text-slate-100"
            />
          </div>

          {/* To Date */}
          <div>
            <input
              type="date"
              value={toDate}
              onChange={(e) => {
                setToDate(e.target.value)
                setPageNumber(1)
              }}
              className="w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-lg focus:outline-hidden focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 text-slate-900 dark:text-slate-100"
            />
          </div>
        </div>

        {(search || selectedAction || selectedEntity || fromDate || toDate) && (
          <div className="flex items-center justify-between pt-1">
            <div className="text-xs text-slate-500 dark:text-slate-400">
              Đang lọc theo tiêu chí tùy chọn
            </div>
            <button
              onClick={handleResetFilters}
              className="text-xs font-medium text-indigo-600 dark:text-indigo-400 hover:underline"
            >
              Đặt lại bộ lọc
            </button>
          </div>
        )}
      </div>

      {/* Data Table */}
      <div className="bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 shadow-xs overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 dark:bg-slate-900/50 border-b border-slate-200 dark:border-slate-700 text-xs uppercase font-semibold text-slate-600 dark:text-slate-400">
              <tr>
                <th className="px-4 py-3.5">Thời Gian</th>
                <th className="px-4 py-3.5">Người Thực Hiện</th>
                <th className="px-4 py-3.5">Hành Động</th>
                <th className="px-4 py-3.5">Thực Thể</th>
                <th className="px-4 py-3.5">Đối Tượng</th>
                <th className="px-4 py-3.5">Trạng Thái</th>
                <th className="px-4 py-3.5 text-right">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              {isLoading ? (
                <tr>
                  <td colSpan={7} className="px-4 py-12 text-center text-slate-500 dark:text-slate-400">
                    <div className="flex items-center justify-center gap-2">
                      <RefreshCw className="w-5 h-5 animate-spin text-indigo-600" />
                      <span>Đang tải nhật ký kiểm toán...</span>
                    </div>
                  </td>
                </tr>
              ) : isError ? (
                <tr>
                  <td colSpan={7} className="px-4 py-12 text-center text-rose-600 dark:text-rose-400">
                    Không thể tải dữ liệu: {(error as any)?.response?.data?.message || (error as any)?.message || 'Lỗi kết nối máy chủ'}.
                  </td>
                </tr>
              ) : logs.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-4 py-12 text-center text-slate-500 dark:text-slate-400">
                    Không tìm thấy bản ghi nhật ký kiểm toán nào phù hợp với bộ lọc.
                  </td>
                </tr>
              ) : (
                logs.map((log) => {
                  const actCfg = actionTypeConfig[log.actionType] || {
                    label: log.actionType,
                    bg: 'bg-slate-50',
                    text: 'text-slate-700',
                    border: 'border-slate-200',
                  }

                  return (
                    <tr
                      key={log.id}
                      onClick={() => handleOpenDetail(log)}
                      className="hover:bg-slate-50/80 dark:hover:bg-slate-750/50 cursor-pointer transition-colors"
                    >
                      {/* Timestamp */}
                      <td className="px-4 py-3 text-xs text-slate-600 dark:text-slate-300 font-mono whitespace-nowrap">
                        {new Date(log.createdAt).toLocaleString('vi-VN')}
                      </td>

                      {/* Actor */}
                      <td className="px-4 py-3 text-xs">
                        <div className="font-semibold text-slate-900 dark:text-slate-100 flex items-center gap-1.5">
                          <span>{log.actorUsername || 'Hệ Thống'}</span>
                          {log.actorRole && (
                            <span className="px-1.5 py-0.2 text-[10px] font-normal bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300 rounded">
                              {log.actorRole}
                            </span>
                          )}
                        </div>
                      </td>

                      {/* Action */}
                      <td className="px-4 py-3 text-xs">
                        <span
                          className={cn(
                            'px-2.5 py-0.5 text-xs font-semibold rounded-full border inline-block whitespace-nowrap',
                            actCfg.bg,
                            actCfg.text,
                            actCfg.border
                          )}
                        >
                          {actCfg.label}
                        </span>
                      </td>

                      {/* Target Entity */}
                      <td className="px-4 py-3 text-xs font-medium text-slate-700 dark:text-slate-300 whitespace-nowrap">
                        {log.targetEntity}
                      </td>

                      {/* Target Display */}
                      <td className="px-4 py-3 text-xs font-mono text-slate-800 dark:text-slate-200 max-w-xs truncate">
                        {log.targetDisplay || log.targetId || '-'}
                      </td>

                      {/* Status */}
                      <td className="px-4 py-3 text-xs whitespace-nowrap">
                        {log.isSuccess ? (
                          <span className="flex items-center gap-1 text-emerald-600 dark:text-emerald-400 font-medium">
                            <CheckCircle2 className="w-4 h-4 shrink-0" />
                            Thành công
                          </span>
                        ) : (
                          <span className="flex items-center gap-1 text-rose-600 dark:text-rose-400 font-medium">
                            <XCircle className="w-4 h-4 shrink-0" />
                            Thất bại
                          </span>
                        )}
                      </td>

                      {/* Actions */}
                      <td className="px-4 py-3 text-xs text-right whitespace-nowrap">
                        <button
                          onClick={(e) => {
                            e.stopPropagation()
                            handleOpenDetail(log)
                          }}
                          className="p-1.5 text-indigo-600 dark:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-950/50 rounded-lg transition-colors inline-flex items-center gap-1 font-medium"
                        >
                          <Eye className="w-4 h-4" />
                          <span>Chi Tiết</span>
                        </button>
                      </td>
                    </tr>
                  )
                })
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        <div className="px-4 py-3.5 border-t border-slate-200 dark:border-slate-700 bg-slate-50/50 dark:bg-slate-900/30 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-slate-600 dark:text-slate-400">
          <div className="flex items-center gap-2">
            <span>Hiển thị</span>
            <select
              value={pageSize}
              onChange={(e) => {
                setPageSize(Number(e.target.value))
                setPageNumber(1)
              }}
              className="px-2 py-1 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded text-slate-900 dark:text-slate-100"
            >
              <option value={10}>10</option>
              <option value={15}>15</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
            </select>
            <span>trên tổng số <strong>{totalCount}</strong> bản ghi</span>
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
              disabled={pageNumber <= 1 || isLoading}
              className="p-1.5 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
            <span className="font-medium text-slate-900 dark:text-slate-100">
              Trang {pageNumber} / {totalPages}
            </span>
            <button
              onClick={() => setPageNumber((p) => Math.min(totalPages, p + 1))}
              disabled={pageNumber >= totalPages || isLoading}
              className="p-1.5 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>

      {/* Detail & Visual Diff Drawer */}
      <AuditLogDetailDrawer
        log={selectedLog}
        isOpen={isDrawerOpen}
        onClose={() => {
          setIsDrawerOpen(false)
          setSelectedLog(null)
        }}
      />
    </div>
  )
}
