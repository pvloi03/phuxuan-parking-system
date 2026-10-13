import { useMemo } from 'react'
import {
  X,
  User,
  Calendar,
  Layers,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  FileText,
  Shield,
} from 'lucide-react'
import type { AuditLog } from '@/types'
import { actionTypeConfig } from '@/lib/auditConfig'
import { cn } from '@/lib/utils'

interface AuditLogDetailDrawerProps {
  log: AuditLog | null
  isOpen: boolean
  onClose: () => void
}

export function AuditLogDetailDrawer({ log, isOpen, onClose }: AuditLogDetailDrawerProps) {
  if (!isOpen || !log) return null

  const actionCfg = actionTypeConfig[log.actionType] || {
    label: log.actionType,
    bg: 'bg-gray-50',
    text: 'text-gray-700',
    border: 'border-gray-200',
  }

  const safeParse = (json?: string) => {
    if (!json) return {}
    try { return JSON.parse(json) } catch { return {} }
  }

  const oldDict = useMemo(() => safeParse(log.oldValues), [log.oldValues])
  const newDict = useMemo(() => safeParse(log.newValues), [log.newValues])

  const allKeys = useMemo(() => Array.from(new Set([
    ...(log.changedProperties || []),
    ...Object.keys(oldDict),
    ...Object.keys(newDict),
  ])), [log.changedProperties, oldDict, newDict])

  return (
    <div className="fixed inset-0 z-50 overflow-hidden">
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-black/40 backdrop-blur-xs transition-opacity animate-in fade-in"
        onClick={onClose}
      />

      <div className="fixed inset-y-0 right-0 max-w-full flex pl-10">
        <div className="w-screen max-w-2xl bg-white dark:bg-slate-900 shadow-2xl border-l border-slate-200 dark:border-slate-800 flex flex-col transform transition ease-in-out duration-300">
          
          {/* Header */}
          <div className="px-6 py-5 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between bg-slate-50/50 dark:bg-slate-900/50">
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-lg bg-indigo-50 dark:bg-indigo-950/40 text-indigo-600 dark:text-indigo-400">
                <FileText className="w-5 h-5" />
              </div>
              <div>
                <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
                  Chi Tiết Nhật Ký Kiểm Toán
                </h2>
                <p className="text-xs text-slate-500 dark:text-slate-400">
                  Mã bản ghi: <span className="font-mono text-slate-700 dark:text-slate-300">{log.id}</span>
                </p>
              </div>
            </div>

            <div className="flex items-center gap-2">
              <span
                className={cn(
                  'px-2.5 py-1 text-xs font-semibold rounded-full border',
                  actionCfg.bg,
                  actionCfg.text,
                  actionCfg.border
                )}
              >
                {actionCfg.label}
              </span>
              <button
                onClick={onClose}
                className="p-1.5 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg transition-colors"
              >
                <X className="w-5 h-5" />
              </button>
            </div>
          </div>

          {/* Content Body */}
          <div className="flex-1 overflow-y-auto p-6 space-y-6">
            
            {/* Summary Cards */}
            <div className="grid grid-cols-2 gap-4">
              <div className="p-3.5 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-200/80 dark:border-slate-700/60">
                <div className="flex items-center gap-2 text-xs font-medium text-slate-500 dark:text-slate-400 mb-1">
                  <User className="w-3.5 h-3.5" />
                  Người Thực Hiện
                </div>
                <div className="text-sm font-semibold text-slate-900 dark:text-slate-100 flex items-center gap-2">
                  <span>{log.actorUsername || 'Hệ Thống'}</span>
                  {log.actorRole && (
                    <span className="px-1.5 py-0.5 text-[10px] font-medium bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-300 rounded">
                      {log.actorRole}
                    </span>
                  )}
                </div>
              </div>

              <div className="p-3.5 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-200/80 dark:border-slate-700/60">
                <div className="flex items-center gap-2 text-xs font-medium text-slate-500 dark:text-slate-400 mb-1">
                  <Calendar className="w-3.5 h-3.5" />
                  Thời Điểm
                </div>
                <div className="text-sm font-semibold text-slate-900 dark:text-slate-100">
                  {new Date(log.createdAt).toLocaleString('vi-VN')}
                </div>
              </div>

              <div className="p-3.5 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-200/80 dark:border-slate-700/60">
                <div className="flex items-center gap-2 text-xs font-medium text-slate-500 dark:text-slate-400 mb-1">
                  <Layers className="w-3.5 h-3.5" />
                  Thực Thể Tác Động
                </div>
                <div className="text-sm font-semibold text-slate-900 dark:text-slate-100">
                  {log.targetEntity}{' '}
                  {log.targetDisplay && (
                    <span className="text-indigo-600 dark:text-indigo-400 font-normal">
                      ({log.targetDisplay})
                    </span>
                  )}
                </div>
              </div>

              <div className="p-3.5 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-200/80 dark:border-slate-700/60">
                <div className="flex items-center gap-2 text-xs font-medium text-slate-500 dark:text-slate-400 mb-1">
                  <Shield className="w-3.5 h-3.5" />
                  Kết Quả Thực Hiện
                </div>
                <div className="text-sm font-semibold flex items-center gap-1.5">
                  {log.isSuccess ? (
                    <>
                      <CheckCircle2 className="w-4 h-4 text-emerald-500" />
                      <span className="text-emerald-600 dark:text-emerald-400">Thành Công</span>
                    </>
                  ) : (
                    <>
                      <XCircle className="w-4 h-4 text-rose-500" />
                      <span className="text-rose-600 dark:text-rose-400">Thất Bại</span>
                    </>
                  )}
                </div>
              </div>
            </div>

            {/* Source */}
            <div className="flex items-center justify-between text-xs text-slate-500 dark:text-slate-400 px-1 py-1">
              <span>Nguồn thực hiện:</span>
              <span className="font-medium text-slate-700 dark:text-slate-300 bg-slate-100 dark:bg-slate-800 px-2 py-0.5 rounded border border-slate-200 dark:border-slate-700">
                {log.source || 'WebAdmin'}
              </span>
            </div>

            {/* Reason or Error Banner */}
            {log.reason && (
              <div className="p-4 rounded-xl bg-amber-50 dark:bg-amber-950/20 border border-amber-200 dark:border-amber-800/60">
                <div className="flex items-start gap-2.5">
                  <AlertTriangle className="w-4 h-4 text-amber-600 dark:text-amber-400 shrink-0 mt-0.5" />
                  <div>
                    <h4 className="text-xs font-semibold text-amber-900 dark:text-amber-300 uppercase tracking-wider">
                      Lý do can thiệp / thao tác:
                    </h4>
                    <p className="text-sm text-amber-800 dark:text-amber-200 mt-0.5">
                      {log.reason}
                    </p>
                  </div>
                </div>
              </div>
            )}

            {log.errorMessage && (
              <div className="p-4 rounded-xl bg-rose-50 dark:bg-rose-950/20 border border-rose-200 dark:border-rose-800/60">
                <div className="flex items-start gap-2.5">
                  <XCircle className="w-4 h-4 text-rose-600 dark:text-rose-400 shrink-0 mt-0.5" />
                  <div>
                    <h4 className="text-xs font-semibold text-rose-900 dark:text-rose-300 uppercase tracking-wider">
                      Chi tiết lỗi:
                    </h4>
                    <p className="text-sm text-rose-800 dark:text-rose-200 mt-0.5">
                      {log.errorMessage}
                    </p>
                  </div>
                </div>
              </div>
            )}

            {/* Visual Diff Section */}
            {allKeys.length > 0 && (
              <div className="space-y-3 pt-2">
                <div className="flex items-center justify-between">
                  <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100 flex items-center gap-2">
                    <span>So Sánh Dữ Liệu Thay Đổi (Visual Diff)</span>
                    <span className="px-2 py-0.5 text-xs bg-indigo-100 dark:bg-indigo-950/60 text-indigo-700 dark:text-indigo-300 rounded-full font-mono font-medium">
                      {allKeys.length} trường
                    </span>
                  </h3>
                </div>

                <div className="rounded-xl border border-slate-200 dark:border-slate-800 overflow-hidden divide-y divide-slate-200 dark:divide-slate-800 bg-white dark:bg-slate-900">
                  <div className="grid grid-cols-2 bg-slate-100 dark:bg-slate-800/80 px-4 py-2.5 text-xs font-semibold text-slate-600 dark:text-slate-300">
                    <div className="flex items-center gap-1.5 text-rose-600 dark:text-rose-400">
                      <span>Giá trị cũ (Before)</span>
                    </div>
                    <div className="flex items-center gap-1.5 text-emerald-600 dark:text-emerald-400 pl-3 border-l border-slate-200 dark:border-slate-700">
                      <span>Giá trị mới (After)</span>
                    </div>
                  </div>

                  {allKeys.map((key) => {
                    const oldVal = oldDict[key]
                    const newVal = newDict[key]
                    const isChanged = oldVal !== newVal

                    return (
                      <div key={key} className="p-3.5 space-y-1.5 hover:bg-slate-50/60 dark:hover:bg-slate-800/30 transition-colors">
                        <div className="text-xs font-mono font-medium text-slate-700 dark:text-slate-300 flex items-center gap-1.5">
                          <span className="w-1.5 h-1.5 rounded-full bg-indigo-500" />
                          <span>{key}</span>
                        </div>

                        <div className="grid grid-cols-2 gap-3 text-xs">
                          {/* Old Value */}
                          <div
                            className={cn(
                              'p-2 rounded-lg font-mono break-all',
                              isChanged && oldVal !== undefined
                                ? 'bg-rose-50/80 dark:bg-rose-950/40 text-rose-800 dark:text-rose-200 border border-rose-200/80 dark:border-rose-900/60'
                                : 'bg-slate-50 dark:bg-slate-800/40 text-slate-400 dark:text-slate-500'
                            )}
                          >
                            {oldVal !== undefined && oldVal !== null ? (
                              typeof oldVal === 'object' ? (
                                JSON.stringify(oldVal)
                              ) : (
                                String(oldVal)
                              )
                            ) : (
                              <span className="italic text-[11px] text-slate-400 dark:text-slate-600">(không có)</span>
                            )}
                          </div>

                          {/* New Value */}
                          <div
                            className={cn(
                              'p-2 rounded-lg font-mono break-all',
                              isChanged && newVal !== undefined
                                ? 'bg-emerald-50/80 dark:bg-emerald-950/40 text-emerald-800 dark:text-emerald-200 border border-emerald-200/80 dark:border-emerald-900/60'
                                : 'bg-slate-50 dark:bg-slate-800/40 text-slate-400 dark:text-slate-500'
                            )}
                          >
                            {newVal !== undefined && newVal !== null ? (
                              typeof newVal === 'object' ? (
                                JSON.stringify(newVal)
                              ) : (
                                String(newVal)
                              )
                            ) : (
                              <span className="italic text-[11px] text-slate-400 dark:text-slate-600">(đã xóa)</span>
                            )}
                          </div>
                        </div>
                      </div>
                    )
                  })}
                </div>
              </div>
            )}

          </div>

          {/* Footer */}
          <div className="px-6 py-4 border-t border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-900 flex justify-end">
            <button
              onClick={onClose}
              className="px-4 py-2 text-sm font-medium text-slate-700 dark:text-slate-300 bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-700 hover:bg-slate-100 dark:hover:bg-slate-700 rounded-lg shadow-xs transition-colors"
            >
              Đóng
            </button>
          </div>

        </div>
      </div>
    </div>
  )
}
