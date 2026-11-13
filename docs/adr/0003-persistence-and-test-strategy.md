# ADR-0003: Persistence and Test Strategy — SQLite with EF Core

- **Status:** accepted
- **Date:** 2026-11-11
- **Decision owner:** architect, approved by the user's criterion
- **Related requirements:** R-07, R-11, R-13, R-19, R-28
- **Supersedes:** the PostgreSQL-by-default assumption in `docs/03-stack-baseline.md`

## Context

The brief requires "a database or other data storage solution" with at least two
tables: application data and users. It also requires persistence through a data
access layer and unit tests for that layer.

The user's criterion is explicit: **"it must be something that's easy to
recreate."** The review environment has no PostgreSQL, no Docker, and no admin
guarantee. The deadline is Friday 2026-11-13, so every setup step that depends on
the reviewer's machine is a risk.

The earlier baseline chose PostgreSQL. That choice is still valid for production,
but it adds a service install, credentials, and container tooling to a two-day
demo whose evaluation criteria are Clean Architecture, testing, code quality,
functionality, presentation, and GenAI fluency — not database operations.

## Decision

Use **SQLite** as the runtime database, through **EF Core** and the
`Microsoft.EntityFrameworkCore.Sqlite` provider.

- The database is a single file, `crudworks.db`, in the API's working directory.
- The connection string comes from configuration
  (`ConnectionStrings__Default`); the development default is
  `Data Source=crudworks.db`.
- Migrations live in `CrudWorks.Infrastructure` and are committed.
- Migrations are applied at startup in Development; production would apply them
  explicitly in a release step.
- The seed runs only in Development and only when the `Users` table is empty.
- Reset procedure: stop the API, delete `crudworks.db`, start the API. Migrations
  and seed recreate the database with no manual SQL.
- Tests use SQLite too: a per-test database (unique file or shared in-memory
  connection held open for the fixture). No Docker, no Testcontainers, no shared
  mutable database across tests.
- The repository interfaces stay in `Application`; the EF implementation stays in
  `Infrastructure`. The boundary is provider-agnostic.

### Schema rules that survive the provider swap

- `User`: `Id` (Guid PK), `Email` (normalized, unique index, max 256),
  `DisplayName` (max 100), `PasswordHash`, `CreatedAtUtc`.
- `TaskItem`: `Id` (Guid PK), `UserId` (FK, indexed), `Title` (max 200),
  `Description` (max 2000), `Status` (string enum), `DueDate` (nullable
  `DateOnly`), `CreatedAtUtc`, `UpdatedAtUtc`.
- `TaskItemStatus` is stored as a string for readable data and stable migrations.
- Timestamps are UTC. `DateOnly` is used for a due date with no time component.
- Foreign keys are enforced; deleting a user is not a feature in this exercise.
- No raw SQL. No provider-specific functions in `Application`.

## Alternatives considered

| Option | Pros | Cons | Why not chosen |
| --- | --- | --- | --- |
| SQLite file (chosen) | Zero install, zero service, zero credentials; reset by deleting one file; works offline; same EF Core model | Not the production target; single writer; provider behavior differs | Matches the user's "easy to recreate" criterion and the two-day deadline |
| PostgreSQL 17 native | Production parity; realistic operations | Needs an admin install, a service, a password, and a port; reset needs `psql` or a script | Setup risk on the reviewer's machine, not required by the brief |
| PostgreSQL in Docker | Reproducible; closest to production | Docker/WSL2 is unavailable; install and reboot risk before Friday | Fails the reproducibility criterion in this environment |
| SQL Server LocalDB | Familiar to .NET panels | Windows-only; heavier install; not requested | Worse fit than SQLite for a portable demo |
| EF Core InMemory provider | Fastest tests | Not a real database; hides relational behavior; not a demo database | Tests would stop proving persistence |

## Consequences

### Positive

- A reviewer clones the repository, runs the API, and gets a working database
  with seed data; there is no install step beyond the .NET SDK.
- The reset is one file deletion; the seed is repeatable.
- Infrastructure and API tests run in CI without Docker or service containers.
- The EF Core model, repositories, migrations, and seed remain real; the
  architecture story is unchanged.

### Negative / trade-offs

- SQLite is not the production database. Concurrency, backup, and operational
  behavior differ from PostgreSQL.
- Provider-specific SQL and migrations must be regenerated if the app moves to
  PostgreSQL; the schema rules above are designed to make that a provider swap,
  not a rewrite.
- SQLite's single-writer model is acceptable for the demo and the test suite; it
  is not a claim about production throughput.

### Production swap (documented, not implemented)

1. Add `Npgsql.EntityFrameworkCore.PostgreSQL`.
2. Change the provider in `Infrastructure` registration and the connection string.
3. Regenerate the migration baseline for PostgreSQL.
4. Run the same integration tests against a PostgreSQL container or service.
5. Keep the repository interfaces and the Application layer unchanged.

The swap is intentionally out of scope for Friday. The README and ADR state it
plainly instead of pretending SQLite is production.

## Validation

- Delete `crudworks.db`, start the API, and confirm migrations and seed recreate
  it with the demo user and at least five tasks.
- Run the unique-email test against SQLite; confirm the constraint is enforced.
- Run the IDOR tests for read, update, and delete against a real SQLite database.
- Confirm every test creates or resets its own database; no test depends on
  another test's data.
- The reviewer checks that no provider-specific code leaked into `Application`.

## Links

- `docs/00-brief.md` — R-07, R-11, R-13, R-19.
- `docs/02-definition-of-done.md` §3 and §6.
- `docs/adr/0001-clean-architecture.md` — repository boundary.
- `docs/06-session-checkpoint.md` §7 — the user's criterion and the decision.
