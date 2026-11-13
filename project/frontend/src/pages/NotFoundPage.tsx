import { Link } from 'react-router-dom'

export default function NotFoundPage() {
  return (
    <main className="page" tabIndex={-1}>
      <h1>Page not found</h1>
      <p>The page you requested does not exist.</p>
      <Link to="/">Back to CrudWorks</Link>
    </main>
  )
}
