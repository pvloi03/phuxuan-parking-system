import { apiClient } from './apiClient'
import type { ApiResponse, AuditLog, PagedResult } from '@/types'

export interface AuditLogQueryParams {
  fromDate?: string
  toDate?: string
  actor?: string
  actionType?: string
  targetEntity?: string
  isSuccess?: boolean
  search?: string
  pageNumber?: number
  pageSize?: number
}

export const auditLogService = {
  getLogs: async (params: AuditLogQueryParams = {}) => {
    const response = await apiClient.get<ApiResponse<PagedResult<AuditLog>>>('/v1/audit-logs', { params })
    return response.data.data
  },

  getLogById: async (id: string) => {
    const response = await apiClient.get<ApiResponse<AuditLog>>(`/v1/audit-logs/${id}`)
    return response.data.data
  },

  exportLogs: async (params: AuditLogQueryParams = {}) => {
    const response = await apiClient.get('/v1/audit-logs/export', {
      params,
      responseType: 'blob',
    })
    const blob = new Blob([response.data], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `AuditLogs_${new Date().toISOString().slice(0, 10)}.xlsx`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    window.URL.revokeObjectURL(url)
  },
}
