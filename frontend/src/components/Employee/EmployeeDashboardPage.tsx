import { useEffect, useState } from 'react'
import axios from 'axios'

type EmployeeAssignment = {
  propertyId: number
}

function getAuthHeaders() {
  const token = window.localStorage.getItem('accessToken')
  if (!token) {
    window.location.assign('/sign-in/employee')
    throw new Error('Your session has expired. Sign in again to continue.')
  }
  return { Authorization: `Bearer ${token}` }
}

function EmployeeDashboardPage() {
  const [propertyId, setPropertyId] = useState<number | null>(null)
  const [canOpenPortfolio, setCanOpenPortfolio] = useState(false)
  const [notice, setNotice] = useState('')
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isActive = true

    async function loadWorkspace() {
      try {
        const headers = getAuthHeaders()
        const [assignmentResponse, adminResponse, ownerResponse] = await Promise.all([
          axios.get<EmployeeAssignment>('/api/shift-notes/assignment', { headers }),
          axios.get<boolean>('/api/employee/IsAdmin', { headers }),
          axios.get<boolean>('/api/employee/IsOwner', { headers }),
        ])
        if (!isActive) return
        setPropertyId(assignmentResponse.data.propertyId)
        setCanOpenPortfolio(adminResponse.data && !ownerResponse.data)
      } catch (error) {
        if (!isActive) return
        if (axios.isAxiosError(error) && error.response?.status === 401) {
          window.localStorage.removeItem('accessToken')
          window.location.assign('/sign-in/employee')
        }
        setNotice(axios.isAxiosError(error) && error.response?.status === 403
          ? 'Your account does not have access to the employee workspace.'
          : error instanceof Error ? error.message : 'Could not load the employee workspace.')
      } finally {
        if (isActive) setIsLoading(false)
      }
    }

    void loadWorkspace()
    return () => {
      isActive = false
    }
  }, [])

  return (
    <div className="min-h-dvh bg-[#F5E9E2] font-['Inter',system-ui,sans-serif] text-[#0B0014] antialiased">
      <header className="border-b border-[#773344] bg-[#F5E9E2]">
        <div className="mx-auto flex min-h-16 max-w-[72rem] items-center justify-between gap-4 px-5">
          <a href="/" className="font-['Fraunces',Georgia,serif] text-2xl font-bold text-[#773344] no-underline">Shiftr</a>
          <nav aria-label="Employee navigation" className="flex items-center gap-2 text-base font-semibold">
            {canOpenPortfolio && (
              <a className="rounded-xl px-3 py-2 text-[#773344] underline-offset-4 hover:underline focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]" href="/dashboard">
                Portfolio dashboard
              </a>
            )}
            <button
              className="min-h-11 rounded-xl px-3 py-2 text-[#773344] hover:bg-[#E3B5A4] focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]"
              onClick={() => {
                window.localStorage.removeItem('accessToken')
                window.location.assign('/sign-in/employee')
              }}
              type="button"
            >
              Sign out
            </button>
          </nav>
        </div>
      </header>

      <main className="mx-auto w-full max-w-[72rem] px-5 py-8 sm:py-10">
        <div className="mb-8">
          <p className="mb-1 text-sm font-semibold text-[#773344]">Employee workspace{propertyId === null ? '' : ` · Property #${propertyId}`}</p>
          <h1 className="font-['Fraunces',Georgia,serif] text-4xl font-semibold leading-tight text-[#773344]">Employee dashboard</h1>
          <p className="mt-2 text-lg leading-relaxed">Choose an activity to get started.</p>
        </div>

        {notice && <p className="mb-6 rounded-xl border-2 border-[#773344] bg-[#E3B5A4] px-4 py-3 text-base" role="alert">{notice}</p>}
        {isLoading ? (
          <p className="py-10 text-base" role="status">Loading your workspace…</p>
        ) : (
          <nav aria-label="Employee activities" className="grid items-stretch gap-6 md:grid-cols-2 xl:grid-cols-3">
            <a className="group flex min-h-64 flex-col rounded-3xl border-2 border-[#773344] bg-[#E3B5A4] p-6 text-[#0B0014] no-underline transition-colors hover:bg-[#DBA692] focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]" href="/employee/daily-activity">
              <span className="text-sm font-semibold">Record the day</span>
              <h2 className="mt-3 font-['Fraunces',Georgia,serif] text-2xl font-semibold">Daily activity log</h2>
              <p className="mt-2 flex-1 text-lg leading-relaxed">Write and review activity notes for your property and team.</p>
              <span className="mt-5 font-semibold underline underline-offset-4">Open daily log</span>
            </a>
            <a className="group flex min-h-64 flex-col rounded-3xl border-2 border-[#773344] bg-[#E3B5A4] p-6 text-[#0B0014] no-underline transition-colors hover:bg-[#DBA692] focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]" href="/employee/amenities">
              <span className="text-sm font-semibold">Manage shared spaces</span>
              <h2 className="mt-3 font-['Fraunces',Georgia,serif] text-2xl font-semibold">Amenity calendar</h2>
              <p className="mt-2 flex-1 text-lg leading-relaxed">Create, update, and cancel resident bookings.</p>
              <span className="mt-5 font-semibold underline underline-offset-4">Open amenity calendar</span>
            </a>
            <a className="group flex min-h-64 flex-col rounded-3xl border-2 border-[#773344] bg-[#E3B5A4] p-6 text-[#0B0014] no-underline transition-colors hover:bg-[#DBA692] focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]" href="/employee/residents">
              <span className="text-sm font-semibold">Find resident details</span>
              <h2 className="mt-3 font-['Fraunces',Georgia,serif] text-2xl font-semibold">Resident lookup</h2>
              <p className="mt-2 flex-1 text-lg leading-relaxed">Search residents by name or unit number.</p>
              <span className="mt-5 font-semibold underline underline-offset-4">Look up a resident</span>
            </a>
          </nav>
        )}
      </main>
    </div>
  )
}

export default EmployeeDashboardPage
