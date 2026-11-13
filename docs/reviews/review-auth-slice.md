# Code Review: Authentication slice (SPEC-001 backend, SPEC-003 T-003-01…06)

- **Reviewer:** reviewer agent
- **Date:** 2026-11-12
- **Scope reviewed:** backend SPEC-001 (all tasks) + frontend SPEC-003 T-003-01…T-003-06, per the supplied change set. No git repo; files read directly.
- **Evidence basis:** code read line by line; build/test/live-run claims taken from `docs/ai/003-auth-slice.md`, `docs/ai/004-auth-ui-slice.md`, and the traceability matrix (not re-run, per orchestrator instruction).

## Verdict

**Request changes** — no blockers; one major (login timing side-channel) must be either fixed or explicitly accepted as a recorded risk before the demo. Everything else is minor or nit.

## Findings

| Severity | File:line | Finding | Why it matters | Suggested fix |
| --- | --- | --- | --- | --- |
| Major | `project/src/CrudWorks.Application/Auth/AuthService.cs:46-52` | Login timing side-channel: an unknown email returns immediately, a known email runs a PBKDF2 `Verify`. The response body and status are identical (tested), but response *time* distinguishes "email exists" from "email unknown". SPEC-001 §8 (`specs/001-auth/spec.md:133`) requires "no user enumeration". The unit test at `tests/CrudWorks.Application.Tests/Auth/AuthServiceTests.cs:98` explicitly codifies the skip (`DidNotReceive().Verify`). | Account enumeration is exactly what the panel probes on an auth exercise. Timing over a local network is clearly measurable; over WAN it works with enough samples. | Either (a) ~5-line fix: on the `user is null` branch, run `passwordHasher.Verify(request.Password!, DummyHash)` against a precomputed hash of a random password before throwing, and change the unit test to assert `Verify` was called in both branches; or (b) accept as a recorded risk: add it to ADR-0002's "Production path" section and SPEC-001 §8 with a change-log entry. Owner: backend. Decision owner: architect/orchestrator. |
| Minor | `project/src/CrudWorks.Application/Auth/AuthService.cs:39-57` | SPEC-001 §8 observability (`specs/001-auth/spec.md:135`): "failed logins log the email domain only" — nothing logs today. The exception handler logs only 500s (`project/src/CrudWorks.Api/ErrorHandling/ProblemDetailsExceptionHandler.cs:41-49`). | An approved spec rule has no implementation and no task; a reviewer following the traceability finds a dead row. | Add a warning log in `AuthService.LoginAsync` (or a host-side handler) containing only the substring after `@` — never the local part or password — plus a unit test; **or** amend SPEC-001 §8 with a change-log entry deferring it. Either way the spec and code must agree. Owner: backend + architect. |
| Minor | `project/src/CrudWorks.Infrastructure/Security/PasswordHasherAdapter.cs:17,23` | `HashPassword(null!, …)` / `VerifyHashedPassword(null!, …)`. Safe today: the shipped default `PasswordHasher<TUser>` never reads the user, the code comments it (`:15-16`), and it is recorded in `docs/ai/003` corrections. It is still a latent contract violation if the hasher is ever swapped for a user-aware implementation. | Passing `null` into a framework API is undefined behavior by contract, not by accident-proofing. | Cheapest honest resolution: keep as an **accepted risk** already recorded in the AI log, and add one sentence to ADR-0002 or the plan. Alternative: extend `IPasswordHasher` later; do not invent a fake `User` just to satisfy the signature. Owner: backend. |
| Minor | `project/frontend/src/pages/LoginPage.tsx:29-32` | The return-path guard rejects `//host` and non-`/` values, but no test exercises the rejection branch (`LoginPage.test.tsx` covers only the happy path; `RequireAuth.test.tsx` uses `/`). | The guard is a security-adjacent behavior (open-redirect style values). Un-tested security checks rot. | Add a LoginPage test: navigate to `/login` with `state.from = '//evil.example'`, submit valid credentials, assert navigation went to `/` (not the external value). Owner: frontend. |
| Minor | `project/src/CrudWorks.Application/Auth/AuthValidation.cs:59-66` | No register-level test for a well-formed email longer than 256 characters. The guard exists in `IsValidEmail` (`:63`); if it regressed, `User.Create` would throw `DomainException` → 500 instead of 400. The domain test (`UserTests.cs:46-51`) does not cover the Application path. | A validation regression would surface as a 500, and nothing would catch it. | Add one `[InlineData]` case with a 260-character valid-format email to the `RegisterAsync_WithInvalidInput` theory (`AuthServiceTests.cs:59-65`). Owner: backend. |
| Nit | `project/src/CrudWorks.Api/Program.cs:63-66` | CORS uses `AllowAnyHeader().AllowAnyMethod()`; ADR-0004 §6 (`docs/adr/0004-api-contract.md:220-222`) lists specific methods and headers. No credentials allowed, so risk is low. | Contract fidelity; a panel reading the ADR then the code sees drift. | `WithMethods("GET","POST","PUT","DELETE")` and explicit headers, or update the ADR. Owner: backend. |
| Nit | `project/src/CrudWorks.Infrastructure/Persistence/UnitOfWork.cs:18-22` | Every unique-constraint violation is reported as "Email already registered." Correct for this slice (Users is the only unique index); will misreport if SPEC-002 adds other unique indexes. | Future misattribution, not a current bug. | When task tables land, key the mapping on the table/index name or rethrow with the constraint name. Owner: backend. |
| Nit | `project/frontend/src/pages/RegisterPage.tsx:28-29` | Client password regex `[A-Za-z]`/`[0-9]` is stricter than the API's `char.IsLetter` (Unicode). API remains authoritative; a Unicode-letter password accepted by the server is rejected by the client form. | Minor validation drift, safe direction. | Accept, or mirror the API with a Unicode letter class. Owner: frontend. |

## Architecture checks

- [x] Dependency rule is respected — verified from project files: `CrudWorks.Domain.csproj` has no references; `CrudWorks.Application.csproj:4` references Domain only; `CrudWorks.Infrastructure.csproj:4` references Application only; `CrudWorks.Api.csproj:19-20` references both.
- [x] Business logic is not in controllers or `DbContext` — `AuthController.cs` binds/delegates/maps only; rules live in `AuthService`/`AuthValidation`/`User`.
- [x] No EF Core / ASP.NET Core / HTTP types in Domain or Application — `Application` usings are BCL + Domain only; the ASP.NET Core package (`Microsoft.Extensions.Identity.Core`) sits in Infrastructure, and `PasswordHasher<User>` is wrapped behind `IPasswordHasher` (Application, `Auth/IPasswordHasher.cs`).
- [x] Repository interfaces in Application (`Users/IUserRepository.cs`), EF implementation in Infrastructure (`Persistence/UserRepository.cs`); no generic repository.
- [x] Composition root in Api (`Program.cs`); controllers thin; no MediatR/CQRS/AutoMapper.

## Security checks

- [x] `[AllowAnonymous]` only on register/login (`AuthController.cs:14,27`); `/me` is `[Authorize]` (`:37`); health is anonymous and unauthenticated.
- [x] Ownership/IDOR: N/A for this slice — no task endpoints exist yet; the pattern is already in place (`NotFoundException` documented for ownership → 404, `Common/Exceptions/NotFoundException.cs:3-6`).
- [x] Generic 401: same title/status/type for unknown email and wrong password (`AuthService.cs:13,48-53`; tested at `AuthApiTests.cs:141-165`).
- [x] Passwords hashed only via `PasswordHasher<User>` (Infrastructure); response bodies assert no password/hash leak (`AuthApiTests.cs:36-38`); no password, hash, token, or auth header appears in any log statement in the change set.
- [x] No secrets in source: `appsettings.json`/`appsettings.Development.json` contain no signing key; the key resolves from config → ephemeral Development fallback with a warning (`Program.cs:32-49`, `Security/JwtSigningKey.cs:23-30`) → fail-fast elsewhere (`JwtSigningKey.cs:23-27`, forced at `Program.cs:72`). Test key is a fixed, non-secret fixture.
- [x] JWT validation: issuer, audience, lifetime, signing key validated; `MapInboundClaims = false` (`AuthenticationSetup.cs:18-30`); 401 challenge writes ProblemDetails with title `Authentication required` (`:32-45`; tested `AuthApiTests.cs:167-180`); token claims sub/email/name/jti (`JwtTokenService.cs:24-30`; round-trip tested).
- [x] Input validated at the boundary in Application, field keys `email`/`displayName`/`password` match ADR-0004 §5 and the client mapping (`AuthValidation.cs:26,31,36` vs `RegisterPage.tsx:35`).
- [x] CORS restricted to the configured origin, no `AllowCredentials` (`Program.cs:60-67`) — see nit on method breadth.
- [x] Error responses safe: ProblemDetails only, no stack traces; unexpected errors log method/path and return a generic title (`ProblemDetailsExceptionHandler.cs:41-49`).

## Test checks

- [x] AC-001-01…07 each have a named test (`AuthApiTests.cs:16,42,60,112,142,168,195` + Application/Infrastructure coverage).
- [x] AC-003-01…04 each have a component test (`RequireAuth.test.tsx:18`, `LoginPage.test.tsx:29,60`, `RegisterPage.test.tsx:15,36,64,92`, `App.test.tsx:28`).
- [x] Data layer: unique normalized email index verified against the real migration (`UserConfiguration.cs:21-22` = migration `InitialCreate.cs:29-33`); race guard tested with the pre-check bypassed (`UserRepositoryTests.cs:96-114`); UTC reattach converter tested (`UserConfiguration.cs:33-38`, `UserRepositoryTests.cs:62`).
- [x] Seed idempotent (`DatabaseSeeder.cs:33-36`), runs in Development only (`Program.cs:80-88`); API test factory exercises the real migrate+seed path against a per-factory temp SQLite (`CrudWorksApiFactory.cs:19-31`); Infrastructure tests use a per-test in-memory connection (`UserRepositoryTests.cs:13-28`).
- [x] Tests assert behavior (status codes, bodies, storage effects), not implementation.
- [x] Frontend tests are deterministic and isolated (`test/setup.ts` clears storage per test).
- [ ] Gaps worth naming: no negative test for the return-path guard; no register-level overlong-email case; no 128/129 password boundary case at the Application level (minor). See findings.

## Frontend checks

- [x] Only `auth/session.ts` touches storage (`AuthProvider.tsx:6`, no other `sessionStorage` use outside tests).
- [x] Auth state centralized in context; components never read the token (`Layout.tsx:5` uses `user` only); no `any`, no `console.*` in committed code (grep over `src/`: only a CSS false positive).
- [x] Guard sends no request (`RequireAuth.test.tsx:39` asserts `fetch` not called); return path preserved via `state.from` and accepted only for internal paths (`LoginPage.tsx:29-32`).
- [x] `handleUnauthorized` is a single idempotent seam (`AuthProvider.tsx:57-60`; double-click tested `AuthProvider.test.tsx:45-49`). Wiring into protected requests is T-003-11, correctly still `todo` in `specs/003-web-client/tasks.md:22` — not a conformance gap for this slice.
- [x] Forms mirror ADR-0004; server 400/409 mapped; labels, `aria-describedby`, `role="alert"`, disabled-while-pending, keyboard Enter submission tested.
- [x] No invented endpoints/fields — client uses exactly `/api/auth/login`, `/api/auth/register`, `/api/health`.
- [x] 360/768/1280 evidence captured in `docs/ai/004` (structural iframe measurements); real keyboard traversal honestly recorded as a limitation there.

## What I verified vs. could not verify

**Verified by reading code/tests/specs/ADRs** (all with `file:line` above): dependency rule, contract shape (201 + `Location: /api/auth/me` at `AuthController.cs:23`/`AuthApiTests.cs:27`; ProblemDetails content type; field-error keys; claim names; generic 401; challenge title), hashing path, key handling, CORS, seed/migration/UTC handling, frontend storage ownership, guard/no-request behavior, 401 seam idempotence, test-to-AC mapping for AC-001-01…07 and AC-003-01…04, task statuses current vs delivered scope.

**Not independently verified (accepted from recorded evidence per orchestrator instruction):** `dotnet build -warnaserror` 0/0, `dotnet test` 52 passed, `npm run lint/test/build` green, live API and UI smoke runs, viewport measurements. These are documented with command output in `docs/ai/003`, `docs/ai/004`, and `docs/04-requirement-traceability.md:61-79`; nothing in the code contradicts them.

**Open questions for the orchestrator:**

1. Demo credentials (`demo@crudworks.local` / `Demo123!`) are in the seed (`DatabaseSeeder.cs:9-13`) but I could not find them in any README (root README has no mention; `project/README.md` does not exist). R-19 is correctly `Planned`, but the demo depends on it — schedule before Friday.
2. When T-003-11 lands, AC-003-08 needs its "clears exactly once / no retry loop" test at the `api.ts` boundary, not only the context-level probe that exists today.

## Summary

- **Good:** clean dependency rule, thin controller, honest spec-to-test mapping for every auth criterion, well-documented trade-offs (AI logs are genuinely specific, including what the AI got wrong), no secrets, no leak channels in responses or logs.
- **Must change before merge/demo:** the login timing side-channel (fix or record as accepted risk), and the SPEC-001 §8 logging rule either implemented or amended.
- **Can wait:** the two missing tests (return-path guard, overlong email), CORS method tightening, the other nits.
