import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'

type ResidentProfile = {
  id: number
  propertyId: number
  propertyName: string
  firstName: string
  lastName: string
  unitNumber: string | null
  callToNotify: boolean
  allowedGuests: string[]
}

type Amenity = {
  id: number
  name: string
  description: string | null
}

type Reservation = {
  id: number
  amenityTypeId: number
  startsAt: string
  endsAt: string
  notes: string | null
}

type ReservationForm = {
  amenityTypeId: string
  startsAt: string
  endsAt: string
  notes: string
}

function getAuthHeaders() {
  const token = window.localStorage.getItem('accessToken')
  if (!token) {
    window.location.assign('/sign-in/resident')
    throw new Error('Your session has expired. Sign in again to continue.')
  }
  return { Authorization: ['Bearer', token].join(' ') }
}

function getErrorMessage(error: unknown) {
  if (axios.isAxiosError<{ detail?: string; title?: string }>(error)) {
    if (error.response?.status === 401) {
      window.localStorage.removeItem('accessToken')
      window.location.assign('/sign-in/resident')
      return 'Your session has expired. Sign in again.'
    }
    if (error.response?.status === 403) return 'Your account does not have resident access.'
    if (error.response?.status === 409) return 'That amenity is already reserved during this time.'
    if (error.response?.status === 400 && typeof error.response.data === 'string') {
      return error.response.data
    }
    return error.response?.data?.detail ?? error.response?.data?.title ?? error.message
  }
  return error instanceof Error ? error.message : 'Something went wrong. Please try again.'
}

function toLocalInput(value: string) {
  const date = new Date(value)
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000)
  return local.toISOString().slice(0, 16)
}

function getEmptyReservationForm(amenityTypeId = ''): ReservationForm {
  const startsAt = new Date()
  startsAt.setMinutes(0, 0, 0)
  startsAt.setHours(startsAt.getHours() + 1)
  const endsAt = new Date(startsAt)
  endsAt.setHours(endsAt.getHours() + 1)
  return {
    amenityTypeId,
    startsAt: toLocalInput(startsAt.toISOString()),
    endsAt: toLocalInput(endsAt.toISOString()),
    notes: '',
  }
}

function formatDate(value: string) {
  return new Date(value).toLocaleString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  })
}

function ResidentDashboardPage() {
  const [profile, setProfile] = useState<ResidentProfile | null>(null)
  const [amenities, setAmenities] = useState<Amenity[]>([])
  const [reservations, setReservations] = useState<Reservation[]>([])
  const [callToNotify, setCallToNotify] = useState(false)
  const [allowedGuests, setAllowedGuests] = useState('')
  const [reservationForm, setReservationForm] = useState<ReservationForm>(getEmptyReservationForm())
  const [editingReservationId, setEditingReservationId] = useState<number | null>(null)
  const [notice, setNotice] = useState(() =>
    new URLSearchParams(window.location.search).get('welcome') === '1'
      ? 'Your resident account is ready.'
      : ''
  )
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)

  useEffect(() => {
    let isActive = true

    async function loadDashboard() {
      try {
        const headers = getAuthHeaders()
        const [profileResponse, amenitiesResponse, reservationsResponse] = await Promise.all([
          axios.get<ResidentProfile>('/api/resident/profile', { headers }),
          axios.get<Amenity[]>('/api/resident/amenities', { headers }),
          axios.get<Reservation[]>('/api/resident/reservations', { headers }),
        ])
        if (!isActive) return
        setProfile(profileResponse.data)
        setCallToNotify(profileResponse.data.callToNotify)
        setAllowedGuests(profileResponse.data.allowedGuests.join('\n'))
        setAmenities(amenitiesResponse.data)
        setReservations(reservationsResponse.data)
        setReservationForm(getEmptyReservationForm(String(amenitiesResponse.data[0]?.id ?? '')))
      } catch (error) {
        if (isActive) setNotice(getErrorMessage(error))
      } finally {
        if (isActive) setIsLoading(false)
      }
    }

    void loadDashboard()
    return () => {
      isActive = false
    }
  }, [])

  const amenityNames = new Map(amenities.map(amenity => [amenity.id, amenity.name]))
  const propertyLabel = profile
    ? [profile.propertyName, profile.unitNumber ? `Unit ${profile.unitNumber}` : null].filter(Boolean).join(' · ')
    : 'Resident workspace'
  const heading = profile ? `Welcome, ${profile.firstName}` : 'Resident dashboard'

  async function savePreferences(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setNotice('')
    const guestNames = allowedGuests.split(/\r?\n/).map(name => name.trim()).filter(Boolean)

    try {
      const { data } = await axios.put<ResidentProfile>('/api/resident/preferences', {
        callToNotify,
        allowedGuests: guestNames,
      }, { headers: getAuthHeaders() })
      setProfile(data)
      setAllowedGuests(data.allowedGuests.join('\n'))
      setNotice('Your preferences are saved.')
    } catch (error) {
      setNotice(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function saveReservation(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const startsAt = new Date(reservationForm.startsAt)
    const endsAt = new Date(reservationForm.endsAt)
    if (Number.isNaN(startsAt.getTime()) || Number.isNaN(endsAt.getTime()) || endsAt <= startsAt) {
      setNotice('Choose an end time after the start time.')
      return
    }

    setIsSaving(true)
    setNotice('')
    const payload = {
      amenityTypeId: Number(reservationForm.amenityTypeId),
      startsAt: startsAt.toISOString(),
      endsAt: endsAt.toISOString(),
      notes: reservationForm.notes.trim() || null,
    }

    try {
      if (editingReservationId === null) {
        const { data } = await axios.post<Reservation>('/api/resident/reservations', payload, {
          headers: getAuthHeaders(),
        })
        setReservations(current => [...current, data].sort(
          (first, second) => Date.parse(first.startsAt) - Date.parse(second.startsAt),
        ))
        setNotice('Your amenity reservation is created.')
      } else {
        const { data } = await axios.put<Reservation>(
          `/api/resident/reservations/${editingReservationId}`,
          payload,
          {
            headers: getAuthHeaders(),
          },
        )
        setReservations(current => current.map(reservation =>
          reservation.id === data.id ? data : reservation,
        ).sort(
          (first, second) => Date.parse(first.startsAt) - Date.parse(second.startsAt),
        ))
        setNotice('Your amenity reservation is updated.')
      }
      setEditingReservationId(null)
      setReservationForm(getEmptyReservationForm(String(amenities[0]?.id ?? '')))
    } catch (error) {
      setNotice(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  function editReservation(reservation: Reservation) {
    setEditingReservationId(reservation.id)
    setReservationForm({
      amenityTypeId: String(reservation.amenityTypeId),
      startsAt: toLocalInput(reservation.startsAt),
      endsAt: toLocalInput(reservation.endsAt),
      notes: reservation.notes ?? '',
    })
    setNotice('')
  }

  async function deleteReservation(reservation: Reservation) {
    if (!window.confirm('Cancel this amenity reservation?')) return
    setNotice('')
    try {
      await axios.delete(`/api/resident/reservations/${reservation.id}`, {
        headers: getAuthHeaders(),
      })
      setReservations(current => current.filter(item => item.id !== reservation.id))
      if (editingReservationId === reservation.id) {
        setEditingReservationId(null)
        setReservationForm(getEmptyReservationForm(String(amenities[0]?.id ?? '')))
      }
      setNotice('Your amenity reservation is canceled.')
    } catch (error) {
      setNotice(getErrorMessage(error))
    }
  }

  return (
    <div className="min-h-dvh bg-canvas font-sans text-ink antialiased">
      <header className="border-b border-brand bg-canvas">
        <div className="mx-auto flex min-h-16 max-w-[72rem] items-center justify-between gap-4 px-5">
          <a href="/" className="font-display text-2xl font-bold text-brand no-underline">Shiftr</a>
          <nav aria-label="Resident navigation" className="flex items-center gap-2 text-base font-semibold">
            <a className="min-h-11 rounded-xl px-3 py-2 text-brand underline-offset-4 hover:underline focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-ink" href="/">
              Home
            </a>
            <button
              className="min-h-11 rounded-xl px-3 py-2 text-brand hover:bg-blush focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-ink"
              onClick={() => {
                window.localStorage.removeItem('accessToken')
                window.location.assign('/sign-in/resident')
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
          <p className="mb-1 text-sm font-semibold text-brand">
            {propertyLabel}
          </p>
          <h1 className="font-display text-4xl font-semibold leading-tight text-brand">
            {heading}
          </h1>
          <p className="mt-2 text-lg leading-relaxed">Manage your preferences and amenity reservations.</p>
        </div>

        {notice && <p className="mb-6 rounded-xl border-2 border-brand bg-blush px-4 py-3 text-base" role="status">{notice}</p>}
        {isLoading ? (
          <p className="py-10 text-base" role="status">Loading your resident workspace…</p>
        ) : !profile ? (
          <p className="rounded-xl border-2 border-brand bg-blush px-4 py-3 text-base">
            Your resident profile could not be loaded. Sign out and try again, or contact your property team.
          </p>
        ) : (
          <div className="grid items-start gap-6 lg:grid-cols-[0.85fr_1.15fr]">
            <section aria-labelledby="preferences-title" className="rounded-3xl border-2 border-brand bg-blush p-6">
              <h2 id="preferences-title" className="font-display text-2xl font-semibold text-brand">Your preferences</h2>
              <p className="mt-2 text-base leading-relaxed">Choose how your building team can reach you and keep your guest list current.</p>
              <form className="mt-5 grid gap-5" onSubmit={savePreferences}>
                <label className="flex min-h-12 items-start gap-3 text-base leading-snug">
                  <input
                    checked={callToNotify}
                    className="mt-1 size-5 accent-brand focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-ink"
                    onChange={event => setCallToNotify(event.target.checked)}
                    type="checkbox"
                  />
                  <span>Allow building staff to call me when needed.</span>
                </label>
                <label className="grid gap-2 text-base font-semibold">
                  Allowed guests
                  <textarea
                    className="min-h-32 w-full rounded-xl border-2 border-brand bg-canvas px-4 py-3 text-base font-normal leading-relaxed text-ink focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-ink"
                    onChange={event => setAllowedGuests(event.target.value)}
                    placeholder="Enter one guest name per line"
                    value={allowedGuests}
                  />
                  <span className="text-sm font-normal">Enter one name per line.</span>
                </label>
                <button
                  className="inline-flex min-h-[3.25rem] items-center justify-center rounded-xl border-2 border-brand bg-brand px-6 py-3 text-base font-semibold text-white hover:bg-brand-dark focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-ink disabled:cursor-wait disabled:opacity-60"
                  disabled={isSaving}
                  type="submit"
                >
                  {isSaving ? 'Saving…' : 'Save preferences'}
                </button>
              </form>
            </section>

            <section aria-labelledby="reservations-title" className="rounded-3xl border-2 border-brand bg-blush p-6">
              <h2 id="reservations-title" className="font-display text-2xl font-semibold text-brand">Your amenity reservations</h2>
              <p className="mt-2 text-base leading-relaxed">Book a shared space or update a reservation you created.</p>
              {reservations.length === 0 ? (
                <p className="mt-5 rounded-xl border-2 border-brand bg-canvas px-4 py-3 text-base">You do not have any amenity reservations yet.</p>
              ) : (
                <ul className="mt-5 grid gap-3 p-0">
                  {reservations.map(reservation => (
                    <li key={reservation.id} className="grid gap-3 rounded-2xl border-2 border-brand bg-canvas p-4 sm:grid-cols-[1fr_auto] sm:items-center">
                      <div>
                        <h3 className="font-display text-xl font-semibold text-brand">{amenityNames.get(reservation.amenityTypeId) ?? 'Amenity'}</h3>
                        <p className="text-base">{formatDate(reservation.startsAt)} – {formatDate(reservation.endsAt)}</p>
                        {reservation.notes && <p className="mt-1 text-base">{reservation.notes}</p>}
                      </div>
                      <div className="flex flex-wrap gap-2">
                        <button className="min-h-11 rounded-xl border-2 border-brand px-4 py-2 text-sm font-semibold text-brand hover:bg-blush focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-ink" onClick={() => editReservation(reservation)} type="button">
                          Edit
                        </button>
                        <button className="min-h-11 rounded-xl border-2 border-brand px-4 py-2 text-sm font-semibold text-brand hover:bg-blush focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-ink" onClick={() => void deleteReservation(reservation)} type="button">
                          Cancel
                        </button>
                      </div>
                    </li>
                  ))}
                </ul>
              )}

              <form className="mt-6 grid gap-4 border-t-2 border-brand pt-5" onSubmit={saveReservation}>
                <h3 className="font-display text-xl font-semibold text-brand">
                  {editingReservationId === null ? 'Book an amenity' : 'Update your reservation'}
                </h3>
                <label className="grid gap-2 text-base font-semibold">
                  Amenity
                  <select
                    className="min-h-12 w-full rounded-xl border-2 border-brand bg-canvas px-4 text-base font-normal focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-ink"
                    onChange={event => setReservationForm(current => ({ ...current, amenityTypeId: event.target.value }))}
                    required
                    value={reservationForm.amenityTypeId}
                  >
                    {amenities.length === 0 && <option value="">No amenities are available</option>}
                    {amenities.map(amenity => <option key={amenity.id} value={amenity.id}>{amenity.name}</option>)}
                  </select>
                </label>
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <label className="grid gap-2 text-base font-semibold">
                    Starts
                    <input className="min-h-12 w-full min-w-0 rounded-xl border-2 border-brand bg-canvas px-3 text-base font-normal focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-ink" onChange={event => setReservationForm(current => ({ ...current, startsAt: event.target.value }))} required type="datetime-local" value={reservationForm.startsAt} />
                  </label>
                  <label className="grid gap-2 text-base font-semibold">
                    Ends
                    <input className="min-h-12 w-full min-w-0 rounded-xl border-2 border-brand bg-canvas px-3 text-base font-normal focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-ink" onChange={event => setReservationForm(current => ({ ...current, endsAt: event.target.value }))} required type="datetime-local" value={reservationForm.endsAt} />
                  </label>
                </div>
                <label className="grid gap-2 text-base font-semibold">
                  Notes <span className="font-normal">(optional)</span>
                  <textarea className="min-h-20 w-full rounded-xl border-2 border-brand bg-canvas px-4 py-3 text-base font-normal focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-ink" maxLength={1000} onChange={event => setReservationForm(current => ({ ...current, notes: event.target.value }))} value={reservationForm.notes} />
                </label>
                <div className="flex flex-wrap gap-3">
                  <button
                    className="inline-flex min-h-[3.25rem] items-center justify-center rounded-xl border-2 border-brand bg-brand px-6 py-3 text-base font-semibold text-white hover:bg-brand-dark focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-ink disabled:cursor-wait disabled:opacity-60"
                    disabled={isSaving || amenities.length === 0}
                    type="submit"
                  >
                    {isSaving ? 'Saving…' : editingReservationId === null ? 'Create reservation' : 'Save reservation'}
                  </button>
                  {editingReservationId !== null && (
                    <button className="min-h-[3.25rem] rounded-xl px-4 py-2 text-base font-semibold text-brand underline underline-offset-4 focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-ink" onClick={() => {
                      setEditingReservationId(null)
                      setReservationForm(getEmptyReservationForm(String(amenities[0]?.id ?? '')))
                    }} type="button">
                      Stop editing
                    </button>
                  )}
                </div>
              </form>
            </section>
          </div>
        )}
      </main>
    </div>
  )
}

export default ResidentDashboardPage
