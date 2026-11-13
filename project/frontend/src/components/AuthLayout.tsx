import type { ReactNode } from 'react'

interface AuthLayoutProps {
  eyebrow: string
  heading: string
  headingId: string
  children: ReactNode
}

/**
 * Shared shell for the public auth screens. On wide viewports the brand story
 * sits on the left and the form card on the right; below 860px the intro is
 * removed and the card is centred as a single column.
 *
 * The `<main>` landmark keeps `tabIndex={-1}` so the existing route-change
 * focus behavior (see `Layout`) still targets the new page content.
 */
export default function AuthLayout({
  eyebrow,
  heading,
  headingId,
  children,
}: AuthLayoutProps) {
  return (
    <main className="auth-page" tabIndex={-1}>
      <div className="auth-shell">
        <aside className="auth-intro" aria-label="About CrudWorks">
          <p className="auth-intro-kicker">Small tasks, clear progress</p>
          <p className="auth-intro-title">Keep the work moving.</p>
          <p className="auth-intro-text">
            One calm place to capture tasks, track what is in progress, and close
            the loop.
          </p>

          <ul className="auth-points">
            <li>Know what is next at a glance.</li>
            <li>Group work by Todo, In progress, and Done.</li>
            <li>Your list stays private to your account.</li>
          </ul>
        </aside>

        <section className="auth-card" aria-labelledby={headingId}>
          <p className="eyebrow">{eyebrow}</p>
          <h1 id={headingId}>{heading}</h1>
          {children}
        </section>
      </div>
    </main>
  )
}
