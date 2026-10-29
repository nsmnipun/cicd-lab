import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import type { Note } from './api'

function note(overrides: Partial<Note> = {}): Note {
  return {
    id: crypto.randomUUID(),
    title: 'A note',
    body: null,
    createdAt: '2026-10-28T09:00:00Z',
    updatedAt: '2026-10-28T09:00:00Z',
    ...overrides,
  }
}

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

const fetchMock = vi.fn<typeof fetch>()

beforeEach(() => {
  vi.stubGlobal('fetch', fetchMock)
})

// Queues one response per call, in order.
function respondWith(...responses: Response[]) {
  for (const response of responses) {
    fetchMock.mockResolvedValueOnce(response)
  }
}

function lastRequest() {
  const [url, init] = fetchMock.mock.lastCall!
  return { url, method: init?.method ?? 'GET', body: init?.body ? JSON.parse(String(init.body)) : undefined }
}

describe('listing notes', () => {
  it('shows a loading state, then the notes', async () => {
    respondWith(json([note({ title: 'Groceries', body: 'Milk' }), note({ title: 'Ideas' })]))

    render(<App />)

    expect(screen.getByRole('status')).toHaveTextContent('Loading notes')
    expect(await screen.findByRole('listitem', { name: 'Groceries' })).toHaveTextContent('Milk')
    expect(screen.getByRole('listitem', { name: 'Ideas' })).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/notes', expect.anything())
  })

  it('shows an empty state when there are no notes', async () => {
    respondWith(json([]))

    render(<App />)

    expect(await screen.findByText('No notes yet.')).toBeInTheDocument()
  })

  it('shows an error and retries', async () => {
    respondWith(json({ title: 'Server error' }, 500), json([note({ title: 'Recovered' })]))
    const user = userEvent.setup()

    render(<App />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load notes: Server error')
    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('listitem', { name: 'Recovered' })).toBeInTheDocument()
  })

  it('shows the app version in the footer', async () => {
    respondWith(json([]))

    render(<App />)

    expect(await screen.findByText('Version dev')).toBeInTheDocument()
  })
})

describe('creating a note', () => {
  it('posts the form and adds the note to the list', async () => {
    respondWith(json([]), json(note({ title: 'New', body: 'Hello' }), 201))
    const user = userEvent.setup()
    render(<App />)
    await screen.findByText('No notes yet.')

    const form = screen.getByRole('region', { name: 'New note' })
    await user.type(within(form).getByLabelText('Title'), 'New')
    await user.type(within(form).getByLabelText('Body'), 'Hello')
    await user.click(within(form).getByRole('button', { name: 'Add note' }))

    expect(await screen.findByRole('listitem', { name: 'New' })).toHaveTextContent('Hello')
    expect(lastRequest()).toEqual({ url: '/api/notes', method: 'POST', body: { title: 'New', body: 'Hello' } })
    expect(within(form).getByLabelText('Title')).toHaveValue('')
  })

  it('shows validation messages from the API', async () => {
    respondWith(
      json([]),
      json(
        {
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: { title: ['Title is required.'], body: ['Body must be at most 10000 characters.'] },
        },
        400,
      ),
    )
    const user = userEvent.setup()
    render(<App />)
    await screen.findByText('No notes yet.')

    const form = screen.getByRole('region', { name: 'New note' })
    await user.click(within(form).getByRole('button', { name: 'Add note' }))

    const title = within(form).getByLabelText('Title')
    expect(await within(form).findByText('Title is required.')).toBeInTheDocument()
    expect(title).toHaveAttribute('aria-invalid', 'true')
    expect(title).toHaveAccessibleDescription('Title is required.')
    expect(within(form).getByLabelText('Body')).toHaveAccessibleDescription('Body must be at most 10000 characters.')
    expect(screen.getByText('No notes yet.')).toBeInTheDocument()
  })
})

describe('editing a note', () => {
  it('updates the note inline', async () => {
    const original = note({ title: 'Old', body: 'Old body' })
    respondWith(json([original]), json({ ...original, title: 'Updated', body: null }))
    const user = userEvent.setup()
    render(<App />)

    const item = await screen.findByRole('listitem', { name: 'Old' })
    await user.click(within(item).getByRole('button', { name: 'Edit' }))
    const title = within(item).getByLabelText('Title')
    expect(title).toHaveValue('Old')
    await user.clear(title)
    await user.type(title, 'Updated')
    await user.clear(within(item).getByLabelText('Body'))
    await user.click(within(item).getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('heading', { name: 'Updated' })).toBeInTheDocument()
    expect(lastRequest()).toEqual({
      url: `/api/notes/${original.id}`,
      method: 'PUT',
      body: { title: 'Updated', body: null },
    })
  })

  it('cancelling discards changes', async () => {
    respondWith(json([note({ title: 'Keep me' })]))
    const user = userEvent.setup()
    render(<App />)

    const item = await screen.findByRole('listitem', { name: 'Keep me' })
    await user.click(within(item).getByRole('button', { name: 'Edit' }))
    await user.type(within(item).getByLabelText('Title'), ' changed')
    await user.click(within(item).getByRole('button', { name: 'Cancel' }))

    expect(screen.getByRole('heading', { name: 'Keep me' })).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })
})

describe('deleting a note', () => {
  it('removes the note from the list', async () => {
    const doomed = note({ title: 'Doomed' })
    respondWith(json([doomed, note({ title: 'Survivor' })]), new Response(null, { status: 204 }))
    const user = userEvent.setup()
    render(<App />)

    const item = await screen.findByRole('listitem', { name: 'Doomed' })
    await user.click(within(item).getByRole('button', { name: 'Delete' }))

    expect(await screen.findByRole('listitem', { name: 'Survivor' })).toBeInTheDocument()
    expect(screen.queryByRole('listitem', { name: 'Doomed' })).not.toBeInTheDocument()
    expect(lastRequest()).toEqual({ url: `/api/notes/${doomed.id}`, method: 'DELETE', body: undefined })
  })

  it('shows an error when the delete fails', async () => {
    respondWith(json([note({ title: 'Stubborn' })]), json({ title: 'Not Found' }, 404))
    const user = userEvent.setup()
    render(<App />)

    const item = await screen.findByRole('listitem', { name: 'Stubborn' })
    await user.click(within(item).getByRole('button', { name: 'Delete' }))

    expect(await within(item).findByRole('alert')).toHaveTextContent('Not Found')
    expect(screen.getByRole('listitem', { name: 'Stubborn' })).toBeInTheDocument()
  })
})
