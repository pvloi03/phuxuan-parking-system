export interface ApiResponse<T = any> {
  success: boolean
  message: string
  data: T
  errors?: string[]
  timestamp: string
}

export type VehicleType = 'Car' | 'Motorcycle' | 'Truck' | 'Bicycle' | 'Other'
export type ParkingSessionStatus = 'Active' | 'Completed' | 'UnmatchedOut'
export type UserRole = 'Admin' | 'Manager' | 'Operator' | 'Security' | 'Viewer'
export type PersonType = 'Employee' | 'Contractor' | 'Visitor' | 'VIP' | 'Other'

export type DeviceType = 'Camera' | 'Controller'
export type DeviceStatus = 'Connected' | 'Disconnected' | 'Error' | 'Maintenance'

export interface ImageStoragePathDto {
  path: string
  isEmpty: boolean
}

export interface ParkingSession {
  id: string
  plateNumber: string
  vehicleType: VehicleType
  status: ParkingSessionStatus
  personId?: string
  personName?: string
  companyName?: string
  departmentName?: string
  personType?: PersonType
  inTime?: string
  inLaneName?: string
  inOverviewImagePath?: string | ImageStoragePathDto
  inPlateImagePath?: string | ImageStoragePathDto
  outTime?: string
  outLaneName?: string
  outOverviewImagePath?: string | ImageStoragePathDto
  outPlateImagePath?: string | ImageStoragePathDto
  note?: string
  isDeleted?: boolean
  deletedAt?: string
  createdAt?: string
}

export interface Vehicle {
  id: string
  plateNumber: string
  type: VehicleType
  ownerPersonId?: string
  ownerPersonName?: string
  isActive: boolean
  isDeleted?: boolean
  deletedAt?: string
  createdAt?: string
}

export interface Person {
  id: string
  code: string
  fullName: string
  phoneNumber?: string
  email?: string
  type: PersonType
  departmentId?: string
  companyId?: string
  contractorId?: string
  isActive: boolean
  isDeleted?: boolean
  deletedAt?: string
  createdAt?: string
}

export interface Company {
  id: string
  code: string
  name: string
  phoneNumber?: string
  email?: string
  note?: string
  isActive: boolean
  isDeleted?: boolean
  deletedAt?: string
  createdAt?: string
}

export interface Department {
  id: string
  code: string
  name: string
  companyId?: string
  managerName?: string
  phoneNumber?: string
  email?: string
  note?: string
  isActive: boolean
  isDeleted?: boolean
  deletedAt?: string
  createdAt?: string
}

export interface Contractor {
  id: string
  code: string
  name: string
  contactPerson?: string
  phoneNumber?: string
  email?: string
  note?: string
  isActive: boolean
  isDeleted?: boolean
  deletedAt?: string
  createdAt?: string
}

export interface Device {
  id: string
  code: string
  name: string
  type: DeviceType
  ipAddress: string
  port: number
  userName?: string
  password?: string
  laneId?: string
  laneName?: string
  status: DeviceStatus
  lastHeartbeat?: string
  errorMessage?: string
  note?: string
  isActive: boolean
  isDeleted?: boolean
  deletedAt?: string
  createdAt?: string
}

export interface UserProfile {
  id: string
  username: string
  fullName: string
  email?: string
  phoneNumber?: string
  role: UserRole
  isActive: boolean
  lastLoginAt?: string
}

export interface LoginResponse {
  token: string
  userId: string
  username: string
  fullName: string
  role: UserRole
  expiresAt: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

export interface HourlyTraffic {
  hour: number
  hourLabel: string
  inCount: number
  outCount: number
}

export interface DashboardMetrics {
  activeVehiclesCount: number
  todayInCount: number
  todayOutCount: number
  todayUnmatchedOutCount: number
  hourlyTraffic: HourlyTraffic[]
}

// --- VIETNAMESE LOCALIZATION HELPERS ---
export const getPersonTypeLabel = (type?: PersonType | number | string) => {
  if (type === 'Employee' || type === 1 || type === '1') return 'Cán bộ / Nhân viên'
  if (type === 'Contractor' || type === 2 || type === '2') return 'Đối tác / Nhà thầu'
  if (type === 'Visitor' || type === 3 || type === '3') return 'Khách thăm'
  if (type === 'VIP' || type === 4 || type === '4') return 'Khách VIP'
  return 'Khách vãng lai'
}

export const getVehicleTypeLabel = (type?: VehicleType | number | string) => {
  if (type === 'Car' || type === 1) return 'Ô tô'
  if (type === 'Motorcycle' || type === 2) return 'Xe máy'
  if (type === 'Truck' || type === 3) return 'Xe tải'
  if (type === 'Bicycle' || type === 4) return 'Xe đạp'
  return 'Khác'
}

export const getDeviceTypeLabel = (type?: DeviceType | number | string) => {
  const v = String(type ?? '').toLowerCase()
  if (v === 'camera' || v === '1') return 'Camera IP'
  if (v === 'controller' || v === '2') return 'Bộ Điều Khiển'
  return String(type ?? 'Khác')
}

export const getDeviceStatusLabel = (status?: DeviceStatus | number | string) => {
  if (status === 'Connected' || status === 1) return 'Đang kết nối (Online)'
  if (status === 'Disconnected' || status === 2) return 'Mất kết nối (Offline)'
  if (status === 'Error' || status === 3) return 'Lỗi tín hiệu'
  if (status === 'Maintenance' || status === 4) return 'Đang bảo trì'
  return 'Chưa xác định'
}
