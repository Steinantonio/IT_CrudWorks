import { beforeEach, describe, expect, it } from 'vitest'
import { SESSION_STORAGE_KEY, readSession, writeSession } from './session'
import type { Session } from './session'

const session: Session = {
  accessToken: 'token-abc',
  expiresAtUtc: '2030-01-01T00:00:00Z',
  user: {
    id: '0b7f0f8e-6c2f-4a6f-9f2a-1f5a1e3c9d10',
    email: 'demo@crudworks.local',
    displayName: 'Demo User',
  },
}

beforeEach(() => {
  sessionStorage.clear()
})

describe('session storage', () => {
  it('returns null when there is no stored session', () => {
    expect(readSession()).toBeNull()
  })

  it('round-trips a stored session', () => {
    writeSession(session)
    expect(readSession()).toEqual(session)
  })

  it('clears malformed JSON', () => {
    sessionStorage.setItem(SESSION_STORAGE_KEY, '{"accessToken":')

    expect(readSession()).toBeNull()
    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
  })

  it('clears a value that is not a session', () => {
    sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify({ accessToken: 42 }))

    expect(readSession()).toBeNull()
    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
  })

  it('clears an expired session', () => {
    writeSession({ ...session, expiresAtUtc: '2026-11-11T18:30:00Z' })

    expect(readSession(Date.parse('2026-11-11T19:00:00Z'))).toBeNull()
    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
  })

  it('treats an unparsable expiry as expired', () => {
    writeSession({ ...session, expiresAtUtc: 'not-a-date' })

    expect(readSession()).toBeNull()
    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
  })

  it('keeps a session that has not expired yet', () => {
    writeSession(session)

    expect(readSession(Date.parse('2029-12-31T23:59:59Z'))).not.toBeNull()
  })
})
