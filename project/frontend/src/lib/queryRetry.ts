import { ApiError } from './api'
import { CursorExpiredError } from './tasks'

/**
 * Shared query retry policy. A `401` means the session is gone, a `400` is a
 * client error, and a stale cursor is reset by the list instead of retried.
 * Every other failure gets one retry.
 */
export function shouldRetryQuery(failureCount: number, error: unknown): boolean {
  if (error instanceof ApiError && (error.status === 401 || error.status === 400)) {
    return false
  }

  if (error instanceof CursorExpiredError) {
    return false
  }

  return failureCount < 1
}
