import { Suspense, lazy } from 'react'
import type { ReactElement } from 'react'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import AppErrorBoundary from './components/AppErrorBoundary'
import RouteLoading from './components/RouteLoading'

const NeverLoads = lazy(
  () => new Promise<{ default: () => ReactElement }>(() => {}),
)

describe('RouteLoading', () => {
  it('announces the loading state while a route chunk resolves', () => {
    render(
      <Suspense fallback={<RouteLoading />}>
        <NeverLoads />
      </Suspense>,
    )

    expect(screen.getByRole('status')).toHaveTextContent('Loading…')
  })
})

describe('AppErrorBoundary', () => {
  it('shows a safe fallback and recovers when the user retries', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    let shouldThrow = true

    function Boom(): ReactElement {
      if (shouldThrow) {
        throw new Error('render exploded')
      }

      return <p>Recovered</p>
    }

    const user = userEvent.setup()
    render(
      <AppErrorBoundary>
        <Boom />
      </AppErrorBoundary>,
    )

    expect(screen.getByRole('heading', { name: /something went wrong/i })).toBeInTheDocument()
    expect(screen.queryByText(/render exploded/i)).not.toBeInTheDocument()

    shouldThrow = false
    await user.click(screen.getByRole('button', { name: /try again/i }))

    expect(await screen.findByText('Recovered')).toBeInTheDocument()
    consoleError.mockRestore()
  })
})
