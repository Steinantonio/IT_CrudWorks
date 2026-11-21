import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import type { QueryClient } from '@tanstack/react-query'
import { apiRequest } from '../lib/api'
import { setUnauthorizedHandler } from '../lib/tasks'
import { AuthContext } from './authContext'
import type { AuthContextValue, RegisteredUser } from './authContext'
import { clearSession, isSession, readSession, writeSession } from './session'
import type { Session } from './session'
import type { components } from '../api/generated/openapi'

type LoginResponse = components['schemas']['LoginResponse']
type LoginRequest = components['schemas']['LoginRequest']
type RegisterRequest = components['schemas']['RegisterRequest']

interface AuthProviderProps {
  children: ReactNode
  /**
   * Optional React Query client. When present (production app), the provider
   * clears the server-state cache on login/logout/unauthorized so a previous
   * account's tasks never leak into the next user's view. Tests may omit it.
   */
  queryClient?: QueryClient
}

export function AuthProvider({ children, queryClient }: AuthProviderProps) {
  const [session, setSession] = useState<Session | null>(() => readSession())

  const login = useCallback(async (email: string, password: string) => {
    const loginRequest: LoginRequest = { email, password }
    const response = await apiRequest<LoginResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify(loginRequest),
    })

    const nextSession: Session = {
      accessToken: response.accessToken,
      expiresAtUtc: response.expiresAtUtc,
      user: response.user,
    }

    if (!isSession(nextSession)) {
      throw new Error('The server returned an invalid session.')
    }

    // A different account may have been cached under the previous session; wipe
    // server state so no task from another user leaks into this view.
    queryClient?.clear()
    writeSession(nextSession)
    setSession(nextSession)
  }, [queryClient])

  const register = useCallback(
    async (displayName: string, email: string, password: string) => {
      const registerRequest: RegisterRequest = { displayName, email, password }
      return apiRequest<RegisteredUser>('/api/auth/register', {
        method: 'POST',
        body: JSON.stringify(registerRequest),
      })
    },
    [],
  )

  const logout = useCallback(() => {
    clearSession()
    queryClient?.clear()
    setSession(null)
  }, [queryClient])

  const handleUnauthorized = useCallback(() => {
    clearSession()
    queryClient?.clear()
    setSession(null)
  }, [queryClient])

  // Task functions live outside React, so the context hands them the `401`
  // reaction once per provider lifetime.
  useEffect(() => {
    setUnauthorizedHandler(handleUnauthorized)
    return () => setUnauthorizedHandler(null)
  }, [handleUnauthorized])

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      user: session?.user ?? null,
      isAuthenticated: session !== null,
      login,
      register,
      logout,
      handleUnauthorized,
    }),
    [session, login, register, logout, handleUnauthorized],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>

}
