import { Routes, Route, Navigate } from 'react-router-dom'
import Layout from './components/Layout'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import AccountDetailPage from './pages/AccountDetailPage'
import TransferPage from './pages/TransferPage'
import PayPage from './pages/PayPage'
import ScheduledPaymentsPage from './pages/ScheduledPaymentsPage'
import NudgeInsightPage from './pages/NudgeInsightPage'
import NudgeHistoryPage from './pages/NudgeHistoryPage'

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<LoginPage />} />
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/accounts/:id" element={<AccountDetailPage />} />
        <Route path="/transfer" element={<TransferPage />} />
        <Route path="/pay" element={<PayPage />} />
        <Route path="/scheduled" element={<ScheduledPaymentsPage />} />
        <Route path="/nudge/:id" element={<NudgeInsightPage />} />
        <Route path="/insights" element={<NudgeHistoryPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  )
}
