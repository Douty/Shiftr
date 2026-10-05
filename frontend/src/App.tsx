import './App.css'
import LandingPage from './components/Landing/LandingPage'
import AuthPage from './components/Auth/AuthPage'
import ShiftNotesPage from './components/Employee/ShiftNotesPage'
import EmployeeDashboardPage from './components/Employee/EmployeeDashboardPage'
import AmenityCalendarPage from './components/Employee/AmenityCalendarPage'
import DailyActivityLogPage from './components/Employee/DailyActivityLogPage'
import ResidentLookupPage from './components/Employee/ResidentLookupPage'
import AccessRequestPage from './components/Employee/AccessRequestPage'
import DashboardPage from './components/Organization/DashboardPage'
import ResidentDashboardPage from './components/Resident/ResidentDashboardPage'

function App() {
  if (window.location.pathname === '/resident/dashboard') {
    return <ResidentDashboardPage />
  }

  if (window.location.pathname.startsWith('/dashboard')) {
    return <DashboardPage />
  }

  if (window.location.pathname === '/employee/dashboard') {
    return <EmployeeDashboardPage />
  }

  if (window.location.pathname === '/employee/amenities') {
    return <AmenityCalendarPage />
  }

  if (window.location.pathname === '/employee/daily-activity') {
    return <DailyActivityLogPage />
  }

  if (window.location.pathname === '/employee/residents') {
    return <ResidentLookupPage />
  }

  if (window.location.pathname.startsWith('/employee/access-request')) {
    return <AccessRequestPage />
  }

  if (window.location.pathname.startsWith('/employee')) {
    return <ShiftNotesPage />
  }

  if (window.location.pathname.startsWith('/sign-in/')) {
    return <AuthPage />
  }

  return <LandingPage />
}

export default App
