export const SESSION_STORAGE_KEY = 'crudworks.session'

export interface SessionUser {
  id: string
  email: string
  displayName: string
}

export interface Session {
  accessToken: string
  expiresAtUtc: string
  user: SessionUser
}

function getStorage(): Storage | null {
  try {
    return window.sessionStorage
  } catch {
    return null
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function isSessionUser(value: unknown): value is SessionUser {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    typeof value.email === 'string' &&
    typeof value.displayName === 'string'
  )
}

export function isSession(value: unknown): value is Session {
  return (
    isRecord(value) &&
    typeof value.accessToken === 'string' &&
    value.accessToken.length > 0 &&
    typeof value.expiresAtUtc === 'string' &&
    isSessionUser(value.user)
  )
}

function isExpired(session: Session, now: number): boolean {
  const expiresAt = Date.parse(session.expiresAtUtc)
  return Number.isNaN(expiresAt) || expiresAt <= now
}

export function clearSession(): void {
  getStorage()?.removeItem(SESSION_STORAGE_KEY)
}

export function writeSession(session: Session): void {
  getStorage()?.setItem(SESSION_STORAGE_KEY, JSON.stringify(session))
}

export function readSession(now: number = Date.now()): Session | null {
  const storage = getStorage()
  const raw = storage?.getItem(SESSION_STORAGE_KEY)
  if (!storage || !raw) {
    return null
  }

  let parsed: unknown
  try {
    parsed = JSON.parse(raw)
  } catch {
    clearSession()
    return null
  }

  if (!isSession(parsed) || isExpired(parsed, now)) {
    clearSession()
    return null
  }

  return parsed
}
