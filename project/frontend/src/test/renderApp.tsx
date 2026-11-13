import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import type { RenderResult } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import type { InitialEntry } from 'react-router-dom'
import AppRoutes from '../AppRoutes'
import { AuthProvider } from '../auth/AuthProvider'

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
          <AppRoutes />
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}
