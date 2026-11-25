# SPEC-007 — Security response headers

- **Status:** implemented
- **Owner:** architect
- **Approved by:** project owner
- **Requirements:** R-10, R-27 (supports approved gaps G19/G21)
- **ADRs:** 0004, 0007
- **Last updated:** 2026-11-25

## 1. Problem

The API serves the built SPA from `wwwroot` in the deployment
(`project/src/CrudWorks.Api/Program.cs:95-117`, `project/Dockerfile:27`), but
sends no security response headers. Nothing today constrains injected inline
script, MIME sniffing, framing, referrer leakage, or a downgrade after the first
HTTPS visit. G19/G21 approved a small middleware; CSP is also the
defense-in-depth layer the XSS review requires before markdown rendering (G14).

## 2. Scope

**In scope**

- One middleware registered in `Program.cs` that adds security headers to every
  response, including API JSON, RFC 7807 errors, static files, and SPA HTML.
- A strict Content-Security-Policy with the exact approved directives.
- `X-Content-Type-Options: nosniff`, `Referrer-Policy:
  strict-origin-when-cross-origin`, `X-Frame-Options: DENY`, and CSP
  `frame-ancestors 'none'`.
- `Strict-Transport-Security` outside Development only.
- Integration tests across Development, Production, SPA mode, API JSON, and
  error responses; header values captured as evidence.
- Documentation updates: spec, plan, tasks, traceability, AI evidence.

**Out of scope**

- HTTPS redirection, forwarded-header configuration, TLS/certificate handling.
- `Permissions-Policy`, COOP/COEP/CORP, SRI, nonce/hash CSP, report-only or
  `report-uri`.
- Cookies, CSRF, token storage (G20 keeps `sessionStorage`).
- Markdown rendering and its sanitizer (G14).
- `npm audit`/Dependabot (G23).
- CORS, routes, request/response bodies, status codes, OpenAPI metadata, and
  CI/CD.

## 3. Actors and glossary

| Term | Meaning |
| --- | --- |
| CSP | `Content-Security-Policy`: restricts which origins may supply scripts, styles, images, connections, and framing. |
| HSTS | `Strict-Transport-Security`: tells the browser to use HTTPS for later visits. |
| SPA mode | `Spa:Serving=true`: the API serves the React build from `wwwroot` and falls back to `index.html` for deep links. |
| Baseline headers | `nosniff`, `Referrer-Policy`, `X-Frame-Options`, and CSP, sent in every environment. |

## 4. User stories

| ID | Story |
| --- | --- |
| US-007-01 | As a user, I want an injected inline script or an embedded frame to be blocked so my session token cannot be stolen through a markup sink. |
| US-007-02 | As a maintainer, I want the header values defined in one place and proven by tests so they cannot drift between environments. |
| US-007-03 | As an operator, I want production responses to instruct browsers to keep using HTTPS after the first secure visit. |

## 5. Acceptance criteria

### AC-007-01 — Baseline headers on every response

- **Given** the API in Development or Production
- **When** any request completes (`/api/health`, `/api/tasks`, a `401`
  problem+json, an unknown route, or the SPA document)
- **Then** the response carries `X-Content-Type-Options: nosniff`,
  `Referrer-Policy: strict-origin-when-cross-origin`,
  `X-Frame-Options: DENY`, and a `Content-Security-Policy`.

### AC-007-02 — The CSP is strict and exact

- **Given** a response from the API
- **When** `Content-Security-Policy` is read
- **Then** it equals
  `default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'`
- **And** it contains no `'unsafe-inline'`, no `'unsafe-eval'`, no `*`, and no
  remote origin.

### AC-007-03 — HSTS only outside Development

- **Given** a Development response
- **When** the headers are read
- **Then** `Strict-Transport-Security` is absent.
- **Given** a Production response
- **When** the headers are read
- **Then** `Strict-Transport-Security` is
  `max-age=31536000; includeSubDomains` and contains no `preload`.

### AC-007-04 — SPA document and deep links are covered

- **Given** Production with `Spa:Serving=true`
- **When** `GET /` or `GET /tasks/123` returns the SPA document
- **Then** the response is `200` `text/html` and carries the same baseline
  headers as AC-007-01 plus HSTS.

### AC-007-05 — No behavior or contract change

- **Given** the Wave 4 change set
- **When** the solution and frontend suites run
- **Then** status codes, response bodies, content types, CORS behavior,
  authentication, routes, and OpenAPI metadata are unchanged, and all existing
  tests pass.

### AC-007-06 — Evidence is captured

- **Given** the implementation
- **When** verification runs
- **Then** the exact header values are captured from real test output, the AI
  evidence entry records them, and no CI workflow was added.

## 6. Rules and validation

| Rule | Constraint | Behavior on violation |
| --- | --- | --- |
| Single source | Header names and values are constants in one class; only HSTS varies by environment. | Review rejects duplicated literals. |
| Ordering | Middleware runs after `UseExceptionHandler()` and before static files, status pages, and endpoints; headers are applied at response start so a rewritten error response still carries them. | Tests must show headers on error and static/HTML responses. |
| CSP | Exact policy from AC-007-02; no unsafe directives. | Test fails. |
| HSTS | Non-Development only; one year plus subdomains; no preload. | Test fails. |
| Scope | No route, body, status code, CORS, OpenAPI, or CI change. | Review rejects. |

## 7. API contract

No route, payload, or status code changes. These headers are cross-cutting
response metadata; OpenAPI does not describe them per operation and this spec
does not add them there. ADR-0004 remains unchanged.

## 8. Non-functional requirements

- **Performance:** five header assignments per response; no measurable cost.
- **Compatibility:** JSON clients ignore unknown response headers; the SPA
  bundle uses external scripts and styles only, so `script-src 'self'` and
  `style-src 'self'` are satisfied by the current build.
- **Security:** defense-in-depth only; CSP does not replace the sanitizer
  required by G14.
- **Observability:** no logging or telemetry is added.

## 9. Dependencies and assumptions

- **Fact:** the deployment serves the SPA from the API
  (`project/src/CrudWorks.Api/Program.cs:95-117`, `project/Dockerfile:27`).
- **Fact:** Development serves the SPA from Vite with an `/api` proxy
  (`project/frontend/vite.config.ts:6-14`), so API headers never reach the dev
  page.
- **Fact:** `index.html` has no inline script or style
  (`project/frontend/index.html:1-13`); the production build emits external
  assets (build output: `dist/assets/*.js`, `dist/assets/*.css`).
- **Fact:** no `dangerouslySetInnerHTML`, `innerHTML`, `eval`, or inline `style`
  attribute exists in `project/frontend/src` (grep on 2026-11-24).
- **Fact:** `Program.cs` has no `UseHsts`, `UseHttpsRedirection`, or
  forwarded-header configuration today (`Program.cs:86-198`).
- **Assumption:** no third-party runtime origin (font, image, script, analytics)
  is required, so `'self'` is sufficient.
- **Decision:** HSTS is emitted outside Development regardless of the request
  scheme because TLS terminates at the platform; browsers ignore HSTS received
  over plain HTTP.

## 10. Open questions

| Question | Owner | Deadline | Resolution |
| --- | --- | --- | --- |
| G19 says to relax CSP in Development for Vite HMR, but Development serves the SPA from Vite, so the API CSP never reaches it. Keep one strict policy everywhere, or add a Development-only relaxed policy? | Project owner | Resolved 2026-11-25 | **Resolved:** one strict policy everywhere. A Development-only variant would only apply to API JSON and would add an untested second policy. |
| Are `max-age=31536000; includeSubDomains` and no `preload` the right HSTS parameters? | Project owner | Resolved 2026-11-25 | **Resolved:** yes; `preload` is intentionally excluded because the demo domain is not submitted to the preload list. |
| Should HSTS be gated on `Request.IsHttps` with forwarded headers configured instead? | Project owner | Resolved 2026-11-25 | **Resolved:** no for this scope; platform TLS plus browser behavior makes the scheme gate unnecessary, and forwarded headers are a separate change. |

## 11. Change log

| Date | Change | Reason | Approved by |
| --- | --- | --- | --- |
| 2026-11-24 | Initial draft for Wave 4: security-headers middleware, strict CSP, baseline headers, HSTS outside Development. | Prepare approved gaps G19/G21 for separate spec review; no implementation is authorized by this draft. | — |
| 2026-11-25 | Approved and implemented Wave 4; all acceptance criteria have evidence. | Owner accepted one strict CSP in every environment, HSTS `max-age=31536000; includeSubDomains` without `preload`, and no scheme gate. Seven header tests pass; 197/197 backend tests; live Development/Production header capture recorded; the independent review's 500-path gap was fixed and sensitivity-checked. | Project owner + Luna verification |
