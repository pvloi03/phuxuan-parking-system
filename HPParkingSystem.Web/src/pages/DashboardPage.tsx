import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  Car,
  ArrowDownRight,
  ArrowUpRight,
  RefreshCw,
  TrendingUp,
} from 'lucide-react'
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from 'recharts'
import { parkingService } from '@/services/parkingService'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Time24Picker } from '@/components/common/Time24Picker'
import type { DashboardPeriod } from '@/types'

function formatLocalDate(d: Date): string {
  const year = d.getFullYear()
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function DashboardPage() {
  const [period, setPeriod] = useState<DashboardPeriod>('today')
  const [fromDate, setFromDate] = useState<string>(() => formatLocalDate(new Date()))
  const [toDate, setToDate] = useState<string>(() => formatLocalDate(new Date()))
  const [fromTime, setFromTime] = useState<string>('00:00')
  const [toTime, setToTime] = useState<string>('23:59')

  const { data: metrics, isLoading, refetch, isFetching } = useQuery({
    queryKey: ['dashboard-metrics', period, fromDate, toDate, fromTime, toTime],
    queryFn: () =>
      parkingService.getMetrics({
        period,
        fromDate: `${fromDate}T${fromTime || '00:00'}:00`,
        toDate: `${toDate}T${toTime || '23:59'}:59`,
      }),
    refetchInterval: period === 'today' ? 10000 : 30000,
  })

  const handleSetPreset = (p: 'today' | 'month' | 'year') => {
    const now = new Date()
    setPeriod(p)
    if (p === 'today') {
      const todayStr = formatLocalDate(now)
      setFromDate(todayStr)
      setToDate(todayStr)
      setFromTime('00:00')
      setToTime('23:59')
    } else if (p === 'month') {
      const monthStart = new Date(now.getFullYear(), now.getMonth(), 1)
      setFromDate(formatLocalDate(monthStart))
      setToDate(formatLocalDate(now))
      setFromTime('00:00')
      setToTime('23:59')
    } else if (p === 'year') {
      const yearStart = new Date(now.getFullYear(), 0, 1)
      setFromDate(formatLocalDate(yearStart))
      setToDate(formatLocalDate(now))
      setFromTime('00:00')
      setToTime('23:59')
    }
  }

  // Nhãn thời gian hiển thị cho thẻ KPI
  const getPeriodDisplayTitle = () => {
    switch (period) {
      case 'today':
        return 'Hôm Nay'
      case 'month':
        return 'Tháng Này'
      case 'year':
        return 'Năm Nay'
      case 'custom':
        return 'Trong Kỳ'
      default:
        return 'Hôm Nay'
    }
  }

  // Tiêu đề biểu đồ linh hoạt
  const getChartTitle = () => {
    switch (period) {
      case 'today':
        return 'Lưu Lượng Xe Theo Khung Giờ (24 Giờ)'
      case 'month':
        return 'Lưu Lượng Xe Theo Ngày Trong Tháng'
      case 'year':
        return 'Lưu Lượng Xe Theo 12 Tháng Trong Năm'
      case 'custom':
        return 'Lưu Lượng Xe Theo Khoảng Thời Gian'
      default:
        return 'Lưu Lượng Xe'
    }
  }

  const getChartDescription = () => {
    if (metrics?.periodLabel) {
      return `Thống kê số lượt xe vào và xe ra: ${metrics.periodLabel}`
    }
    return 'Số lượt xe vào và xe ra theo khoảng thời gian được chọn'
  }

  const chartData = metrics?.trafficChart?.length
    ? metrics.trafficChart
    : (metrics?.hourlyTraffic || []).map((h) => ({
      label: h.hourLabel,
      inCount: h.inCount,
      outCount: h.outCount,
    }))

  return (
    <div className="space-y-6">
      {/* Top Header Actions & Presets */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
            Tổng Quan Hệ Thống
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
            Dữ liệu thống kê giám sát lưu lượng và công suất ({metrics?.periodLabel ?? 'Thời gian thực'})
          </p>
        </div>

        {/* Quick Period Buttons */}
        <div className="inline-flex rounded-lg bg-slate-100 dark:bg-slate-800 p-1 border border-slate-200 dark:border-slate-700 shrink-0">
          <button
            type="button"
            onClick={() => handleSetPreset('today')}
            className={`px-3 py-1.5 text-xs font-semibold rounded-md transition-all cursor-pointer ${period === 'today'
              ? 'bg-white dark:bg-slate-900 text-blue-600 dark:text-blue-400 shadow-xs'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
              }`}
          >
            Hôm nay
          </button>
          <button
            type="button"
            onClick={() => handleSetPreset('month')}
            className={`px-3 py-1.5 text-xs font-semibold rounded-md transition-all cursor-pointer ${period === 'month'
              ? 'bg-white dark:bg-slate-900 text-blue-600 dark:text-blue-400 shadow-xs'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
              }`}
          >
            Tháng này
          </button>
          <button
            type="button"
            onClick={() => handleSetPreset('year')}
            className={`px-3 py-1.5 text-xs font-semibold rounded-md transition-all cursor-pointer ${period === 'year'
              ? 'bg-white dark:bg-slate-900 text-blue-600 dark:text-blue-400 shadow-xs'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
              }`}
          >
            Năm này
          </button>
          <button
            type="button"
            onClick={() => setPeriod('custom')}
            className={`px-3 py-1.5 text-xs font-semibold rounded-md transition-all cursor-pointer ${period === 'custom'
              ? 'bg-white dark:bg-slate-900 text-blue-600 dark:text-blue-400 shadow-xs'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
              }`}
          >
            Tùy chọn
          </button>
        </div>
      </div>

      {/* Date & Time Range Filters + Refresh Button (Luôn hiện, đặt bên trái nút Làm mới) */}
      <div className="flex flex-wrap items-center gap-2.5 bg-white dark:bg-slate-900/60 p-2.5 rounded-xl border border-slate-200 dark:border-slate-800 shadow-xs relative z-20">
        {/* Cụm Từ: Ngày + Giờ */}
        <div className="bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-700/80 rounded-lg px-2.5 h-9 flex items-center gap-1.5 focus-within:ring-2 focus-within:ring-blue-500/20 focus-within:border-blue-500 transition-all">
          <span className="text-[11px] font-semibold text-slate-500 dark:text-slate-400 shrink-0 select-none">
            Từ:
          </span>
          <input
            type="date"
            value={fromDate}
            onChange={(e) => {
              setFromDate(e.target.value)
              setPeriod('custom')
            }}
            className="bg-transparent text-xs text-slate-800 dark:text-slate-200 focus:outline-none cursor-pointer w-28 shrink-0 dark:scheme-dark"
          />
          <span className="text-slate-300 dark:text-slate-600">|</span>
          <Time24Picker
            value={fromTime}
            onChange={(val) => {
              setFromTime(val)
              setPeriod('custom')
            }}
            placeholder="00:00"
          />
        </div>

        {/* Cụm Đến: Ngày + Giờ */}
        <div className="bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-700/80 rounded-lg px-2.5 h-9 flex items-center gap-1.5 focus-within:ring-2 focus-within:ring-blue-500/20 focus-within:border-blue-500 transition-all">
          <span className="text-[11px] font-semibold text-slate-500 dark:text-slate-400 shrink-0 select-none">
            Đến:
          </span>
          <input
            type="date"
            value={toDate}
            onChange={(e) => {
              setToDate(e.target.value)
              setPeriod('custom')
            }}
            className="bg-transparent text-xs text-slate-800 dark:text-slate-200 focus:outline-none cursor-pointer w-28 shrink-0 dark:scheme-dark"
          />
          <span className="text-slate-300 dark:text-slate-600">|</span>
          <Time24Picker
            value={toTime}
            onChange={(val) => {
              setToTime(val)
              setPeriod('custom')
            }}
            placeholder="23:59"
          />
        </div>

        {/* Nút Làm mới đặt bên phải của bộ chọn ngày giờ */}
        <Button
          variant="outline"
          size="sm"
          onClick={() => refetch()}
          disabled={isFetching}
          className="h-9 gap-2 text-xs font-semibold cursor-pointer shadow-xs shrink-0"
        >
          <RefreshCw className={`h-3.5 w-3.5 ${isFetching ? 'animate-spin' : ''}`} />
          {isFetching ? 'Đang tải...' : 'Làm mới'}
        </Button>
      </div>

      {/* 3 KPI Metrics Cards (Đã loại bỏ Cảnh Báo Ra Không Vào theo yêu cầu) */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {/* Active Vehicles Card */}
        <Card className="border-blue-200/50 dark:border-blue-900/30 bg-linear-to-br from-blue-500/5 via-transparent to-transparent">
          <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
            <CardTitle className="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
              Xe Đang Trong Bãi
            </CardTitle>
            <div className="h-9 w-9 rounded-xl bg-blue-100 dark:bg-blue-900/40 text-blue-600 dark:text-blue-400 flex items-center justify-center shadow-xs">
              <Car className="h-5 w-5" />
            </div>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-extrabold text-slate-900 dark:text-slate-50 tracking-tight">
              {isLoading ? '--' : metrics?.activeVehiclesCount ?? 0}
            </div>
            <p className="text-xs text-blue-600 dark:text-blue-400 font-medium mt-1">
              Phương tiện đang đỗ thực tế
            </p>
          </CardContent>
        </Card>

        {/* Period In Card */}
        <Card className="border-emerald-200/50 dark:border-emerald-900/30 bg-linear-to-br from-emerald-500/5 via-transparent to-transparent">
          <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
            <CardTitle className="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider truncate">
              Lượt Vào {getPeriodDisplayTitle()}
            </CardTitle>
            <div className="h-9 w-9 rounded-xl bg-emerald-100 dark:bg-emerald-900/40 text-emerald-600 dark:text-emerald-400 flex items-center justify-center shadow-xs">
              <ArrowDownRight className="h-5 w-5" />
            </div>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-extrabold text-slate-900 dark:text-slate-50 tracking-tight">
              {isLoading ? '--' : metrics?.periodInCount ?? metrics?.todayInCount ?? 0}
            </div>
            <p className="text-xs text-emerald-600 dark:text-emerald-400 font-medium mt-1">
              Xe đã check-in ({getPeriodDisplayTitle().toLowerCase()})
            </p>
          </CardContent>
        </Card>

        {/* Period Out Card */}
        <Card className="border-indigo-200/50 dark:border-indigo-900/30 bg-linear-to-br from-indigo-500/5 via-transparent to-transparent">
          <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
            <CardTitle className="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider truncate">
              Lượt Ra {getPeriodDisplayTitle()}
            </CardTitle>
            <div className="h-9 w-9 rounded-xl bg-indigo-100 dark:bg-indigo-900/40 text-indigo-600 dark:text-indigo-400 flex items-center justify-center shadow-xs">
              <ArrowUpRight className="h-5 w-5" />
            </div>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-extrabold text-slate-900 dark:text-slate-50 tracking-tight">
              {isLoading ? '--' : metrics?.periodOutCount ?? metrics?.todayOutCount ?? 0}
            </div>
            <p className="text-xs text-indigo-600 dark:text-indigo-400 font-medium mt-1">
              Xe đã check-out hoàn tất
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Traffic Bar Chart Section */}
      <div className="grid grid-cols-1 gap-6">
        <Card className="shadow-xs">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <div>
              <CardTitle className="text-base flex items-center gap-2">
                <TrendingUp className="h-4.5 w-4.5 text-blue-500" />
                {getChartTitle()}
              </CardTitle>
              <CardDescription className="text-xs">
                {getChartDescription()}
              </CardDescription>
            </div>
          </CardHeader>
          <CardContent>
            <div className="h-80 w-full">
              {isLoading ? (
                <div className="h-full flex items-center justify-center text-xs text-slate-400">
                  <RefreshCw className="h-4 w-4 animate-spin mr-2" />
                  Đang nạp biểu đồ...
                </div>
              ) : (
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={chartData} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                    <CartesianGrid strokeDasharray="3 3" opacity={0.15} />
                    <XAxis dataKey="label" fontSize={11} stroke="#888888" interval="preserveStartEnd" />
                    <YAxis fontSize={11} stroke="#888888" allowDecimals={false} />
                    <Tooltip
                      contentStyle={{
                        backgroundColor: '#0f172a',
                        borderColor: '#334155',
                        borderRadius: '0.5rem',
                        fontSize: '12px',
                        color: '#f8fafc',
                      }}
                      formatter={(value: any, name: any) => [
                        `${value} lượt`,
                        name === 'inCount' || name === 'Lượt Vào' ? 'Lượt Vào' : 'Lượt Ra',
                      ]}
                      labelFormatter={(label) => `Mốc: ${label}`}
                    />
                    <Legend
                      wrapperStyle={{ fontSize: '12px', paddingTop: '10px' }}
                      formatter={(value) => (value === 'inCount' || value === 'Lượt Vào' ? 'Lượt Vào' : 'Lượt Ra')}
                    />
                    <Bar dataKey="inCount" name="Lượt Vào" fill="#3b82f6" radius={[4, 4, 0, 0]} />
                    <Bar dataKey="outCount" name="Lượt Ra" fill="#10b981" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              )}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
