import { apiClient } from './apiClient'
import type { RecycleBinCounts, RecycleBinPagedResult } from '@/types'

export interface GetRecycleBinParams {
  itemType?: string
  search?: string
  pageNumber?: number
  pageSize?: number
}

export interface ItemKey {
  itemType: string
  id: string
}

export const recycleBinService = {
  getCounts: async (): Promise<RecycleBinCounts> => {
    const res = await apiClient.get<{ success: boolean; data: RecycleBinCounts }>('/recycle-bin/counts')
    return res.data.data
  },

  getItems: async (params: GetRecycleBinParams): Promise<RecycleBinPagedResult> => {
    const res = await apiClient.get<{ success: boolean; data: RecycleBinPagedResult }>('/recycle-bin', {
      params,
    })
    return res.data.data
  },

  restoreItem: async (itemType: string, id: string): Promise<{ success: boolean; message: string }> => {
    const res = await apiClient.post<{ success: boolean; message: string }>('/recycle-bin/restore', {
      itemType,
      id,
    })
    return res.data
  },

  restoreBatch: async (items: ItemKey[]): Promise<{ success: boolean; message: string; restoredCount: number; errors?: string[] }> => {
    const res = await apiClient.post<{ success: boolean; message: string; restoredCount: number; errors?: string[] }>('/recycle-bin/restore-batch', {
      items,
    })
    return res.data
  },

  hardDeleteItem: async (itemType: string, id: string): Promise<{ success: boolean; message: string }> => {
    const res = await apiClient.delete<{ success: boolean; message: string }>(`/recycle-bin/hard-delete/${itemType}/${id}`)
    return res.data
  },

  hardDeleteBatch: async (items: ItemKey[]): Promise<{ success: boolean; message: string; deletedCount: number; errors?: string[] }> => {
    const res = await apiClient.post<{ success: boolean; message: string; deletedCount: number; errors?: string[] }>('/recycle-bin/hard-delete-batch', {
      items,
    })
    return res.data
  },

  emptyRecycleBin: async (itemType?: string): Promise<{ success: boolean; message: string; totalDeleted: number }> => {
    const res = await apiClient.delete<{ success: boolean; message: string; totalDeleted: number }>('/recycle-bin/empty', {
      params: itemType ? { itemType } : undefined,
    })
    return res.data
  },
}
