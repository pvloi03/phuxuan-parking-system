import { Routes, Route, Navigate } from 'react-router-dom'
import { ProtectedRoute } from './ProtectedRoute'
import { MainLayout } from '@/components/layout/MainLayout'
import { LoginPage } from '@/pages/LoginPage'
import { DashboardPage } from '@/pages/DashboardPage'
import { HistoryPage } from '@/pages/HistoryPage'
import { VehiclesPage } from '@/pages/VehiclesPage'
import { PeoplePage } from '@/pages/PeoplePage'
import { CompaniesPage } from '@/pages/CompaniesPage'
import { DepartmentsPage } from '@/pages/DepartmentsPage'
import { PartnersPage } from '@/pages/PartnersPage'
import { DevicesPage } from '@/pages/DevicesPage'
import { LanesPage } from '@/pages/LanesPage'
import { UsersPage } from '@/pages/UsersPage'
import { RecycleBinPage } from '@/pages/RecycleBinPage'
import { LicensePage } from '@/pages/LicensePage'
import { NotFoundPage } from '@/pages/NotFoundPage'

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      {/* Protected Routes with MainLayout */}
      <Route element={<ProtectedRoute />}>
        <Route element={<MainLayout />}>
          {/* Public to all authenticated users */}
          <Route path="/" element={<DashboardPage />} />
          <Route path="/history" element={<HistoryPage />} />

          {/* Org & Management Routes (Admin & Manager) */}
          <Route element={<ProtectedRoute allowedRoles={['Admin', 'Manager', '1', '2']} />}>
            <Route path="/vehicles" element={<VehiclesPage />} />
            <Route path="/companies" element={<CompaniesPage />} />
            <Route path="/departments" element={<DepartmentsPage />} />
            <Route path="/partners" element={<PartnersPage />} />
            <Route path="/people" element={<PeoplePage />} />
            <Route path="/lanes" element={<LanesPage />} />
            <Route path="/devices" element={<DevicesPage />} />
            <Route path="/users" element={<UsersPage />} />
            <Route path="/license" element={<LicensePage />} />
          </Route>

          {/* Super Admin only Routes */}
          <Route element={<ProtectedRoute allowedRoles={['Admin', '1']} />}>
            <Route path="/recycle-bin" element={<RecycleBinPage />} />
            <Route path="/trash" element={<Navigate to="/recycle-bin" replace />} />
          </Route>
        </Route>
      </Route>

      {/* Fallback - 404 Page */}
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}
