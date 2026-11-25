# PLAN-006 — Frontend resilience implementation plan

- **Spec:** `specs/006-frontend-resilience/spec.md`
- **Status:** draft
- **Owner:** architect
- **Last updated:** 2026-11-23

## 1. Architecture impact

Frontend-only. No backend, API contract, persistence, auth, or CI change.

| Layer / area | Change |
| --- | --- |
| Domain | No change. |
| Application | No change. |
| Infrastructure | No change. |
| Api | No change. |
| Frontend routing | `AppRoutes` uses `React.lazy` for page components and wraps the route tree in `Suspense` with an accessible loading fallback. |
| Frontend resilience | New top-level error boundary around the route tree with safe copy and retry/reset; no stack traces. |
| Frontend tests | New tests for loading fallback, boundary catch/recovery, route rendering, and guard preservation. |
| CI / deployment | No change; CI remains deferred by ADR-0007. |

## 2. Data model

None. No database, migration, seed, or persistence change.

## 3. Contract

No route, payload, status code, or OpenAPI change. ADR-0004 remains unchanged.

## 4. Test strategy

| Layer | What is proven | Where |
| --- | --- | --- |
| Component/routing | Lazy page renders after load; loading fallback is announced; all routes render. | `project/frontend/src/**/*.test.tsx` (RTL) |
| Error boundary | A child render error shows the safe fallback; Retry resets and re-renders. | New boundary test |
| Auth guard | Unauthenticated protected routes still redirect to `/login` and preserve the attempted path. | Existing `RequireAuth`/route tests |
| Build output | Page chunks are emitted and the initial entry excludes non-initial screens. | `npm run build` output, manual review |
| Regression | Existing 88 frontend tests and lint/build stay green. | `npm run lint`, `npm run test`, `npm run build` |

## 5. Task order

The executable list is in `tasks.md`; implementation is gated on separate
SPEC-006 approval.

1. Add failing tests for the loading fallback and boundary behavior.
2. Convert page imports to `React.lazy` and add the Suspense fallback.
3. Add the error boundary with reset/reload behavior.
4. Run lint/tests/build, inspect chunks, record bundle evidence and AI log.
5. Independent verification and review.

## 6. Risks and fallbacks

| Risk | Impact | Fallback |
| --- | --- | --- |
| Lazy loading breaks tests that assume synchronous rendering. | Suite fails or tests become flaky. | Use `findBy*`/`waitFor`; keep the fallback minimal and deterministic. |
| Suspense fallback flashes or steals focus. | UX/accessibility regression. | Use a status live region, no autofocus; verify with RTL and manual keyboard pass. |
| Error boundary hides API errors or misleads users. | Wrong recovery advice. | Boundary catches render errors only; existing query/error states stay in place. |
| Bundle split is negligible. | Extra complexity for no gain. | Compare build output; keep only if chunks are emitted and UX is unchanged. |
| Reset does not recover from a persistent error. | User is stuck. | Offer a reload action as the fallback recovery path. |

## 7. Definition of done

- [x] SPEC-006 is separately approved before implementation.
- [x] Every acceptance criterion has a test or named manual check.
- [x] All page components are lazy; a shared accessible Suspense fallback exists.
- [x] The error boundary catches render errors, shows safe copy, and supports retry/reset.
- [x] Existing routes, guards, redirects, and the full frontend suite (90/90) remain green.
- [x] Lint and production build pass with captured output; chunks are verified.
- [x] No backend/contract/CI change is present.
- [x] AI evidence entry exists and a second-pass verification found no blocker.
