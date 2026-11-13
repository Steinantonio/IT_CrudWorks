# TASKS-001 — Authentication

- **Spec:** `specs/001-auth/spec.md`
- **Plan:** `specs/001-auth/plan.md`
- **Status:** implemented
- **Last updated:** 2026-11-12

## Tasks

| ID | Task | Owner | Depends on | Test / evidence | Status |
| --- | --- | --- | --- | --- | --- |
| T-001-01 | `User` entity with normalized email and invariant guards | Backend | — | `UserTests` | done |
| T-001-02 | Application abstractions: repository, hasher, token service, DTOs | Backend | T-001-01 | compiles; dependency rule | done |
| T-001-03 | `AuthService.RegisterAsync`: validate, normalize, hash, persist | Backend | T-001-02 | `Register…` tests | done |
| T-001-04 | Duplicate email returns `409` under a real unique index | Backend | T-001-03 | `Register_DuplicateEmail…` | done |
| T-001-05 | `AuthService.LoginAsync`: verify hash, generic failure, issue token | Backend | T-001-02 | `Login…` tests | done |
| T-001-06 | `GetCurrentUserAsync` scoped by the token subject | Backend | T-001-03 | `Me…` tests | done |
| T-001-07 | EF `UserConfiguration` + migration | Backend | T-001-01 | `Infrastructure` tests | done |
| T-001-08 | `PasswordHasherAdapter` in Infrastructure | Backend | T-001-02 | login integration test | done |
| T-001-09 | `JwtTokenService` + validated `JwtOptions` | Backend | T-001-02 | token round-trip test | done |
| T-001-10 | `AuthController` + ProblemDetails mapping | Backend | T-001-03..09 | `AuthApiTests` | done |
| T-001-11 | JWT bearer + `[Authorize]` + `[AllowAnonymous]` | Backend | T-001-09 | `401`/`200` API tests | done |
| T-001-12 | Seed demo user | Backend | T-001-07 | clean reset + login | done |
| T-001-13 | Login and register screens | Frontend | T-001-10 | `LoginPage.test.tsx`, `RegisterPage.test.tsx` | done |
| T-001-14 | Auth guard and session module | Frontend | T-001-13 | `RequireAuth.test.tsx`, `session.test.ts` | done |

## Notes

- T-001-03 and T-001-05 must start with a failing test.
- T-001-04 needs both an application check and a database unique index.
- T-001-12 is shared with TASKS-002; do it once in the seed.
- All 14 tasks are `done`. The auth slice was independently reviewed
  (`docs/reviews/review-auth-slice.md`): 0 blockers, 1 major (login timing
  side-channel), 4 minors, 3 nits — all resolved, including the constant-time
  verification on the unknown-email path. Evidence: `docs/ai/003`, `docs/ai/004`,
  `docs/ai/005`, `docs/ai/006`.
- The spec moves to `implemented` (see `specs/README.md` §2): every acceptance
  criterion has automated coverage, the clean-database run proves the live
  behaviour, and the review has no open findings.
