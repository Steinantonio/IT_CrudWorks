import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { SESSION_STORAGE_KEY } from '../auth/session'
import { installFetchMock, jsonResponse } from '../test/http'
import { renderApp } from '../test/renderApp'

const loginResponse = {
  accessToken: 'token-abc',
  expiresAtUtc: '2030-01-01T00:00:00Z',
  user: {
    id: '0b7f0f8e-6c2f-4a6f-9f2a-1f5a1e3c9d10',
    email: 'demo@crudworks.local',
    displayName: 'Demo User',
  },
}

function storedAccessToken(): string | null {
  const raw = sessionStorage.getItem(SESSION_STORAGE_KEY)
  if (!raw) {
    return null
  }

  const parsed = JSON.parse(raw) as { accessToken?: string }
  return parsed.accessToken ?? null
}

describe('LoginPage', () => {
  it('stores the session and returns to the originally requested route', async () => {
    const fetchMock = installFetchMock((url) => {
      if (url === '/api/auth/login') {
        return jsonResponse(loginResponse)
      }

      if (url === '/api/tasks') {
        return jsonResponse([])
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp()

    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument()

    await user.type(screen.getByLabelText('Email'), 'demo@crudworks.local')
    await user.type(screen.getByLabelText('Password'), 'Secret123')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('heading', { name: 'Tasks' })).toBeInTheDocument()
    expect(screen.getByText('Demo User')).toBeInTheDocument()
    expect(storedAccessToken()).toBe('token-abc')
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/auth/login',
      expect.objectContaining({ method: 'POST' }),
    )
  })

  it.each([
    ['protocol-relative', '//evil.example'],
    ['absolute-url', 'https://evil.example'],
  ])('ignores an external return path (%s) and lands on the home route', async (_label, from) => {
    installFetchMock((url) => {
      if (url === '/api/auth/login') {
        return jsonResponse(loginResponse)
      }

      if (url === '/api/tasks') {
        return jsonResponse([])
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: [{ pathname: '/login', state: { from } }] })

    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument()

    await user.type(screen.getByLabelText('Email'), 'demo@crudworks.local')
    await user.type(screen.getByLabelText('Password'), 'Secret123')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('heading', { name: 'Tasks' })).toBeInTheDocument()
    expect(screen.getByText('Demo User')).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Page not found' })).not.toBeInTheDocument()
    expect(storedAccessToken()).toBe('token-abc')
  })

  it('shows a safe message on invalid credentials and keeps the email', async () => {
    installFetchMock((url) => {
      if (url === '/api/auth/login') {
        return jsonResponse({ title: 'Invalid email or password', status: 401 }, 401)
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/login'] })

    const email = screen.getByLabelText('Email')
    await user.type(email, 'demo@crudworks.local')
    await user.type(screen.getByLabelText('Password'), 'WrongPassword1')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid email or password.')
    expect(email).toHaveValue('demo@crudworks.local')
    expect(screen.getByRole('button', { name: 'Sign in' })).toBeEnabled()
    expect(storedAccessToken()).toBeNull()
  })

  it('validates the fields before sending a request', async () => {
    const fetchMock = installFetchMock(() => {
      throw new Error('No request expected')
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/login'] })
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByText('Enter your password.')).toBeInTheDocument()
    expect(screen.getByText('Enter a valid email address.')).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('submits with Enter from the password field', async () => {
    const fetchMock = installFetchMock((url) => {
      if (url === '/api/auth/login') {
        return jsonResponse({ title: 'Invalid email or password', status: 401 }, 401)
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/login'] })
    await user.type(screen.getByLabelText('Email'), 'demo@crudworks.local')
    await user.type(screen.getByLabelText('Password'), 'WrongPassword1{Enter}')

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid email or password.')
    expect(fetchMock).toHaveBeenCalledWith('/api/auth/login', expect.anything())
  })

  it('reports a network failure and keeps the email', async () => {
    installFetchMock(() => {
      throw new TypeError('Failed to fetch')
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/login'] })

    const email = screen.getByLabelText('Email')
    await user.type(email, 'demo@crudworks.local')
    await user.type(screen.getByLabelText('Password'), 'Secret123')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Could not reach the server. Check your connection and try again.',
    )
    expect(email).toHaveValue('demo@crudworks.local')
  })

  it('walks the form controls with Tab in the visual order', async () => {
    installFetchMock(() => {
      throw new Error('No request expected')
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/login'] })

    screen.getByLabelText('Email').focus()

    await user.tab()
    expect(screen.getByLabelText('Password')).toHaveFocus()

    await user.tab()
    expect(screen.getByRole('button', { name: 'Sign in' })).toHaveFocus()

    await user.tab()
    expect(screen.getByRole('link', { name: 'Create an account' })).toHaveFocus()
  })

  it('fills the demo credentials with the demo account button', async () => {
    const fetchMock = installFetchMock(() => {
      throw new Error('No request expected')
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/login'] })

    expect(screen.getByText('Demo access')).toBeInTheDocument()
    expect(screen.getByText('demo@crudworks.local')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Use demo account' }))

    expect(screen.getByLabelText('Email')).toHaveValue('demo@crudworks.local')
    expect(screen.getByLabelText('Password')).toHaveValue('Demo123!')
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('renders the auth screen inside a single main landmark', () => {
    renderApp({ entries: ['/login'] })

    expect(screen.getAllByRole('main')).toHaveLength(1)
  })

  it('returns to the route the auth guard interrupted and keeps the focus on the page', async () => {
    installFetchMock((url) => {
      if (url === '/api/auth/login') {
        return jsonResponse(loginResponse)
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/tasks/new'] })

    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
    expect(screen.getByRole('main')).toHaveFocus()

    await user.type(screen.getByLabelText('Email'), 'demo@crudworks.local')
    await user.type(screen.getByLabelText('Password'), 'Secret123')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('heading', { name: 'New task' })).toBeInTheDocument()
    expect(screen.getByRole('main')).toHaveFocus()
  })
})
