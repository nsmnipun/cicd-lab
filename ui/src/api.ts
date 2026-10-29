export interface Note {
  id: string
  title: string
  body: string | null
  createdAt: string
  updatedAt: string
}

export interface NoteInput {
  title: string
  body: string | null
}

export type FieldErrors = Record<string, string[]>

// Thrown for any non-2xx response. `fieldErrors` holds the API's validation messages, keyed by field.
export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: FieldErrors

  constructor(status: number, message: string, fieldErrors: FieldErrors = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    ...init,
    headers: init?.body ? { 'Content-Type': 'application/json' } : undefined,
  })

  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    throw new ApiError(
      response.status,
      problem?.title ?? `Request failed with status ${response.status}`,
      problem?.errors ?? {},
    )
  }

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
}

export const listNotes = () => request<Note[]>('/api/notes')

export const createNote = (input: NoteInput) =>
  request<Note>('/api/notes', { method: 'POST', body: JSON.stringify(input) })

export const updateNote = (id: string, input: NoteInput) =>
  request<Note>(`/api/notes/${id}`, { method: 'PUT', body: JSON.stringify(input) })

export const deleteNote = (id: string) => request<void>(`/api/notes/${id}`, { method: 'DELETE' })
