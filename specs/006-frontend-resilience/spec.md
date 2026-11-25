# SPEC-006 — Frontend resilience (lazy routes and error boundary)

- **Status:** implemented
- **Owner:** architect
- **Approved by:** project owner
- **Requirements:** R-14, R-15, R-17
- **ADRs:** 0005, 0007
- **Last updated:** 2026-11-23

## 1. Problem

The SPA imports every screen eagerly in `AppRoutes` and has no top-level React
error boundary. A render-time exception can blank the application, and every
screen is part of the initial bundle even when the user never opens it. Wave 3/A7
of ADR-0007 adds route-level code splitting and one safe fallback for unexpected
render errors without changing routes, auth guards, or API behavior.

## 2. Scope

**In scope**

- Convert page imports in `project/frontend/src/AppRoutes.tsx` to `React.lazy`.
- Wrap the route tree in `Suspense` with an accessible loading fallback.
- Add a top-level error boundary with a safe message and a retry/reset path.
- Keep all current routes, `RequireAuth` guards, redirects, and navigation
  behavior unchanged.
- Add tests for the loading fallback, boundary fallback/recovery, and route
  rendering; verify the production build emits route chunks.
- Record bundle-size before/after and keep the change only if the split is real.

**Out of scope**

- API, OpenAPI, DTO, auth, validation, or status-code changes.
- Redesigning pages, changing copy, or adding a design system.
- Catching rejected promises, event-handler errors, or API errors in the error
  boundary; existing query/error states remain responsible for those.
- CI/CD or deployment changes; CI remains deferred by ADR-0007.
- Suspense-based data fetching, server rendering, or route loaders.

## 3. Actors and glossary

| Term | Meaning |
| --- | --- |
| Route chunk | A JavaScript chunk emitted by Vite for a lazily imported page. |
| Loading fallback | The accessible UI shown while a route chunk loads. |
| Error boundary | A React class component that catches render-time errors in its subtree. |
| Retry/reset | A user action that clears the boundary error and re-renders the route tree. |

## 4. User stories

| ID | Story |
| --- | --- |
| US-006-01 | As a user, I want the first screen to load without downloading every screen so that the app opens faster. |
| US-006-02 | As a user, I want a clear recovery screen when a rendering error occurs instead of a blank page. |
| US-006-03 | As a maintainer, I want the existing routes and auth guards to keep working exactly as before. |

## 5. Acceptance criteria

### AC-006-01 — Routes load on demand

- **Given** the production frontend build
- **When** the route chunks are inspected
- **Then** each page module is emitted as a separate chunk and the initial entry
  does not statically include the non-initial screens.
- **And** navigating to `/`, `/tasks/new`, `/tasks/:id/edit`, `/login`,
  `/register`, and an unknown route renders the same content as before.

### AC-006-02 — Loading state is accessible

- **Given** a route chunk is still loading
- **When** the fallback renders
- **Then** it exposes a visible status message through `role="status"` (or an
  equivalent live region), does not steal focus, and disappears once the page
  renders.

### AC-006-03 — Render errors have a safe recovery path

- **Given** a descendant component throws during render
- **When** the error boundary catches it
- **Then** the user sees a safe fallback with a short message and a Retry/Reload
  action, no stack trace or internal detail is shown, and retrying re-renders the
  route tree.

### AC-006-04 — Existing behavior is preserved

- **Given** the existing frontend test suite and auth guard behavior
- **When** the lazy routes and boundary are in place
- **Then** all existing tests pass, unauthenticated protected routes still
  redirect to `/login`, and the attempted path is preserved.

### AC-006-05 — No backend or contract change

- **Given** the Wave 3 change set
- **When** the diff is reviewed
- **Then** no API route, OpenAPI metadata, DTO, auth rule, validation, database
  code, or CI workflow changed.

## 6. Rules and validation

| Rule | Constraint | Behavior on violation |
| --- | --- | --- |
| Lazy imports | Every page component is loaded through `React.lazy`; no eager page import remains in `AppRoutes`. | Build/test review fails. |
| Suspense fallback | Accessible status text; no focus capture; no layout jump beyond a simple placeholder. | Component test fails. |
| Error boundary | Catches render errors only; safe copy; retry/reset; no stack trace. | Component test fails; review rejects. |
| Routes/auth | Same paths, guards, redirects, and `RequireAuth` behavior. | Existing tests fail. |
| Scope | Frontend-only; no backend/contract/CI changes. | Review rejects. |

## 7. API contract

Not applicable: this spec adds no route or payload. ADR-0004 remains unchanged.

## 8. Non-functional requirements

- **Performance:** Non-initial screens leave the initial entry chunk; record the
  build output before/after.
- **Accessibility:** Loading fallback uses a live region; boundary fallback is
  keyboard reachable and has a clear heading/action.
- **Resilience:** A render error in one route must not blank the whole app.
- **Observability:** No new logging service; the boundary may log to the console
  in development only, never expose internals to the user.
- **Compatibility:** React 19, Vite 8, existing test stack.

## 9. Dependencies and assumptions

- **Fact:** `AppRoutes` eagerly imports all five pages and `NotFoundPage`
  (`project/frontend/src/AppRoutes.tsx:1-8`).
- **Fact:** `App` wraps providers but has no error boundary
  (`project/frontend/src/App.tsx:20-29`).
- **Fact:** `Layout` focuses the `main` landmark after client-side navigation
  (`project/frontend/src/components/Layout.tsx:15-22`), so the loading fallback
  must not break that behavior.
- **Fact:** The frontend suite currently has 88 tests and runs with Vitest/RTL
  (`project/frontend/package.json:6-12`).
- **Decision (ADR-0007):** Wave 3 adopts lazy routes and a top-level error
  boundary; it is frontend-only and does not add CI.
- **Assumption:** Lazy loading all pages, including `NotFoundPage`, is acceptable;
  the fallback is shared.

## 10. Open questions

| Question | Owner | Deadline | Resolution |
| --- | --- | --- | --- |
| Should the boundary action reload the page or reset in place? | Frontend implementer | Before T-006-03 | Prefer reset-in-place for the same route; offer reload as the fallback when reset cannot recover. |
| Is the measured initial bundle reduction meaningful enough to keep splitting? | Frontend implementer | Before T-006-04 | Compare Vite build output before/after; keep the change if chunks are emitted and no UX regression appears. |
| Should `NotFoundPage` be lazy too? | Frontend implementer | Before T-006-02 | Default yes for consistency; it is small, so keeping it eager is acceptable if it simplifies the fallback. |

## 11. Change log

| Date | Change | Reason | Approved by |
| --- | --- | --- | --- |
| 2026-11-23 | Initial draft for Wave 3/A7: lazy routes, accessible Suspense fallback, and a top-level error boundary. | Prepare the approved ADR-0007 wave for separate spec review; no implementation is authorized by this draft. | — |
| 2026-11-23 | Approved and implemented Wave 3; all acceptance criteria have evidence. | Six page chunks emitted; initial entry reduced from 437.79 kB to 291.88 kB; loading fallback and error boundary tested; 90/90 frontend tests, lint and build green. | Project owner + Luna verification |
