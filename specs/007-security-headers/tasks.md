# TASKS-007 — Security response headers

- **Spec:** `specs/007-security-headers/spec.md`
- **Plan:** `specs/007-security-headers/plan.md`
- **Status:** implemented
- **Last updated:** 2026-11-25

## Status legend

`todo` · `red` (failing test written) · `green` (test passes) · `done` (refactored,
verified, documented) · `blocked`

## Tasks

| ID | Task | Owner | Depends on | Test / evidence | Status |
| --- | --- | --- | --- | --- | --- |
| T-007-01 | Add failing integration tests for the baseline headers on health JSON, protected JSON, `401` problem+json, unknown route, SPA HTML and deep links; assert the exact CSP; assert HSTS absent in Development and present in Production. | Backend implementer; QA reviews | SPEC-007 approval | Red run captured: 6/6 failed with `The given header was not found`; `SecurityHeadersTests.cs` covers all criteria plus the 500 exception-handler path. Sensitivity check: switching the middleware to eager headers fails the 500 test. | done |
| T-007-02 | Add `SecurityHeadersMiddleware` with the exact header constants and register it after `UseExceptionHandler()` and before static files/status pages/endpoints. HSTS is added only outside Development. | Backend implementer | T-007-01 | `SecurityHeadersMiddleware.cs` uses `Response.OnStarting`; `Program.cs` registers `UseSecurityHeaders()` after the exception handler. Focused tests: 7/7 pass. | done |
| T-007-03 | Run the full backend suite and confirm routes, status codes, bodies, content types, CORS, and OpenAPI metadata are unchanged. | Backend implementer | T-007-02 | `dotnet build CrudWorks.slnx` 0 warnings/0 errors; `dotnet test CrudWorks.slnx` 197/197 (Domain 37, Application 57, Infrastructure 25, Architecture 6, Api 72); existing pipeline/OpenAPI tests unchanged. | done |
| T-007-04 | Run frontend lint/tests/build, capture the exact header values and build output, and update the AI evidence entry and traceability. | Backend + frontend implementers | T-007-03 | `npm run lint` clean; `npm run test` 90/90; `npm run build` succeeded; live Development/Production header capture recorded in `docs/ai/035`. | done |
| T-007-05 | Independently verify the change set, header values, middleware order, environment split, and scope boundary; confirm no frontend/CI change. | Reviewer / QA | T-007-04 | Independent read-only review found one Major (no 500 exception-handler header test) and four Minor doc/evidence issues; all were fixed. No blocker remains. | done |

## Notes

- SPEC-007 is approved and implemented; all tasks are done after the independent review's findings were fixed. The owner chose one strict CSP in every environment, HSTS `max-age=31536000; includeSubDomains` without `preload`, and no scheme gate.
- The SPA is served by the API only in deployment (`Program.cs:95-117`, `Dockerfile:27`); Development serves it from Vite, so API headers do not reach the dev page.
- `index.html` has no inline script or style, and `project/frontend/src` has no HTML sink or inline `style` attribute, so the strict CSP is feasible without `'unsafe-inline'`.
- No API route, payload, status code, OpenAPI, frontend, database, or CI change is in scope.
