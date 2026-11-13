import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider } from './AuthProvider'
import RequireAuth from './RequireAuth'

function LoginProbe() {
  const location = useLocation()
  const state = location.state as { from?: string } | null
  return <p>Redirected from {state?.from ?? 'unknown'}</p>
}

describe('RequireAuth', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn())
  })

  it('redirects an unauthenticated visitor to login and preserves the target path', () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <AuthProvider>
          <Routes>
            <Route path="/login" element={<LoginProbe />} />
            <Route
              path="/"
              element={
                <RequireAuth>
                  <p>Protected content</p>
                </RequireAuth>
              }
            />
          </Routes>
        </AuthProvider>
      </MemoryRouter>,
    )

    expect(screen.getByText('Redirected from /')).toBeInTheDocument()
    expect(screen.queryByText('Protected content')).not.toBeInTheDocument()
    expect(fetch).not.toHaveBeenCalled()
  })
})
