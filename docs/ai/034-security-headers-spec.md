# AI Evidence Log

## Entry 034

- **Date:** 2026-11-24
- **Agent:** orchestrator (GPT-6 Luna)
- **Task:** Draft SPEC-007, PLAN-007, and TASKS-007 for Wave 4 (security response
  headers, gaps G19/G21).
- **Tool/model:** GPT-6 Luna (OpenCode).

## Prompt

Faithful summary: after Wave 3 was committed, the owner chose security headers
(G19/G21) as the next wave and asked to proceed. Prepare the spec, plan, and
ordered tasks first, keeping the spec in draft; no implementation until separate
approval. Scope: CSP, `X-Content-Type-Options`, `Referrer-Policy`,
frame-ancestors/`X-Frame-Options`, and HSTS outside Development, applied by API
middleware because the API serves the SPA in deployment.

## Output summary

- Created `specs/007-security-headers/spec.md` with testable criteria for
  baseline headers on every response, the exact strict CSP, HSTS outside
  Development, SPA HTML/deep-link coverage, and no behavior/contract change.
- Created `PLAN-007` and `TASKS-007` with a test-first order and independent
  verification task.
- Added SPEC-007 to the current-spec inventory and Wave 4 to the Track 5 table as
  draft ready for owner review.
- No code, package, test, or CI file was changed.

## Validation

- [ ] Build: not run — documentation-only task.
- [ ] Tests: not run — documentation-only task.
- [x] Source review: SPA is served from `wwwroot` in deployment
  (`Program.cs:95-117`, `Dockerfile:27`); Development serves the SPA from Vite
  (`vite.config.ts:6-14`); `index.html` has no inline script/style
  (`index.html:1-13`); no HTML sink or inline `style` in `frontend/src` (grep);
  `Program.cs` has no HSTS/HTTPS-redirection/forwarded-header configuration.
- [x] Scope review: Wave 4 is backend-only and leaves routes, bodies, status
  codes, CORS, OpenAPI, frontend, database, and CI untouched.

## Corrections

| What was suggested | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| Apply the approved G19 wording "relax CSP in Development for Vite HMR" mechanically | Development serves the SPA from Vite, so the API CSP never reaches the dev page; a Development-only policy would only cover API JSON and add a second untested policy | Spec proposes one strict policy everywhere and records the deviation as an open question for the owner | Keep one policy and one test surface without pretending a relaxation is needed |
| Gate HSTS on `Request.IsHttps` | The app does not configure forwarded headers and TLS terminates at the platform, so `IsHttps` would be false in production | Emit HSTS outside Development regardless of scheme; browsers ignore HSTS over plain HTTP | Make the protection reach browsers behind the proxy without a separate forwarded-headers change |

## Edge cases considered

- Headers must appear on error responses (`401`, `404`) and static/SPA responses,
  so middleware order is part of the acceptance criteria.
- A future dependency that injects inline style/script would need a nonce/hash,
  never `'unsafe-inline'`; the current build uses external assets only.
- `frame-ancestors 'none'` and `X-Frame-Options: DENY` are both sent for older
  browsers.
- HSTS excludes `preload` because the demo domain is not submitted to the
  preload list.
- No third-party runtime origin is assumed; `'self'` is sufficient today.

## Files affected

- `specs/007-security-headers/spec.md`
- `specs/007-security-headers/plan.md`
- `specs/007-security-headers/tasks.md`
- `specs/README.md`
- `docs/improvement-plan.md`
- `docs/ai/034-security-headers-spec.md`

## Final decision

**Draft ready for owner review.** SPEC-007 is `draft`; implementation is not
authorized until the owner approves the spec, the ordered tasks, and the three
open questions (Development CSP, HSTS parameters, scheme gating).
