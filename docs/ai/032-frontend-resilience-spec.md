# AI Evidence Log

## Entry 032

- **Date:** 2026-11-23
- **Agent:** orchestrator (GPT-6 Luna)
- **Task:** Draft SPEC-006, PLAN-006, and TASKS-006 for Wave 3/A7.
- **Tool/model:** GPT-6 Luna (OpenCode).

## Prompt

Faithful summary: the owner said to proceed with Wave 3/A7 from ADR-0007
(route-level lazy loading plus a top-level error boundary). Prepare the spec,
plan, and ordered tasks first, keeping the spec in draft; no implementation until
separate approval. Frontend-only; no API, contract, auth, database, or CI change.

## Output summary

- Created `specs/006-frontend-resilience/spec.md` with testable criteria for
  lazy routes, accessible loading fallback, render-error recovery, preserved
  routes/auth, and no backend/contract change.
- Created `PLAN-006` and `TASKS-006` with a test-first order and independent
  verification task.
- Added SPEC-006 to the current-spec inventory and marked Track 5/Wave 3 as draft
  ready for owner review.
- No code, package, test, or CI file was changed.

## Validation

- [ ] Build: not run — documentation-only task.
- [ ] Tests: not run — documentation-only task.
- [x] Source review: `AppRoutes.tsx:1-8` eagerly imports all pages;
  `App.tsx:20-29` has providers but no error boundary; `Layout.tsx:15-22` focuses
  the main landmark after navigation; the frontend has 88 tests.
- [x] Scope review: Wave 3 remains frontend-only; ADR-0004 and the API are
  untouched.

## Corrections

| What was suggested | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| Treat the error boundary as catching API/query failures | React error boundaries catch render errors, not rejected promises or event handlers | Spec limits the boundary to render-time errors and keeps existing query/error states responsible for API failures | Avoid a false resilience claim |
| Assume the bundle split is automatically worth it | The app is small; chunking may have negligible impact | Plan requires before/after build output and keeps the change only if chunks are real | Measure instead of assuming |

## Edge cases considered

- Suspense fallback must not steal focus; `Layout` already manages focus on
  navigation.
- A persistent render error may not recover by reset; the boundary offers reload
  as a fallback.
- Lazy routes change test timing; existing tests may need `findBy*`/`waitFor`.
- `NotFoundPage` may remain eager if that simplifies the fallback; the spec leaves
  that as a small implementation choice.
- No API/contract/CI change is authorized.

## Files affected

- `specs/006-frontend-resilience/spec.md`
- `specs/006-frontend-resilience/plan.md`
- `specs/006-frontend-resilience/tasks.md`
- `specs/README.md`
- `docs/improvement-plan.md`
- `docs/ai/032-frontend-resilience-spec.md`

## Final decision

**Draft ready for owner review.** SPEC-006 is `draft`; implementation is not
authorized until the owner approves the spec and ordered tasks.
