import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Car, Search, Plus, Trash2, CheckCircle2, XCircle } from 'lucide-react'
import { apiClient } from '@/services/apiClient'
import type { PagedResult, Vehicle } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'

export function VehiclesPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [newPlate, setNewPlate] = useState('')
  const [newType, setNewType] = useState<'Car' | 'Motorcycle'>('Car')

  const { data, isLoading } = useQuery({
    queryKey: ['vehicles-list', search],
    queryFn: async () => {
      const res = await apiClient.get<{ data: PagedResult<Vehicle> }>('/vehicles', {
        params: { search: search || undefined, pageSize: 50 },
      })
      return res.data.data
    },
  })

  const createMutation = useMutation({
    mutationFn: async () => {
      await apiClient.post('/vehicles', {
        plateNumber: newPlate.trim(),
        type: newType,
        isActive: true,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicles-list'] })
      setIsCreateOpen(false)
      setNewPlate('')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/vehicles/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicles-list'] })
    },
  })

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
            Quản Lý Phương Tiện
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
            Danh sách các phương tiện đã đăng ký trong hệ thống bãi đỗ xe
          </p>
        </div>

        <Button
          onClick={() => setIsCreateOpen(true)}
          size="sm"
          className="self-start sm:self-auto gap-2 text-xs font-semibold bg-blue-600 hover:bg-blue-700"
        >
          <Plus className="h-4 w-4" />
          Đăng Ký Xe Mới
        </Button>
      </div>

      <Card className="shadow-xs">
        <CardContent className="p-4">
          <div className="relative max-w-sm">
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
            <Input
              placeholder="Tìm kiếm theo biển số..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-9 text-xs"
            />
          </div>
        </CardContent>
      </Card>

      <Card className="shadow-xs overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-slate-100/80 dark:bg-slate-800/60 text-slate-700 dark:text-slate-300 font-semibold border-b border-slate-200 dark:border-slate-800">
              <tr>
                <th className="p-3.5 pl-4">Biển Số Xe</th>
                <th className="p-3.5">Loại Phương Tiện</th>
                <th className="p-3.5">Trạng Thái</th>
                <th className="p-3.5">Ngày Đăng Ký</th>
                <th className="p-3.5 text-right pr-4">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {isLoading ? (
                <tr>
                  <td colSpan={5} className="p-8 text-center text-slate-400">
                    Đang tải danh sách phương tiện...
                  </td>
                </tr>
              ) : data?.items && data.items.length > 0 ? (
                data.items.map((vehicle) => (
                  <tr key={vehicle.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/40">
                    <td className="p-3.5 pl-4 font-bold text-slate-900 dark:text-slate-100">
                      {vehicle.plateNumber}
                    </td>
                    <td className="p-3.5">
                      <Badge variant="secondary">
                        {vehicle.type === 'Car' ? 'Ô tô' : 'Xe máy'}
                      </Badge>
                    </td>
                    <td className="p-3.5">
                      {vehicle.isActive ? (
                        <span className="inline-flex items-center text-emerald-600 gap-1 font-medium">
                          <CheckCircle2 className="h-3.5 w-3.5" /> Hoạt động
                        </span>
                      ) : (
                        <span className="inline-flex items-center text-slate-400 gap-1">
                          <XCircle className="h-3.5 w-3.5" /> Khóa
                        </span>
                      )}
                    </td>
                    <td className="p-3.5 text-slate-500">
                      {vehicle.createdAt ? new Date(vehicle.createdAt).toLocaleDateString('vi-VN') : '--'}
                    </td>
                    <td className="p-3.5 text-right pr-4">
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => {
                          if (confirm(`Bạn có chắc muốn xóa xe biển số ${vehicle.plateNumber}?`)) {
                            deleteMutation.mutate(vehicle.id)
                          }
                        }}
                        className="h-7 px-2 text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950/30"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={5} className="p-8 text-center text-slate-400 italic">
                    Chưa có phương tiện nào trong danh sách
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>

      {/* Modal Thêm Xe */}
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Car className="h-5 w-5 text-blue-600" />
              Đăng Ký Phương Tiện Mới
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-2">
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">Biển số xe</label>
              <Input
                placeholder="VD: 30A-123.45"
                value={newPlate}
                onChange={(e) => setNewPlate(e.target.value)}
              />
            </div>
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700 dark:text-slate-300">Loại xe</label>
              <select
                value={newType}
                onChange={(e) => setNewType(e.target.value as any)}
                className="flex h-9 w-full rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 px-3 py-1 text-xs text-slate-900 dark:text-slate-100 shadow-sm"
              >
                <option value="Car">Ô tô</option>
                <option value="Motorcycle">Xe máy</option>
              </select>
            </div>
          </div>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setIsCreateOpen(false)}>Hủy</Button>
            <Button
              disabled={!newPlate.trim() || createMutation.isPending}
              onClick={() => createMutation.mutate()}
              className="bg-blue-600 hover:bg-blue-700"
            >
              {createMutation.isPending ? 'Đang lưu...' : 'Lưu Phương Tiện'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
