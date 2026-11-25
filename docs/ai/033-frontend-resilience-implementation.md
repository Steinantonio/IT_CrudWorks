# AI Evidence Log

## Entry 033

- **Date:** 2026-11-23
- **Agent:** orchestrator / GPT-6 Luna
- **Task:** Implement and verify Wave 3/A7 (lazy routes and top-level error boundary).
- **Tool/model:** GPT-6 Luna (OpenCode); Node v26.13.1; npm 11.20.0.

## Prompt

Faithful summary: the owner approved SPEC-006 and asked to implement Wave 3/A7.
Add route-level lazy loading with an accessible Suspense fallback and a top-level
error boundary with safe copy and retry/reset, preserving routes, auth guards,
and API behavior. Frontend-only; no CI or commit.

## Output summary

- `AppRoutes` now lazy-loads all six pages (including `NotFoundPage`).
- `Layout` wraps `Outlet` in `Suspense` with `RouteLoading` (`role="status"`),
  keeping the header visible during chunk loads.
- `AppErrorBoundary` wraps the app; it catches render errors, shows a safe
  heading/message, and offers Try again and Reload. It logs details only in
  development and never exposes them to the user.
- `renderApp` test harness includes the boundary; `waitForPage` waits for the
  lazy route heading.
- `AppResilience.test.tsx` covers the loading fallback and boundary recovery.
- Existing Register/TaskForm tests now wait for the lazy page before interacting.

## Validation

- [x] Initial test run after lazy routes: **16 failed, 74 passed** — the failures
  were synchronous assumptions in Register/TaskForm tests, not behavior changes.
  Fixed by awaiting the lazy page (`waitForPage`).
- [x] Final tests: `npm run test` → **12 files, 90/90 passed**.
- [x] Lint: `npm run lint` → clean.
- [x] Build: `npm run build` → succeeded; emitted six page chunks:
  `NotFoundPage` 0.32 kB, `AuthLayout` 1.05 kB, `LoginPage` 2.98 kB,
  `RegisterPage` 3.47 kB, `TaskFormPage` 5.40 kB, `TaskListPage` 9.65 kB;
  initial entry `index` **291.88 kB** (previous single JS bundle 437.79 kB).
- [x] Routes/auth: existing route and guard tests remain green; unauthenticated
  protected routes still redirect to `/login`.
- [x] Scope: no API, OpenAPI, DTO, auth, database, or CI file changed.

## Corrections

| What was found | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| 16 tests failed after lazy loading | They queried page controls immediately after render, before the chunk resolved | Added `waitForPage` and awaited it in Register/TaskForm route tests | Keep the tests meaningful under asynchronous rendering |
| A single `Suspense` around `Routes` would replace the whole layout during load | The header would disappear on every navigation | Suspense wraps `Outlet` inside `Layout` | Preserve the shell while a page chunk loads |

## Edge cases considered

- Loading fallback has `role="status"` and does not autofocus.
- Error boundary catches render errors only; API/query errors keep their
  component-level handling.
- Retry resets the boundary; Reload is available for a persistent error.
- `NotFoundPage` is lazy too; the shared fallback covers it.
- Production build chunk list proves the split is real.

## Auth and validation decisions

- No auth, API, validation, or persistence behavior changed.
- No CI workflow or commit was made.

## Files affected

- `project/frontend/src/AppRoutes.tsx`
- `project/frontend/src/App.tsx`
- `project/frontend/src/components/Layout.tsx`
- `project/frontend/src/components/RouteLoading.tsx`
- `project/frontend/src/components/AppErrorBoundary.tsx`
- `project/frontend/src/test/renderApp.tsx`
- `project/frontend/src/AppResilience.test.tsx`
- `project/frontend/src/App.test.tsx`
- `project/frontend/src/pages/RegisterPage.test.tsx`
- `project/frontend/src/pages/TaskFormPage.test.tsx`
- `specs/006-frontend-resilience/`
- `specs/README.md`
- `docs/improvement-plan.md`

## Final decision

**Implemented and verified.** All SPEC-006 acceptance criteria have evidence;
90/90 frontend tests, lint and build are green. No backend/contract/CI change.
