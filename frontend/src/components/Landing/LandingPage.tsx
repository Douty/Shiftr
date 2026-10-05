import { useEffect, useState } from 'react'
import axios from 'axios'
import AboutSection from './AboutSection'
import BenefitsSection from './BenefitsSection'
import HeroSection from './HeroSection'

type DashboardLink = {
  href: string
  label: string
}

function LandingPage() {
  const [dashboardLink, setDashboardLink] = useState<DashboardLink | null>(null)
  const [isCheckingSession, setIsCheckingSession] = useState(
    () => Boolean(window.localStorage.getItem('accessToken'))
  )

  useEffect(() => {
    const token = window.localStorage.getItem('accessToken')
    if (!token) return

    let isActive = true
    const headers = { Authorization: ['Bearer', token].join(' ') }

    async function findDashboard() {
      try {
        try {
          await axios.get('/api/resident/profile', { headers })
          if (isActive) setDashboardLink({ href: '/resident/dashboard', label: 'Resident dashboard' })
          return
        } catch (error) {
          if (axios.isAxiosError(error) && error.response?.status === 401) {
            window.localStorage.removeItem('accessToken')
            return
          }
          if (!axios.isAxiosError(error) || error.response?.status !== 403) return
        }

        try {
          const { data: isAdmin } = await axios.get<boolean>('/api/employee/IsAdmin', { headers })
          if (isAdmin) {
            if (isActive) setDashboardLink({ href: '/dashboard', label: 'Organization dashboard' })
            return
          }
          const { data: hasEmployeeProfile } = await axios.get<boolean>('/api/employee/HasProfile', { headers })
          if (hasEmployeeProfile && isActive) {
            setDashboardLink({ href: '/employee/dashboard', label: 'Employee dashboard' })
          }
        } catch (error) {
          if (axios.isAxiosError(error) && error.response?.status === 401) {
            window.localStorage.removeItem('accessToken')
          }
        }
      } finally {
        if (isActive) setIsCheckingSession(false)
      }
    }

    void findDashboard()
    return () => {
      isActive = false
    }
  }, [])

  return (
    <div className="min-h-dvh bg-canvas pb-[env(safe-area-inset-bottom)] pt-[env(safe-area-inset-top)] font-sans text-lg leading-relaxed text-ink antialiased">
      <a
        href="#main"
        className="sr-only rounded-lg bg-ink px-4 py-3 text-canvas focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-10 focus-visible:outline focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-ink"
      >
        Skip to main content
      </a>
      <main id="main" className="mx-auto w-full max-w-[72rem] px-5">
        <HeroSection dashboardLink={dashboardLink} isCheckingSession={isCheckingSession} />
        <AboutSection />
        <BenefitsSection />
      </main>
    </div>
  )
}

export default LandingPage
