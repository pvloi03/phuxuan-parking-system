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
export type PersonType = 'Employee' | 'Contractor' | 'Visitor' | 'Resident'

export interface ImageStoragePathDto {
  path: string
  isEmpty: boolean
}

export interface ParkingSession {
  id: string
  plateNumber: string
  vehicleType: VehicleType
  status: ParkingSessionStatus
  personName?: string
  inTime?: string
  inLaneName?: string
  inOverviewImagePath?: string | ImageStoragePathDto
  inPlateImagePath?: string | ImageStoragePathDto
  outTime?: string
  outLaneName?: string
  outOverviewImagePath?: string | ImageStoragePathDto
  outPlateImagePath?: string | ImageStoragePathDto
  note?: string
  createdAt?: string
}

export interface Vehicle {
  id: string
  plateNumber: string
  type: VehicleType
  ownerPersonId?: string
  isActive: boolean
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
  createdAt?: string
}

export interface Department {
  id: string
  code: string
  name: string
  description?: string
  companyId?: string
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
