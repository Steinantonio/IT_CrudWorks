import { Suspense, useEffect, useRef } from 'react'
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/authContext'
import RouteLoading from './RouteLoading'

export default function Layout() {
  const { isAuthenticated, user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const previousPath = useRef(location.pathname)

  // A client-side route change leaves the focus on the element that was
  // focused before the navigation, which is often removed from the page.
  // Moving the focus to the main landmark keeps the keyboard and the screen
  // reader on the new content (after the auth guard redirects, for example).
  useEffect(() => {
    if (previousPath.current === location.pathname) {
      return
    }

    previousPath.current = location.pathname
    document.querySelector<HTMLElement>('main')?.focus()
  }, [location.pathname])

  function handleLogout() {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <>
      <header className="app-header">
        <div className="app-header-inner">
          <Link className="brand" to="/">
            <span className="brand-mark" aria-hidden="true">
              C
            </span>
            <span className="brand-name">CrudWorks</span>
          </Link>

          {isAuthenticated && user && (
            <div className="session-bar">
              <span className="session-user">{user.displayName}</span>
              <button type="button" className="button button-quiet" onClick={handleLogout}>
                Log out
              </button>
            </div>
          )}
        </div>
      </header>

      <Suspense fallback={<RouteLoading />}>
        <Outlet />
      </Suspense>
    </>
  )
}
