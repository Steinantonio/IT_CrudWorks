# IT CrudWorks

A full-stack task manager built as the deliverable for the **BLA .NET technical
interview exercise**: authenticated users manage **their own** tasks through a
.NET 10 API (Clean Architecture, TDD) and a responsive React client. Every
design decision, test, and AI correction is documented so a review panel can
inspect any behavior and get a defensible answer.

**Demo credentials:** `demo@crudworks.local` / `Demo123!` — seeded automatically
in Development (development/demo only; do not reuse elsewhere).

## Quick start

Prerequisites: [.NET SDK 10.0.404](https://dotnet.microsoft.com) (pinned in
`global.json`) and Node.js LTS with npm 11+. No database server — SQLite is a
single file created by migrations.

```powershell
# 1. Backend (from project/)
dotnet run --project src/CrudWorks.Api --launch-profile http
# -> http://localhost:5080 (migrations + demo seed run automatically in Development)

# 2. Frontend (from project/frontend/)
npm install
npm run dev
# -> http://localhost:5173 (proxies /api to http://localhost:5080)
```

Open `http://localhost:5173`, use the **Use demo account** button on the login
screen (fills the seeded credentials), and manage the five seeded tasks. To
rebuild the database from scratch, stop the API, delete
`src/CrudWorks.Api/crudworks.db`, and start the API again.

## Stack

| Layer | Choice |
| --- | --- |
| Backend | .NET 10 (ASP.NET Core Web API), C# with nullable reference types, EF Core 10 + SQLite |
| Frontend | React 19, TypeScript, Vite 8, TanStack Query, React Hook Form + Zod |
| Tests | xUnit + NSubstitute + `WebApplicationFactory` (backend), Vitest + Testing Library (frontend) |
| Auth | JWT bearer (HMAC-SHA256, 60 min), passwords via `PasswordHasher<User>` (PBKDF2) |

## Architecture

Clean Architecture with a visible dependency rule, enforced by project
references (not convention):

```
CrudWorks.Domain          (no dependencies — entities, invariants)
        ▲
CrudWorks.Application     (Domain only — use cases, ports, DTOs, validation)
        ▲
CrudWorks.Infrastructure  (Application — EF Core, repositories, JWT, hashing)
        ▲
CrudWorks.Api             (composition root — thin controllers, DI, auth wiring)
```

- **Business rules live in Application** (`AuthService`, `TaskService`,
  validators) and **invariants live in Domain entities** — never in controllers,
  `DbContext`, or React components. No EF Core or ASP.NET types in either core
  project.
- **Controllers are thin**: bind, delegate, map, return. Ownership always comes
  from the JWT subject, never the request body.
- **Repositories are explicit per aggregate** (`ITaskRepository`,
  `IUserRepository` as Application ports; EF Core implementations in
  Infrastructure). No generic repositories, no MediatR/CQRS/AutoMapper.
- **The React client** keeps one API client (`src/lib/api.ts`), one auth module
  (`src/auth/`), feature-first pages/components, and TanStack Query for server
  state.

Key decisions and their rationale: `docs/adr/` (architecture, auth strategy,
persistence, API contract, frontend structure).

## Database

SQLite (`Data Source=crudworks.db`), two tables, created by EF Core migrations:

| Table | Fields | Notes |
| --- | --- | --- |
| `Users` | `Id` PK, `Email` (unique, normalized), `DisplayName`, `PasswordHash`, `CreatedAtUtc` | Hash never returned by the API |
| `Tasks` | `Id` PK, `UserId` FK → Users (indexed), `Title`, `Description?`, `Status`, `DueDate?`, `CreatedAtUtc`, `UpdatedAtUtc` | `Status`: `Todo` / `InProgress` / `Done`, stored as string |

Migrations apply on startup in Development; a seeder then creates the demo
user and five story-shaped tasks (all three statuses, descriptions and due
dates) when the tables are empty. PostgreSQL is the documented production swap
(`docs/adr/0003-persistence-and-test-strategy.md`).

## API surface

Full contract (payloads, error bodies, examples): `docs/adr/0004-api-contract.md`.

| Method | Route | Auth | Success | Errors |
| --- | --- | --- | --- | --- |
| GET | `/api/health` | anonymous | 200 | — |
| POST | `/api/auth/register` | anonymous | 201 + `Location` | 400, 409 |
| POST | `/api/auth/login` | anonymous | 200 + token | 400, 401 |
| GET | `/api/auth/me` | bearer | 200 | 401 |
| GET | `/api/tasks?take=&cursor=` | bearer | 200 + `{items,nextCursor}` | 400, 401 |
| GET | `/api/tasks/{id}` | bearer | 200 | 401, 404 |
| POST | `/api/tasks` | bearer | 201 + `Location` | 400, 401 |
| PUT | `/api/tasks/{id}` | bearer | 200 | 400, 401, 404 |
| DELETE | `/api/tasks/{id}` | bearer | 204 | 401, 404 |

Errors use RFC 7807 `application/problem+json`. Another user's task returns
`404`, never `403` — the API does not confirm the resource exists (IDOR seam).
`GET /api/tasks` is keyset-paginated (ADR-0006): `take` defaults to 10 with a cap
of 100, and `cursor` is the opaque `nextCursor` from the previous page.

## Testing

| Suite | Command (from `project/`) | Current state |
| --- | --- | --- |
| Backend build | `dotnet build CrudWorks.slnx -warnaserror` | 0 warnings, 0 errors |
| Backend tests | `dotnet test CrudWorks.slnx` | 216 passing (Domain 37, Application 62, Infrastructure 28, Architecture 6, Api 83) |
| Frontend lint | `npm run lint` (in `frontend/`) | clean (oxlint) |
| Frontend tests | `npm run test` | 113 passing (13 files) |
| Frontend build | `npm run build` | production bundle |
| API end-to-end | `node docs/qa/e2e-api-check.js` (API running) | 40/40 checks from a clean database |

Coverage: 100% line coverage on `Domain` + `Application` (floor: 80%);
`docs/qa/coverage-summary.md` has per-file numbers. Backend tests use
per-factory temporary SQLite files — no test shares mutable state.

## Security notes

- Passwords hashed with `PasswordHasher<User>` (PBKDF2); never stored, returned,
  or logged in plaintext.
- Login returns the same `401` for unknown email and wrong password, and
  verifies in constant time (dummy hash on the unknown-email path).
- Failed logins are audited with the email **domain only**.
- No signing key in source: Development uses an ephemeral per-process key with a
  logged warning; any other environment fails fast without `Jwt:SigningKey`.
- CORS restricted to the configured frontend origin, explicit methods/headers.
- Every response carries CSP, `X-Content-Type-Options`, `Referrer-Policy`, and
  frame-ancestors/`X-Frame-Options`; HSTS is added outside Development (SPEC-007).
- The browser keeps the token in `sessionStorage` for the demo; the XSS
  trade-off and the production cookie alternative are documented in
  `docs/adr/0002-auth-strategy.md`.

## How this project was built

The workspace applies **Spec-Driven Development** with an AI agent team:
specs define behavior before code (`specs/`), six specialized agents own phases
(`AGENTS.md`, `agents/`), every AI-assisted change has an evidence entry with
prompt, output, validation, corrections and rejected suggestions
(`docs/ai/`), and requirement coverage is traced end to end
(`docs/04-requirement-traceability.md`, R-01…R-30).

| Want to see | Go to |
| --- | --- |
| Requirement coverage (R-01…R-30) | `docs/04-requirement-traceability.md` |
| User story and demo script | `docs/user-story.md`, `docs/presentation.md` |
| Architecture decisions | `docs/adr/` |
| Independent reviews | `docs/reviews/` |
| QA evidence (clean runs, logs, tools) | `docs/qa/` |
| GenAI evidence log | `docs/ai/` |
| The original exercise brief | `InstructionsEx/` + structured summary in `docs/00-brief.md` |

## License

MIT — see [LICENSE](LICENSE).
