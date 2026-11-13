# CrudWorks

A small full-stack task manager built for the BLA .NET technical interview
exercise: authenticated users manage **their own** tasks through a .NET 10 API
(Clean Architecture, TDD) and a React client.

> Demo credentials: `demo@crudworks.local` / `Demo123!` — seeded automatically in
> Development. Development/demo only; do not reuse outside this exercise.
>
> **Status:** authentication (register/login/me) and task CRUD are implemented and
> tested. Every route in the table below is live.

## Stack

| Layer | Choice |
| --- | --- |
| Backend | .NET 10 (ASP.NET Core Web API), C# 14 (`LangVersion=latest`), EF Core 10 + SQLite |
| Frontend | React 19, TypeScript, Vite 8, TanStack Query, React Hook Form + Zod |
| Tests | xUnit + NSubstitute + `WebApplicationFactory` (backend), Vitest + RTL (frontend) |
| Auth | JWT bearer (HMAC-SHA256, 60 min), passwords via `PasswordHasher<User>` |

## Prerequisites

- .NET SDK **10.0.404** (pinned in `global.json`)
- Node.js LTS (tested with 26.13.1) and npm 11+
- No database server: SQLite is a single file created by migrations

## Run the backend

```powershell
cd project
dotnet run --project src/CrudWorks.Api --launch-profile http
# -> http://localhost:5080
```

On startup in Development the API applies migrations and seeds the demo user.
Without `Jwt:SigningKey` configured, Development uses an ephemeral per-process
key (tokens do not survive a restart, a warning is logged); any other
environment fails fast when the key is missing.

## Run the frontend

```powershell
cd project/frontend
npm install
npm run dev
# -> http://localhost:5173 (proxies /api to http://localhost:5080)
```

Open `http://localhost:5173`, sign in with the demo credentials, and manage the
seeded tasks. The login screen also has a **Use demo account** button that fills
the seeded credentials for you, so the demo starts in one click.

## Reset the database (easy recreate)

```powershell
# stop the API, then:
Remove-Item src/CrudWorks.Api/crudworks.db -ErrorAction SilentlyContinue
dotnet run --project src/CrudWorks.Api --launch-profile http
# migrations + seed recreate Users and Tasks
```

## Tests

```powershell
# backend (from project/)
dotnet build CrudWorks.slnx -warnaserror
dotnet test CrudWorks.slnx

# frontend (from project/frontend/)
npm run lint
npm run test
npm run build
```

Backend tests use per-factory temporary SQLite files; no test shares mutable
state.

Current counts: **166 backend tests** (Domain 37, Application 57,
Infrastructure 25, Api 47) and **80 frontend tests** (11 files).

### Coverage

```powershell
dotnet test CrudWorks.slnx --collect:"XPlat Code Coverage"
node ../docs/qa/tools/coverage-report.js --verify --write
```

The DoD floor is 80% line coverage on `Domain` + `Application`. Measured:
`Domain` 100.00%, `Application` 100.00%, **combined 348/348 = 100.00%**. Per-file
numbers and the exact uncovered lines are in
[`docs/qa/coverage-summary.md`](../docs/qa/coverage-summary.md).

### End-to-end API check

```powershell
# from the repository root, with the API running on http://localhost:5080
node docs/qa/e2e-api-check.js
```

37 checks over the real HTTP pipeline: the full CRUD path, the IDOR seams
(foreign task → `404`), anonymous access (`401`), the validation matrix
(`400`), duplicate email (`409`), and the rule that a client-supplied `userId`
is ignored. Exits non-zero on any failure. The leak detector has its own
self-test: `node --test docs/qa/e2e-api-check.test.js`.

The clean-reset procedure used by the QA run is also available as
`node docs/qa/tools/api-ctl.js reset`, which stops the API, deletes the
database files and restarts it so migrations and the seeder rebuild the
schema and the demo data.

## API summary

Full contract: `docs/adr/0004-api-contract.md`.

| Method | Route | Auth | Success | Errors |
| --- | --- | --- | --- | --- |
| GET | `/api/health` | anonymous | 200 | — |
| POST | `/api/auth/register` | anonymous | 201 + `Location` | 400, 409 |
| POST | `/api/auth/login` | anonymous | 200 + token | 400, 401 |
| GET | `/api/auth/me` | bearer | 200 | 401 |
| GET | `/api/tasks` | bearer | 200 + list | 401 |
| GET | `/api/tasks/{id}` | bearer | 200 | 401, 404 |
| POST | `/api/tasks` | bearer | 201 + `Location` | 400, 401 |
| PUT | `/api/tasks/{id}` | bearer | 200 | 400, 401, 404 |
| DELETE | `/api/tasks/{id}` | bearer | 204 | 401, 404 |

Errors use RFC 7807 `application/problem+json`; validation failures include an
`errors` map. Another user's task returns `404`, never `403`: the API does not
confirm that the resource exists.

Example:

```powershell
$body = @{ email = 'demo@crudworks.local'; password = 'Demo123!' } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/auth/login `
  -ContentType 'application/json' -Body $body
```

## Architecture

```
CrudWorks.Domain          (no dependencies)
        ▲
CrudWorks.Application     (Domain only — use cases, ports, DTOs, validation)
        ▲
CrudWorks.Infrastructure  (Application — EF Core, repositories, JWT, hashing)
        ▲
CrudWorks.Api             (composition root — controllers, DI, auth wiring)
```

- Business rules live in `Application` (`AuthService`, validators); invariants
  live in `Domain` entities; controllers are thin.
- Repository interfaces live in `Application`; EF Core implementations live in
  `Infrastructure`. No generic repositories, no MediatR/CQRS/AutoMapper.
- The React client keeps one API client (`src/lib/api.ts`), one auth module
  (`src/auth/`), and TanStack Query for server state.

## Security notes

- Passwords: PBKDF2 via `PasswordHasher<User>`; never stored, returned, or logged
  in plaintext.
- Login returns the same `401` for an unknown email and a wrong password, and
  verifies in constant time (a dummy hash on the unknown-email path).
- Failed logins are logged with the email **domain only**.
- Secrets: no signing key in source; Development generates an ephemeral key and
  logs a warning; production fails fast without `Jwt__SigningKey`.
- CORS is restricted to the configured frontend origin.
- The browser keeps the token in `sessionStorage` for the demo; the XSS trade-off
  and the production cookie alternative are documented in `docs/adr/0002-auth-strategy.md`.

## Where to look

| Want to see | Go to |
| --- | --- |
| Domain invariants | `src/CrudWorks.Domain/Users/User.cs` |
| Use cases and validation | `src/CrudWorks.Application/Auth/AuthService.cs` |
| Persistence and seed | `src/CrudWorks.Infrastructure/Persistence/` |
| Thin controllers and error mapping | `src/CrudWorks.Api/Controllers/`, `ErrorHandling/` |
| Auth client | `frontend/src/auth/` |
| Decision records | `../docs/adr/` |
| Specs and acceptance criteria | `../specs/` |
| AI usage evidence | `../docs/ai/` |
