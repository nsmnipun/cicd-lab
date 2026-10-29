import { useState } from 'react'
import type { Note, NoteInput } from './api'
import { NoteForm } from './NoteForm'

interface NoteItemProps {
  note: Note
  onUpdate: (id: string, input: NoteInput) => Promise<void>
  onDelete: (id: string) => Promise<void>
}

export function NoteItem({ note, onUpdate, onDelete }: NoteItemProps) {
  const [editing, setEditing] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleDelete() {
    setDeleting(true)
    setError(null)
    try {
      await onDelete(note.id)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not delete note')
      setDeleting(false)
    }
  }

  if (editing) {
    return (
      <li className="note" aria-label={note.title}>
        <NoteForm
          initial={{ title: note.title, body: note.body }}
          submitLabel="Save"
          onSubmit={async (input) => {
            await onUpdate(note.id, input)
            setEditing(false)
          }}
          onCancel={() => setEditing(false)}
        />
      </li>
    )
  }

  return (
    <li className="note" aria-label={note.title}>
      <h3>{note.title}</h3>
      {note.body && <p className="note-body">{note.body}</p>}
      <p className="meta">Updated {new Date(note.updatedAt).toLocaleString()}</p>
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}
      <div className="actions">
        <button type="button" className="secondary" onClick={() => setEditing(true)} disabled={deleting}>
          Edit
        </button>
        <button type="button" className="danger" onClick={handleDelete} disabled={deleting}>
          {deleting ? 'Deleting…' : 'Delete'}
        </button>
      </div>
    </li>
  )
}
