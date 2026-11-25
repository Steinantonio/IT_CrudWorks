import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from './auth/AuthProvider'
import AppRoutes from './AppRoutes'
import AppErrorBoundary from './components/AppErrorBoundary'
import { shouldRetryQuery } from './lib/queryRetry'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // A `401` means the session is gone, a `400` is a client error, and a
      // stale cursor is reset by the list instead of retried. See queryRetry.
      retry: shouldRetryQuery,
      staleTime: 30_000,
    },
  },
})

export default function App() {
  return (
    <AppErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <AuthProvider queryClient={queryClient}>
            <AppRoutes />
          </AuthProvider>
        </BrowserRouter>
      </QueryClientProvider>
    </AppErrorBoundary>
  )
}
