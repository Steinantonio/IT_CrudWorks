# PLAN-001 — Authentication implementation plan

- **Spec:** `specs/001-auth/spec.md`
- **Status:** approved
- **Owner:** architect
- **Last updated:** 2026-11-11

## 1. Architecture impact

| Layer | Change |
| --- | --- |
| Domain | `User` entity; invariant guards; no framework types. |
| Application | `IAuthService`, `IUserRepository`, `IPasswordHasher`, `ITokenService`, DTOs, `JwtOptions`, typed exceptions. |
| Infrastructure | `UserRepository`, `PasswordHasherAdapter` (wraps `PasswordHasher<User>`), `JwtTokenService`, EF configuration. |
| Api | `AuthController`, JWT bearer setup, exception mapping, DI wiring. |
| Frontend | Consumes the contract in SPEC-003; no backend types leak into the client. |

## 2. Data model

`Users`

| Column | Type | Constraint |
| --- | --- | --- |
| `Id` | TEXT (Guid) | PK |
| `Email` | TEXT | required, max 256, unique index |
| `DisplayName` | TEXT | required, max 100 |
| `PasswordHash` | TEXT | required |
| `CreatedAtUtc` | TEXT (DateTime) | required |

Email is stored normalized (trimmed, lowercase). The unique index is the final
guard against a race between two registrations.

## 3. Contract

`docs/adr/0004-api-contract.md` §3. Routes: register, login, me. Errors are
ProblemDetails; validation errors use the `errors` map.

## 4. Test strategy

| Layer | What is proven | Where |
| --- | --- | --- |
| Domain unit | `User` rejects empty email/hash and trims the display name | `tests/CrudWorks.Domain.Tests` |
| Application unit | Register hashes the password, rejects duplicates, normalizes email; login verifies the hash and returns a token | `tests/CrudWorks.Application.Tests` |
| Infrastructure integration | Unique email index; repository lookup; migration applies | `tests/CrudWorks.Infrastructure.Tests` |
| API integration | `201`, `409`, `400`, `200`, `401`, `me` with real HTTP pipeline | `tests/CrudWorks.Api.Tests` |
| Frontend component | Login/register states per SPEC-003 | `project/frontend/src/**/*.test.tsx` |

## 5. Task order

1. Domain `User` + tests (red/green).
2. Application abstractions and `AuthService` + tests with substitutes.
3. EF configuration, repository, migration.
4. Password hasher and JWT token service.
5. API controller, JWT middleware, exception mapping.
6. API integration tests for the full auth flow.
7. Seed demo user (shared with SPEC-002).
8. Frontend auth screens per SPEC-003.

## 6. Risks and fallbacks

| Risk | Impact | Fallback |
| --- | --- | --- |
| JWT configuration drift | `401` on valid tokens | One `JwtOptions` bound and validated at startup |
| Development signing key mistaken for a secret | Review finding | ADR-0002 note: dev fixture only; production requires an env var |
| Password hashing package in Application | Dependency-rule drift | `IPasswordHasher` in Application; `PasswordHasher<User>` only in Infrastructure |

## 7. Definition of done

- [ ] Every acceptance criterion in SPEC-001 has a test.
- [ ] `dotnet build -warnaserror` and `dotnet test` pass.
- [ ] No password, hash, or signing key in logs or responses.
- [ ] Seed user logs in after a clean database reset.
- [ ] AI evidence entry exists.
- [ ] Reviewer has no open blockers.
