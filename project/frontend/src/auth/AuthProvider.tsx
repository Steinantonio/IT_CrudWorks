import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { apiRequest } from '../lib/api'
import { setUnauthorizedHandler } from '../lib/tasks'
import { AuthContext } from './authContext'
import type { AuthContextValue, RegisteredUser } from './authContext'
import { clearSession, isSession, readSession, writeSession } from './session'
import type { Session, SessionUser } from './session'

interface LoginResponse {
  accessToken: string
  expiresAtUtc: string
  user: SessionUser
}

interface AuthProviderProps {
  children: ReactNode
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [session, setSession] = useState<Session | null>(() => readSession())

  const login = useCallback(async (email: string, password: string) => {
    const response = await apiRequest<LoginResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    })

    const nextSession: Session = {
      accessToken: response.accessToken,
      expiresAtUtc: response.expiresAtUtc,
      user: response.user,
    }

    if (!isSession(nextSession)) {
      throw new Error('The server returned an invalid session.')
    }

    writeSession(nextSession)
    setSession(nextSession)
  }, [])

  const register = useCallback(
    async (displayName: string, email: string, password: string) => {
      return apiRequest<RegisteredUser>('/api/auth/register', {
        method: 'POST',
        body: JSON.stringify({ displayName, email, password }),
      })
    },
    [],
  )

  const logout = useCallback(() => {
    clearSession()
    setSession(null)
  }, [])

  const handleUnauthorized = useCallback(() => {
    clearSession()
    setSession(null)
  }, [])

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
