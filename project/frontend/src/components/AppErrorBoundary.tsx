import { Component } from 'react'
import type { ErrorInfo, ReactNode } from 'react'

interface AppErrorBoundaryProps {
  children: ReactNode
}

interface AppErrorBoundaryState {
  hasError: boolean
}

/**
 * Top-level safety net for unexpected render errors. It deliberately catches
 * render errors only; rejected API calls and event-handler errors keep their
 * existing component-level handling.
 */
export default class AppErrorBoundary extends Component<AppErrorBoundaryProps, AppErrorBoundaryState> {
  state: AppErrorBoundaryState = { hasError: false }

  static getDerivedStateFromError(): AppErrorBoundaryState {
    return { hasError: true }
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    if (import.meta.env.DEV) {
      console.error('Unexpected render error.', error, info)
    }
  }

  private handleRetry = (): void => {
    this.setState({ hasError: false })
  }

  render(): ReactNode {
    if (this.state.hasError) {
      return (
        <main className="page" tabIndex={-1}>
          <div className="error-state" role="alert">
            <h1>Something went wrong</h1>
            <p>We could not display this page. Try again, or reload the application.</p>
            <button type="button" className="button" onClick={this.handleRetry}>
              Try again
            </button>
            <button
              type="button"
              className="button button-quiet"
              onClick={() => window.location.reload()}
            >
              Reload
            </button>
          </div>
        </main>
      )
    }

    return this.props.children
  }
}
