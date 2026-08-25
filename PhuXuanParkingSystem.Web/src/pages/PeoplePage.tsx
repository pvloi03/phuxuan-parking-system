import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Users, Search, Plus, Trash2, Mail, Phone } from 'lucide-react'
import { apiClient } from '@/services/apiClient'
import type { PagedResult, Person } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'

export function PeoplePage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [newCode, setNewCode] = useState('')
  const [newName, setNewName] = useState('')
  const [newPhone, setNewPhone] = useState('')
  const [newEmail, setNewEmail] = useState('')

  const { data, isLoading } = useQuery({
    queryKey: ['people-list', search],
    queryFn: async () => {
      const res = await apiClient.get<{ data: PagedResult<Person> }>('/people', {
        params: { search: search || undefined, pageSize: 50 },
      })
      return res.data.data
    },
  })

  const createMutation = useMutation({
    mutationFn: async () => {
      await apiClient.post('/people', {
        code: newCode.trim(),
        fullName: newName.trim(),
        phoneNumber: newPhone.trim() || undefined,
        email: newEmail.trim() || undefined,
        type: 'Employee',
        isActive: true,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['people-list'] })
      setIsCreateOpen(false)
      setNewCode('')
      setNewName('')
      setNewPhone('')
      setNewEmail('')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/people/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['people-list'] })
    },
  })

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
            Quản Lý Nhân Sự
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
            Danh sách nhân viên, cán bộ và khách thuộc cơ sở Phú Xuân
          </p>
        </div>

        <Button
          onClick={() => setIsCreateOpen(true)}
          size="sm"
          className="self-start sm:self-auto gap-2 text-xs font-semibold bg-blue-600 hover:bg-blue-700"
        >
          <Plus className="h-4 w-4" />
          Thêm Nhân Sự Mới
        </Button>
      </div>

      <Card className="shadow-xs">
        <CardContent className="p-4">
          <div className="relative max-w-sm">
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
            <Input
              placeholder="Tìm theo mã hoặc họ tên..."
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
                <th className="p-3.5 pl-4">Mã Nhân Viên</th>
                <th className="p-3.5">Họ Và Tên</th>
                <th className="p-3.5">Loại Nhân Sự</th>
                <th className="p-3.5">Liên Hệ</th>
                <th className="p-3.5 text-right pr-4">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {isLoading ? (
                <tr>
                  <td colSpan={5} className="p-8 text-center text-slate-400">
                    Đang tải danh sách nhân sự...
                  </td>
                </tr>
              ) : data?.items && data.items.length > 0 ? (
                data.items.map((person) => (
                  <tr key={person.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/40">
                    <td className="p-3.5 pl-4 font-mono font-semibold text-slate-800 dark:text-slate-200">
                      {person.code || '--'}
                    </td>
                    <td className="p-3.5 font-bold text-slate-900 dark:text-slate-100">
                      {person.fullName}
                    </td>
                    <td className="p-3.5">
                      <Badge variant="secondary">{person.type}</Badge>
                    </td>
                    <td className="p-3.5 text-slate-500 space-y-0.5">
                      {person.phoneNumber && (
                        <div className="flex items-center gap-1">
                          <Phone className="h-3 w-3" /> {person.phoneNumber}
                        </div>
                      )}
                      {person.email && (
                        <div className="flex items-center gap-1">
                          <Mail className="h-3 w-3" /> {person.email}
                        </div>
                      )}
                      {!person.phoneNumber && !person.email && <span>--</span>}
                    </td>
                    <td className="p-3.5 text-right pr-4">
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => {
                          if (confirm(`Bạn có chắc muốn xóa nhân sự ${person.fullName}?`)) {
                            deleteMutation.mutate(person.id)
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
                    Chưa có nhân sự nào trong danh sách
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>

      {/* Modal Thêm Nhân Sự */}
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Users className="h-5 w-5 text-blue-600" />
              Thêm Nhân Sự Mới
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-3 py-2">
            <div className="space-y-1">
              <label className="text-xs font-semibold">Mã nhân viên / Định danh</label>
              <Input placeholder="VD: NV-001" value={newCode} onChange={(e) => setNewCode(e.target.value)} />
            </div>
            <div className="space-y-1">
              <label className="text-xs font-semibold">Họ và tên *</label>
              <Input placeholder="VD: Nguyễn Văn A" value={newName} onChange={(e) => setNewName(e.target.value)} />
            </div>
            <div className="space-y-1">
              <label className="text-xs font-semibold">Số điện thoại</label>
              <Input placeholder="VD: 0912345678" value={newPhone} onChange={(e) => setNewPhone(e.target.value)} />
            </div>
            <div className="space-y-1">
              <label className="text-xs font-semibold">Email</label>
              <Input placeholder="VD: a.nguyen@phuxuan.vn" value={newEmail} onChange={(e) => setNewEmail(e.target.value)} />
            </div>
          </div>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setIsCreateOpen(false)}>Hủy</Button>
            <Button
              disabled={!newName.trim() || createMutation.isPending}
              onClick={() => createMutation.mutate()}
              className="bg-blue-600 hover:bg-blue-700"
            >
              {createMutation.isPending ? 'Đang lưu...' : 'Lưu Nhân Sự'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
