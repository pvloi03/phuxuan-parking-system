import { apiClient } from './apiClient';

export interface LicenseQuota {
  maxLanes: number;
  currentLanes: number;
  isLanesLimitReached: boolean;
  maxCameras: number;
  currentCameras: number;
  isCamerasLimitReached: boolean;
  maxControllers: number;
  currentControllers: number;
  isControllersLimitReached: boolean;
}

export interface LicenseStatus {
  machineCode: string;
  isValid: boolean;
  isExpired: boolean;
  isPermanent: boolean;
  daysRemaining: number;
  customerName: string;
  expiryDate: string;
  issuedAt: string;
  message: string;
  quota: LicenseQuota;
  features: string[];
}

export const licenseService = {
  getStatus: async (): Promise<LicenseStatus> => {
    const res = await apiClient.get<LicenseStatus>('/api/license/status');
    return res.data;
  },

  getMachineCode: async (): Promise<string> => {
    const res = await apiClient.get<{ machineCode: string }>('/api/license/machine-code');
    return res.data.machineCode;
  },

  activate: async (licenseKey: string): Promise<any> => {
    const res = await apiClient.post('/api/license/activate', { licenseKey });
    return res.data;
  },

  uploadFile: async (file: File): Promise<any> => {
    const formData = new FormData();
    formData.append('file', file);
    const res = await apiClient.post('/api/license/upload-lic', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return res.data;
  },
};
