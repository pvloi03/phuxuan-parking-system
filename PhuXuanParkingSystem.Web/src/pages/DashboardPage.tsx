import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  Car,
  ArrowDownRight,
  ArrowUpRight,
  AlertTriangle,
  RefreshCw,
  TrendingUp,
  Calendar,
  CalendarRange,
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
import { Input } from '@/components/ui/input'
import type { DashboardPeriod } from '@/types'

export function DashboardPage() {
  const [period, setPeriod] = useState<DashboardPeriod>('today')
  const [fromDate, setFromDate] = useState<string>(() => {
    const d = new Date()
    d.setDate(d.getDate() - 7)
    return d.toISOString().slice(0, 10)
  })
  const [toDate, setToDate] = useState<string>(() => new Date().toISOString().slice(0, 10))

  const { data: metrics, isLoading, refetch, isFetching } = useQuery({
    queryKey: ['dashboard-metrics', period, fromDate, toDate],
    queryFn: () =>
      parkingService.getMetrics({
        period,
        fromDate: period === 'custom' ? fromDate : undefined,
        toDate: period === 'custom' ? toDate : undefined,
      }),
    refetchInterval: period === 'today' ? 10000 : 30000,
  })

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
      {/* Top Header Actions & Time Range Filters */}
      <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
            Tổng Quan Hệ Thống
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
            Dữ liệu thống kê giám sát lưu lượng và công suất ({metrics?.periodLabel ?? 'Thời gian thực'})
          </p>
        </div>

        {/* Filter Controls */}
        <div className="flex flex-wrap items-center gap-2">
          {/* Quick Period Buttons */}
          <div className="inline-flex rounded-lg bg-slate-100 dark:bg-slate-800 p-1 border border-slate-200 dark:border-slate-700">
            <button
              type="button"
              onClick={() => setPeriod('today')}
              className={`px-3 py-1.5 text-xs font-semibold rounded-md transition-all cursor-pointer ${period === 'today'
                ? 'bg-white dark:bg-slate-900 text-blue-600 dark:text-blue-400 shadow-xs'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
                }`}
            >
              Hôm nay
            </button>
            <button
              type="button"
              onClick={() => setPeriod('month')}
              className={`px-3 py-1.5 text-xs font-semibold rounded-md transition-all cursor-pointer ${period === 'month'
                ? 'bg-white dark:bg-slate-900 text-blue-600 dark:text-blue-400 shadow-xs'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
                }`}
            >
              Tháng này
            </button>
            <button
              type="button"
              onClick={() => setPeriod('year')}
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
              className={`px-3 py-1.5 text-xs font-semibold rounded-md transition-all cursor-pointer flex items-center gap-1.5 ${period === 'custom'
                ? 'bg-white dark:bg-slate-900 text-blue-600 dark:text-blue-400 shadow-xs'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
                }`}
            >
              <CalendarRange className="h-3.5 w-3.5" />
              Tùy chọn
            </button>
          </div>

          {/* Custom Date Pickers */}
          {period === 'custom' && (
            <div className="flex items-center gap-1.5 bg-white dark:bg-slate-900 p-1 rounded-lg border border-slate-200 dark:border-slate-700 shadow-xs animate-in fade-in duration-200">
              <div className="flex items-center gap-1">
                <Calendar className="h-3.5 w-3.5 text-slate-400 ml-1.5 shrink-0" />
                <Input
                  type="date"
                  value={fromDate}
                  onChange={(e) => setFromDate(e.target.value)}
                  className="h-7 text-xs w-32 border-0 bg-transparent shadow-none px-1 focus-visible:ring-0"
                />
              </div>
              <span className="text-xs text-slate-400 font-medium">-</span>
              <Input
                type="date"
                value={toDate}
                onChange={(e) => setToDate(e.target.value)}
                className="h-7 text-xs w-32 border-0 bg-transparent shadow-none px-1 focus-visible:ring-0"
              />
            </div>
          )}

          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            disabled={isFetching}
            className="gap-2 text-xs font-semibold cursor-pointer shadow-xs shrink-0"
          >
            <RefreshCw className={`h-3.5 w-3.5 ${isFetching ? 'animate-spin' : ''}`} />
            {isFetching ? 'Đang tải...' : 'Làm mới'}
          </Button>
        </div>
      </div>

      {/* 4 KPI Metrics Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {/* Active Vehicles Card */}
        <Card className="border-blue-200/50 dark:border-blue-900/30 bg-gradient-to-br from-blue-500/5 via-transparent to-transparent">
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
        <Card className="border-emerald-200/50 dark:border-emerald-900/30 bg-gradient-to-br from-emerald-500/5 via-transparent to-transparent">
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
        <Card className="border-indigo-200/50 dark:border-indigo-900/30 bg-gradient-to-br from-indigo-500/5 via-transparent to-transparent">
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

        {/* Period Unmatched Out Warnings Card */}
        <Card className="border-amber-200/50 dark:border-amber-900/30 bg-gradient-to-br from-amber-500/5 via-transparent to-transparent">
          <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
            <CardTitle className="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider truncate">
              Cảnh Báo Ra Không Vào
            </CardTitle>
            <div className="h-9 w-9 rounded-xl bg-amber-100 dark:bg-amber-900/40 text-amber-600 dark:text-amber-400 flex items-center justify-center shadow-xs">
              <AlertTriangle className="h-5 w-5" />
            </div>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-extrabold text-slate-900 dark:text-slate-50 tracking-tight">
              {isLoading ? '--' : metrics?.periodUnmatchedOutCount ?? metrics?.todayUnmatchedOutCount ?? 0}
            </div>
            <p className="text-xs text-amber-600 dark:text-amber-400 font-medium mt-1">
              Lượt Unmatched Out {getPeriodDisplayTitle().toLowerCase()}
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
