import type { PersonType } from '@/types'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'

interface PersonTypeMeta {
  label: string
  shortLabel: string
  variant?: 'outline' | 'secondary' | 'default'
  className: string
}

const PERSON_TYPE_MAP: Record<PersonType, PersonTypeMeta> = {
  Employee: {
    label: 'Cán bộ / Nhân viên',
    shortLabel: 'Cán bộ / NV',
    variant: 'outline',
    className: 'bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-950/40 dark:text-blue-300 dark:border-blue-800',
  },
  Contractor: {
    label: 'Đối tác / Nhà thầu',
    shortLabel: 'Đối tác / NT',
    variant: 'outline',
    className: 'bg-purple-50 text-purple-700 border-purple-200 dark:bg-purple-950/40 dark:text-purple-300 dark:border-purple-800',
  },
  VIP: {
    label: 'Khách VIP',
    shortLabel: 'Khách VIP',
    variant: 'outline',
    className: 'bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-950/40 dark:text-amber-300 dark:border-amber-800',
  },
  Visitor: {
    label: 'Khách thăm',
    shortLabel: 'Khách thăm',
    variant: 'outline',
    className: 'bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-300 dark:border-emerald-800',
  },
  Guest: {
    label: 'Khách vãng lai',
    shortLabel: 'Khách vãng lai',
    variant: 'secondary',
    className: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400',
  },
  Other: {
    label: 'Khác',
    shortLabel: 'Khác',
    variant: 'secondary',
    className: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400',
  },
}

const DEFAULT_META: PersonTypeMeta = {
  label: 'Khách vãng lai',
  shortLabel: 'Khách vãng lai',
  variant: 'secondary',
  className: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400',
}

export interface PersonTypeBadgeProps {
  type?: PersonType | null | string
  short?: boolean
  className?: string
}

export function PersonTypeBadge({ type, short = false, className }: PersonTypeBadgeProps) {
  const meta = (type && PERSON_TYPE_MAP[type as PersonType]) || DEFAULT_META
  const text = short ? meta.shortLabel : meta.label

  return (
    <Badge
      variant={meta.variant}
      className={cn(
        short ? 'text-[10px] px-1.5 py-0 font-medium shadow-2xs' : 'text-[11px] px-2 py-0.5 font-medium',
        meta.className,
        className
      )}
    >
      {text}
    </Badge>
  )
}
