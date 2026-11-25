# AI Evidence Log

## Entry 035

- **Date:** 2026-11-25
- **Agent:** orchestrator / GPT-6 Luna
- **Task:** Implement and verify Wave 4 (security response headers, gaps G19/G21).
- **Tool/model:** GPT-6 Luna (OpenCode); .NET SDK 10.0.404; Node v26.13.1.

## Prompt

Faithful summary: the owner approved SPEC-007 and authorized implementation. Add
one API middleware that sends a strict CSP, `X-Content-Type-Options`,
`Referrer-Policy`, and `X-Frame-Options` on every response, plus HSTS outside
Development. Tests first; no route, body, status code, CORS, OpenAPI, frontend,
or CI change. The owner accepted one strict CSP in every environment, HSTS
`max-age=31536000; includeSubDomains` without `preload`, and no scheme gate.

## Output summary

- Added `project/tests/CrudWorks.Api.Tests/Pipeline/SecurityHeadersTests.cs` with
  seven integration tests over Development, Production, SPA mode, and the
  exception-handler path.
- Added `project/src/CrudWorks.Api/Security/SecurityHeadersMiddleware.cs` with the
  exact header constants and a `Response.OnStarting` callback.
- Registered `app.UseSecurityHeaders()` in `Program.cs` after
  `UseExceptionHandler()` and before static files/status pages/endpoints.
- Updated SPEC-007, PLAN-007, TASKS-007, the spec inventory, the Track 5 wave
  table, requirement traceability, and `project/README.md`.

## Validation

- [x] Red (T-007-01), before the middleware: `dotnet test
  tests/CrudWorks.Api.Tests/CrudWorks.Api.Tests.csproj --filter
  "FullyQualifiedName~SecurityHeadersTests" --verbosity minimal` →
  **6 failed, 0 passed**, each with `System.InvalidOperationException: The given
  header was not found`.
- [x] Green (T-007-02), after the middleware and the review's 500-path test:
  same command → **7/7 passed**.
- [x] Sensitivity check for the `OnStarting` rationale: temporarily applying the
  headers before `await next()` made
  `UnhandledException_CarriesBaselineHeadersOnTheProblemJsonResponse` fail with
  `The given header was not found`; reverting to `OnStarting` restored 7/7.
- [x] Build: `dotnet build CrudWorks.slnx --verbosity minimal` → **0 warnings,
  0 errors**.
- [x] Full backend suite: `dotnet test CrudWorks.slnx --verbosity minimal` →
  Domain 37, Application 57, Infrastructure 25, Architecture 6, Api 72
  (**197 passed, 0 failed**).
- [x] Frontend: `npm run lint` clean; `npm run test` → **12 files, 90/90 passed**;
  `npm run build` succeeded with the same chunk output.
- [x] Live capture (Development on :5080, Production on :5081):
  - Development: CSP exact, `nosniff`, `strict-origin-when-cross-origin`,
    `DENY`; `Strict-Transport-Security` **absent**.
  - Production: the same four plus
    `Strict-Transport-Security: max-age=31536000; includeSubDomains`.
- [x] Scope: no route, body, status code, CORS, OpenAPI, frontend, database, or
  CI file changed.
- [x] No commit or push was performed.

## Corrections

| What was considered | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| Set the headers before `await next()` | The exception handler calls `Response.Clear()` before writing problem+json, which drops headers set eagerly | Registered a `Response.OnStarting` callback instead | Headers are applied when the final response starts, including the rewritten 500 response |
| Add a Development-only relaxed CSP because G19 says so | Development serves the SPA from Vite, so the API CSP never reaches the dev page; a second policy would only cover API JSON and add test surface | Kept one strict policy everywhere, with the owner's approval recorded in the spec | One policy, one test surface, no false claim |
| Gate HSTS on `Request.IsHttps` | No forwarded-header configuration exists and TLS terminates at the platform, so `IsHttps` would be false in production | Emit HSTS outside Development regardless of scheme | Browsers ignore HSTS received over plain HTTP. **Assumption:** the platform forwards the response header end-to-end; verify with `curl -I` against the deployment after the next release |
| The first test set covered only paths that never clear headers | The `OnStarting` rationale was untested; eager headers would have passed every test | Added the 500 exception-handler test and sensitivity-checked it | The review found a real evidence gap; the test now fails if the middleware regresses to eager headers |

## Edge cases considered

- Headers are present on `401` problem+json and routing-level `404` responses,
  proving the middleware runs before status-code pages.
- Headers are also present on the `500` problem+json written by the exception
  handler after it clears the response, which is the reason for `OnStarting`.
- SPA `/` and `/tasks/123` responses carry the headers and HSTS in Production.
- The CSP has no `'unsafe-inline'`, `'unsafe-eval'`, `*`, or remote origin.
- `frame-ancestors 'none'` and `X-Frame-Options: DENY` are both sent for older
  browsers.
- HSTS excludes `preload` because the demo domain is not submitted to the
  preload list.
- Static assets use the same pipeline as the SPA document; no separate origin is
  required today.

## Auth and validation decisions

- No authentication, authorization, validation, route, status code, or
  persistence behavior changed.
- The headers are defense-in-depth; CSP does not replace the sanitizer required
  by G14.
- No CI workflow or commit was added.

## Files affected

- `project/src/CrudWorks.Api/Security/SecurityHeadersMiddleware.cs`
- `project/src/CrudWorks.Api/Program.cs`
- `project/tests/CrudWorks.Api.Tests/Pipeline/SecurityHeadersTests.cs`
- `project/README.md`
- `specs/007-security-headers/spec.md`
- `specs/007-security-headers/plan.md`
- `specs/007-security-headers/tasks.md`
- `specs/README.md`
- `docs/improvement-plan.md`
- `docs/04-requirement-traceability.md`

## Final decision

**Implemented and verified.** All SPEC-007 acceptance criteria have evidence:
7/7 header tests, 197/197 backend tests, frontend 90/90, lint and build green,
and live Development/Production header capture. The independent review's one
Major finding (missing 500-path test) and four Minor documentation issues were
fixed. No blocker; commit approval is pending.
