import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'

type Amenity = {
  id: number
  name: string
  description: string | null
}

type Resident = {
  id: number
  firstName: string
  lastName: string
  unitNumber: string | null
  canBookAmenities: boolean
}

type Reservation = {
  id: number
  amenityTypeId: number
  residentId: number | null
  startsAt: string
  endsAt: string
  notes: string | null
}

type EmployeeAssignment = {
  propertyId: number
}

type BookingForm = {
  amenityTypeId: string
  residentId: string
  startsAt: string
  endsAt: string
  notes: string
}

function getAuthHeaders() {
  const token = window.localStorage.getItem('accessToken')
  if (!token) {
    window.location.assign('/sign-in/employee')
    throw new Error('Your session has expired. Sign in again to continue.')
  }
  return { Authorization: `Bearer ${token}` }
}

function getErrorMessage(error: unknown) {
  if (axios.isAxiosError<{ detail?: string; title?: string }>(error)) {
    if (error.response?.status === 401) {
      window.localStorage.removeItem('accessToken')
      window.location.assign('/sign-in/employee')
      return 'Your session has expired. Sign in again.'
    }
    if (error.response?.status === 403) return 'Your account cannot manage bookings for this property.'
    if (error.response?.status === 409) return 'That amenity is already booked for part of this time.'
    if (error.response?.status === 400) {
      return typeof error.response.data === 'string'
        ? error.response.data
        : error.response.data?.detail ?? 'Check the booking details and try again.'
    }
    return error.response?.data?.detail ?? error.response?.data?.title ?? error.message
  }
  return error instanceof Error ? error.message : 'Something went wrong. Please try again.'
}

function dateKey(date: Date) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function getReservationDay(value: string) {
  return dateKey(new Date(value))
}

function reservationOverlapsDay(reservation: Reservation, day: Date) {
  const dayStart = new Date(day.getFullYear(), day.getMonth(), day.getDate()).getTime()
  const nextDayStart = new Date(day.getFullYear(), day.getMonth(), day.getDate() + 1).getTime()
  return Date.parse(reservation.startsAt) < nextDayStart && Date.parse(reservation.endsAt) > dayStart
}

function toLocalInput(value: string) {
  const date = new Date(value)
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000)
  return local.toISOString().slice(0, 16)
}

function formatDate(value: string) {
  return new Date(value).toLocaleString(undefined, {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  })
}

function getEmptyForm(date: string): BookingForm {
  return {
    amenityTypeId: '',
    residentId: '',
    startsAt: `${date}T09:00`,
    endsAt: `${date}T10:00`,
    notes: '',
  }
}

function AmenityCalendarPage() {
  const today = new Date()
  const [propertyId, setPropertyId] = useState<number | null>(null)
  const [amenities, setAmenities] = useState<Amenity[]>([])
  const [residents, setResidents] = useState<Resident[]>([])
  const [reservations, setReservations] = useState<Reservation[]>([])
  const [currentMonth, setCurrentMonth] = useState(new Date(today.getFullYear(), today.getMonth(), 1))
  const [selectedDay, setSelectedDay] = useState(dateKey(today))
  const [form, setForm] = useState<BookingForm>(getEmptyForm(dateKey(today)))
  const [editingReservationId, setEditingReservationId] = useState<number | null>(null)
  const [notice, setNotice] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [deletingReservationId, setDeletingReservationId] = useState<number | null>(null)

  useEffect(() => {
    let isActive = true

    async function loadCalendar() {
      try {
        const headers = getAuthHeaders()
        const { data: assignment } = await axios.get<EmployeeAssignment>(
          '/api/shift-notes/assignment',
          { headers },
        )
        const [amenitiesResponse, reservationsResponse, residentsResponse] = await Promise.all([
          axios.get<Amenity[]>(`/api/properties/${assignment.propertyId}/amenities`, { headers }),
          axios.get<Reservation[]>(`/api/properties/${assignment.propertyId}/reservations`, { headers }),
          axios.get<Resident[]>(`/api/properties/${assignment.propertyId}/residents`, { headers }),
        ])
        if (!isActive) return
        setPropertyId(assignment.propertyId)
        setAmenities(amenitiesResponse.data)
        setReservations(reservationsResponse.data)
        setResidents(residentsResponse.data)
        if (amenitiesResponse.data.length > 0) {
          setForm(current => ({ ...current, amenityTypeId: String(amenitiesResponse.data[0].id) }))
        }
      } catch (error) {
        if (isActive) setNotice(getErrorMessage(error))
      } finally {
        if (isActive) setIsLoading(false)
      }
    }

    void loadCalendar()
    return () => {
      isActive = false
    }
  }, [])

  const amenityNames = new Map(amenities.map(amenity => [amenity.id, amenity.name]))
  const residentNames = new Map(residents.map(resident => [
    resident.id,
    `${resident.firstName} ${resident.lastName}${resident.unitNumber ? ` · Unit ${resident.unitNumber}` : ''}`,
  ]))
  const firstDay = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), 1)
  const calendarStart = new Date(
    firstDay.getFullYear(),
    firstDay.getMonth(),
    1 - ((firstDay.getDay() + 6) % 7),
  )
  const calendarDays = Array.from({ length: 42 }, (_, index) => {
    const day = new Date(calendarStart.getFullYear(), calendarStart.getMonth(), calendarStart.getDate() + index)
    const dayReservations = reservations.filter(reservation => reservationOverlapsDay(reservation, day))
    return { day, dayReservations }
  })
  const selectedReservations = reservations
    .filter(reservation => reservationOverlapsDay(reservation, new Date(`${selectedDay}T12:00:00`)))
    .sort((first, second) => Date.parse(first.startsAt) - Date.parse(second.startsAt))
  const availableResidents = residents.filter(resident => resident.canBookAmenities)

  function moveMonth(monthOffset: number) {
    const nextMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth() + monthOffset, 1)
    setCurrentMonth(nextMonth)
    setSelectedDay(dateKey(nextMonth))
  }

  function startCreateBooking(day = selectedDay) {
    setEditingReservationId(null)
    setForm({
      ...getEmptyForm(day),
      amenityTypeId: amenities[0] ? String(amenities[0].id) : '',
    })
    setNotice('')
  }

  function startEditingBooking(reservation: Reservation) {
    if (reservation.residentId === null) {
      setNotice('This booking is linked to a resident record that cannot be edited here. Contact a manager.')
      return
    }
    setEditingReservationId(reservation.id)
    setForm({
      amenityTypeId: String(reservation.amenityTypeId),
      residentId: String(reservation.residentId),
      startsAt: toLocalInput(reservation.startsAt),
      endsAt: toLocalInput(reservation.endsAt),
      notes: reservation.notes ?? '',
    })
    setNotice('')
  }

  async function saveBooking(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (propertyId === null || !form.amenityTypeId || !form.residentId) return
    const startsAt = new Date(form.startsAt)
    const endsAt = new Date(form.endsAt)
    if (Number.isNaN(startsAt.getTime()) || Number.isNaN(endsAt.getTime()) || endsAt <= startsAt) {
      setNotice('Choose an end time after the start time.')
      return
    }

    setIsSaving(true)
    setNotice('')
    const headers = getAuthHeaders()
    const payload = {
      amenityTypeId: Number(form.amenityTypeId),
      residentId: Number(form.residentId),
      startsAt: startsAt.toISOString(),
      endsAt: endsAt.toISOString(),
      notes: form.notes.trim() || null,
    }
    try {
      if (editingReservationId === null) {
        const { data } = await axios.post<Reservation>(
          `/api/properties/${propertyId}/reservations`,
          payload,
          { headers },
        )
        setReservations(current => [...current, data].sort(
          (first, second) => Date.parse(first.startsAt) - Date.parse(second.startsAt),
        ))
        setSelectedDay(getReservationDay(data.startsAt))
        setForm(getEmptyForm(getReservationDay(data.startsAt)))
        setNotice('Amenity booking created.')
      } else {
        const { data } = await axios.put<Reservation>(
          `/api/properties/${propertyId}/reservations/${editingReservationId}`,
          payload,
          { headers },
        )
        setReservations(current => current.map(reservation => reservation.id === data.id ? data : reservation)
          .sort((first, second) => Date.parse(first.startsAt) - Date.parse(second.startsAt)))
        setSelectedDay(getReservationDay(data.startsAt))
        setForm(getEmptyForm(getReservationDay(data.startsAt)))
        setNotice('Amenity booking updated.')
      }
      setEditingReservationId(null)
    } catch (error) {
      setNotice(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function deleteBooking(reservation: Reservation) {
    const name = amenityNames.get(reservation.amenityTypeId) ?? 'this amenity'
    if (!window.confirm(`Cancel the ${name} booking for ${formatDate(reservation.startsAt)}?`)) return
    if (propertyId === null) return

    setDeletingReservationId(reservation.id)
    setNotice('')
    try {
      await axios.delete(
        `/api/properties/${propertyId}/reservations/${reservation.id}`,
        { headers: getAuthHeaders() },
      )
      setReservations(current => current.filter(item => item.id !== reservation.id))
      if (editingReservationId === reservation.id) {
        setEditingReservationId(null)
        setForm(getEmptyForm(selectedDay))
      }
      setNotice('Amenity booking cancelled.')
    } catch (error) {
      setNotice(getErrorMessage(error))
    } finally {
      setDeletingReservationId(null)
    }
  }

  return (
    <div className="min-h-dvh bg-[#F5E9E2] font-['Inter',system-ui,sans-serif] text-[#0B0014] antialiased">
      <header className="border-b border-[#773344] bg-[#F5E9E2]">
        <div className="mx-auto flex min-h-16 max-w-[72rem] items-center justify-between gap-4 px-5">
          <a href="/" className="font-['Fraunces',Georgia,serif] text-2xl font-bold text-[#773344] no-underline">Shiftr</a>
          <nav aria-label="Employee navigation" className="flex items-center gap-2 text-base font-semibold">
            <a className="rounded-xl px-3 py-2 text-[#773344] underline-offset-4 hover:underline focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]" href="/employee/dashboard">Dashboard</a>
            <a className="hidden rounded-xl px-3 py-2 text-[#773344] underline-offset-4 hover:underline focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014] sm:inline" href="/employee/daily-activity">Daily activity</a>
            <a className="hidden rounded-xl px-3 py-2 text-[#773344] underline-offset-4 hover:underline focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014] sm:inline" href="/employee/residents">Residents</a>
          </nav>
        </div>
      </header>

      <main className="mx-auto w-full max-w-[72rem] px-5 py-8 sm:py-10">
        <a className="font-semibold text-[#773344] underline underline-offset-4" href="/employee/dashboard">Employee dashboard</a>
        <div className="mb-8 mt-5">
          <p className="mb-1 text-sm font-semibold text-[#773344]">Shared spaces{propertyId === null ? '' : ` · Property #${propertyId}`}</p>
          <h1 className="font-['Fraunces',Georgia,serif] text-4xl font-semibold leading-tight text-[#773344]">Amenity calendar</h1>
          <p className="mt-2 text-lg leading-relaxed">Create, update, and cancel resident bookings.</p>
        </div>
        {notice && <p className="mb-6 rounded-xl border-2 border-[#773344] bg-[#E3B5A4] px-4 py-3 text-base" role="status">{notice}</p>}
        {isLoading ? <p className="py-8 text-base" role="status">Loading the amenity calendar…</p> : (
          <div className="grid items-start gap-6 xl:grid-cols-[minmax(0,1.25fr)_minmax(20rem,0.9fr)]">
            <section aria-label="Monthly reservation calendar" className="rounded-3xl border-2 border-[#773344] bg-[#E3B5A4] p-4 sm:p-6">
              <div className="mb-5 flex items-center justify-between gap-3">
                <button
                  aria-label="Previous month"
                  className="grid size-11 place-items-center rounded-xl border border-[#773344] text-lg font-bold text-[#773344] hover:bg-[#DBA692] focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]"
                  onClick={() => moveMonth(-1)}
                  type="button"
                >
                  ‹
                </button>
                <h2 className="m-0 font-['Fraunces',Georgia,serif] text-2xl font-semibold">
                  {currentMonth.toLocaleDateString(undefined, { month: 'long', year: 'numeric' })}
                </h2>
                <button
                  aria-label="Next month"
                  className="grid size-11 place-items-center rounded-xl border border-[#773344] text-lg font-bold text-[#773344] hover:bg-[#DBA692] focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]"
                  onClick={() => moveMonth(1)}
                  type="button"
                >
                  ›
                </button>
              </div>
              <div className="grid grid-cols-7 text-center text-sm font-semibold">
                {['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'].map(weekday => <span className="py-2" key={weekday}>{weekday}</span>)}
              </div>
              <div className="grid grid-cols-7 border-l border-t border-[#773344]">
                {calendarDays.map(({ day, dayReservations }) => {
                  const key = dateKey(day)
                  const isCurrentMonth = day.getMonth() === currentMonth.getMonth()
                  const isSelected = key === selectedDay
                  return (
                    <button
                      aria-label={`${day.toLocaleDateString(undefined, { month: 'long', day: 'numeric', year: 'numeric' })}${dayReservations.length ? `, ${dayReservations.length} reservation${dayReservations.length === 1 ? '' : 's'}` : ''}`}
                      aria-pressed={isSelected}
                      className={`flex min-h-16 flex-col items-center justify-start gap-1 border-b border-r border-[#773344] px-1 py-2 text-base sm:min-h-20 ${isCurrentMonth ? 'text-[#0B0014]' : 'bg-[#F5E9E2] text-[#773344]'} ${isSelected ? 'bg-[#F5E9E2] font-bold ring-2 ring-inset ring-[#773344]' : 'hover:bg-[#DBA692]'} focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[-3px] focus-visible:outline-[#0B0014]`}
                      key={key}
                      onClick={() => {
                        setSelectedDay(key)
                        if (!isCurrentMonth) setCurrentMonth(new Date(day.getFullYear(), day.getMonth(), 1))
                      }}
                      type="button"
                    >
                      <span>{day.getDate()}</span>
                      {dayReservations.length > 0 && <span className="text-sm font-semibold">{dayReservations.length} booking{dayReservations.length === 1 ? '' : 's'}</span>}
                    </button>
                  )
                })}
              </div>
            </section>

            <div className="grid gap-6">
              <section aria-labelledby="bookings-title" className="rounded-3xl border-2 border-[#773344] bg-[#E3B5A4] p-5 sm:p-6">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <p className="m-0 text-sm font-semibold">Selected day</p>
                    <h2 className="mb-0 mt-1 font-['Fraunces',Georgia,serif] text-2xl font-semibold" id="bookings-title">
                      {new Date(`${selectedDay}T12:00:00`).toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric' })}
                    </h2>
                  </div>
                  <button
                    className="min-h-11 rounded-xl bg-[#773344] px-4 py-2 text-base font-semibold text-white hover:bg-[#5E2836] focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]"
                    onClick={() => startCreateBooking()}
                    type="button"
                  >
                    New booking
                  </button>
                </div>
                {selectedReservations.length === 0 ? (
                  <p className="mt-4 border-y border-[#773344] py-4 text-base">No bookings for this day.</p>
                ) : (
                  <ul className="mt-4 list-none divide-y divide-[#773344] border-y border-[#773344] p-0">
                    {selectedReservations.map(reservation => (
                      <li className="py-4" key={reservation.id}>
                        <div className="flex flex-wrap items-start justify-between gap-3">
                          <div>
                            <h3 className="m-0 text-lg font-bold">{amenityNames.get(reservation.amenityTypeId) ?? 'Amenity'}</h3>
                            <p className="mb-0 mt-1 text-base">{formatDate(reservation.startsAt)}</p>
                            <p className="mb-0 mt-1 text-base">Until {new Date(reservation.endsAt).toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' })}</p>
                            <p className="mb-0 mt-1 text-base">{reservation.residentId === null ? 'Resident booking' : residentNames.get(reservation.residentId) ?? 'Resident no longer listed'}</p>
                            {reservation.notes && <p className="mb-0 mt-2 whitespace-pre-wrap text-base">{reservation.notes}</p>}
                          </div>
                          <div className="flex gap-2">
                            <button
                              className="min-h-11 rounded-xl border border-[#773344] px-3 py-2 text-base font-semibold text-[#0B0014] hover:bg-[#DBA692] focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]"
                              onClick={() => startEditingBooking(reservation)}
                              type="button"
                            >
                              Edit
                            </button>
                            <button
                              className="min-h-11 rounded-xl border border-[#773344] px-3 py-2 text-base font-semibold text-[#0B0014] hover:bg-[#DBA692] focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014] disabled:opacity-60"
                              disabled={deletingReservationId !== null}
                              onClick={() => void deleteBooking(reservation)}
                              type="button"
                            >
                              {deletingReservationId === reservation.id ? 'Cancelling…' : 'Cancel'}
                            </button>
                          </div>
                        </div>
                      </li>
                    ))}
                  </ul>
                )}
              </section>

              <section aria-labelledby="booking-form-title" className="rounded-3xl border-2 border-[#773344] bg-[#E3B5A4] p-5 sm:p-6">
                <h2 className="mb-4 mt-0 font-['Fraunces',Georgia,serif] text-2xl font-semibold" id="booking-form-title">
                  {editingReservationId === null ? 'Create amenity booking' : 'Update amenity booking'}
                </h2>
                {amenities.length === 0 || availableResidents.length === 0 ? (
                  <p className="border-y border-[#773344] py-4 text-base">
                    {amenities.length === 0
                      ? 'An amenity must be added before bookings can be created.'
                      : 'No residents with booking accounts are available for this property.'}
                  </p>
                ) : (
                  <form className="grid gap-4" onSubmit={event => void saveBooking(event)}>
                    <label className="grid gap-1.5 text-base font-semibold" htmlFor="booking-amenity">
                      Amenity
                      <select
                        className="min-h-11 rounded-xl border border-[#773344] bg-[#F5E9E2] px-3 text-base font-normal text-[#0B0014] focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]"
                        id="booking-amenity"
                        onChange={event => setForm(current => ({ ...current, amenityTypeId: event.target.value }))}
                        required
                        value={form.amenityTypeId}
                      >
                        <option value="">Choose an amenity</option>
                        {amenities.map(amenity => <option key={amenity.id} value={amenity.id}>{amenity.name}</option>)}
                      </select>
                    </label>
                    <label className="grid gap-1.5 text-base font-semibold" htmlFor="booking-resident">
                      Resident
                      <select
                        className="min-h-11 rounded-xl border border-[#773344] bg-[#F5E9E2] px-3 text-base font-normal text-[#0B0014] focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]"
                        id="booking-resident"
                        onChange={event => setForm(current => ({ ...current, residentId: event.target.value }))}
                        required
                        value={form.residentId}
                      >
                        <option value="">Choose a resident</option>
                        {availableResidents.map(resident => (
                          <option key={resident.id} value={resident.id}>
                            {resident.firstName} {resident.lastName}{resident.unitNumber ? ` · Unit ${resident.unitNumber}` : ''}
                          </option>
                        ))}
                      </select>
                    </label>
                    <div className="grid gap-4 sm:grid-cols-2">
                      <label className="grid gap-1.5 text-base font-semibold" htmlFor="booking-start">
                        Starts
                        <input
                          className="min-h-11 min-w-0 rounded-xl border border-[#773344] bg-[#F5E9E2] px-3 text-base font-normal text-[#0B0014] focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]"
                          id="booking-start"
                          onChange={event => setForm(current => ({ ...current, startsAt: event.target.value }))}
                          required
                          type="datetime-local"
                          value={form.startsAt}
                        />
                      </label>
                      <label className="grid gap-1.5 text-base font-semibold" htmlFor="booking-end">
                        Ends
                        <input
                          className="min-h-11 min-w-0 rounded-xl border border-[#773344] bg-[#F5E9E2] px-3 text-base font-normal text-[#0B0014] focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]"
                          id="booking-end"
                          onChange={event => setForm(current => ({ ...current, endsAt: event.target.value }))}
                          required
                          type="datetime-local"
                          value={form.endsAt}
                        />
                      </label>
                    </div>
                    <label className="grid gap-1.5 text-base font-semibold" htmlFor="booking-notes">
                      Notes (optional)
                      <textarea
                        className="min-h-24 resize-y rounded-xl border border-[#773344] bg-[#F5E9E2] px-3 py-2 text-base font-normal leading-relaxed text-[#0B0014] focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]"
                        id="booking-notes"
                        maxLength={1000}
                        onChange={event => setForm(current => ({ ...current, notes: event.target.value }))}
                        value={form.notes}
                      />
                    </label>
                    <div className="flex flex-wrap gap-3">
                      <button className="min-h-[52px] rounded-xl bg-[#773344] px-5 py-2 text-base font-semibold text-white hover:bg-[#5E2836] focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014] disabled:opacity-60" disabled={isSaving} type="submit">
                        {isSaving ? 'Saving…' : editingReservationId === null ? 'Create booking' : 'Save booking'}
                      </button>
                      {editingReservationId !== null && (
                        <button
                          className="min-h-11 rounded-xl border border-[#773344] px-4 py-2 text-base font-semibold text-[#0B0014] hover:bg-[#DBA692] focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]"
                          onClick={() => startCreateBooking()}
                          type="button"
                        >
                          Stop editing
                        </button>
                      )}
                    </div>
                  </form>
                )}
              </section>
            </div>
          </div>
        )}
      </main>
    </div>
  )
}

export default AmenityCalendarPage
