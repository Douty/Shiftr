import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'

type ShiftNote = {
  id: number
  title: string
  content: string
  authorName: string
  createdAt: string
  updatedAt: string
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

function getErrorMessage(error: unknown) {
  if (axios.isAxiosError<{ detail?: string; title?: string }>(error)) {
    if (error.response?.status === 401) {
      window.localStorage.removeItem('accessToken')
      window.location.assign('/sign-in/employee')
      return 'Your session has expired. Sign in again.'
    }
    return error.response?.data?.detail ?? error.response?.data?.title ?? error.message
  }
  return error instanceof Error ? error.message : 'Something went wrong. Please try again.'
}

function getDailyLogTitle(date: Date) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `Daily activities — ${year}-${month}-${day}`
}

function formatDate(value: string) {
  return new Date(value).toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  })
}

function DailyActivityLogPage() {
  const [propertyId, setPropertyId] = useState<number | null>(null)
  const [notes, setNotes] = useState<ShiftNote[]>([])
  const [logTitle, setLogTitle] = useState('')
  const [dailyLogId, setDailyLogId] = useState<number | null>(null)
  const [content, setContent] = useState('')
  const [notice, setNotice] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)

  useEffect(() => {
    let isActive = true

    async function loadDailyLogs() {
      try {
        const headers = getAuthHeaders()
        const [assignmentResponse, notesResponse] = await Promise.all([
          axios.get<EmployeeAssignment>('/api/shift-notes/assignment', { headers }),
          axios.get<ShiftNote[]>('/api/shift-notes', { headers }),
        ])
        if (!isActive) return
        const title = getDailyLogTitle(new Date())
        const todayLog = notesResponse.data.find(note => note.title === title)
        setPropertyId(assignmentResponse.data.propertyId)
        setNotes(notesResponse.data)
        setLogTitle(title)
        setDailyLogId(todayLog?.id ?? null)
        setContent(todayLog?.content ?? '')
      } catch (error) {
        if (isActive) setNotice(getErrorMessage(error))
      } finally {
        if (isActive) setIsLoading(false)
      }
    }

    void loadDailyLogs()
    return () => {
      isActive = false
    }
  }, [])

  async function saveDailyLog(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!logTitle || !content.trim()) return

    setIsSaving(true)
    setNotice('')
    try {
      const headers = getAuthHeaders()
      const payload = { title: logTitle, content: content.trim() }
      if (dailyLogId === null) {
        const { data } = await axios.post<ShiftNote>('/api/shift-notes', payload, { headers })
        setDailyLogId(data.id)
        setNotes(current => [data, ...current])
      } else {
        const { data } = await axios.put<ShiftNote>(`/api/shift-notes/${dailyLogId}`, payload, { headers })
        setNotes(current => [data, ...current.filter(note => note.id !== data.id)]
          .sort((first, second) => Date.parse(second.updatedAt) - Date.parse(first.updatedAt)))
      }
      setNotice('Daily activity log saved.')
    } catch (error) {
      setNotice(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <div className="min-h-dvh bg-[#F5E9E2] font-['Inter',system-ui,sans-serif] text-[#0B0014] antialiased">
      <header className="border-b border-[#773344] bg-[#F5E9E2]">
        <div className="mx-auto flex min-h-16 max-w-[72rem] items-center justify-between gap-4 px-5">
          <a href="/" className="font-['Fraunces',Georgia,serif] text-2xl font-bold text-[#773344] no-underline">Shiftr</a>
          <nav aria-label="Employee navigation" className="flex items-center gap-2 text-base font-semibold">
            <a className="rounded-xl px-3 py-2 text-[#773344] underline-offset-4 hover:underline focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]" href="/employee/dashboard">Dashboard</a>
            <a className="hidden rounded-xl px-3 py-2 text-[#773344] underline-offset-4 hover:underline focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014] sm:inline" href="/employee/amenities">Amenity calendar</a>
            <a className="hidden rounded-xl px-3 py-2 text-[#773344] underline-offset-4 hover:underline focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014] sm:inline" href="/employee/residents">Residents</a>
          </nav>
        </div>
      </header>
      <main className="mx-auto w-full max-w-[72rem] px-5 py-8 sm:py-10">
        <a className="font-semibold text-[#773344] underline underline-offset-4" href="/employee/dashboard">Employee dashboard</a>
        <div className="mb-8 mt-5">
          <p className="mb-1 text-sm font-semibold text-[#773344]">Daily record{propertyId === null ? '' : ` · Property #${propertyId}`}</p>
          <h1 className="font-['Fraunces',Georgia,serif] text-4xl font-semibold leading-tight text-[#773344]">Daily activity log</h1>
          <p className="mt-2 text-lg leading-relaxed">Record what happened across the property during your shift.</p>
        </div>
        {notice && <p className="mb-6 rounded-xl border-2 border-[#773344] bg-[#E3B5A4] px-4 py-3 text-base" role="status">{notice}</p>}
        {isLoading ? <p className="py-8 text-base" role="status">Loading activity logs…</p> : (
          <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1.2fr)_minmax(18rem,0.8fr)]">
            <section aria-labelledby="daily-log-editor-title" className="rounded-3xl border-2 border-[#773344] bg-[#E3B5A4] p-5 sm:p-7">
              <p className="m-0 text-sm font-semibold">{logTitle.replace('Daily activities — ', '')}</p>
              <h2 id="daily-log-editor-title" className="mb-4 mt-1 font-['Fraunces',Georgia,serif] text-2xl font-semibold">Log today’s activities</h2>
              <form className="grid gap-4" onSubmit={event => void saveDailyLog(event)}>
                <label className="grid gap-2 text-base font-semibold" htmlFor="daily-activity-content">
                  Activity details
                  <textarea
                    className="min-h-[22rem] resize-y rounded-xl border border-[#773344] bg-[#F5E9E2] px-4 py-3 text-base font-normal leading-relaxed text-[#0B0014] outline-none focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]"
                    id="daily-activity-content"
                    maxLength={10000}
                    onChange={event => setContent(event.target.value)}
                    placeholder="Record tasks completed, resident requests, building rounds, incidents, maintenance follow-ups, and details for the next shift."
                    required
                    rows={14}
                    value={content}
                  />
                </label>
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <p className="m-0 text-sm">{content.length}/10000 characters</p>
                  <button className="min-h-[52px] rounded-xl bg-[#773344] px-6 py-3 text-base font-semibold text-white hover:bg-[#5E2836] focus-visible:outline-solid focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014] disabled:cursor-wait disabled:opacity-60" disabled={isSaving} type="submit">
                    {isSaving ? 'Saving…' : dailyLogId === null ? 'Save daily log' : 'Update daily log'}
                  </button>
                </div>
              </form>
            </section>
            <section aria-labelledby="recent-logs-title" className="rounded-3xl border-2 border-[#773344] bg-[#E3B5A4] p-5 sm:p-7">
              <h2 id="recent-logs-title" className="m-0 font-['Fraunces',Georgia,serif] text-2xl font-semibold">Recent activity logs</h2>
              {notes.length === 0 ? (
                <p className="mt-4 border-y border-[#773344] py-4 text-base">Saved daily logs will appear here.</p>
              ) : (
                <ul className="mt-4 max-h-[40rem] list-none divide-y divide-[#773344] overflow-y-auto border-y border-[#773344] p-0">
                  {notes.map(note => (
                    <li className="py-4" key={note.id}>
                      <h3 className="m-0 text-base font-bold">{note.title}</h3>
                      <p className="mb-2 mt-2 whitespace-pre-wrap text-base leading-relaxed">{note.content}</p>
                      <p className="m-0 text-sm">{note.authorName} · {formatDate(note.updatedAt)}</p>
                    </li>
                  ))}
                </ul>
              )}
            </section>
          </div>
        )}
      </main>
    </div>
  )
}

export default DailyActivityLogPage
