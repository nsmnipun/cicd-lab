import { useId, useState, type FormEvent } from 'react'
import { ApiError, type FieldErrors, type NoteInput } from './api'

interface NoteFormProps {
  initial?: NoteInput
  submitLabel: string
  onSubmit: (input: NoteInput) => Promise<void>
  onCancel?: () => void
}

export function NoteForm({ initial, submitLabel, onSubmit, onCancel }: NoteFormProps) {
  const id = useId()
  const [title, setTitle] = useState(initial?.title ?? '')
  const [body, setBody] = useState(initial?.body ?? '')
  const [saving, setSaving] = useState(false)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSaving(true)
    setFieldErrors({})
    setError(null)
    try {
      await onSubmit({ title, body: body === '' ? null : body })
      if (!initial) {
        setTitle('')
        setBody('')
      }
    } catch (err) {
      if (err instanceof ApiError && Object.keys(err.fieldErrors).length > 0) {
        setFieldErrors(err.fieldErrors)
      } else {
        setError(err instanceof Error ? err.message : 'Something went wrong')
      }
    } finally {
      setSaving(false)
    }
  }

  const titleErrors = fieldErrors.title ?? []
  const bodyErrors = fieldErrors.body ?? []

  return (
    <form className="note-form" onSubmit={handleSubmit} noValidate>
      <label htmlFor={`${id}-title`}>Title</label>
      <input
        id={`${id}-title`}
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        aria-invalid={titleErrors.length > 0}
        aria-describedby={titleErrors.length > 0 ? `${id}-title-error` : undefined}
      />
      {titleErrors.length > 0 && (
        <p id={`${id}-title-error`} className="field-error">
          {titleErrors.join(' ')}
        </p>
      )}

      <label htmlFor={`${id}-body`}>Body</label>
      <textarea
        id={`${id}-body`}
        value={body}
        rows={4}
        onChange={(e) => setBody(e.target.value)}
        aria-invalid={bodyErrors.length > 0}
        aria-describedby={bodyErrors.length > 0 ? `${id}-body-error` : undefined}
      />
      {bodyErrors.length > 0 && (
        <p id={`${id}-body-error`} className="field-error">
          {bodyErrors.join(' ')}
        </p>
      )}

      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}

      <div className="actions">
        <button type="submit" disabled={saving}>
          {saving ? 'Saving…' : submitLabel}
        </button>
        {onCancel && (
          <button type="button" className="secondary" onClick={onCancel} disabled={saving}>
            Cancel
          </button>
        )}
      </div>
    </form>
  )
}
