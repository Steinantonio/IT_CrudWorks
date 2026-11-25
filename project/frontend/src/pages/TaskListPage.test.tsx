import { QueryClient } from '@tanstack/react-query'
import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { UserEvent } from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { SESSION_STORAGE_KEY } from '../auth/session'
import type { Session } from '../auth/session'
import type { Task } from '../lib/tasks'
import { installFetchMock, jsonResponse } from '../test/http'
import { renderApp } from '../test/renderApp'

const session: Session = {
  accessToken: 'token-abc',
  expiresAtUtc: '2030-01-01T00:00:00Z',
  user: {
    id: '0b7f0f8e-6c2f-4a6f-9f2a-1f5a1e3c9d10',
    email: 'demo@crudworks.local',
    displayName: 'Demo User',
  },
}

const tasks: Task[] = [
  {
    id: '5a0f6a6e-1a3f-4d6f-9c2e-8b7d4f1a2c33',
    title: 'Write the proposal',
    description: 'Draft and review the proposal.',
    status: 'InProgress',
    dueDate: '2026-11-14',
    createdAtUtc: '2026-11-11T18:30:00Z',
    updatedAtUtc: '2026-11-11T18:45:00Z',
  },
  {
    id: '9d2b1c4e-7a5f-4e8b-b1c2-3d4e5f6a7b8c',
    title: 'Book the review',
    description: null,
    status: 'Todo',
    dueDate: null,
    createdAtUtc: '2026-11-10T09:00:00Z',
    updatedAtUtc: '2026-11-10T09:00:00Z',
  },
]

function pageOf(items: Task[], nextCursor: string | null = null) {
  return { items, nextCursor }
}

function signIn() {
  sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session))
}

function rowFor(title: string): HTMLElement {
  const row = screen.getByText(title).closest('li')
  if (!row) {
    throw new Error(`Row for "${title}" not found`)
  }

  return row
}

function deleteButtonFor(title: string) {
  return within(rowFor(title)).getByRole('button', { name: `Delete ${title}` })
}

/**
 * Presses Tab until the wanted element owns the focus. It proves that the
 * element is reachable with the keyboard alone, without asserting the whole
 * tab order of the page.
 */
async function tabTo(user: UserEvent, target: HTMLElement, maxTabs = 25): Promise<boolean> {
  for (let attempt = 0; attempt < maxTabs; attempt += 1) {
    if (document.activeElement === target) {
      return true
    }

    await user.tab()
  }

  return document.activeElement === target
}

describe('TaskListPage', () => {
  it('shows a loading state while the tasks are pending', async () => {
    signIn()
    installFetchMock(() => new Promise<Response>(() => {}))

    renderApp({ entries: ['/'] })

    expect(await screen.findByText('Loading tasks…')).toBeInTheDocument()
    expect(screen.queryByText('No tasks yet')).not.toBeInTheDocument()
  })

  it('renders the task rows with fields and actions in order', async () => {
    signIn()
    installFetchMock(() => jsonResponse(pageOf(tasks)))

    renderApp({ entries: ['/'] })

    const rows = await screen.findAllByRole('listitem')
    expect(rows).toHaveLength(2)
    expect(rows[0]).toHaveTextContent('Write the proposal')
    expect(rows[1]).toHaveTextContent('Book the review')

    const firstRow = within(rows[0])
    expect(firstRow.getByText('In progress')).toBeInTheDocument()
    expect(firstRow.getByText('Draft and review the proposal.')).toBeInTheDocument()
    expect(firstRow.getByText(/^(Due|Overdue ·) Nov 14, 2026$/)).toBeInTheDocument()
    expect(
      firstRow.getByRole('link', { name: 'Edit Write the proposal' }),
    ).toHaveAttribute('href', `/tasks/${tasks[0].id}/edit`)
    expect(firstRow.getByRole('button', { name: 'Delete Write the proposal' })).toBeInTheDocument()

    expect(within(rows[1]).getByText('Todo')).toBeInTheDocument()
    expect(within(rows[1]).queryByText(/^Due/)).not.toBeInTheDocument()
  })

  it('summarises the loaded tasks by status', async () => {
    signIn()
    installFetchMock(() => jsonResponse(pageOf(tasks)))

    renderApp({ entries: ['/'] })

    await screen.findByText('Write the proposal')

    const summary = screen.getByRole('group', { name: 'Task summary' })
    expect(summary).toHaveTextContent(/Todo\s*1/)
    expect(summary).toHaveTextContent(/In progress\s*1/)
    expect(summary).toHaveTextContent(/Done\s*0/)
  })

  it('loads the next page from the cursor and shows the end of the list', async () => {
    signIn()
    const thirdTask: Task = {
      ...tasks[1],
      id: '1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c5d',
      title: 'Third task',
      createdAtUtc: '2026-11-09T09:00:00Z',
    }
    const fetchMock = installFetchMock((url) => {
      if (url === '/api/tasks') {
        return jsonResponse(pageOf(tasks, 'cursor-1'))
      }

      if (url === '/api/tasks?cursor=cursor-1') {
        return jsonResponse(pageOf([thirdTask]))
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })

    expect(await screen.findByText('Write the proposal')).toBeInTheDocument()
    expect(screen.getByText('Counts and filters include loaded tasks.')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Load more' }))

    expect(await screen.findByText('Third task')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Load more' })).not.toBeInTheDocument()
    expect(screen.getByText('You have reached the end of the list.')).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/tasks?cursor=cursor-1', expect.anything())
  })

  it('resets to the first page when the server rejects a cursor', async () => {
    signIn()
    let listCalls = 0
    installFetchMock((url) => {
      if (url === '/api/tasks') {
        listCalls += 1
        return listCalls === 1
          ? jsonResponse(pageOf(tasks, 'stale-cursor'))
          : jsonResponse(pageOf(tasks))
      }

      if (url === '/api/tasks?cursor=stale-cursor') {
        return jsonResponse({ title: 'Invalid cursor.', status: 400 }, 400)
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })

    expect(await screen.findByText('Write the proposal')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Load more' }))

    await waitFor(() => expect(listCalls).toBe(2))

    expect(screen.getByText('Write the proposal')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Load more' })).not.toBeInTheDocument()
  })

  it('shows an error for a first-page 400 and does not loop', async () => {
    signIn()
    let calls = 0
    installFetchMock(() => {
      calls += 1
      return jsonResponse({ title: 'Page size must be between 1 and 100.', status: 400 }, 400)
    })

    renderApp({ entries: ['/'] })

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Could not load your tasks. Please try again.',
    )
    expect(calls).toBe(1)
  })

  it('disables Load more and announces loading while the next page is in flight', async () => {
    signIn()
    let resolveNextPage: (response: Response) => void = () => {}
    const nextPage = new Promise<Response>((resolve) => {
      resolveNextPage = resolve
    })
    installFetchMock((url) => {
      if (url === '/api/tasks') {
        return jsonResponse(pageOf(tasks, 'cursor-1'))
      }

      if (url === '/api/tasks?cursor=cursor-1') {
        return nextPage
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })
    expect(await screen.findByText('Write the proposal')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Load more' }))

    expect(screen.getByRole('button', { name: 'Loading more…' })).toBeDisabled()
    expect(screen.getByText('Loading more tasks…')).toBeInTheDocument()

    resolveNextPage(jsonResponse(pageOf([{ ...tasks[1], title: 'Third task' }])))

    expect(await screen.findByText('Third task')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Load more' })).not.toBeInTheDocument()
  })

  it('shows a next-page error and retries the same cursor', async () => {
    signIn()
    let cursorCalls = 0
    installFetchMock((url) => {
      if (url === '/api/tasks') {
        return jsonResponse(pageOf(tasks, 'cursor-1'))
      }

      if (url === '/api/tasks?cursor=cursor-1') {
        cursorCalls += 1
        if (cursorCalls === 1) {
          return jsonResponse({ title: 'Server error', status: 500 }, 500)
        }

        return jsonResponse(pageOf([{ ...tasks[1], title: 'Third task' }]))
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })
    expect(await screen.findByText('Write the proposal')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Load more' }))
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Could not load your tasks. Please try again.',
    )

    await user.click(screen.getByRole('button', { name: 'Retry' }))

    expect(await screen.findByText('Third task')).toBeInTheDocument()
    expect(cursorCalls).toBe(2)
  })

  it('retries a background refetch failure with refetch', async () => {
    signIn()
    let listCalls = 0
    installFetchMock((url, init) => {
      if (url === '/api/tasks' && !init?.method) {
        listCalls += 1
        if (listCalls === 1) {
          return jsonResponse(pageOf(tasks))
        }

        if (listCalls === 2) {
          return jsonResponse({ title: 'Server error', status: 500 }, 500)
        }

        return jsonResponse(pageOf([tasks[1]]))
      }

      if (init?.method === 'DELETE') {
        return new Response(null, { status: 204 })
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })
    expect(await screen.findByText('Write the proposal')).toBeInTheDocument()

    await user.click(deleteButtonFor('Write the proposal'))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByRole('button', { name: 'Delete task' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Could not load your tasks. Please try again.',
    )

    await user.click(screen.getByRole('button', { name: 'Retry' }))

    await waitFor(() => expect(listCalls).toBe(3))
    expect(screen.getByText('Book the review')).toBeInTheDocument()
    expect(screen.queryByText('Write the proposal')).not.toBeInTheDocument()
  })

  it('shows an error when the first page fails after a cursor reset', async () => {
    signIn()
    let listCalls = 0
    installFetchMock((url) => {
      if (url === '/api/tasks') {
        listCalls += 1
        return listCalls === 1
          ? jsonResponse(pageOf(tasks, 'stale-cursor'))
          : jsonResponse({ title: 'Server error', status: 500 }, 500)
      }

      if (url === '/api/tasks?cursor=stale-cursor') {
        return jsonResponse({ title: 'Invalid cursor.', status: 400 }, 400)
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })
    expect(await screen.findByText('Write the proposal')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Load more' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Could not load your tasks. Please try again.',
    )
    expect(listCalls).toBe(2)
  })

  it('marks a past due date as overdue', async () => {
    signIn()
    installFetchMock(() => jsonResponse(pageOf([{ ...tasks[0], dueDate: '2020-01-01' }])))

    renderApp({ entries: ['/'] })

    expect(await screen.findByText('Overdue · Jan 1, 2020')).toBeInTheDocument()
  })

  it('does not mark a completed task as overdue', async () => {
    signIn()
    installFetchMock(() =>
      jsonResponse(pageOf([{ ...tasks[0], status: 'Done', dueDate: '2020-01-01' }])),
    )

    renderApp({ entries: ['/'] })

    expect(await screen.findByText('Due Jan 1, 2020')).toBeInTheDocument()
    expect(screen.queryByText(/Overdue/)).not.toBeInTheDocument()
  })

  it('shows the empty state with a create action when there are no tasks', async () => {
    signIn()
    installFetchMock(() => jsonResponse(pageOf([])))

    renderApp({ entries: ['/'] })

    expect(await screen.findByText('No tasks yet — create your first task.')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Create a task' })).toHaveAttribute(
      'href',
      '/tasks/new',
    )
  })

  it('shows an error state with retry and recovers after refetching', async () => {
    signIn()
    let calls = 0
    installFetchMock((url) => {
      calls += 1
      if (calls === 1) {
        return jsonResponse({ title: 'Server error', status: 500 }, 500)
      }

      if (url === '/api/tasks') {
        return jsonResponse(pageOf(tasks))
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Could not load your tasks. Please try again.',
    )

    await user.click(screen.getByRole('button', { name: 'Retry' }))

    expect(await screen.findByText('Write the proposal')).toBeInTheDocument()
  })

  it('reports a network failure with a connection-specific message', async () => {
    signIn()
    installFetchMock(() => {
      throw new TypeError('Failed to fetch')
    })

    renderApp({ entries: ['/'] })

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Could not reach the server. Check your connection and try again.',
    )
  })

  it('cancelling the delete confirmation keeps the row and sends no request', async () => {
    signIn()
    const fetchMock = installFetchMock(() => jsonResponse(pageOf(tasks)))
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })
    await screen.findByText('Write the proposal')

    await user.click(deleteButtonFor('Write the proposal'))
    expect(await screen.findByRole('dialog')).toBeInTheDocument()

    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Cancel' }))

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(screen.getByText('Write the proposal')).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalledWith(
      `/api/tasks/${tasks[0].id}`,
      expect.objectContaining({ method: 'DELETE' }),
    )
  })

  it('confirming the delete sends DELETE and removes the row', async () => {
    signIn()
    const remaining = [...tasks]
    installFetchMock((url, init) => {
      if (url === '/api/tasks') {
        return jsonResponse(pageOf(remaining))
      }

      if (init?.method === 'DELETE') {
        remaining.splice(
          remaining.findIndex((candidate) => candidate.id === tasks[0].id),
          1,
        )
        return new Response(null, { status: 204 })
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })
    await screen.findByText('Write the proposal')

    await user.click(deleteButtonFor('Write the proposal'))
    const dialog = await screen.findByRole('dialog')

    await user.click(within(dialog).getByRole('button', { name: 'Delete task' }))

    await screen.findByText('Book the review')
    expect(screen.queryByText('Write the proposal')).not.toBeInTheDocument()
  })

  it('closes the confirmation dialog with Escape without deleting', async () => {
    signIn()
    const fetchMock = installFetchMock(() => jsonResponse(pageOf(tasks)))
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })
    await screen.findByText('Write the proposal')

    await user.click(deleteButtonFor('Write the proposal'))
    const dialog = await screen.findByRole('dialog')
    expect(dialog).toHaveAttribute('aria-modal', 'true')

    await user.keyboard('{Escape}')

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(screen.getByText('Write the proposal')).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalledWith(
      `/api/tasks/${tasks[0].id}`,
      expect.objectContaining({ method: 'DELETE' }),
    )
  })

  it('keeps the row and shows a safe message when the delete fails', async () => {
    signIn()
    installFetchMock((url, init) => {
      if (url === '/api/tasks') {
        return jsonResponse(pageOf(tasks))
      }

      if (init?.method === 'DELETE') {
        return jsonResponse({ title: 'Server error', status: 500 }, 500)
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })
    await screen.findByText('Write the proposal')

    await user.click(deleteButtonFor('Write the proposal'))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByRole('button', { name: 'Delete task' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Could not delete the task. Please try again.',
    )
    expect(screen.getByText('Write the proposal')).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('clears the session and returns to login once when the list returns 401', async () => {
    signIn()
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: 1, retryDelay: 0, gcTime: 0 },
        mutations: { retry: false },
      },
    })
    const fetchMock = installFetchMock(() =>
      jsonResponse({ title: 'Authentication required', status: 401 }, 401),
    )

    renderApp({ entries: ['/'], queryClient })

    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
    expect(fetchMock.mock.calls.length).toBeLessThanOrEqual(2)
  })

  it('conveys the task status as text and not only with colour', async () => {
    signIn()
    installFetchMock(() => jsonResponse(pageOf(tasks)))

    renderApp({ entries: ['/'] })

    await screen.findByText('Write the proposal')

    const badge = within(rowFor('Write the proposal')).getByText('In progress')
    expect(badge).toHaveTextContent('In progress')
    expect(badge).not.toHaveAttribute('aria-hidden', 'true')
    expect(within(rowFor('Book the review')).getByText('Todo')).toBeInTheDocument()
  })

  it('reaches the row actions with the keyboard and opens the edit form with Enter', async () => {
    signIn()
    installFetchMock((url) => {
      if (url === '/api/tasks') {
        return jsonResponse(pageOf(tasks))
      }

      if (url === `/api/tasks/${tasks[0].id}`) {
        return jsonResponse(tasks[0])
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })
    await screen.findByText('Write the proposal')

    const editLink = within(rowFor('Write the proposal')).getByRole('link', {
      name: 'Edit Write the proposal',
    })
    expect(await tabTo(user, editLink)).toBe(true)

    await user.keyboard('{Enter}')

    expect(await screen.findByRole('heading', { name: 'Edit task' })).toBeInTheDocument()
    expect(screen.getByLabelText('Title')).toHaveValue('Write the proposal')
  })

  it('reaches the delete action with the keyboard and opens the dialog with Enter', async () => {
    signIn()
    const fetchMock = installFetchMock((url) => {
      if (url === '/api/tasks') {
        return jsonResponse(pageOf(tasks))
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })
    await screen.findByText('Write the proposal')

    const deleteButton = deleteButtonFor('Write the proposal')
    expect(await tabTo(user, deleteButton)).toBe(true)

    await user.keyboard('{Enter}')

    expect(await screen.findByRole('dialog')).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalledWith(
      `/api/tasks/${tasks[0].id}`,
      expect.objectContaining({ method: 'DELETE' }),
    )
  })

  it('returns the focus to the delete trigger when the dialog is dismissed with Escape', async () => {
    signIn()
    installFetchMock(() => jsonResponse(pageOf(tasks)))
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })
    await screen.findByText('Write the proposal')

    const deleteButton = deleteButtonFor('Write the proposal')
    await user.click(deleteButton)
    await screen.findByRole('dialog')

    await user.keyboard('{Escape}')

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(deleteButton).toHaveFocus()
  })

  it('moves the focus to the list heading after a task is deleted', async () => {
    signIn()
    const remaining = [...tasks]
    installFetchMock((url, init) => {
      if (url === '/api/tasks') {
        return jsonResponse(pageOf(remaining))
      }

      if (init?.method === 'DELETE') {
        remaining.splice(
          remaining.findIndex((candidate) => candidate.id === tasks[0].id),
          1,
        )
        return new Response(null, { status: 204 })
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })
    await screen.findByText('Write the proposal')

    await user.click(deleteButtonFor('Write the proposal'))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByRole('button', { name: 'Delete task' }))

    await screen.findByText('Book the review')
    expect(screen.getByRole('heading', { name: 'Tasks' })).toHaveFocus()
  })

  it('filters the visible list when a summary card is clicked and clears it on a second click', async () => {
    signIn()
    installFetchMock(() => jsonResponse(pageOf(tasks)))
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })
    await screen.findByText('Write the proposal')

    const summary = screen.getByRole('group', { name: 'Task summary' })
    const todoCard = within(summary).getByRole('button', { name: 'Filter by Todo (1)' })

    await user.click(todoCard)

    expect(todoCard).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByText('Book the review')).toBeInTheDocument()
    expect(screen.queryByText('Write the proposal')).not.toBeInTheDocument()
    // Counts stay computed from the loaded pages while the filter is active.
    expect(
      within(summary).getByRole('button', { name: 'Filter by In progress (1)' }),
    ).toBeInTheDocument()

    await user.click(todoCard)

    expect(todoCard).toHaveAttribute('aria-pressed', 'false')
    expect(screen.getByText('Write the proposal')).toBeInTheDocument()
    expect(screen.getByText('Book the review')).toBeInTheDocument()
  })

  it('shows an inline note when the active status filter matches no tasks', async () => {
    signIn()
    installFetchMock(() => jsonResponse(pageOf(tasks)))
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })
    await screen.findByText('Write the proposal')

    const doneCard = within(screen.getByRole('group', { name: 'Task summary' })).getByRole(
      'button',
      { name: 'Filter by Done (0)' },
    )
    await user.click(doneCard)

    expect(await screen.findByText('No tasks with this status.')).toBeInTheDocument()
    // The note is not the empty state: the account is not empty.
    expect(screen.queryByText('No tasks yet')).not.toBeInTheDocument()

    await user.click(doneCard)

    expect(screen.queryByText('No tasks with this status.')).not.toBeInTheDocument()
    expect(screen.getByText('Write the proposal')).toBeInTheDocument()
  })

  it('saves an edited description with a full-replace PUT that keeps title, status and due date', async () => {
    signIn()
    const stored = [...tasks]
    const fetchMock = installFetchMock((url, init) => {
      if (url === '/api/tasks') {
        return jsonResponse(pageOf(stored))
      }

      if (url === `/api/tasks/${tasks[0].id}` && init?.method === 'PUT') {
        const body = JSON.parse(String(init.body)) as Partial<Task>
        stored[0] = { ...tasks[0], ...body }
        return jsonResponse(stored[0])
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })
    await screen.findByText('Write the proposal')

    const row = rowFor('Write the proposal')
    await user.click(within(row).getByRole('button', { name: 'Edit description' }))

    const textarea = within(row).getByLabelText('Description')
    await user.clear(textarea)
    await user.type(textarea, 'Updated description text.')
    await user.click(within(row).getByRole('button', { name: 'Save description' }))

    expect(await within(row).findByText('Updated description text.')).toBeInTheDocument()

    const putCall = fetchMock.mock.calls.find(([, init]) => init?.method === 'PUT')
    expect(putCall?.[0]).toBe(`/api/tasks/${tasks[0].id}`)
    expect(JSON.parse(String(putCall?.[1]?.body))).toEqual({
      title: 'Write the proposal',
      description: 'Updated description text.',
      status: 'InProgress',
      dueDate: '2026-11-14',
    })
  })

  it('sends null and clears the description when it is saved empty', async () => {
    signIn()
    const stored = [...tasks]
    const fetchMock = installFetchMock((url, init) => {
      if (url === '/api/tasks') {
        return jsonResponse(pageOf(stored))
      }

      if (url === `/api/tasks/${tasks[0].id}` && init?.method === 'PUT') {
        const body = JSON.parse(String(init.body)) as Partial<Task>
        stored[0] = { ...tasks[0], ...body }
        return jsonResponse(stored[0])
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })
    await screen.findByText('Write the proposal')

    const row = rowFor('Write the proposal')
    await user.click(within(row).getByRole('button', { name: 'Edit description' }))

    await user.clear(within(row).getByLabelText('Description'))
    await user.click(within(row).getByRole('button', { name: 'Save description' }))

    const putCall = fetchMock.mock.calls.find(([, init]) => init?.method === 'PUT')
    expect(JSON.parse(String(putCall?.[1]?.body))).toEqual({
      title: 'Write the proposal',
      description: null,
      status: 'InProgress',
      dueDate: '2026-11-14',
    })
    expect(await within(row).findByRole('button', { name: 'Add description' })).toBeInTheDocument()
    expect(within(row).queryByText('Draft and review the proposal.')).not.toBeInTheDocument()
  })

  it('shows an inline error and blocks saving a description longer than 2000 characters', async () => {
    signIn()
    const fetchMock = installFetchMock((url) => {
      if (url === '/api/tasks') {
        return jsonResponse(pageOf(tasks))
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })
    await screen.findByText('Write the proposal')

    const row = rowFor('Write the proposal')
    await user.click(within(row).getByRole('button', { name: 'Edit description' }))

    const textarea = within(row).getByLabelText('Description')
    fireEvent.change(textarea, { target: { value: 'a'.repeat(2001) } })

    expect(within(row).getByText('Description must be at most 2000 characters.')).toBeInTheDocument()
    expect(within(row).getByRole('button', { name: 'Save description' })).toBeDisabled()
    expect(fetchMock).not.toHaveBeenCalledWith(
      `/api/tasks/${tasks[0].id}`,
      expect.objectContaining({ method: 'PUT' }),
    )
  })

  it('reverts an unsaved description edit without sending a request', async () => {
    signIn()
    const fetchMock = installFetchMock((url) => {
      if (url === '/api/tasks') {
        return jsonResponse(pageOf(tasks))
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/'] })
    await screen.findByText('Write the proposal')

    const row = rowFor('Write the proposal')
    await user.click(within(row).getByRole('button', { name: 'Edit description' }))

    const textarea = within(row).getByLabelText('Description')
    await user.clear(textarea)
    await user.type(textarea, 'Temporary text.')
    await user.click(within(row).getByRole('button', { name: 'Cancel' }))

    expect(within(row).getByText('Draft and review the proposal.')).toBeInTheDocument()
    expect(within(row).queryByRole('button', { name: 'Save description' })).not.toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalledWith(
      `/api/tasks/${tasks[0].id}`,
      expect.objectContaining({ method: 'PUT' }),
    )
  })

  it('shows a short ID chip, exposes the full ID and copies it on click', async () => {
    signIn()
    installFetchMock(() => jsonResponse(pageOf(tasks)))
    const user = userEvent.setup()
    // Install the clipboard stub after `userEvent.setup()`: the setup
    // replaces `navigator.clipboard` with its own internal stub.
    const writeText: (text: string) => Promise<void> = vi.fn(async () => undefined)
    Object.defineProperty(navigator, 'clipboard', {
      value: { writeText },
      configurable: true,
    })

    try {
      renderApp({ entries: ['/'] })
      await screen.findByText('Write the proposal')

      const chip = within(rowFor('Write the proposal')).getByRole('button', {
        name: 'Copy task ID #5A0F6A6E',
      })
      expect(chip).toHaveAttribute('title', tasks[0].id)
      expect(chip).toHaveTextContent('#5A0F6A6E')

      await user.click(chip)

      expect(writeText).toHaveBeenCalledWith(tasks[0].id)
      expect(await screen.findByText('Copied')).toBeInTheDocument()
    } finally {
      Object.defineProperty(navigator, 'clipboard', { value: undefined, configurable: true })
    }
  })

  it('decorates the card meta with inline icons', async () => {
    signIn()
    installFetchMock(() => jsonResponse(pageOf(tasks)))

    renderApp({ entries: ['/'] })
    await screen.findByText('Write the proposal')

    const row = rowFor('Write the proposal')

    const icons = row.querySelectorAll('svg')
    expect(icons).toHaveLength(3)
    for (const icon of icons) {
      expect(icon).toHaveAttribute('aria-hidden', 'true')
    }
    expect(row.querySelector('.task-icon-title')).not.toBeNull()
    expect(within(row).getByText(/^(Due|Overdue ·) Nov 14, 2026$/)).toBeInTheDocument()
  })
})
