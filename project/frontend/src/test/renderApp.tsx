import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import type { RenderResult } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import type { InitialEntry } from 'react-router-dom'
import AppRoutes from '../AppRoutes'
import { AuthProvider } from '../auth/AuthProvider'
import AppErrorBoundary from '../components/AppErrorBoundary'

interface RenderAppOptions {
  entries?: InitialEntry[]
  queryClient?: QueryClient
}

export function createTestQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  })
}

export function renderApp({ entries = ['/'], queryClient }: RenderAppOptions = {}): RenderResult {
  return render(
    <QueryClientProvider client={queryClient ?? createTestQueryClient()}>
      <MemoryRouter initialEntries={entries}>
        <AuthProvider>
          <AppErrorBoundary>
            <AppRoutes />
          </AppErrorBoundary>
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

/** Waits for the lazy route chunk to render a page with a heading. */
export async function waitForPage(): Promise<void> {
  await screen.findByRole('heading')
}
