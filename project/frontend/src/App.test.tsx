import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { SESSION_STORAGE_KEY } from './auth/session'
import type { Session } from './auth/session'
import NotFoundPage from './pages/NotFoundPage'
import { installFetchMock, jsonResponse } from './test/http'
import { renderApp } from './test/renderApp'

const session: Session = {
  accessToken: 'token-abc',
  expiresAtUtc: '2030-01-01T00:00:00Z',
  user: {
    id: '0b7f0f8e-6c2f-4a6f-9f2a-1f5a1e3c9d10',
    email: 'demo@crudworks.local',
    displayName: 'Demo User',
  },
}

describe('AppRoutes', () => {
  it('renders the not found page for unknown routes', () => {
    renderApp({ entries: ['/missing'] })

    expect(screen.getByRole('heading', { name: 'Page not found' })).toBeInTheDocument()
  })

  it('shows the signed-in user and logs out back to login', async () => {
    sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session))
    installFetchMock((url) => {
      if (url === '/api/tasks') {
        return jsonResponse([])
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp()

    expect(await screen.findByText('Demo User')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Log out' }))

    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
  })
})

describe('NotFoundPage', () => {
  it('offers a way back to the application', () => {
    render(
      <MemoryRouter>
        <NotFoundPage />
      </MemoryRouter>,
    )

    expect(screen.getByRole('heading', { name: /page not found/i })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /back to crudworks/i })).toHaveAttribute('href', '/')
  })
})
