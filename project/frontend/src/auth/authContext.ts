import { createContext, useContext } from 'react'
import type { Session, SessionUser } from './session'
import type { components } from '../api/generated/openapi'

export type RegisteredUser = components['schemas']['AuthUserResponse']

export interface AuthContextValue {
  session: Session | null
  user: SessionUser | null
  isAuthenticated: boolean
  /** Calls `POST /api/auth/login` and persists the returned session. */
  login: (email: string, password: string) => Promise<void>
  /**
   * Calls `POST /api/auth/register`. The API returns the created user, not a
   * token, so the caller must send the user to the login screen.
   */
  register: (displayName: string, email: string, password: string) => Promise<RegisteredUser>
  /**
   * Clears the session. Navigation is the caller's responsibility so the
   * provider stays independent of the router.
   */
  logout: () => void
  /**
   * Reaction to a `401` from a protected call: clears the session so
   * `RequireAuth` redirects to `/login` on the next render. Idempotent.
   */
  handleUnauthorized: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth(): AuthContextValue {
  const value = useContext(AuthContext)

  if (!value) {
    throw new Error('useAuth must be used inside an AuthProvider.')
  }

  return value
}
