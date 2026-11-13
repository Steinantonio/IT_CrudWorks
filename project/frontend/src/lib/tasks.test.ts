import { clearSession, SESSION_STORAGE_KEY, writeSession } from '../auth/session'
import type { Session } from '../auth/session'
import { installFetchMock, jsonResponse } from '../test/http'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from './api'
import {
  createTask,
  getTask,
  listTasks,
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
  it('sends the bearer token and returns the list', async () => {
    writeSession(session)
    const fetchMock = installFetchMock(() => jsonResponse([task]))

    const tasks = await listTasks(session.accessToken)

    expect(tasks).toEqual([task])
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/tasks',
      expect.objectContaining({
        headers: expect.any(Headers),
      }),
    )
    const init = fetchMock.mock.calls[0]?.[1]
    expect(new Headers(init?.headers).get('Authorization')).toBe('Bearer token-abc')
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

    await expect(listTasks(session.accessToken)).rejects.toMatchObject<Partial<ApiError>>({
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

    await expect(listTasks('stale-token')).rejects.toBeInstanceOf(ApiError)

    expect(onUnauthorized).not.toHaveBeenCalled()
  })

  it('calls the handler once across repeated failed attempts instead of looping', async () => {
    writeSession(session)
    const onUnauthorized = spyOnUnauthorized()
    setUnauthorizedHandler(onUnauthorized)
    installFetchMock(() =>
      jsonResponse({ title: 'Authentication required', status: 401 }, 401),
    )

    await expect(listTasks(session.accessToken)).rejects.toBeInstanceOf(ApiError)
    await expect(listTasks(session.accessToken)).rejects.toBeInstanceOf(ApiError)
    await expect(listTasks(session.accessToken)).rejects.toBeInstanceOf(ApiError)

    expect(onUnauthorized).toHaveBeenCalledTimes(1)
    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
  })
})
