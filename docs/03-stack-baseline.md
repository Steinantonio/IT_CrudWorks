# 03 — Stack Baseline

- **Status:** accepted; confirmed by ADR-0001…0005.
- **Environment checked:** 2026-11-11 on the candidate's Windows workspace.

## 1. Environment check

| Tool | Found | Consequence |
| --- | --- | --- |
| .NET SDK | **10.0.404** (also 9.0.101, 9.0.201) | Pinned to 10.0.404 in `project/global.json`. |
| ASP.NET Core runtimes | 10.0 and earlier | API runs on .NET 10. |
| Node.js / npm | **26.13.1 / 11.20.0** | Frontend ready; prepend `C:\Program Files\nodejs` to PATH per shell. |
| Docker | not installed | Not required; SQLite is the runtime database. |
| Git | 2.49.0.windows.1 | Ready. |

**Recommendation:** the toolchain is installed and verified in `docs/ai/001-environment-setup.md`. Docker is not required. The SDK is pinned and committed.

## 2. Backend

| Concern | Choice | Why | Alternative |
| --- | --- | --- | --- |
| Runtime | .NET 10 (LTS) | Modern LTS, current for the interview window | .NET 8 LTS if .NET 10 cannot be installed |
| Web framework | ASP.NET Core Web API with controllers | Explicit, familiar to reviewers, easy to test | Minimal APIs (less ceremony, but can hide layering) |
| Architecture | Four projects: Domain, Application, Infrastructure, Api | Visible dependency rule; matches the brief | Vertical slices (modern, but harder to show Clean Architecture) |
| Persistence | EF Core 10 + SQLite provider | Productive, testable, zero-setup demo | Dapper (more SQL control, more boilerplate) |
| Database | SQLite (demo) with PostgreSQL 17 as the documented production swap | Zero install, reset by deleting one file; same EF Core model | PostgreSQL native (production parity, but install and service risk before Friday) |
| Auth | JWT bearer + `PasswordHasher<T>` | Simple SPA flow, easy to demo | Cookie auth + antiforgery (more secure, more setup) |
| Validation | Manual validators in Application | Keeps validation out of controllers, testable, no extra dependency | FluentValidation (nicer DSL, one more package) |
| Mapping | Manual extension methods | Explicit, no magic, easy to review | AutoMapper (less code, more indirection) |
| API docs | Built-in OpenAPI JSON | Modern .NET, no extra dependency | Swashbuckle/Scalar (familiar UI, extra package) |
| Logging | `ILogger` structured logs | Built-in, sufficient | Serilog (nicer sinks, extra dependency) |
| Health | `AddHealthChecks()` + `DbContext` check | Demonstrates operational thinking | None |

### What we are deliberately not using

- **MediatR/CQRS:** the exercise is small; an extra indirection layer would make the code harder to defend, not more impressive.
- **AutoMapper:** explicit mapping is easier to review and debug.
- **Generic repository:** hides query behavior and adds no value for two aggregates.
- **Microservices/message brokers:** the brief asks for one application, not a distributed system. Adding them would be scope creep.
- **Full ASP.NET Core Identity UI:** we use the password hasher and our own user table, not the whole Identity stack.

## 3. Frontend

| Concern | Choice | Why | Alternative |
| --- | --- | --- | --- |
| Framework | React 19 + TypeScript | Mainstream, reviewable, strong ecosystem | Vue 3 (also fine) |
| Build | Vite | Fast, simple, modern | Next.js (SSR is unnecessary here) |
| Server state | TanStack Query | Caching, loading/error states, mutations | Plain fetch + useEffect (more boilerplate) |
| Forms | React Hook Form + Zod | Typed validation, good UX | Controlled forms + manual validation |
| Styling | Plain CSS with variables | No build step, full control, responsive with media queries | Tailwind CSS 4 (rejected: ADR-0005 chose plain CSS) |
| Routing | React Router | Standard SPA routing | TanStack Router |
| Tests | Vitest + React Testing Library (mocked `fetch`) | Fast, behavior-focused | MSW (not shipped; the API client boundary is mocked instead) |

**Timebox note:** TanStack Query, React Hook Form, and Zod are accelerators, not
requirements. If setup slips, use plain `fetch` + a typed `useApi` hook and
controlled forms. Keep the feature folders, centralized auth state, and
loading/error/empty states. Do not cut the API contract or the auth flow.

### Token storage decision (accepted)

Use a short-lived JWT in `sessionStorage` for the demo, with an ADR that documents the trade-off. Production would use an httpOnly, Secure, SameSite cookie plus refresh-token rotation and CSRF protection. The point is to show that the trade-off is understood, not to pretend `sessionStorage` is best practice.

## 4. Testing

| Layer | Tool | Scope |
| --- | --- | --- |
| Domain | xUnit | Invariants, value objects, status rules |
| Application | xUnit + NSubstitute | Use cases, validators, authorization decisions |
| Infrastructure | xUnit + SQLite | EF Core mappings, repositories, migrations |
| API | xUnit + `WebApplicationFactory<Program>` | Status codes, auth, validation, ProblemDetails |
| Frontend | Vitest + React Testing Library (mocked `fetch`) | Forms, list, auth guard, error states |
| E2E | API-level end-to-end script (Playwright was cut) | Register → login → CRUD → logout over the real HTTP pipeline |

Coverage target: 80% line coverage on `Domain` + `Application`; every endpoint and every critical UI flow covered. Coverage is a floor, not a goal.

## 5. DevOps and delivery

| Concern | Choice | Why |
| --- | --- | --- |
| Local database | SQLite file; delete to reset | Zero install, reproducible demo |
| API container | Optional multi-stage Dockerfile, non-root | Delivery basics if the timebox allows |
| Frontend | Vite dev server locally; optional Nginx image | Keep the demo simple |
| CI | GitHub Actions: backend build/test, frontend lint/test/build | Shows the quality gate; no service container needed |
| Config | `appsettings.json` + environment variables; ephemeral JWT key in Development | No secrets in git; production fails fast without a key |
| Migrations | `dotnet ef database update` on startup in Development only | Convenient demo; explicit in production |
| Demo data | Seed on startup if the database is empty | Repeatable demo |

## 6. Decision log

| ID | Decision | Status | Owner | ADR |
| --- | --- | --- | --- | --- |
| D-01 | Task manager domain | accepted | Architect | 0001 |
| D-02 | Four-project Clean Architecture | accepted | Architect | 0001 |
| D-03 | JWT bearer + sessionStorage for demo | accepted | Architect | 0002 |
| D-04 | EF Core + SQLite + test strategy | accepted | Architect | 0003 |
| D-05 | API contract and error model | accepted | Architect | 0004 |
| D-06 | React + TypeScript + Vite | accepted | Architect | 0005 |
| D-07 | xUnit + SQLite + WebApplicationFactory | accepted | Architect/QA | 0003 |
| D-08 | Plain CSS, no UI framework | accepted | Frontend | 0005 |

## 7. Risks

| Risk | Impact | Mitigation |
| --- | --- | --- |
| SDK drift | Build picks .NET 9 or .NET 11 packages | `global.json` pins 10.0.404; packages pinned to 10.0.15 |
| npm PATH or execution policy | Frontend commands fail in a fresh shell | Documented PATH prefix; `RemoteSigned` for CurrentUser |
| Database reset drift | Demo state differs from a clean clone | Documented reset + QA clean-run check |
| Scope creep | Misses the review window | Non-goals in `docs/00-brief.md`; orchestrator enforces |
| Auth complexity | Eats the schedule | Start with JWT + one protected endpoint; harden later |
| SQLite provider divergence | Behavior differs from PostgreSQL | Keep provider-specific code in Infrastructure; document the swap |
| AI-generated drift | Architecture violations | Reviewer checks the dependency rule and ownership on every change |
