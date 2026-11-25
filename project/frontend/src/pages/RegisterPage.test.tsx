import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { SESSION_STORAGE_KEY } from '../auth/session'
import { installFetchMock, jsonResponse } from '../test/http'
import { renderApp, waitForPage } from '../test/renderApp'

async function fillValidForm(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText('Display name'), 'Demo User')
  await user.type(screen.getByLabelText('Email'), 'demo@crudworks.local')
  await user.type(screen.getByLabelText('Password'), 'Secret123')
}

describe('RegisterPage', () => {
  it('shows the duplicate email message on 409 and keeps the input', async () => {
    installFetchMock((url) => {
      if (url === '/api/auth/register') {
        return jsonResponse({ title: 'Email already registered', status: 409 }, 409)
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/register'] })
    await waitForPage()
    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: 'Create account' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'That email is already registered.',
    )
    expect(screen.getByLabelText('Email')).toHaveValue('demo@crudworks.local')
    expect(screen.getByLabelText('Display name')).toHaveValue('Demo User')
  })

  it('returns to login with a success message after registering', async () => {
    installFetchMock((url) => {
      if (url === '/api/auth/register') {
        return jsonResponse(
          {
            id: '0b7f0f8e-6c2f-4a6f-9f2a-1f5a1e3c9d10',
            email: 'demo@crudworks.local',
            displayName: 'Demo User',
            createdAtUtc: '2026-11-11T18:30:00Z',
          },
          201,
        )
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/register'] })
    await waitForPage()
    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: 'Create account' }))

    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
    expect(screen.getByRole('status')).toHaveTextContent('Your account is ready')
    expect(screen.getByLabelText('Email')).toHaveValue('demo@crudworks.local')
    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
  })

  it('maps server field errors onto the matching input', async () => {
    installFetchMock((url) => {
      if (url === '/api/auth/register') {
        return jsonResponse(
          {
            title: 'One or more validation errors occurred.',
            status: 400,
            errors: { email: ['Email must be a valid address.'] },
          },
          400,
        )
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/register'] })
    await waitForPage()
    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: 'Create account' }))

    expect(await screen.findByText('Email must be a valid address.')).toBeInTheDocument()
    expect(screen.getByLabelText('Email')).toHaveAttribute(
      'aria-describedby',
      'register-email-error',
    )
  })

  it('links client-side validation messages to their inputs', async () => {
    const fetchMock = installFetchMock(() => {
      throw new Error('No request expected')
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/register'] })
    await waitForPage()
    await user.click(screen.getByRole('button', { name: 'Create account' }))

    expect(
      await screen.findByText('Display name must be between 2 and 100 characters.'),
    ).toBeInTheDocument()
    expect(screen.getByLabelText('Display name')).toHaveAttribute(
      'aria-describedby',
      'register-display-name-error',
    )
    expect(screen.getByLabelText('Password')).toHaveAttribute(
      'aria-describedby',
      'register-password-hint register-password-error',
    )
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('walks the form controls with Tab in the visual order', async () => {
    installFetchMock(() => {
      throw new Error('No request expected')
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/register'] })
    await waitForPage()

    screen.getByLabelText('Display name').focus()

    await user.tab()
    expect(screen.getByLabelText('Email')).toHaveFocus()

    await user.tab()
    expect(screen.getByLabelText('Password')).toHaveFocus()

    await user.tab()
    expect(screen.getByRole('button', { name: 'Create account' })).toHaveFocus()

    await user.tab()
    expect(screen.getByRole('link', { name: 'Sign in' })).toHaveFocus()
  })

  it('submits with Enter from the password field', async () => {
    const fetchMock = installFetchMock((url) => {
      if (url === '/api/auth/register') {
        return jsonResponse(
          {
            id: '0b7f0f8e-6c2f-4a6f-9f2a-1f5a1e3c9d10',
            email: 'demo@crudworks.local',
            displayName: 'Demo User',
            createdAtUtc: '2026-11-11T18:30:00Z',
          },
          201,
        )
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/register'] })
    await waitForPage()
    await user.type(screen.getByLabelText('Display name'), 'Demo User')
    await user.type(screen.getByLabelText('Email'), 'demo@crudworks.local')
    await user.type(screen.getByLabelText('Password'), 'Secret123{Enter}')

    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/auth/register',
      expect.objectContaining({ method: 'POST' }),
    )
  })

  it('accepts a password that contains a non-ASCII letter and a digit (mirrors the API rule)', async () => {
    // The API rule is password.Any(char.IsLetter) && password.Any(char.IsDigit),
    // and char.IsLetter is true for Unicode letters. The client must mirror
    // that, so a password such as "senhaé1" is accepted rather than rejected.
    installFetchMock((url) => {
      if (url === '/api/auth/register') {
        return jsonResponse(
          {
            id: '0b7f0f8e-6c2f-4a6f-9f2a-1f5a1e3c9d10',
            email: 'demo@crudworks.local',
            displayName: 'Demo User',
            createdAtUtc: '2026-11-11T18:30:00Z',
          },
          201,
        )
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    const user = userEvent.setup()

    renderApp({ entries: ['/register'] })
    await waitForPage()
    await user.type(screen.getByLabelText('Display name'), 'Demo User')
    await user.type(screen.getByLabelText('Email'), 'demo@crudworks.local')
    await user.type(screen.getByLabelText('Password'), 'senhaé12')
    await user.click(screen.getByRole('button', { name: 'Create account' }))

    // The password passes client-side validation, so no field error is shown
    // and the submit reaches the API (which would also accept it).
    expect(screen.queryByRole('alert')).toBeNull()
    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
  })
})
