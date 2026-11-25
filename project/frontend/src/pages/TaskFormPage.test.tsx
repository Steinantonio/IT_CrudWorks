import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { SESSION_STORAGE_KEY } from '../auth/session'
import type { Session } from '../auth/session'
import type { Task } from '../lib/tasks'
import { installFetchMock, jsonResponse } from '../test/http'
import { renderApp, waitForPage } from '../test/renderApp'

const session: Session = {
  accessToken: 'token-abc',
  expiresAtUtc: '2030-01-01T00:00:00Z',
  user: {
    id: '0b7f0f8e-6c2f-4a6f-9f2a-1f5a1e3c9d10',
    email: 'demo@crudworks.local',
    displayName: 'Demo User',
  },
}

const taskId = '5a0f6a6e-1a3f-4d6f-9c2e-8b7d4f1a2c33'

const existingTask: Task = {
  id: taskId,
  title: 'Write the proposal',
  description: 'Draft and review the proposal.',
  status: 'InProgress',
  dueDate: '2026-11-14',
  createdAtUtc: '2026-11-11T18:30:00Z',
  updatedAtUtc: '2026-11-11T18:45:00Z',
}

const createdTask: Task = {
  id: '9d2b1c4e-7a5f-4e8b-b1c2-3d4e5f6a7b8c',
  title: 'Write the proposal',
  description: 'Draft and review the proposal.',
  status: 'InProgress',
  dueDate: '2026-11-14',
  createdAtUtc: '2026-11-12T09:00:00Z',
  updatedAtUtc: '2026-11-12T09:00:00Z',
}

function signIn() {
  sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session))
}

function bodyOf(init: RequestInit | undefined): unknown {
  return JSON.parse(init?.body as string) as unknown
}

describe('TaskFormPage (create)', () => {
  it('validates the fields before sending a request', async () => {
    signIn()
    const fetchMock = installFetchMock(() => {
      throw new Error('No request expected')
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/tasks/new'] })
    await waitForPage()

    await user.click(screen.getByRole('button', { name: 'Create task' }))

    expect(
      await screen.findByText('Title must be between 3 and 200 characters.'),
    ).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('shows the empty-title message linked to the input', async () => {
    signIn()
    installFetchMock(() => {
      throw new Error('No request expected')
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/tasks/new'] })
    await waitForPage()
    await user.click(screen.getByRole('button', { name: 'Create task' }))

    const titleInput = screen.getByLabelText('Title')
    expect(titleInput).toHaveAttribute('aria-invalid', 'true')
    expect(titleInput).toHaveAccessibleDescription(
      'Title must be between 3 and 200 characters.',
    )
  })

  it('posts the entered values and returns to the list', async () => {
    signIn()
    const fetchMock = installFetchMock((url, init) => {
      if (init?.method === 'POST') {
        return jsonResponse(createdTask, 201)
      }

      if (url === '/api/tasks') {
        return jsonResponse([createdTask])
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/tasks/new'] })
    await waitForPage()

    await user.type(screen.getByLabelText('Title'), 'Write the proposal')
    await user.type(screen.getByLabelText('Description'), 'Draft and review the proposal.')
    await user.selectOptions(screen.getByLabelText('Status'), 'InProgress')
    await user.type(screen.getByLabelText('Due date'), '2026-11-14')
    await user.click(screen.getByRole('button', { name: 'Create task' }))

    expect(await screen.findByRole('heading', { name: 'Tasks' })).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/tasks',
      expect.objectContaining({ method: 'POST' }),
    )
    const createInit = fetchMock.mock.calls.find(
      ([, init]) => (init as RequestInit).method === 'POST',
    )?.[1]
    expect(bodyOf(createInit)).toEqual({
      title: 'Write the proposal',
      description: 'Draft and review the proposal.',
      status: 'InProgress',
      dueDate: '2026-11-14',
    })
  })

  it('defaults the status to Todo and clears empty optional fields', async () => {
    signIn()
    const fetchMock = installFetchMock((url, init) => {
      if (init?.method === 'POST') {
        return jsonResponse({ ...createdTask, status: 'Todo', description: null, dueDate: null }, 201)
      }

      if (url === '/api/tasks') {
        return jsonResponse([])
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/tasks/new'] })
    await waitForPage()

    await user.type(screen.getByLabelText('Title'), 'Water the plants')
    await user.click(screen.getByRole('button', { name: 'Create task' }))

    expect(await screen.findByRole('heading', { name: 'Tasks' })).toBeInTheDocument()
    const createInit = fetchMock.mock.calls.find(
      ([, init]) => (init as RequestInit).method === 'POST',
    )?.[1]
    expect(bodyOf(createInit)).toEqual({
      title: 'Water the plants',
      description: null,
      status: 'Todo',
      dueDate: null,
    })
  })

  it('returns to the list without submitting when cancelled', async () => {
    signIn()
    const fetchMock = installFetchMock((url, init) => {
      if (url === '/api/tasks' && !init?.method) {
        return jsonResponse([])
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/tasks/new'] })
    await waitForPage()
    await user.click(screen.getByRole('link', { name: 'Cancel' }))

    expect(await screen.findByRole('heading', { name: 'Tasks' })).toBeInTheDocument()
    const submitted = fetchMock.mock.calls.some(([, init]) => init?.method !== undefined)
    expect(submitted).toBe(false)
  })

  it('walks the form controls with Tab in the visual order', async () => {
    signIn()
    installFetchMock(() => {
      throw new Error('No request expected')
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/tasks/new'] })
    await waitForPage()

    screen.getByLabelText('Title').focus()

    await user.tab()
    expect(screen.getByLabelText('Description')).toHaveFocus()

    await user.tab()
    expect(screen.getByLabelText('Status')).toHaveFocus()

    await user.tab()
    expect(screen.getByLabelText('Due date')).toHaveFocus()

    await user.tab()
    expect(screen.getByRole('button', { name: 'Create task' })).toHaveFocus()

    await user.tab()
    expect(screen.getByRole('link', { name: 'Cancel' })).toHaveFocus()
  })

  it('moves the focus to the first invalid field and announces the message', async () => {
    signIn()
    const fetchMock = installFetchMock(() => {
      throw new Error('No request expected')
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/tasks/new'] })
    await waitForPage()
    await user.click(screen.getByRole('button', { name: 'Create task' }))

    expect(
      await screen.findByText('Title must be between 3 and 200 characters.'),
    ).toBeInTheDocument()
    expect(screen.getByLabelText('Title')).toHaveFocus()
    expect(screen.getByLabelText('Title')).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByRole('alert')).toHaveTextContent(
      'Title must be between 3 and 200 characters.',
    )
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('submits with Enter from the title field', async () => {
    signIn()
    const fetchMock = installFetchMock((url, init) => {
      if (init?.method === 'POST') {
        return jsonResponse(createdTask, 201)
      }

      if (url === '/api/tasks') {
        return jsonResponse([createdTask])
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/tasks/new'] })
    await waitForPage()
    await user.type(screen.getByLabelText('Title'), 'Write the proposal{Enter}')

    expect(await screen.findByRole('heading', { name: 'Tasks' })).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/tasks',
      expect.objectContaining({ method: 'POST' }),
    )
  })

  it('cancels with the keyboard from the cancel link', async () => {
    signIn()
    const fetchMock = installFetchMock((url) => {
      if (url === '/api/tasks') {
        return jsonResponse([])
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/tasks/new'] })
    await waitForPage()
    screen.getByRole('link', { name: 'Cancel' }).focus()
    await user.keyboard('{Enter}')

    expect(await screen.findByRole('heading', { name: 'Tasks' })).toBeInTheDocument()
    const submitted = fetchMock.mock.calls.some(([, init]) => init?.method !== undefined)
    expect(submitted).toBe(false)
  })
})

describe('TaskFormPage (edit)', () => {
  it('loads the task and sends the replacement with PUT', async () => {
    signIn()
    const fetchMock = installFetchMock((url, init) => {
      if (init?.method === 'PUT') {
        return jsonResponse({ ...existingTask, title: 'Write the final proposal' })
      }

      if (url === `/api/tasks/${taskId}`) {
        return jsonResponse(existingTask)
      }

      if (url === '/api/tasks') {
        return jsonResponse([{ ...existingTask, title: 'Write the final proposal' }])
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: [`/tasks/${taskId}/edit`] })

    const titleInput = await screen.findByLabelText('Title')
    expect(titleInput).toHaveValue('Write the proposal')
    expect(screen.getByLabelText('Status')).toHaveValue('InProgress')
    expect(screen.getByLabelText('Due date')).toHaveValue('2026-11-14')
    expect(screen.getByLabelText('Description')).toHaveValue('Draft and review the proposal.')

    await user.clear(titleInput)
    await user.type(titleInput, 'Write the final proposal')
    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    expect(await screen.findByRole('heading', { name: 'Tasks' })).toBeInTheDocument()
    const putInit = fetchMock.mock.calls.find(
      ([, init]) => (init as RequestInit).method === 'PUT',
    )?.[1]
    expect(bodyOf(putInit)).toEqual({
      title: 'Write the final proposal',
      description: 'Draft and review the proposal.',
      status: 'InProgress',
      dueDate: '2026-11-14',
    })
    expect(fetchMock).toHaveBeenCalledWith(
      `/api/tasks/${taskId}`,
      expect.objectContaining({ method: 'PUT' }),
    )
  })

  it('shows a safe not-found state for a missing task', async () => {
    signIn()
    installFetchMock(() =>
      jsonResponse({ title: 'Resource not found', status: 404 }, 404),
    )

    renderApp({ entries: [`/tasks/${taskId}/edit`] })

    expect(await screen.findByRole('heading', { name: 'Task not found' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Back to tasks' })).toHaveAttribute('href', '/')
  })

  it('maps server 400 field errors to the matching inputs', async () => {
    signIn()
    installFetchMock((url, init) => {
      if (init?.method === 'PUT') {
        return jsonResponse(
          {
            title: 'One or more validation errors occurred.',
            status: 400,
            errors: {
              title: ['Title must be between 3 and 200 characters.'],
              dueDate: ['Due date must be a valid date.'],
            },
          },
          400,
        )
      }

      if (url === `/api/tasks/${taskId}`) {
        return jsonResponse(existingTask)
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: [`/tasks/${taskId}/edit`] })
    await screen.findByLabelText('Title')

    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    expect(
      await screen.findByText('Title must be between 3 and 200 characters.'),
    ).toBeInTheDocument()
    expect(screen.getByLabelText('Title')).toHaveAccessibleDescription(
      'Title must be between 3 and 200 characters.',
    )
    expect(screen.getByText('Due date must be a valid date.')).toBeInTheDocument()
  })

  it('shows a safe form-level message when no server error matches a field', async () => {
    signIn()
    installFetchMock((url, init) => {
      if (init?.method === 'PUT') {
        return jsonResponse(
          {
            title: 'One or more validation errors occurred.',
            status: 400,
            errors: { userId: ['UserId is assigned by the server.'] },
          },
          400,
        )
      }

      if (url === `/api/tasks/${taskId}`) {
        return jsonResponse(existingTask)
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: [`/tasks/${taskId}/edit`] })
    await screen.findByLabelText('Title')

    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Some details are invalid. Check the form and try again.',
    )
  })

  it('shows a safe message when saving reports the task is gone', async () => {
    signIn()
    installFetchMock((url, init) => {
      if (init?.method === 'PUT') {
        return jsonResponse({ title: 'Resource not found', status: 404 }, 404)
      }

      if (url === `/api/tasks/${taskId}`) {
        return jsonResponse(existingTask)
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: [`/tasks/${taskId}/edit`] })
    await screen.findByLabelText('Title')

    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'This task no longer exists.',
    )
    expect(screen.getByRole('heading', { name: 'Edit task' })).toBeInTheDocument()
  })
})
