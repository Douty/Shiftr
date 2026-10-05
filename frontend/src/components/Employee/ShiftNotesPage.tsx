import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'

type ShiftNote = {
  id: number
  title: string
  content: string
  authorEmployeeId: number
  authorName: string
  createdAt: string
  updatedAt: string
}

type SaveShiftNote = Pick<ShiftNote, 'title' | 'content'>

function getAuthHeaders() {
  const token = window.localStorage.getItem('accessToken')
  if (!token) {
    window.location.assign('/sign-in/employee')
    throw new Error('Your session has expired. Sign in again to continue.')
  }
  return { Authorization: `Bearer ${token}` }
}

function ShiftNotesPage() {
  const [notes, setNotes] = useState<ShiftNote[]>([])
  const [selectedNoteId, setSelectedNoteId] = useState<number | null>(null)
  const [title, setTitle] = useState('')
  const [content, setContent] = useState('')
  const [search, setSearch] = useState('')
  const [notice, setNotice] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)

  useEffect(() => {
    let isActive = true

    async function loadNotes() {
      try {
        const { data } = await axios.get<ShiftNote[]>('/api/shift-notes', {
          headers: getAuthHeaders(),
        })
        if (isActive) setNotes(data)
      } catch (error) {
        if (isActive) setNotice(getErrorMessage(error))
      } finally {
        if (isActive) setIsLoading(false)
      }
    }

    void loadNotes()
    return () => {
      isActive = false
    }
  }, [])

  const selectedNote = notes.find(note => note.id === selectedNoteId)
  const visibleNotes = notes.filter(note =>
    `${note.title} ${note.content} ${note.authorName}`.toLowerCase().includes(search.toLowerCase()),
  )

  function startNewNote() {
    setSelectedNoteId(null)
    setTitle('')
    setContent('')
    setNotice('')
  }

  function startEditing(note: ShiftNote) {
    setSelectedNoteId(note.id)
    setTitle(note.title)
    setContent(note.content)
    setNotice('')
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setNotice('')
    const payload: SaveShiftNote = { title: title.trim(), content: content.trim() }

    try {
      const headers = getAuthHeaders()
      if (selectedNoteId === null) {
        const { data } = await axios.post<ShiftNote>('/api/shift-notes', payload, { headers })
        setNotes(current => [data, ...current])
        setSelectedNoteId(data.id)
        setNotice('Note saved.')
      } else {
        const { data } = await axios.put<ShiftNote>(`/api/shift-notes/${selectedNoteId}`, payload, { headers })
        setNotes(current => current.map(note => note.id === data.id ? data : note)
          .sort((first, second) => Date.parse(second.updatedAt) - Date.parse(first.updatedAt)))
        setNotice('Changes saved.')
      }
    } catch (error) {
      setNotice(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function deleteNote(note: ShiftNote) {
    if (!window.confirm(`Delete "${note.title}"? This cannot be undone.`)) return

    try {
      await axios.delete(`/api/shift-notes/${note.id}`, { headers: getAuthHeaders() })
      setNotes(current => current.filter(item => item.id !== note.id))
      if (selectedNoteId === note.id) startNewNote()
      setNotice('Note deleted.')
    } catch (error) {
      setNotice(getErrorMessage(error))
    }
  }

  return (
    <div className="min-h-dvh bg-[#F5E9E2] font-['Inter',system-ui,sans-serif] text-[#0B0014] antialiased">
      <header className="border-b border-[#D9C2B8] bg-[#FFF9F5]">
        <div className="mx-auto flex min-h-16 max-w-7xl items-center justify-between gap-4 px-5">
          <a href="/" className="font-['Fraunces',Georgia,serif] text-2xl font-bold text-[#773344] no-underline">Shiftr</a>
          <div className="flex items-center gap-4 text-sm font-semibold">
            <span className="hidden text-[#654E4A] sm:inline">Employee workspace</span>
            <a
              className="rounded-md px-3 py-2 text-[#773344] underline-offset-4 hover:underline focus-visible:outline-solid focus-visible:outline-2 focus-visible:outline-[#773344]"
              href="/employee/dashboard"
            >
              Employee dashboard
            </a>
            <button
              type="button"
              className="rounded-md px-3 py-2 text-[#773344] hover:bg-[#F5E9E2] focus-visible:outline-solid focus-visible:outline-2 focus-visible:outline-[#773344]"
              onClick={() => {
                window.localStorage.removeItem('accessToken')
                window.location.assign('/sign-in/employee')
              }}
            >
              Sign out
            </button>
          </div>
        </div>
      </header>

      <main className="mx-auto w-full max-w-7xl px-5 py-8 sm:py-10">
        <div className="mb-7 flex flex-wrap items-end justify-between gap-4">
          <div>
            <p className="mb-1 text-xs font-bold uppercase tracking-[0.12em] text-[#773344]">Team workspace</p>
            <h1 className="font-['Fraunces',Georgia,serif] text-4xl font-semibold leading-tight text-[#351E24]">Shift handoff</h1>
            <p className="mt-2 text-base text-[#654E4A]">Shared notes for the next person at the desk.</p>
          </div>
          <button
            type="button"
            onClick={startNewNote}
            className="min-h-11 rounded-md bg-[#773344] px-4 py-2 text-sm font-bold text-white transition-colors hover:bg-[#5E2836] focus-visible:outline-solid focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#0B0014]"
          >
            New note
          </button>
        </div>

        {notice && (
          <p role="status" className="mb-5 rounded-md border border-[#B98482] bg-[#FFF9F5] px-4 py-3 text-sm">
            {notice}
          </p>
        )}

        <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,0.9fr)_minmax(24rem,1.1fr)]">
          <section aria-labelledby="notes-title" className="min-w-0">
            <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
              <h2 id="notes-title" className="text-lg font-bold">Recent notes <span className="ml-1 text-sm font-medium text-[#654E4A]">{notes.length}</span></h2>
              <label className="sr-only" htmlFor="note-search">Search notes</label>
              <input
                id="note-search"
                className="min-h-10 w-full max-w-64 rounded-md border border-[#B98482] bg-[#FFF9F5] px-3 text-sm outline-none focus:border-[#773344] focus:ring-2 focus:ring-[#773344]/20"
                onChange={event => setSearch(event.target.value)}
                placeholder="Search notes"
                type="search"
                value={search}
              />
            </div>

            {isLoading ? (
              <p className="border-y border-[#D9C2B8] py-6 text-sm text-[#654E4A]">Loading notes…</p>
            ) : visibleNotes.length === 0 ? (
              <div className="border-y border-[#D9C2B8] py-8">
                <p className="font-semibold">{search ? 'No matching notes' : 'No handoff notes yet'}</p>
                <p className="mt-1 text-sm text-[#654E4A]">{search ? 'Try a different search.' : 'Add the first update for your team.'}</p>
              </div>
            ) : (
              <ul className="m-0 list-none divide-y divide-[#D9C2B8] border-y border-[#D9C2B8] p-0">
                {visibleNotes.map(note => (
                  <li key={note.id}>
                    <div className={`py-4 ${selectedNoteId === note.id ? 'border-l-4 border-[#D44D5C] pl-3' : 'pl-4'}`}>
                      <button
                        className="block w-full text-left focus-visible:outline-solid focus-visible:outline-2 focus-visible:outline-[#773344]"
                        onClick={() => startEditing(note)}
                        type="button"
                      >
                        <span className="block truncate font-bold">{note.title}</span>
                        <span className="mt-1 block overflow-hidden text-ellipsis whitespace-nowrap text-sm text-[#654E4A]">{note.content}</span>
                      </button>
                      <div className="mt-2 flex flex-wrap items-center justify-between gap-2 text-xs text-[#654E4A]">
                        <span>{note.authorName} · {formatDate(note.updatedAt)}</span>
                        <span className="flex gap-3">
                          <button type="button" className="font-semibold text-[#773344] underline underline-offset-2" onClick={() => startEditing(note)}>Edit</button>
                          <button type="button" className="font-semibold text-[#773344] underline underline-offset-2" onClick={() => void deleteNote(note)}>Delete</button>
                        </span>
                      </div>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section aria-labelledby="editor-title" className="border-t border-[#D9C2B8] pt-5 lg:border-l lg:border-t-0 lg:pl-6 lg:pt-0">
            <div className="mb-4">
              <p className="text-xs font-bold uppercase tracking-widest text-[#773344]">{selectedNote ? 'Update shared note' : 'New handoff'}</p>
              <h2 id="editor-title" className="mt-1 text-xl font-bold">{selectedNote ? 'Edit note' : 'Write a note'}</h2>
            </div>
            <form className="grid gap-4" onSubmit={event => void handleSubmit(event)}>
              <label className="grid gap-1.5 text-sm font-semibold" htmlFor="note-title">
                Title
                <input
                  autoComplete="off"
                  className="min-h-11 rounded-md border border-[#B98482] bg-[#FFF9F5] px-3 text-base font-normal outline-none focus:border-[#773344] focus:ring-2 focus:ring-[#773344]/20"
                  id="note-title"
                  maxLength={120}
                  onChange={event => setTitle(event.target.value)}
                  required
                  value={title}
                />
              </label>
              <label className="grid gap-1.5 text-sm font-semibold" htmlFor="note-content">
                Handoff details
                <textarea
                  className="min-h-56 resize-y rounded-md border border-[#B98482] bg-[#FFF9F5] px-3 py-2 text-base font-normal leading-relaxed outline-none focus:border-[#773344] focus:ring-2 focus:ring-[#773344]/20"
                  id="note-content"
                  maxLength={10000}
                  onChange={event => setContent(event.target.value)}
                  placeholder="What happened, what is still open, and what the next shift should know"
                  required
                  value={content}
                />
              </label>
              {selectedNote && (
                <p className="text-xs text-[#654E4A]">Created by {selectedNote.authorName} · Updated {formatDate(selectedNote.updatedAt)}</p>
              )}
              <div className="flex flex-wrap gap-3">
                <button
                  className="min-h-11 rounded-md bg-[#773344] px-5 py-2 text-sm font-bold text-white hover:bg-[#5E2836] focus-visible:outline-solid focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#0B0014] disabled:cursor-wait disabled:opacity-60"
                  disabled={isSaving}
                  type="submit"
                >
                  {isSaving ? 'Saving…' : selectedNoteId === null ? 'Save note' : 'Save changes'}
                </button>
                {selectedNote && (
                  <button
                    className="min-h-11 rounded-md border border-[#B98482] px-4 py-2 text-sm font-semibold text-[#773344] hover:bg-[#FFF9F5] focus-visible:outline-solid focus-visible:outline-2 focus-visible:outline-[#773344]"
                    onClick={startNewNote}
                    type="button"
                  >
                    Cancel edit
                  </button>
                )}
              </div>
            </form>
          </section>
        </div>
      </main>
    </div>
  )
}

function getErrorMessage(error: unknown) {
  if (axios.isAxiosError<{ detail?: string; title?: string }>(error)) {
    return error.response?.data?.detail ?? error.response?.data?.title ?? error.message
  }
  return error instanceof Error ? error.message : 'Unable to connect to the server.'
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

export default ShiftNotesPage