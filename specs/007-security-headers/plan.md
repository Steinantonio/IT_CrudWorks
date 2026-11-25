# PLAN-007 — Security response headers implementation plan

- **Spec:** `specs/007-security-headers/spec.md`
- **Status:** implemented
- **Owner:** architect
- **Last updated:** 2026-11-25

## 1. Architecture impact

Backend-only, at the composition root. No Domain, Application, Infrastructure,
frontend, or CI change.

| Layer / area | Change |
| --- | --- |
| Domain | No change. |
| Application | No change. |
| Infrastructure | No change. |
| Api | New `SecurityHeadersMiddleware` (or equivalent) in `CrudWorks.Api/Security/`, registered in `Program.cs` after `UseExceptionHandler()` and before static files, status pages, CORS, auth, and endpoints. It registers a `Response.OnStarting` callback so headers are applied even when a downstream handler rewrites the response. |
| Frontend | No change; the current build already satisfies `script-src 'self'` and `style-src 'self'`. |
| Tests | New `SecurityHeadersTests` in `project/tests/CrudWorks.Api.Tests/Pipeline/`, reusing `CrudWorksApiFactory`, `NonDevelopmentApiFactory`, and `SpaServingApiFactory`. |
| CI / deployment | No change; CI remains deferred by ADR-0007. |

## 2. Data model

None. No database, migration, or seed change.

## 3. Contract

No route, payload, status code, or OpenAPI change. The headers are added to
every response as cross-cutting metadata.

Exact values:

| Header | Value | Environments |
| --- | --- | --- |
| `Content-Security-Policy` | `default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'` | All |
| `X-Content-Type-Options` | `nosniff` | All |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | All |
| `X-Frame-Options` | `DENY` | All |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | Non-Development |

## 4. Test strategy

| Layer | What is proven | Where |
| --- | --- | --- |
| Integration | Baseline headers on health JSON, protected JSON, `401` problem+json, unknown route, and SPA HTML/deep links. | `SecurityHeadersTests` with the existing factories. |
| Environment | HSTS absent in Development, present in Production. | Same test class, two factories. |
| Policy | Exact CSP string; no `unsafe-inline`, `unsafe-eval`, `*`, or remote origin. | String assertions on the captured header. |
| Regression | Existing backend and frontend suites, lint, and build stay green. | `dotnet test`, `npm run lint`, `npm run test`, `npm run build`. |
| Scope | No route/body/status/OpenAPI/CI change. | Diff review plus the existing pipeline tests. |

## 5. Task order

The executable list is in `tasks.md`; implementation is gated on separate
SPEC-007 approval.

1. Add failing integration tests for the headers and HSTS split.
2. Implement the middleware and register it at the correct pipeline position.
3. Run the full backend suite; confirm no behavior or contract change.
4. Run frontend lint/tests/build; capture header evidence and update the AI log.
5. Independent verification and review.

## 6. Risks and fallbacks

| Risk | Impact | Fallback |
| --- | --- | --- |
| CSP breaks the SPA because a dependency injects an inline style or script. | Blank or unstyled page in production. | Build output and `src` grep show none today; if a future dependency needs it, prefer a nonce/hash over `'unsafe-inline'`, or scope a separate spec. |
| Headers are missing on error or static responses because of middleware order. | Partial protection. | Register after `UseExceptionHandler()` and before static files/status pages; tests cover `401`, `404`, and SPA HTML. |
| HSTS never reaches browsers behind a TLS-terminating proxy. | No HSTS benefit. | Emit it outside Development regardless of scheme; browsers ignore it over HTTP. Revisit forwarded headers separately. |
| A future external origin (font, analytics) is blocked by `'self'`. | Broken asset. | Add the exact origin to the relevant directive in a reviewed change; never loosen to `*`. |
| Header duplication or drift across environments. | Inconsistent policy. | Single constants class; only HSTS varies by environment. |

## 7. Definition of done

- [x] SPEC-007 is separately approved before implementation.
- [x] Every acceptance criterion has an integration test or a named manual check.
- [x] Baseline headers are present on JSON, error, unknown-route, and SPA responses.
- [x] CSP matches the exact policy and contains no unsafe directive.
- [x] HSTS is absent in Development and present in Production without `preload`.
- [x] Existing backend and frontend suites, lint, and build pass with captured output.
- [x] No route, body, status code, CORS, OpenAPI, or CI change is present.
- [x] AI evidence entry exists and a second-pass review has no blocker.
