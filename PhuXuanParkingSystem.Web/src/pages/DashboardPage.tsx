import { useQuery } from '@tanstack/react-query'
import {
  Car,
  ArrowDownRight,
  ArrowUpRight,
  AlertTriangle,
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

export function DashboardPage() {
  const { data: metrics, isLoading, refetch, isFetching } = useQuery({
    queryKey: ['dashboard-metrics'],
    queryFn: parkingService.getMetrics,
    refetchInterval: 10000, // Tự động làm mới mỗi 10 giây
  })

  return (
    <div className="space-y-6">
      {/* Top Header Actions */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
            Tổng Quan Bãi Xe
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
            Dữ liệu thống kê giám sát lưu lượng và công suất theo thời gian thực
          </p>
        </div>

        <Button
          variant="outline"
          size="sm"
          onClick={() => refetch()}
          disabled={isFetching}
          className="self-start sm:self-auto gap-2 text-xs font-semibold"
        >
          <RefreshCw className={`h-3.5 w-3.5 ${isFetching ? 'animate-spin' : ''}`} />
          {isFetching ? 'Đang tải...' : 'Làm mới'}
        </Button>
      </div>

      {/* 4 KPI Metrics Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {/* Active Vehicles Card */}
        <Card className="border-blue-200/50 dark:border-blue-900/30 bg-gradient-to-br from-blue-500/5 via-transparent to-transparent">
          <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
            <CardTitle className="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
              Xe Đang Trong Bãi
            </CardTitle>
            <div className="h-9 w-9 rounded-xl bg-blue-100 dark:bg-blue-900/40 text-blue-600 dark:text-blue-400 flex items-center justify-center">
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

        {/* Today In Card */}
        <Card className="border-emerald-200/50 dark:border-emerald-900/30 bg-gradient-to-br from-emerald-500/5 via-transparent to-transparent">
          <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
            <CardTitle className="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
              Lượt Vào Hôm Nay
            </CardTitle>
            <div className="h-9 w-9 rounded-xl bg-emerald-100 dark:bg-emerald-900/40 text-emerald-600 dark:text-emerald-400 flex items-center justify-center">
              <ArrowDownRight className="h-5 w-5" />
            </div>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-extrabold text-slate-900 dark:text-slate-50 tracking-tight">
              {isLoading ? '--' : metrics?.todayInCount ?? 0}
            </div>
            <p className="text-xs text-emerald-600 dark:text-emerald-400 font-medium mt-1">
              Xe đã check-in hôm nay
            </p>
          </CardContent>
        </Card>

        {/* Today Out Card */}
        <Card className="border-indigo-200/50 dark:border-indigo-900/30 bg-gradient-to-br from-indigo-500/5 via-transparent to-transparent">
          <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
            <CardTitle className="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
              Lượt Ra Hôm Nay
            </CardTitle>
            <div className="h-9 w-9 rounded-xl bg-indigo-100 dark:bg-indigo-900/40 text-indigo-600 dark:text-indigo-400 flex items-center justify-center">
              <ArrowUpRight className="h-5 w-5" />
            </div>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-extrabold text-slate-900 dark:text-slate-50 tracking-tight">
              {isLoading ? '--' : metrics?.todayOutCount ?? 0}
            </div>
            <p className="text-xs text-indigo-600 dark:text-indigo-400 font-medium mt-1">
              Xe đã check-out hoàn tất
            </p>
          </CardContent>
        </Card>

        {/* Unmatched Out Warnings Card */}
        <Card className="border-amber-200/50 dark:border-amber-900/30 bg-gradient-to-br from-amber-500/5 via-transparent to-transparent">
          <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
            <CardTitle className="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
              Cảnh Báo Ra Không Vào
            </CardTitle>
            <div className="h-9 w-9 rounded-xl bg-amber-100 dark:bg-amber-900/40 text-amber-600 dark:text-amber-400 flex items-center justify-center">
              <AlertTriangle className="h-5 w-5" />
            </div>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-extrabold text-slate-900 dark:text-slate-50 tracking-tight">
              {isLoading ? '--' : metrics?.todayUnmatchedOutCount ?? 0}
            </div>
            <p className="text-xs text-amber-600 dark:text-amber-400 font-medium mt-1">
              Lượt Unmatched Out hôm nay
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Charts Section */}
      <div className="grid grid-cols-1 gap-6">
        {/* Hourly Traffic Chart (Full width) */}
        <Card className="shadow-xs">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <div>
              <CardTitle className="text-base flex items-center gap-2">
                <TrendingUp className="h-4.5 w-4.5 text-blue-500" />
                Lưu Lượng Xe Theo Khung Giờ (24 Giờ)
              </CardTitle>
              <CardDescription className="text-xs">
                Số lượt xe vào và xe ra trong từng khung giờ hôm nay
              </CardDescription>
            </div>
          </CardHeader>
          <CardContent>
            <div className="h-80 w-full">
              {isLoading ? (
                <div className="h-full flex items-center justify-center text-xs text-slate-400">
                  Đang nạp biểu đồ...
                </div>
              ) : (
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={metrics?.hourlyTraffic || []} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                    <CartesianGrid strokeDasharray="3 3" opacity={0.15} />
                    <XAxis dataKey="hourLabel" fontSize={11} stroke="#888888" />
                    <YAxis fontSize={11} stroke="#888888" allowDecimals={false} />
                    <Tooltip
                      contentStyle={{
                        backgroundColor: '#0f172a',
                        borderColor: '#334155',
                        borderRadius: '0.5rem',
                        fontSize: '12px',
                        color: '#f8fafc',
                      }}
                    />
                    <Legend wrapperStyle={{ fontSize: '12px', paddingTop: '10px' }} />
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
