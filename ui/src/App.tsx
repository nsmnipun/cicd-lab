import { useEffect, useState } from 'react'
import { createNote, deleteNote, listNotes, updateNote, type Note, type NoteInput } from './api'
import { NoteForm } from './NoteForm'
import { NoteItem } from './NoteItem'

const appVersion = import.meta.env.VITE_APP_VERSION || 'dev'

export default function App() {
  const [notes, setNotes] = useState<Note[]>([])
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)

  const [loadAttempt, setLoadAttempt] = useState(0)

  useEffect(() => {
    let ignore = false
    listNotes()
      .then((loaded) => {
        if (ignore) return
        setNotes(loaded)
        setLoadError(null)
      })
      .catch((err: unknown) => {
        if (ignore) return
        setLoadError(err instanceof Error ? err.message : 'Could not load notes')
      })
      .finally(() => {
        if (!ignore) setLoading(false)
      })
    return () => {
      ignore = true
    }
  }, [loadAttempt])

  function retry() {
    setLoading(true)
    setLoadAttempt((n) => n + 1)
  }

  async function handleCreate(input: NoteInput) {
    const created = await createNote(input)
    setNotes((current) => [created, ...current])
  }

  async function handleUpdate(id: string, input: NoteInput) {
    const updated = await updateNote(id, input)
    setNotes((current) => current.map((n) => (n.id === id ? updated : n)))
  }

  async function handleDelete(id: string) {
    await deleteNote(id)
    setNotes((current) => current.filter((n) => n.id !== id))
  }

  return (
    <div className="app">
      <header>
        <h1>Notes</h1>
      </header>

      <main>
        <section aria-labelledby="new-note-heading">
          <h2 id="new-note-heading">New note</h2>
          <NoteForm submitLabel="Add note" onSubmit={handleCreate} />
        </section>

        <section aria-labelledby="notes-heading">
          <h2 id="notes-heading">Your notes</h2>
          {loading ? (
            <p role="status">Loading notes…</p>
          ) : loadError ? (
            <div role="alert" className="error">
              <p>Could not load notes: {loadError}</p>
              <button type="button" onClick={retry}>
                Try again
              </button>
            </div>
          ) : notes.length === 0 ? (
            <p className="empty">No notes yet.</p>
          ) : (
            <ul className="notes">
              {notes.map((note) => (
                <NoteItem key={note.id} note={note} onUpdate={handleUpdate} onDelete={handleDelete} />
              ))}
            </ul>
          )}
        </section>
      </main>

      <footer>Version {appVersion}</footer>
    </div>
  )
}
