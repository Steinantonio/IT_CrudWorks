import { ApiError, apiRequest } from './api'
import { readSession } from '../auth/session'

export const TASK_STATUS_VALUES = ['Todo', 'InProgress', 'Done'] as const

export type TaskStatus = (typeof TASK_STATUS_VALUES)[number]

export interface Task {
  id: string
  title: string
  description: string | null
  status: TaskStatus
  dueDate: string | null
  createdAtUtc: string
  updatedAtUtc: string
}

/** Editable task fields as defined by ADR-0004 (`PUT` replaces all of them). */
export interface TaskInput {
  title: string
  description: string | null
  status: TaskStatus
  dueDate: string | null
}

export const taskKeys = {
  all: ['tasks'] as const,
  detail: (id: string) => ['tasks', id] as const,
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

export function listTasks(token: string): Promise<Task[]> {
  return taskRequest<Task[]>('/api/tasks', token)
}

export function getTask(token: string, id: string): Promise<Task> {
  return taskRequest<Task>(`/api/tasks/${encodeURIComponent(id)}`, token)
}

export function createTask(token: string, input: TaskInput): Promise<Task> {
  return taskRequest<Task>('/api/tasks', token, {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function updateTask(token: string, id: string, input: TaskInput): Promise<Task> {
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
