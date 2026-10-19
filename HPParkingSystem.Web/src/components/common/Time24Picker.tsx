import { useState, useRef, useEffect, useCallback } from 'react'
import { createPortal } from 'react-dom'
import { Clock } from 'lucide-react'

export interface Time24PickerProps {
  value: string // định dạng 'HH:mm' hoặc ''
  onChange: (value: string) => void
  placeholder?: string
  className?: string
  disabled?: boolean
}

export function Time24Picker({
  value,
  onChange,
  placeholder = '00:00',
  className = '',
  disabled = false,
}: Time24PickerProps) {
  const [isOpen, setIsOpen] = useState(false)
  const triggerRef = useRef<HTMLDivElement>(null)
  const popoverRef = useRef<HTMLDivElement>(null)
  const hoursListRef = useRef<HTMLDivElement>(null)
  const minutesListRef = useRef<HTMLDivElement>(null)

  const [coords, setCoords] = useState<{ top: number; left: number }>({ top: 0, left: 0 })

  // Tách giờ và phút hiện tại từ value
  const parseTime = useCallback((val: string) => {
    if (!val || !val.includes(':')) {
      return { hour: '00', minute: '00', isSet: false }
    }
    const [h, m] = val.split(':')
    const hNum = Math.min(23, Math.max(0, parseInt(h || '0', 10)))
    const mNum = Math.min(59, Math.max(0, parseInt(m || '0', 10)))
    return {
      hour: hNum.toString().padStart(2, '0'),
      minute: mNum.toString().padStart(2, '0'),
      isSet: true,
    }
  }, [])

  const { hour, minute, isSet } = parseTime(value)

  // Tính toán vị trí hiển thị popover (Fixed position trong document.body)
  const updatePosition = useCallback(() => {
    if (triggerRef.current) {
      const rect = triggerRef.current.getBoundingClientRect()
      const popoverWidth = 208 // w-52
      const popoverHeight = 245

      // Canh mép phải popover theo mép phải trigger
      let left = rect.right - popoverWidth
      if (left < 10) left = 10
      if (left + popoverWidth > window.innerWidth - 10) {
        left = window.innerWidth - popoverWidth - 10
      }

      // Kiểm tra khoảng trống bên dưới, nếu hẹp quá thì mở lên trên
      const spaceBelow = window.innerHeight - rect.bottom
      let top: number
      if (spaceBelow < popoverHeight && rect.top > popoverHeight) {
        top = rect.top - popoverHeight - 6
      } else {
        top = rect.bottom + 6
      }

      setCoords({ top, left })
    }
  }, [])

  // Mở picker và tính vị trí
  const handleOpen = () => {
    if (disabled) return
    updatePosition()
    setIsOpen(true)
  }

  // Tự động cập nhật vị trí khi scroll hoặc resize
  useEffect(() => {
    if (isOpen) {
      updatePosition()
      const onScrollOrResize = () => updatePosition()
      window.addEventListener('scroll', onScrollOrResize, true)
      window.addEventListener('resize', onScrollOrResize)
      return () => {
        window.removeEventListener('scroll', onScrollOrResize, true)
        window.removeEventListener('resize', onScrollOrResize)
      }
    }
  }, [isOpen, updatePosition])

  // Cuộn đến mục đang chọn khi mở popover
  useEffect(() => {
    if (isOpen) {
      setTimeout(() => {
        if (hoursListRef.current) {
          const activeHourElem = hoursListRef.current.querySelector('[data-active="true"]') as HTMLElement | null
          if (activeHourElem) {
            activeHourElem.scrollIntoView({ block: 'center', behavior: 'auto' })
          }
        }
        if (minutesListRef.current) {
          const activeMinElem = minutesListRef.current.querySelector('[data-active="true"]') as HTMLElement | null
          if (activeMinElem) {
            activeMinElem.scrollIntoView({ block: 'center', behavior: 'auto' })
          }
        }
      }, 30)
    }
  }, [isOpen])

  // Đóng popover khi click ra ngoài
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (
        popoverRef.current &&
        !popoverRef.current.contains(e.target as Node) &&
        triggerRef.current &&
        !triggerRef.current.contains(e.target as Node)
      ) {
        setIsOpen(false)
      }
    }
    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside)
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isOpen])

  // Xử lý chọn giờ
  const handleSelectHour = (newHour: string) => {
    const curMin = isSet ? minute : '00'
    onChange(`${newHour}:${curMin}`)
  }

  // Xử lý chọn phút
  const handleSelectMinute = (newMinute: string) => {
    const curHour = isSet ? hour : '00'
    onChange(`${curHour}:${newMinute}`)
  }

  // Xử lý gõ phím trực tiếp (Masking tự động chèn dấu :)
  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    let raw = e.target.value.replace(/[^0-9]/g, '')
    if (raw.length > 4) raw = raw.slice(0, 4)

    if (raw.length >= 3) {
      onChange(`${raw.slice(0, 2)}:${raw.slice(2)}`)
    } else {
      onChange(raw)
    }
  }

  // Xử lý khi rời chuột (blur) để chuẩn hóa định dạng hợp lệ
  const handleInputBlur = (e: React.FocusEvent<HTMLInputElement>) => {
    const raw = e.target.value.trim()
    if (!raw) {
      onChange('')
      return
    }

    if (!raw.includes(':')) {
      const num = parseInt(raw, 10)
      if (!isNaN(num) && num >= 0 && num <= 23) {
        onChange(`${num.toString().padStart(2, '0')}:00`)
      } else {
        onChange('00:00')
      }
    } else {
      const parts = raw.split(':')
      const hNum = Math.min(23, Math.max(0, parseInt(parts[0] || '0', 10)))
      const mNum = Math.min(59, Math.max(0, parseInt(parts[1] || '0', 10)))
      onChange(`${hNum.toString().padStart(2, '0')}:${mNum.toString().padStart(2, '0')}`)
    }
  }

  return (
    <>
      <div ref={triggerRef} className={`relative inline-flex items-center ${className}`}>
        {/* Ô nhập hiển thị giá trị HH:mm */}
        <input
          type="text"
          value={value}
          disabled={disabled}
          maxLength={5}
          placeholder={placeholder}
          onClick={handleOpen}
          onChange={handleInputChange}
          onBlur={handleInputBlur}
          className="w-13 text-center font-mono font-semibold text-xs bg-transparent text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 rounded py-0.5 cursor-pointer selection:bg-blue-100 dark:selection:bg-blue-900"
        />

        {/* Icon đồng hồ mở picker */}
        <button
          type="button"
          disabled={disabled}
          onClick={() => {
            if (isOpen) setIsOpen(false)
            else handleOpen()
          }}
          className="p-1 hover:bg-slate-200/70 dark:hover:bg-slate-700/60 rounded text-slate-400 hover:text-slate-600 dark:text-slate-400 dark:hover:text-slate-200 cursor-pointer transition-colors"
          title="Chọn giờ và phút"
        >
          <Clock className="w-3.5 h-3.5" />
        </button>
      </div>

      {/* Popover 2 cột (Render qua Portal vào document.body với z-[9999], không bao giờ bị che/cắt bởi Card hay Table) */}
      {isOpen &&
        createPortal(
          <div
            ref={popoverRef}
            style={{
              position: 'fixed',
              top: `${coords.top}px`,
              left: `${coords.left}px`,
              width: '208px',
              zIndex: 9999,
            }}
            className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl shadow-2xl p-2.5 text-xs animate-in fade-in zoom-in-95 duration-100"
          >
            {/* Header 2 cột */}
            <div className="flex items-center justify-between px-1 pb-1.5 mb-1.5 border-b border-slate-100 dark:border-slate-800 text-[11px] font-semibold text-slate-500 dark:text-slate-400">
              <span className="w-1/2 text-center">Giờ (0-23)</span>
              <span className="w-1/2 text-center">Phút (0-59)</span>
            </div>

            {/* Nội dung cuộn 2 cột */}
            <div className="grid grid-cols-2 gap-1.5 h-44">
              {/* Cột Giờ */}
              <div
                ref={hoursListRef}
                style={{ scrollbarWidth: 'thin' }}
                className="overflow-y-auto pr-1 space-y-0.5 text-center font-mono"
              >
                {Array.from({ length: 24 }, (_, i) => {
                  const val = i.toString().padStart(2, '0')
                  const isActive = isSet && hour === val
                  return (
                    <button
                      key={val}
                      type="button"
                      data-active={isActive}
                      onClick={() => handleSelectHour(val)}
                      className={`w-full py-1 rounded text-xs transition-colors cursor-pointer ${
                        isActive
                          ? 'bg-blue-600 text-white font-bold shadow-xs'
                          : 'hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300'
                      }`}
                    >
                      {val}
                    </button>
                  )
                })}
              </div>

              {/* Cột Phút */}
              <div
                ref={minutesListRef}
                style={{ scrollbarWidth: 'thin' }}
                className="overflow-y-auto pl-1 border-l border-slate-100 dark:border-slate-800 space-y-0.5 text-center font-mono"
              >
                {Array.from({ length: 60 }, (_, i) => {
                  const val = i.toString().padStart(2, '0')
                  const isActive = isSet && minute === val
                  return (
                    <button
                      key={val}
                      type="button"
                      data-active={isActive}
                      onClick={() => handleSelectMinute(val)}
                      className={`w-full py-1 rounded text-xs transition-colors cursor-pointer ${
                        isActive
                          ? 'bg-emerald-600 text-white font-bold shadow-xs'
                          : 'hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300'
                      }`}
                    >
                      {val}
                    </button>
                  )
                })}
              </div>
            </div>

            {/* Phím tắt phút phổ biến & nút đóng */}
            <div className="pt-2 mt-1.5 border-t border-slate-100 dark:border-slate-800 flex items-center justify-between text-[11px]">
              <div className="flex gap-1 text-slate-500 dark:text-slate-400 font-mono">
                {['00', '15', '30', '45'].map((quickMin) => (
                  <button
                    key={quickMin}
                    type="button"
                    onClick={() => handleSelectMinute(quickMin)}
                    className="px-1 py-0.5 hover:bg-slate-100 dark:hover:bg-slate-800 hover:text-blue-600 dark:hover:text-blue-400 rounded cursor-pointer"
                  >
                    :{quickMin}
                  </button>
                ))}
              </div>
              <button
                type="button"
                onClick={() => setIsOpen(false)}
                className="px-2.5 py-0.5 bg-blue-600 hover:bg-blue-700 text-white rounded font-medium cursor-pointer shadow-2xs"
              >
                Xong
              </button>
            </div>
          </div>,
          document.body
        )}
    </>
  )
}
