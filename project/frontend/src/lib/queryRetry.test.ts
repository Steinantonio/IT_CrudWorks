import { describe, expect, it } from 'vitest'
import { ApiError } from './api'
import { CursorExpiredError } from './tasks'
import { shouldRetryQuery } from './queryRetry'

describe('shouldRetryQuery', () => {
  it('does not retry a 401', () => {
    expect(shouldRetryQuery(0, new ApiError(401, 'Authentication required'))).toBe(false)
  })

  it('does not retry a 400', () => {
    expect(shouldRetryQuery(0, new ApiError(400, 'Page size must be between 1 and 100.'))).toBe(false)
  })

  it('does not retry a stale cursor', () => {
    expect(shouldRetryQuery(0, new CursorExpiredError('stale-cursor'))).toBe(false)
  })

  it('retries another failure once', () => {
    expect(shouldRetryQuery(0, new Error('network'))).toBe(true)
    expect(shouldRetryQuery(1, new Error('network'))).toBe(false)
  })
})
