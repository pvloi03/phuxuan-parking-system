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

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      {/* Protected Routes */}
      <Route element={<ProtectedRoute />}>
        <Route element={<MainLayout />}>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/history" element={<HistoryPage />} />
          <Route path="/vehicles" element={<VehiclesPage />} />
          <Route path="/companies" element={<CompaniesPage />} />
          <Route path="/departments" element={<DepartmentsPage />} />
          <Route path="/partners" element={<PartnersPage />} />
          <Route path="/people" element={<PeoplePage />} />
        </Route>
      </Route>

      {/* Fallback */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
