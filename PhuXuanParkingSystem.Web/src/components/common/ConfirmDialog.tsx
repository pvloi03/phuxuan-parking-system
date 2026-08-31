import React from 'react'
import {
  Dialog,
  DialogContent,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { AlertTriangle, Trash2 } from 'lucide-react'

export interface ConfirmDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title?: string
  description?: React.ReactNode
  confirmText?: string
  cancelText?: string
  variant?: 'destructive' | 'default'
  isLoading?: boolean
  icon?: React.ReactNode
  confirmIcon?: React.ReactNode
  onConfirm: () => void
}

export function ConfirmDialog({
  open,
  onOpenChange,
  title = 'Xác Nhận Xóa',
  description = 'Bạn có chắc chắn muốn thực hiện thao tác này? Thao tác không thể hoàn tác.',
  confirmText = 'Xác Nhận Xóa',
  cancelText = 'Hủy Bỏ',
  variant = 'destructive',
  isLoading = false,
  icon,
  confirmIcon,
  onConfirm,
}: ConfirmDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md p-0 overflow-hidden flex flex-col bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 shadow-2xl">
        <div className="p-6 flex items-start gap-4">
          <div className="h-11 w-11 rounded-2xl bg-red-100 dark:bg-red-950/60 text-red-600 dark:text-red-400 flex items-center justify-center shrink-0 shadow-xs">
            {icon ?? <AlertTriangle className="h-5 w-5" />}
          </div>
          <div className="space-y-1.5 flex-1 min-w-0">
            <DialogTitle className="text-base font-bold text-slate-900 dark:text-slate-100">
              {title}
            </DialogTitle>
            <div className="text-xs text-slate-500 dark:text-slate-400 leading-relaxed">
              {description}
            </div>
          </div>
        </div>

        <DialogFooter className="p-4 pt-3 border-t border-slate-200 dark:border-slate-800 gap-2 bg-slate-50/70 dark:bg-slate-900/60">
          <Button
            variant="outline"
            size="sm"
            onClick={() => onOpenChange(false)}
            disabled={isLoading}
            className="text-xs cursor-pointer"
          >
            {cancelText}
          </Button>
          <Button
            size="sm"
            variant={variant}
            onClick={() => {
              onConfirm()
            }}
            disabled={isLoading}
            className="gap-1.5 text-xs font-semibold cursor-pointer shadow-xs bg-red-600 hover:bg-red-700 text-white"
          >
            {isLoading ? (
              'Đang xử lý...'
            ) : (
              <>
                {confirmIcon ?? <Trash2 className="h-3.5 w-3.5" />}
                {confirmText}
              </>
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
