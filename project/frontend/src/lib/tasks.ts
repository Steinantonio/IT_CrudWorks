import { ApiError, apiRequest } from './api'
import { readSession } from '../auth/session'
import type { components } from '../api/generated/openapi'

export type TaskStatus = components['schemas']['TaskItemStatus']

export const TASK_STATUS_VALUES = ['Todo', 'InProgress', 'Done'] as const satisfies readonly TaskStatus[]

export type Task = components['schemas']['TaskResponse']

/** One page of tasks as defined by ADR-0006 (`GET /api/tasks`). */
export type TaskPage = components['schemas']['TaskPageResponse']

/** Create payload as defined by ADR-0004 (`POST /api/tasks`). */
export type TaskCreateInput = components['schemas']['CreateTaskRequest']

/** Full-replacement payload as defined by ADR-0004 (`PUT /api/tasks/{id}`). */
export type TaskUpdateInput = components['schemas']['UpdateTaskRequest']

export const taskKeys = {
  all: ['tasks'] as const,
  /** Matches every page/generation of one user's list, and nothing else. */
  listRoot: (userId: string) => ['tasks', 'list', userId] as const,
  list: (userId: string, generation = 0) =>
    ['tasks', 'list', userId, generation] as const,
  detail: (id: string) => ['tasks', 'detail', id] as const,
}

type UnauthorizedHandler = () => void

let unauthorizedHandler: UnauthorizedHandler | null = null

/**
 * Injects the reaction to a `401` from a protected call. The handler lives in
 * the auth context, which is outside React's reach from this module, so
 * `AuthProvider` registers it once on mount.
 */
export function setUnauthorizedHandler(handler: UnauthorizedHandler | null): void {
  unauthorizedHandler = handler
}

function notifyUnauthorized(): void {
  // Only notify while there is still a session to clear. This keeps the
  // reaction to one call even when a query retry repeats the failed request.
  if (unauthorizedHandler && readSession()) {
    unauthorizedHandler()
  }
}

async function taskRequest<T>(
  path: string,
  token: string,
  init: RequestInit = {},
): Promise<T> {
  try {
    return await apiRequest<T>(path, { token, ...init })
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      notifyUnauthorized()
    }

    throw error
  }
}

export interface TaskPageOptions {
  cursor?: string
  take?: number
}

/**
 * The server rejected a cursor the client sent. The list discards the loaded
 * pages and refetches from the first page; the cursor is never retried as-is.
 */
export class CursorExpiredError extends Error {
  readonly cursor: string

  constructor(cursor: string) {
    super('The task cursor is no longer valid.')
    this.name = 'CursorExpiredError'
    this.cursor = cursor
  }
}

export async function listTasksPage(
  token: string,
  options: TaskPageOptions = {},
): Promise<TaskPage> {
  const params = new URLSearchParams()
  if (options.take !== undefined) {
    params.set('take', String(options.take))
  }
  if (options.cursor !== undefined) {
    params.set('cursor', options.cursor)
  }

  const query = params.toString()
  const path = `/api/tasks${query ? `?${query}` : ''}`

  try {
    return await taskRequest<TaskPage>(path, token)
  } catch (error) {
    if (options.cursor !== undefined && error instanceof ApiError && error.status === 400) {
      throw new CursorExpiredError(options.cursor)
    }

    throw error
  }
}

export function getTask(token: string, id: string): Promise<Task> {
  return taskRequest<Task>(`/api/tasks/${encodeURIComponent(id)}`, token)
}

export function createTask(token: string, input: TaskCreateInput): Promise<Task> {
  return taskRequest<Task>('/api/tasks', token, {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function updateTask(token: string, id: string, input: TaskUpdateInput): Promise<Task> {
  return taskRequest<Task>(`/api/tasks/${encodeURIComponent(id)}`, token, {
    method: 'PUT',
    body: JSON.stringify(input),
  })
}

export function removeTask(token: string, id: string): Promise<void> {
  return taskRequest<void>(`/api/tasks/${encodeURIComponent(id)}`, token, {
    method: 'DELETE',
  })
}
