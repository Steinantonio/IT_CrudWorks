# AI Evidence Log

## Entry 003

- **Date:** 2026-11-12
- **Agent:** backend
- **Task:** SPEC-001 backend slice — tasks T-001-01 through T-001-12
- **Tool/model:** OpenCode subagent (DeepSeek V4.1 Flash Free)

## Prompt

The orchestrator asked for the complete authentication backend slice in the existing
four-project scaffold: `User` entity with normalization and invariants; Application
abstractions (`IUserRepository`, `IPasswordHasher`, `ITokenService`, `IAuthService`),
DTOs, typed exceptions, validation, and `AuthService`; EF Core `DbContext`,
configuration, repository, `PasswordHasher<User>` adapter, JWT service, migration, and
Development seed; thin `AuthController`, ProblemDetails mapping, JWT bearer wiring, and
`ICurrentUser` backed by `IHttpContextAccessor`. Constraints: TDD, no MediatR/CQRS/
AutoMapper/generic repository, no framework types in Domain/Application, no secrets in
git, no password or hash in responses or logs, real command output, no commits.

## Output summary

Created the Domain entity, the Application use case and abstractions, the EF Core
persistence layer with the `InitialCreate` migration, the security adapters, the API
controller and error handling, and 52 tests across four test projects. The register
response uses a relative `Location: /api/auth/me`; login returns the JWT, expiry, and
user summary; `/api/auth/me` resolves the caller from the `sub` claim.

## Validation

- [x] Build: `dotnet build CrudWorks.slnx -warnaserror` → `0 Aviso(s) 0 Erro(s)` (success)
- [x] Unit tests: `dotnet test CrudWorks.slnx` → Domain 15, Application 15, Infrastructure 11, Api 11; 0 failed, 0 skipped (52 total)
- [x] Integration/API tests: `AuthApiTests` covers 201, 400 (values, omitted fields, malformed JSON), 409, 200 login, 401 wrong password, 401 unknown email, 401 no/malformed/expired token, 200 `/me`
- [x] Manual behavior: ran the built API on `http://127.0.0.1:5199` against a temp SQLite file; health 200, register 201, duplicate 409, invalid 400, demo login 200, `/me` 200 with token and 401 without
- [x] Security review: only `PasswordHasher<User>` hashes passwords; responses contain no hash; no secrets in `appsettings.json`; CORS restricted to `http://localhost:5173`; `[AllowAnonymous]` only on register and login
- [x] Architecture review: `Domain` has no references; `Application` references `Domain` only; `Infrastructure` references `Application`; `Api` is the composition root

## Corrections

| What the AI suggested | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| `IUserRepository.AddAsync` as the only persistence call | ADR-0001 line 38 says repositories never save implicitly and `IUnitOfWork` exposes `SaveChangesAsync` | Added `IUnitOfWork`; `AuthService` calls `SaveChangesAsync` explicitly | Keep the ADR's transaction boundary and the brief's repository signature |
| `CreatedAtAction(nameof(Me), response)` | Produces an absolute `Location`; ADR-0004 documents `/api/auth/me` | `Created(Url.Action(nameof(Me)), response)` | Return the relative path the contract documents |
| Default EF Core SQLite `DateTime` handling | Reads back `DateTimeKind.Unspecified`, so timestamps would serialize without `Z` | Value converter reattaches `DateTimeKind.Utc` on read | Contract requires ISO 8601 UTC with `Z` |
| `PasswordHasher<User>` called without a user instance | The generic API requires a user argument although the default PBKDF2 implementation ignores it | Adapter passes `null!` with a comment; malformed hashes return `false` instead of throwing | Keep the Application contract password-only without a fake user |
| API tests with a per-factory in-memory connection | Replacing EF Core's `AddDbContext` registration depends on internal service descriptors; Windows file locking is a known hazard | Unique SQLite file per factory (`Pooling=False`), deleted on dispose | Deterministic, exercises the real `AddDbContext` + migration path; verified no stray database is created |

## Edge cases considered

- Duplicate email with different case and surrounding spaces → normalized before lookup → 409.
- Omitted JSON fields → Application validation returns `email`, `displayName`, `password` errors (camelCase keys).
- Malformed JSON → framework 400 as `application/problem+json`.
- Wrong password and unknown email return the same status and title (`Invalid email or password`).
- No token, malformed token, and expired token all return 401; the no-token title is `Authentication required`.
- A valid token whose user no longer exists returns 401 from `GetCurrentUserAsync`.
- Email longer than 256 and display name outside 2–100 are rejected; passwords are never trimmed.
- The domain unique index rejects a duplicate even if the application pre-check is bypassed.
- No password, hash, token, or authorization header is logged.

## Auth and validation decisions

- Email format uses `System.Net.Mail.MailAddress` with an exact-address check; this accepts
  `demo@crudworks.local`, which a TLD-requiring regex would reject.
- Login validates only that the email is well formed and the password is present, so any
  wrong password still returns the generic 401 instead of a 400 that hints at the input.
- Development without `Jwt:SigningKey` generates one 64-byte base64 key per process and
  logs a warning; other environments throw during startup. Tests inject a fixed key.
- `MapInboundClaims = false` keeps the raw `sub` claim so `ICurrentUser` reads it directly.

## Files affected

- `project/src/CrudWorks.Domain/Users/User.cs`
- `project/src/CrudWorks.Application/Auth/*`, `Common/*`, `Users/*`
- `project/src/CrudWorks.Infrastructure/Persistence/*`, `Security/*`
- `project/src/CrudWorks.Api/Program.cs`, `Controllers/AuthController.cs`, `Authentication/*`, `ErrorHandling/*`, `Security/*`, `appsettings.json`
- `project/tests/CrudWorks.Domain.Tests/Users/UserTests.cs`
- `project/tests/CrudWorks.Application.Tests/Auth/AuthServiceTests.cs`
- `project/tests/CrudWorks.Infrastructure.Tests/Persistence/UserRepositoryTests.cs`, `Security/*`
- `project/tests/CrudWorks.Api.Tests/CrudWorksApiFactory.cs`, `Auth/AuthApiTests.cs`, `HealthTests.cs`

## Final decision

Accepted. All 12 backend tasks are implemented and verified with real build and test
output; frontend tasks T-001-13 and T-001-14 remain with the frontend owner.

## Review follow-up (2026-11-12, review-auth-slice.md)

Addressed four review findings on the auth slice, ordered by severity:

1. **MAJOR — login timing side-channel (constant-time verification).**
   `IPasswordHasher.Verify` now accepts a nullable hash (`string?`); the Application
   contract documents why (`IPasswordHasher.cs:5-7`). `PasswordHasherAdapter.Verify`
   substitutes a precomputed dummy hash — produced once, lazily and statically, by
   hashing a 16-character random password with the real `PasswordHasher<User>` — when
   the stored hash is null or empty, and still runs the full PBKDF2 verification
   (`PasswordHasherAdapter.cs:22-36`), so the unknown-email path costs the same as the
   wrong-password path. `AuthService.LoginAsync` calls `Verify` exactly once on both
   paths (`AuthService.cs:52`), with no `user is null` branch before the verify, and
   throws the same generic `UnauthorizedException`. A defensively-guarded `user is null`
   after a (practically impossible) dummy-hash success still throws 401
   (`AuthService.cs:53`).
2. **MINOR — failed-login audit logging (SPEC-001 §8).** New Application port
   `ILoginAudit.FailedLogin(string emailDomain)` (`Auth/ILoginAudit.cs`), synchronous by
   design (documented on the interface). `AuthService.LoginAsync` calls it on every
   failed login — unknown email or wrong password — with only the substring after the
   last `@` of the normalized email (`AuthService.cs:55-57`); the local part, full
   email, and password are never passed. Implemented in the Api layer as
   `LoginAudit` backed by a dedicated `ILogger<LoginAudit>` category
   (`Api/Security/LoginAudit.cs`), registered as a singleton in DI (`Program.cs:57`);
   Application stays free of framework types. Successful logins do not audit.
3. **MINOR — overlong-email regression guard.** Added an `[InlineData]` case to
   `RegisterAsync_WithInvalidInput` with a well-formed 262-character email (262 > the
   256 maximum), asserting a `ValidationException` (400), not a `DomainException` (500).
4. **NIT — CORS fidelity to ADR-0004 §6.** `Program.cs` now uses
   `WithHeaders("Content-Type", "Authorization")` and
   `WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")` with the configured origin
   instead of `AllowAnyHeader().AllowAnyMethod()`; comment updated to cite the ADR.

Not changed (per instruction): the JWT flow, the controller, the migration, the seed,
the frontend, the API contract, and the accepted `PasswordHasherAdapter` `null!` user
argument (being recorded in ADR-0002 by the architect).

### Validation commands (real output)

- `dotnet build CrudWorks.slnx -warnaserror` → `Compilação com êxito. 0 Aviso(s) 0 Erro(s)` (Tempo Decorrido 00:00:09.25)
- `dotnet test CrudWorks.slnx --no-build` → Domain 15, Application 19, Infrastructure 13, Api 11; `Com falha: 0` (58 total, was 52)

New/changed tests:

- `LoginAsync_WithUnknownEmail_VerifiesAgainstNullHashAndThrowsGenericMessage` (replaces
  `LoginAsync_WithUnknownEmail_ThrowsUnauthorizedWithGenericMessage`; now asserts `Verify`
  WAS called with a null hash)
- `LoginAsync_FailurePaths_ThrowTheSameExceptionTypeAndMessage` (new)
- `LoginAsync_WithUnknownEmail_AuditsOnlyTheEmailDomain` (new)
- `LoginAsync_WithWrongPassword_AuditsOnlyTheEmailDomain` (new)
- `LoginAsync_WithValidCredentials_ReturnsTokenAndUserSummary` (extended: asserts no audit on success)
- `RegisterAsync_WithInvalidInput` (new overlong-email `[InlineData]` case)
- `Verify_WithNullHash_ReturnsFalse`, `Verify_WithEmptyHash_ReturnsFalse` (new, Infrastructure)

## Delegation brief (verbatim)

The orchestrator sent this prompt to the backend implementer subagent:

```text
Implement the backend slice for SPEC-001 (authentication) in the existing IT CrudWorks scaffold.

Workspace root: C:\Trabalho\IT_CrudWorks. Work only under project/, docs/ai/, and the status column of specs/001-auth/tasks.md.

READ FIRST (in this order):
- AGENTS.md
- specs/README.md and specs/constitution.md
- specs/001-auth/spec.md, plan.md, tasks.md
- docs/adr/0001-clean-architecture.md, 0002-auth-strategy.md, 0004-api-contract.md
- docs/02-definition-of-done.md, docs/03-stack-baseline.md
- Current project/src/** and project/tests/** code

CURRENT SCAFFOLD FACTS (verified):
- .NET SDK 10.0.404; project/global.json pins it.
- project/CrudWorks.slnx has 4 src projects (Domain, Application, Infrastructure, Api) and 4 xUnit test projects.
- Directory.Build.props sets net10.0, nullable, warnings as errors.
- Packages are pinned to 10.0.15 (EF Core Sqlite/Design, Identity.Core in Infrastructure; JwtBearer, EF Design, OpenApi in Api; Mvc.Testing, NSubstitute, EF Sqlite in tests).
- Program.cs already has controllers, OpenAPI, ProblemDetails, CORS, health at /api/health, public partial class Program.
- Domain has only Common/DomainException.cs. There is no DbContext yet.

SCOPE: tasks T-001-01 through T-001-12. Backend only. Do NOT touch project/frontend/**.

REQUIRED DESIGN:

Domain (`CrudWorks.Domain.Users.User`):
- Id (Guid), Email, DisplayName, PasswordHash, CreatedAtUtc.
- Static factory `Create(string email, string displayName, string passwordHash)` that trims/normalizes email to lowercase, validates non-empty and max lengths, sets Guid.NewGuid and DateTime.UtcNow.
- Private parameterless constructor for EF.
- No framework types. Add domain tests for normalization and invalid input.

Application (`CrudWorks.Application`):
- Namespaces: `Auth`, `Users`, `Common`.
- `IUserRepository`: `Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken)`, `Task AddAsync(User user, CancellationToken)`, `Task<User?> FindByIdAsync(Guid id, CancellationToken)`.
- `IPasswordHasher`: `string Hash(string password)`, `bool Verify(string password, string hash)`.
- `ITokenService`: `TokenResult Create(User user)` with `AccessToken` and `ExpiresAtUtc`.
- `IAuthService`: `Task<AuthUserResponse> RegisterAsync(RegisterRequest, CancellationToken)`, `Task<LoginResponse> LoginAsync(LoginRequest, CancellationToken)`, `Task<AuthUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken)`.
- DTOs: `RegisterRequest`, `LoginRequest`, `AuthUserResponse`, `LoginResponse`, `UserSummary`.
- `AuthService` implements validation with field-level errors, duplicate email -> ConflictException, wrong credentials -> UnauthorizedException with the generic message "Invalid email or password".
- Password rules: 8–128 chars, at least one letter and one digit. Email: valid format, max 256. DisplayName: 2–100 after trim.
- Typed exceptions in `Common/Exceptions`: `ValidationException` with `IReadOnlyDictionary<string,string[]> Errors`, `ConflictException`, `UnauthorizedException`, `NotFoundException`.
- No EF Core, ASP.NET Core, or HTTP types in Application.

Infrastructure (`CrudWorks.Infrastructure`):
- `Persistence/CrudWorksDbContext` with `DbSet<User> Users`.
- `Persistence/Configurations/UserConfiguration`: table Users, Email max 256 required + unique index, DisplayName max 100, PasswordHash required, CreatedAtUtc required.
- `Persistence/UserRepository` implementing Application interface, AsNoTracking for reads.
- `Security/PasswordHasherAdapter` wrapping `Microsoft.AspNetCore.Identity.PasswordHasher<User>`.
- `Security/JwtTokenService` using `JwtOptions` from Application: HMAC-SHA256, issuer CrudWorks, audience CrudWorks.Client, 60-minute expiry, claims sub/email/name/jti.
- `Persistence/Seed/DatabaseSeeder` (or equivalent) that runs migrations and seeds the demo user `demo@crudworks.local` / `Demo123!` with display name "Demo User" when the Users table is empty. Tasks are NOT part of this slice.
- Migration `InitialCreate` committed under `Persistence/Migrations`.
- If `System.IdentityModel.Tokens.Jwt` is required in Infrastructure, pin version 8.19.2 (the version resolved by JwtBearer 10.0.15). Do not let NuGet resolve .NET 11 packages.

Api (`CrudWorks.Api`):
- `Controllers/AuthController`: POST /api/auth/register (201 + Location /api/auth/me + user body), POST /api/auth/login (200 + token + user), GET /api/auth/me ([Authorize], 200 + user).
- Exception mapping: ValidationException -> 400 ProblemDetails with `errors`; ConflictException -> 409; UnauthorizedException -> 401; NotFoundException -> 404. Use an exception handler/filter/middleware; keep controllers thin.
- JWT bearer authentication configured and validated. Resolve the current user id from the `sub` claim; add an `ICurrentUser` abstraction in Application and an Api implementation backed by IHttpContextAccessor.
- Signing key: bind `Jwt:SigningKey`. In Development only, if absent, generate an ephemeral 64-byte base64 key once per process and log a warning; non-Development must fail fast. Tests inject a fixed key. Never commit a signing key.
- Apply migrations and run the seed at startup in Development only (tests use the same path).
- Configure `JsonStringEnumConverter` only when needed for later task statuses (not required for this slice).
- Keep `public partial class Program`.

TESTS (must be real and pass):
- Domain: `UserTests` for email normalization, display-name trim, invalid inputs.
- Application: `AuthServiceTests` with NSubstitute fakes for duplicate email, hashing call, invalid credentials, token issuance, current user.
- Infrastructure: `UserRepositoryTests` with a real per-test SQLite database (unique file or open in-memory connection), including the unique email constraint.
- Api: `AuthApiTests` using a custom `WebApplicationFactory<Program>` that replaces the DbContext with a per-factory SQLite in-memory connection and sets a fixed JWT key. Cover 201, 409, 400 with field errors, 200 login, 401 wrong password, 401 unknown email (same body), 401 /me without token, 200 /me with token.
- Use one database per test class/factory; do not share mutable state across tests.

VERIFY AND CAPTURE REAL OUTPUT:
- `dotnet build CrudWorks.slnx -warnaserror` -> 0 warnings, 0 errors.
- `dotnet test CrudWorks.slnx` -> all tests pass; report counts.
- `dotnet ef migrations list -p src/CrudWorks.Infrastructure -s src/CrudWorks.Api` if useful.
- If a command fails, fix it; do not claim success without output.

DOCS:
- Update the status column of specs/001-auth/tasks.md (T-001-01..12).
- Write docs/ai/003-auth-slice.md using templates/ai-evidence-log.md: prompt summary, output, commands with real results, corrections, edge cases, files.
- Do not edit specs/001-auth/spec.md or plan.md; if the spec is wrong, report it instead.

RULES:
- No commits, pushes, resets, or clean.
- No secrets in git; no plaintext passwords in logs.
- No MediatR, CQRS, AutoMapper, generic repository, or extra features.
- Keep the dependency rule: Domain -> nothing, Application -> Domain, Infrastructure -> Application, Api -> both.

RETURN a handoff with: files created/changed, exact commands and short real output, test names/results, decisions/assumptions, blockers, and the next task. Be specific and honest.
```

