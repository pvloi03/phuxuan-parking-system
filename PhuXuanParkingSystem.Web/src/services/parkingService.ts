import { apiClient } from './apiClient'
import type { ApiResponse, DashboardMetrics, PagedResult, ParkingSession, ParkingSessionStatus } from '@/types'

export interface SessionFilterParams {
  plateNumber?: string
  status?: ParkingSessionStatus
  fromDate?: string
  toDate?: string
  laneName?: string
  pageNumber?: number
  pageSize?: number
}

export const parkingService = {
  getMetrics: async (): Promise<DashboardMetrics> => {
    const res = await apiClient.get<ApiResponse<DashboardMetrics>>('/dashboard/metrics')
    return res.data.data
  },

  getSessions: async (params: SessionFilterParams = {}): Promise<PagedResult<ParkingSession>> => {
    const res = await apiClient.get<ApiResponse<PagedResult<ParkingSession>>>('/parkingsessions', {
      params,
    })
    return res.data.data
  },

  getSessionById: async (id: string): Promise<ParkingSession> => {
    const res = await apiClient.get<ApiResponse<ParkingSession>>(`/parkingsessions/${id}`)
    return res.data.data
  },

  exportExcel: async (params: SessionFilterParams = {}) => {
    const res = await apiClient.get('/parkingsessions/export-excel', {
      params,
      responseType: 'blob',
    })
    const url = window.URL.createObjectURL(new Blob([res.data]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', `BaoCao_LichSuXe_${new Date().toISOString().slice(0, 10)}.xlsx`)
    document.body.appendChild(link)
    link.click()
    link.remove()
  },

  deleteSession: async (id: string): Promise<void> => {
    await apiClient.delete<ApiResponse<void>>(`/parkingsessions/${id}`)
  },

  deleteBatch: async (ids: string[]): Promise<void> => {
    await apiClient.post<ApiResponse<void>>('/parkingsessions/delete-batch', ids)
  },
}
