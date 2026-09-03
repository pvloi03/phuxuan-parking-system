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

export type DeviceType = 'PlateCamera' | 'OverviewCamera' | 'Controller'
export type DeviceStatus = 'Connected' | 'Disconnected' | 'Error' | 'Maintenance' | 'Connecting' | 'Streaming'

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
  departmentName?: string
  companyId?: string
  companyName?: string
  contractorId?: string
  contractorName?: string
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
  status: DeviceStatus
  lastHeartbeat?: string
  errorMessage?: string
  note?: string
  isActive: boolean
  isDeleted?: boolean
  deletedAt?: string
  createdAt?: string
}

export interface Lane {
  id: string
  code: string
  name: string
  direction: 'In' | 'Out' | 1 | 2
  description?: string
  isActive: boolean
  overviewCameraDeviceId?: string
  plateCameraDeviceId?: string
  controllerDeviceId?: string
  triggerAuxPort?: number
  isDeleted?: boolean
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

export interface TrafficDataPoint {
  label: string
  inCount: number
  outCount: number
}

export type DashboardPeriod = 'today' | 'month' | 'year' | 'custom'

export interface DashboardMetrics {
  activeVehiclesCount: number
  periodInCount: number
  periodOutCount: number
  periodUnmatchedOutCount: number
  periodLabel: string
  periodType: DashboardPeriod
  trafficChart: TrafficDataPoint[]
  todayInCount?: number
  todayOutCount?: number
  todayUnmatchedOutCount?: number
  hourlyTraffic?: HourlyTraffic[]
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
  if (status === 'Connecting' || status === 5) return 'Đang kết nối...'
  if (status === 'Streaming' || status === 6) return 'Đang phát luồng (Live)'
  return 'Chưa xác định'
}

// --- USER & RBAC TYPES ---
export interface User {
  id: string
  username: string
  fullName: string
  email?: string | null
  phoneNumber?: string | null
  role: UserRole
  roleLabel?: string
  isActive: boolean
  lastLoginAt?: string | null
  createdAt: string
  isDeleted?: boolean
  deletedAt?: string | null
}

export interface CreateUserPayload {
  username: string
  password: string
  fullName: string
  email?: string
  phoneNumber?: string
  role: UserRole
  isActive: boolean
}

export interface UpdateUserPayload {
  fullName: string
  email?: string
  phoneNumber?: string
  role: UserRole
  isActive: boolean
}

export interface ChangePasswordPayload {
  oldPassword?: string
  newPassword: string
}

export interface UserPagedResult {
  items: User[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

export const getUserRoleLabel = (role?: UserRole | number | string) => {
  if (role === 'Admin' || role === 1 || role === '1') return 'Quản Trị Viên'
  if (role === 'Manager' || role === 2 || role === '2') return 'Quản Lý'
  if (role === 'Operator' || role === 3 || role === '3') return 'Nhân Viên Vận Hành'
  if (role === 'Security' || role === 4 || role === '4') return 'Bảo Vệ Trực Làn'
  if (role === 'Viewer' || role === 5 || role === '5') return 'Người Xem'
  return String(role ?? 'Khác')
}

// --- RECYCLE BIN (THÙNG RÁC) TYPES ---
export interface RecycleBinItem {
  id: string
  itemType: 'Vehicle' | 'Person' | 'Contractor' | 'Department' | 'Company' | 'Device' | 'Lane' | 'ParkingSession' | 'User'
  itemTypeLabel: string
  identifier: string
  title: string
  description: string
  deletedAt?: string
  createdAt: string
  canRestore: boolean
  warningMessage?: string | null
}

export interface RecycleBinCounts {
  totalCount: number
  vehicleCount: number
  personCount: number
  contractorCount: number
  departmentCount: number
  companyCount: number
  deviceCount: number
  laneCount: number
  parkingSessionCount: number
  userCount?: number
}

export interface RecycleBinPagedResult {
  items: RecycleBinItem[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

// --- AUDIT LOG TYPES ---
export type AuditActionType =
  | 'Login'
  | 'Logout'
  | 'Create'
  | 'Update'
  | 'Delete'
  | 'ChangePassword'
  | 'ChangeRole'
  | 'LicenseUpdate'
  | 'Export'
  | 'ManualOverride'

export interface AuditLog {
  id: string
  actorId?: string
  actorUsername: string
  actorRole: string
  source: string
  ipAddress?: string
  userAgent?: string
  actionType: AuditActionType
  targetEntity: string
  targetId?: string
  targetDisplay?: string
  oldValues?: string
  newValues?: string
  changedProperties: string[]
  reason?: string
  isSuccess: boolean
  errorMessage?: string
  createdAt: string
}
