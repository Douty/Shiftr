import { useEffect, useState } from 'react'
import axios from 'axios'

type Resident = {
  id: number
  firstName: string
  lastName: string
  unitNumber: string | null
  canBookAmenities: boolean
}

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

function ResidentLookupPage() {
  const [propertyId, setPropertyId] = useState<number | null>(null)
  const [residents, setResidents] = useState<Resident[]>([])
  const [search, setSearch] = useState('')
  const [notice, setNotice] = useState('')
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isActive = true

    async function loadResidents() {
      try {
        const headers = getAuthHeaders()
        const assignmentResponse = await axios.get<EmployeeAssignment>('/api/shift-notes/assignment', { headers })
        const { data } = await axios.get<Resident[]>(
          `/api/properties/${assignmentResponse.data.propertyId}/residents`,
          { headers },
        )
        if (!isActive) return
        setPropertyId(assignmentResponse.data.propertyId)
        setResidents(data)
      } catch (error) {
        if (!isActive) return
        if (axios.isAxiosError(error) && error.response?.status === 401) {
          window.localStorage.removeItem('accessToken')
          window.location.assign('/sign-in/employee')
        }
        setNotice(axios.isAxiosError<{ detail?: string; title?: string }>(error)
          ? error.response?.data?.detail ?? error.response?.data?.title ?? error.message
          : error instanceof Error ? error.message : 'Could not load residents.')
      } finally {
        if (isActive) setIsLoading(false)
      }
    }

    void loadResidents()
    return () => {
      isActive = false
    }
  }, [])

  const normalizedSearch = search.trim().toLocaleLowerCase()
  const matches = normalizedSearch
    ? residents.filter(resident =>
      `${resident.firstName} ${resident.lastName} ${resident.unitNumber ?? ''}`
        .toLocaleLowerCase()
        .includes(normalizedSearch),
    )
    : residents

  return (
    <div className="min-h-dvh bg-[#F5E9E2] font-['Inter',system-ui,sans-serif] text-[#0B0014] antialiased">
      <header className="border-b border-[#773344] bg-[#F5E9E2]">
        <div className="mx-auto flex min-h-16 max-w-[72rem] items-center justify-between gap-4 px-5">
          <a href="/" className="font-['Fraunces',Georgia,serif] text-2xl font-bold text-[#773344] no-underline">Shiftr</a>
          <nav aria-label="Employee navigation" className="flex items-center gap-2 text-base font-semibold">
            <a className="rounded-xl px-3 py-2 text-[#773344] underline-offset-4 hover:underline focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]" href="/employee/dashboard">Dashboard</a>
            <a className="hidden rounded-xl px-3 py-2 text-[#773344] underline-offset-4 hover:underline focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014] sm:inline" href="/employee/daily-activity">Daily activity</a>
            <a className="hidden rounded-xl px-3 py-2 text-[#773344] underline-offset-4 hover:underline focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014] sm:inline" href="/employee/amenities">Amenity calendar</a>
          </nav>
        </div>
      </header>
      <main className="mx-auto w-full max-w-[72rem] px-5 py-8 sm:py-10">
        <a className="font-semibold text-[#773344] underline underline-offset-4" href="/employee/dashboard">Employee dashboard</a>
        <div className="mb-8 mt-5">
          <p className="mb-1 text-sm font-semibold text-[#773344]">Resident services{propertyId === null ? '' : ` · Property #${propertyId}`}</p>
          <h1 className="font-['Fraunces',Georgia,serif] text-4xl font-semibold leading-tight text-[#773344]">Resident lookup</h1>
          <p className="mt-2 text-lg leading-relaxed">Find a resident by name or unit number.</p>
        </div>
        {notice && <p className="mb-6 rounded-xl border-2 border-[#773344] bg-[#E3B5A4] px-4 py-3 text-base" role="alert">{notice}</p>}
        {isLoading ? <p className="py-8 text-base" role="status">Loading residents…</p> : (
          <section aria-labelledby="resident-list-title" className="rounded-3xl border-2 border-[#773344] bg-[#E3B5A4] p-5 sm:p-7">
            <label className="grid max-w-xl gap-2 text-base font-semibold" htmlFor="resident-search">
              Search by name or unit number
              <input
                autoComplete="off"
                className="min-h-12 rounded-xl border border-[#773344] bg-[#F5E9E2] px-4 text-base font-normal text-[#0B0014] outline-none focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]"
                id="resident-search"
                onChange={event => setSearch(event.target.value)}
                placeholder="Enter a name or unit"
                type="search"
                value={search}
              />
            </label>
            <div className="mt-7 flex flex-wrap items-baseline justify-between gap-3">
              <h2 className="m-0 font-['Fraunces',Georgia,serif] text-2xl font-semibold" id="resident-list-title">Residents</h2>
              <span className="text-base">{matches.length} {matches.length === 1 ? 'resident' : 'residents'}</span>
            </div>
            {matches.length === 0 ? (
              <p aria-live="polite" className="mt-4 border-y border-[#773344] py-5 text-base">
                {normalizedSearch ? 'No residents match that search.' : 'No residents are listed for this property.'}
              </p>
            ) : (
              <ul className="mt-4 list-none divide-y divide-[#773344] border-y border-[#773344] p-0" aria-live="polite">
                {matches.map(resident => (
                  <li className="flex flex-wrap items-center justify-between gap-3 py-4" key={resident.id}>
                    <span className="text-lg font-semibold">{resident.firstName} {resident.lastName}</span>
                    {resident.unitNumber && <span className="text-base">Unit {resident.unitNumber}</span>}
                  </li>
                ))}
              </ul>
            )}
          </section>
        )}
      </main>
    </div>
  )
}

export default ResidentLookupPage
