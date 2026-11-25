import { clearSession, SESSION_STORAGE_KEY, writeSession } from '../auth/session'
import type { Session } from '../auth/session'
import { installFetchMock, jsonResponse } from '../test/http'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from './api'
import {
  createTask,
  CursorExpiredError,
  getTask,
  listTasksPage,
  removeTask,
  setUnauthorizedHandler,
  updateTask,
} from './tasks'

const session: Session = {
  accessToken: 'token-abc',
  expiresAtUtc: '2030-01-01T00:00:00Z',
  user: {
    id: '0b7f0f8e-6c2f-4a6f-9f2a-1f5a1e3c9d10',
    email: 'demo@crudworks.local',
    displayName: 'Demo User',
  },
}

const task = {
  id: '5a0f6a6e-1a3f-4d6f-9c2e-8b7d4f1a2c33',
  title: 'Write the proposal',
  description: 'Draft and review the proposal.',
  status: 'InProgress',
  dueDate: '2026-11-14',
  createdAtUtc: '2026-11-11T18:30:00Z',
  updatedAtUtc: '2026-11-11T18:45:00Z',
}

const input = {
  title: 'Write the proposal',
  description: 'Draft and review the proposal.',
  status: 'InProgress' as const,
  dueDate: '2026-11-14',
}

afterEach(() => {
  setUnauthorizedHandler(null)
  clearSession()
})

describe('task API functions', () => {
  it('sends the bearer token and returns the page', async () => {
    writeSession(session)
    const page = { items: [task], nextCursor: null }
    const fetchMock = installFetchMock(() => jsonResponse(page))

    const result = await listTasksPage(session.accessToken)

    expect(result).toEqual(page)
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/tasks',
      expect.objectContaining({
        headers: expect.any(Headers),
      }),
    )
    const init = fetchMock.mock.calls[0]?.[1]
    expect(new Headers(init?.headers).get('Authorization')).toBe('Bearer token-abc')
  })

  it('builds the take and cursor query string', async () => {
    installFetchMock(() => jsonResponse({ items: [task], nextCursor: null }))

    await listTasksPage(session.accessToken, { take: 10, cursor: 'cursor-1' })

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/tasks?take=10&cursor=cursor-1',
      expect.anything(),
    )
  })

  it('wraps a 400 from a cursor request as CursorExpiredError', async () => {
    installFetchMock(() =>
      jsonResponse({ title: 'Cursor is invalid.', status: 400 }, 400),
    )

    await expect(
      listTasksPage(session.accessToken, { cursor: 'stale' }),
    ).rejects.toBeInstanceOf(CursorExpiredError)
  })

  it('does not wrap a 400 from the first page', async () => {
    installFetchMock(() =>
      jsonResponse({ title: 'Page size must be between 1 and 100.', status: 400 }, 400),
    )

    await expect(listTasksPage(session.accessToken)).rejects.toBeInstanceOf(ApiError)
  })

  it('gets a single task by id', async () => {
    installFetchMock(() => jsonResponse(task))

    const result = await getTask(session.accessToken, task.id)

    expect(result).toEqual(task)
    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      `/api/tasks/${task.id}`,
      expect.anything(),
    )
  })

  it('posts the create payload', async () => {
    installFetchMock(() => jsonResponse(task, 201))

    const result = await createTask(session.accessToken, input)

    expect(result).toEqual(task)
    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/tasks',
      expect.objectContaining({ method: 'POST', body: JSON.stringify(input) }),
    )
  })

  it('puts the replacement payload to the task URL', async () => {
    installFetchMock(() => jsonResponse(task))

    const result = await updateTask(session.accessToken, task.id, input)

    expect(result).toEqual(task)
    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      `/api/tasks/${task.id}`,
      expect.objectContaining({ method: 'PUT', body: JSON.stringify(input) }),
    )
  })

  it('sends DELETE and resolves without a body', async () => {
    installFetchMock(() => new Response(null, { status: 204 }))

    await expect(removeTask(session.accessToken, task.id)).resolves.toBeUndefined()
    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      `/api/tasks/${task.id}`,
      expect.objectContaining({ method: 'DELETE' }),
    )
  })
})

describe('unauthorized reaction', () => {
  // The real handler is the auth context's `handleUnauthorized`, which clears
  // the session. The spy mirrors that so the "no session left" guard is
  // exercised exactly as in production.
  function spyOnUnauthorized() {
    return vi.fn(() => clearSession())
  }

  it('calls the injected handler once and rethrows the ApiError', async () => {
    writeSession(session)
    const onUnauthorized = spyOnUnauthorized()
    setUnauthorizedHandler(onUnauthorized)
    installFetchMock(() =>
      jsonResponse({ title: 'Authentication required', status: 401 }, 401),
    )

    await expect(listTasksPage(session.accessToken)).rejects.toMatchObject<Partial<ApiError>>({
      name: 'ApiError',
      status: 401,
    })

    expect(onUnauthorized).toHaveBeenCalledTimes(1)
    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
  })

  it('does not call the handler when no session exists', async () => {
    const onUnauthorized = vi.fn()
    setUnauthorizedHandler(onUnauthorized)
    installFetchMock(() =>
      jsonResponse({ title: 'Authentication required', status: 401 }, 401),
    )

    await expect(listTasksPage('stale-token')).rejects.toBeInstanceOf(ApiError)

    expect(onUnauthorized).not.toHaveBeenCalled()
  })

  it('calls the handler once across repeated failed attempts instead of looping', async () => {
    writeSession(session)
    const onUnauthorized = spyOnUnauthorized()
    setUnauthorizedHandler(onUnauthorized)
    installFetchMock(() =>
      jsonResponse({ title: 'Authentication required', status: 401 }, 401),
    )

    await expect(listTasksPage(session.accessToken)).rejects.toBeInstanceOf(ApiError)
    await expect(listTasksPage(session.accessToken)).rejects.toBeInstanceOf(ApiError)
    await expect(listTasksPage(session.accessToken)).rejects.toBeInstanceOf(ApiError)

    expect(onUnauthorized).toHaveBeenCalledTimes(1)
    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
  })
})
