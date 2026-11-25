# TASKS-006 — Frontend resilience

- **Spec:** `specs/006-frontend-resilience/spec.md`
- **Plan:** `specs/006-frontend-resilience/plan.md`
- **Status:** implemented — approved by project owner
- **Last updated:** 2026-11-23

## Status legend

`todo` · `red` (failing test written) · `green` (test passes) · `done` (refactored,
verified, documented) · `blocked`

## Tasks

| ID | Task | Owner | Depends on | Test / evidence | Status |
| --- | --- | --- | --- | --- | --- |
| T-006-01 | Add failing tests for the Suspense loading fallback, boundary fallback, retry/reset, and unchanged route/auth behavior. | Frontend implementer | SPEC-006 approval | Added `AppResilience.test.tsx`; initial run exposed the lazy-render timing failures in Register/TaskForm tests (16), fixed by waiting for the route page | done |
| T-006-02 | Convert page imports in `AppRoutes` to `React.lazy` and wrap the route tree in `Suspense` with an accessible status fallback. | Frontend implementer | T-006-01 | `AppRoutes` now lazy-loads all six pages; `Layout` wraps `Outlet` in `Suspense` with `RouteLoading` (`role="status"`); no eager page imports remain | done |
| T-006-03 | Add a top-level error boundary with safe copy and retry/reset (reload fallback when reset cannot recover). | Frontend implementer | T-006-02 | `AppErrorBoundary` wraps the app; test proves safe fallback, no raw error text, and retry recovery | done |
| T-006-04 | Run lint/tests/build, inspect emitted chunks and initial bundle, record before/after evidence, and update the AI log. | Frontend implementer | T-006-03 | `npm run lint` clean; `npm run test` 90/90; `npm run build` emitted six page chunks; initial entry 437.79 kB → 291.88 kB; evidence in `docs/ai/033-frontend-resilience-implementation.md` | done |
| T-006-05 | Independently verify the change set, routes/auth behavior, accessibility fallback, and scope boundary; confirm no backend/CI change. | Reviewer / QA | T-006-04 | Luna second-pass review: routes/auth preserved, 90/90 tests, no backend/CI change; no blocker | done |

## Notes

- SPEC-006 is approved and implemented; all tasks are done.
- The frontend previously imported all pages eagerly (`project/frontend/src/AppRoutes.tsx:1-8`) and had no error boundary (`project/frontend/src/App.tsx:20-29`); Wave 3 replaced both.
- The suite grew from 88 to 90 tests; lint and build are green.
- No API, OpenAPI, DTO, auth, database, or CI change is in scope.
