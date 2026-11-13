import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { AuthProvider } from './AuthProvider'
import { useAuth } from './authContext'
import { SESSION_STORAGE_KEY } from './session'
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

function SessionProbe() {
  const { isAuthenticated, handleUnauthorized } = useAuth()

  return (
    <>
      <p>{isAuthenticated ? 'Authenticated' : 'Anonymous'}</p>
      <button type="button" onClick={handleUnauthorized}>
        Expire session
      </button>
    </>
  )
}

describe('AuthProvider', () => {
  it('clears the session once when a protected call reports an unauthorized response', async () => {
    sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session))
    const user = userEvent.setup()

    render(
      <AuthProvider>
        <SessionProbe />
      </AuthProvider>,
    )

    expect(screen.getByText('Authenticated')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Expire session' }))
    await user.click(screen.getByRole('button', { name: 'Expire session' }))

    expect(screen.getByText('Anonymous')).toBeInTheDocument()
    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
  })
})
