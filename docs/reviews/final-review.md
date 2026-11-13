# Code Review: Final delivery — IT CrudWorks (whole deliverable)

- **Reviewer:** reviewer agent (final gate review)
- **Date:** 2026-11-12
- **Scope reviewed:** the complete deliverable — `project/src/**` (4 backend projects),
  `project/tests/**` (4 xUnit projects), `project/frontend/**` (React SPA + 10 test
  files), `docs/ai/000`–`015`, `docs/qa/**`, `specs/**`, `docs/adr/0001`–`0005`,
  `docs/00-brief.md`, `docs/02-definition-of-done.md`, `docs/04-requirement-traceability.md`,
  `docs/presentation.md`, `README.md`, `project/README.md`.
- **Evidence basis:** every file below was read directly (there is no git repository, per
  `AGENTS.md` §12 and the DoD "Open items" table). This review covers the material written
  **after** the two prior reviews (`docs/reviews/review-auth-slice.md`,
  `docs/reviews/review-task-slice.md`): `docs/ai/013`, `014`, `015`, `docs/qa/**`,
  `docs/qa/tools/**`, and the wave-2/3 documentation.
- **Commands:** **not re-run in this review** — the reviewer session has no shell
  permission (`Permission denied: shell` for `dotnet --version`). Every command claim
  below is therefore quoted from the captured output in `docs/qa/` and `docs/ai/`, and
  marked **Fact (recorded)**. Every code claim is **Fact (read)** with `file:line`.
  Where I could not verify something, it is listed in "What I could not verify".

---

## Verdict

**Approve with comments** — 0 blockers, 2 majors, 6 minors, 5 nits.

No open blockers. The ownership/IDOR guarantee is airtight end to end, the JWT
handling fails safe outside Development, the error responses leak nothing, the CORS
policy is genuinely restrictive and is asserted on the real (non-preflight) path, and
every blocker/major from the two prior reviews is verifiably closed. The two majors
are **documentation-truth** defects, not behaviour defects: two release-evidence
artifacts still contradict facts the project has already corrected and recorded
elsewhere. Both are one-line edits and neither touches `project/**`.

The four open items in `docs/02-definition-of-done.md` are correctly open, with one
correction to the owner of the console item (see "DoD walkthrough").

---

## Findings

| Severity | File:line | Finding | Why it matters | Suggested fix |
| --- | --- | --- | --- | --- |
| Major | `docs/qa/test-plan.md:390`, `:408`, `:453` (also `:55`, `:300`, `:386`, `:425`, `:535`) | The QA test plan states that **the browser console was never observed** and that the frontend suite is the wave-1 measurement (**51 cases / 9 files, "not re-run in wave 2"**). `docs/ai/015` records a real-Chromium walkthrough of the whole demo flow with a clean console and **10 files / 69 tests**; `docs/02-definition-of-done.md:24` and `project/README.md:78-79` already carry the 69 figure. Two release-evidence artifacts in the same repository make directly contradictory claims about the same facts. | The test plan is the artifact the panel will use to decide "what is proven". A reviewer who finds "the console was never observed" and "the frontend is 51 tests" next to "the console was clean" and "69 tests" concludes that the evidence is not reconciled — which is precisely the criterion this exercise scores. It also makes the plan's own totals wrong (`165 + 51 + 37 + 6` at `:535` where the current figure is `165 + 69 + 37 + 6`). | Owner: **QA engineer**. Refresh §"Test pyramid" (frontend row → 10 files / 68 declarations / 69 cases, wave-3), §"Commands" frontend row (the wave-3 numbers from `docs/ai/015:88-101`), the "Risks" row and the "Not covered" bullet on the console (change "never observed" → "observed clean in a real Chromium session, `docs/ai/015`; not yet re-observed during the rehearsal"), tick the two exit-criteria boxes, and update the totals. |
| Major | `docs/04-requirement-traceability.md:117` | The "Known gaps" row still carries the **already-corrected** Infrastructure gap description: "migration `Up` methods, a `PasswordHasherAdapter` fallback, 3 seeder branches". `docs/qa/coverage-summary.md:245-264` and `docs/ai/013:211-215` / `docs/ai/014:232-237` explicitly correct all three: the uncovered migration lines are the **`Down` (rollback) bodies**, `PasswordHasherAdapter.cs` is **18/18 = 100.00%**, and the seeder contributes **5** lines (`:36-37`, `:61-63`), not three. The correction was propagated to three artifacts and not to the traceability matrix. | The traceability matrix is the artifact the DoD §11 names as the evidence gate ("The requirement traceability matrix has evidence for every row"). As written it says the **schema-apply path is unproven** — it is proven by `Migration_AppliesInitialCreate` (`project/tests/CrudWorks.Infrastructure.Tests/Persistence/UserRepositoryTests.cs:31`) and `Migration_AppliesAddTasks` (`.../TaskRepositoryTests.cs:41`). A false claim in the matrix, about the very thing the matrix exists to make honest, is the kind of thing a panel will check against `docs/qa/coverage-summary.md`. | Owner: **orchestrator**. Replace the row with the corrected list (design-time factory 13–16/18–19; the two migration `Down` bodies; `DatabaseSeeder.cs:36-37` and `:61-63`; `UnitOfWork.cs:34-35`) — the wording already exists at `docs/qa/coverage-summary.md:230-239`; copy it verbatim. |
| Minor | `docs/04-requirement-traceability.md:30` | R-24 claims "**nine** likely questions" in `docs/presentation.md` §9. §9 contains **eight** (`docs/presentation.md:168`, `:171`, `:173`, `:176`, `:179`, `:182`, `:185`, `:187`). | A numeric claim the panel can verify in twenty seconds, in the row whose whole point is "we prepared for the questions". | Owner: orchestrator. Change "nine" to "eight", or add the missing Q&A entry (the obvious gap is a rate-limiting / brute-force question, which ADR-0002 §"Negative" already flags as not implemented). |
| Minor | `docs/04-requirement-traceability.md:26`, `:36` | R-20 cites `docs/ai/000`–`014` and R-30 claims "**13** evidence entries". `docs/ai/` holds **16** entries (`000` … `015`), and `docs/02-definition-of-done.md:11` already says `000`–`015`. | The GenAI-evidence row understates the evidence the exercise explicitly scores, and contradicts the DoD's own evidence index. | Owner: orchestrator. R-20 → "`docs/ai/000`–`015`"; R-30 → "16 evidence entries". |
| Minor | `specs/README.md:108-111` | The "Current specs" table lists SPEC-001, SPEC-002 and SPEC-003 as **Approved**. Each spec's own header says **implemented** (`specs/001-auth/spec.md:3`, `specs/002-task-crud/spec.md:3`, `specs/003-web-client/spec.md:3`), each with a dated change-log row justifying the move (e.g. `specs/002-task-crud/spec.md:182`). | `specs/README.md` is the index a reviewer opens first; an index that contradicts the authoritative status of all three specs invites the question "is the spec lifecycle actually enforced, or just narrated?". | Owner: **architect** (index owner is the orchestrator per AGENTS.md §11; the content is the architect's). Update the table to `Implemented`. |
| Minor | `docs/adr/0001-clean-architecture.md:3` | ADR-0001 — the central Clean Architecture decision — is still `Status: proposed`, while ADR-0002/0003/0004/0005 are `accepted` and the code implements the decision in full. | A panel reading "proposed" next to a shipped four-project solution asks whether the architecture was ever actually accepted. The content of the ADR is final (the folder map is accurate except for one nit below); only the status is stale. | Owner: **architect**. Set `Status: accepted` with the approval date and owner, matching ADR-0003's "approved by the user's criterion" wording. |
| Minor | `project/README.md:17` | The stack table says "**C# 13**". The solution targets `net10.0` with `<LangVersion>latest</LangVersion>` (`project/Directory.Build.props:6`) under SDK **10.0.404** (`project/global.json:3`) — that is C# 14. | A one-word claim about the toolchain that is wrong, in the document the panel uses to set up the environment. Harmless in effect, but it is a false statement. | Owner: orchestrator (README owner). Write "C# 14 (net10.0)" or drop the version. |
| Minor | `project/frontend/package.json:31` | `@vitest/coverage-v8` is installed but **unused**: there is no `coverage` script (`package.json:6-13`) and no `coverage` block in `project/frontend/vite.config.ts:15-21`. DoD §7 checks "No dead code or unused dependencies". Same question for `Microsoft.EntityFrameworkCore.Design` in `project/src/CrudWorks.Api/CrudWorks.Api.csproj:12-15`, duplicated from Infrastructure and unexplained. | Unused dependencies are the classic panel question, and DoD §7 is explicitly checked. The `@vitest/coverage-v8` package appears to be scaffold-provided (`docs/ai/002`), so this is inherited rather than introduced — but the checkbox is ticked and the dependency is unused. | Owner: **frontend implementer** (`@vitest/coverage-v8`) / **backend implementer** (the EF Design package). Either remove them, or add a one-line comment saying why they must stay. The EF Design package in the startup project is defensible — the EF tools need it there — so a comment is the cheapest honest answer. |
| Nit | `docs/qa/test-plan.md:18`, `:507` | Cites `docs/ai/015-frontend-accessibility-pass.md`; the file is `docs/ai/015-frontend-accessibility-pass.md`. | A broken cross-reference in the release-evidence artifact. | Owner: QA. Fix the filename. |
| Nit | `docs/qa/e2e-api-check.js:195-203` | The script registers a new user (`qa-user-b+<timestamp>@crudworks.local`) on every run and never deletes it, so the "idempotent" claim (`docs/qa/clean-run-report.md:89-91`, `docs/qa/test-plan.md:176-177`) is true for the **tasks** but not for the `Users` table — each run leaves a row behind. | The idempotency claim is narrower than stated. Harmless for the demo (the seeded account is untouched), but a panel that reruns the script twice and then inspects `Users` will notice. | Owner: QA. Either delete the probe user at the end of the run, or narrow the claim to "the task set is restored; one scratch user is left per run". |
| Nit | `docs/adr/0001-clean-architecture.md:67` | The folder map lists `Controllers/{AuthController,TasksController,HealthController}.cs`; there is no `HealthController.cs` — health is `app.MapHealthChecks` in `project/src/CrudWorks.Api/Program.cs:103-114`. | A reader looking for `HealthController.cs` to judge "is the health check thin?" will not find it. | Owner: architect. Fix the map, or add one sentence saying health is a middleware-style endpoint and why. |
| Nit | `README.md:28`, `README.md:115-121` | "Reviews \| Two independent reviews" — there are now three, this one included. The repository map also omits `docs/qa/tools/inspect-coverage.js` and `docs/qa/logs/`. | The panel opens the root README first. | Owner: orchestrator. Update after this review lands. |
| Nit | `project/frontend/src/pages/RegisterPage.tsx:28-29` | The client password rule is `/[A-Za-z]/` + `/[0-9]/`; the API's rule is `char.IsLetter`/`char.IsDigit` (`project/src/CrudWorks.Application/Auth/AuthValidation.cs:79`). A Unicode-letter password the server accepts is rejected by the form. Raised as a nit in the auth-slice review (`docs/reviews/review-auth-slice.md:23`); neither fixed nor re-recorded as an accepted risk. | Validation drift in the safe direction; the API is authoritative (ADR-0004 §5). | Owner: frontend. Accept and record, or mirror the API with a Unicode letter class. |
| Nit | `docs/qa/tools/inspect-coverage.js:2` | The header calls it "Ad-hoc inspector used while reconciling the wave-2 coverage numbers", but it is now a **cited reproduction step** (`docs/qa/coverage-summary.md:295-297`). | An "ad-hoc" label on a tool the release evidence depends on invites the panel to treat the uncovered-line tables as unreproducible. | Owner: QA. Re-word the header to describe it as the per-file reproduction command. |

---

## Architecture checks

- [x] **Dependency rule is respected.** Verified from the project files, not from convention:
  `project/src/CrudWorks.Domain/CrudWorks.Domain.csproj` has **no** `ProjectReference` and **no**
  `PackageReference` at all; `CrudWorks.Application.csproj:4` → Domain only;
  `CrudWorks.Infrastructure.csproj:4` → Application only; `CrudWorks.Api.csproj:19-20` → Application
  + Infrastructure. `project/Directory.Build.props:7` sets `TreatWarningsAsErrors=true` and `:5`
  `Nullable=enable` for every project.
- [x] **Business logic is not in controllers or `DbContext`.** `TasksController.cs:14-58` binds,
  delegates and maps; the ownership decision is in `TaskService.cs:73-75`, the validation in
  `TaskValidation.cs:20-73`, the invariants in `TaskItem.cs:58-123`. `AuthController.cs:13-48`
  is 35 lines of bind/delegate/map. `CrudWorksDbContext.cs` holds configuration only
  (`Persistence/Configurations/TaskConfiguration.cs`, `UserConfiguration.cs`).
- [x] **No EF Core / ASP.NET Core / HTTP types in `Domain` or `Application`.** Grep over
  `project/src/CrudWorks.Domain/**` and `project/src/CrudWorks.Application/**` for
  `Microsoft.EntityFrameworkCore|Microsoft.AspNetCore|Microsoft.Data.Sqlite|HttpClient|DbContext`
  returns **zero matches in source files**; the only hits are under `obj/` (implicit
  `GlobalUsings`), which are build artifacts, not source. The EF Core, `Microsoft.Extensions.Identity.Core`
  and `System.IdentityModel.Tokens.Jwt` packages sit in `Infrastructure` only
  (`CrudWorks.Infrastructure.csproj:8-15`); `PasswordHasher<User>` is behind
  `Application/Auth/IPasswordHasher.cs`.
- [x] **Repository interfaces are in `Application`; implementations in `Infrastructure`.**
  `Application/Tasks/ITaskRepository.cs:10-21`, `Application/Users/IUserRepository.cs`,
  `Application/Users/IUnitOfWork.cs`; EF implementations `Infrastructure/Persistence/TaskRepository.cs`,
  `UserRepository.cs`, `UnitOfWork.cs`. **No generic repository** — one explicit interface per
  aggregate, as ADR-0001:34 requires. No MediatR / CQRS / AutoMapper / generic-repository /
  microservice drift: grep over `project/**` for `MediatR|AutoMapper|Quartz|MassTransit`
  returns only third-party `node_modules` type definitions.
- [x] **The composition root is in `Api`.** `Program.cs:20-114`: controllers, DI (`:59-67`),
  JWT wiring (`:69`), CORS (`:71-79`), exception handler (`:26`), health (`:103-114`),
  OpenAPI (`:92-100`).
- [x] **Migration ↔ model consistency.** `20261112184118_AddTasks.cs` (TEXT status, TEXT dueDate,
  FK Restrict, `IX_Tasks_UserId`) matches `TaskConfiguration.cs` and
  `CrudWorksDbContextModelSnapshot.cs`; enforced by `TasksTable_HasTheUserIdIndex` and
  `TasksTable_EnforcesTheForeignKeyToUsers` (`project/tests/CrudWorks.Infrastructure.Tests/Persistence/TaskRepositoryTests.cs`).

---

## Security checks

- [x] **Protected endpoints require authentication.** `TasksController.cs:10` carries `[Authorize]`
  at class level, covering all five task routes. `AuthController.cs:14,27` are the only
  `[AllowAnonymous]` attributes in the codebase; `AuthController.cs:37` puts `[Authorize]` on
  `/api/auth/me`. Health is anonymous (`Program.cs:103`). Pinned by
  `EveryTaskRoute_WithoutAToken_Returns401` (theory, 5 routes —
  `project/tests/CrudWorks.Api.Tests/Tasks/TasksApiTests.cs:248-270`) and by the live checks
  E1–E5 (`docs/qa/logs/e2e-run.log:27-31`).
- [x] **Ownership checks prevent IDOR.** Traced end to end below; the guarantee is at the
  **repository query**, not at the controller, so a foreign task never enters the Application
  layer at all. Tested for read, update and delete with the owner's row re-verified intact
  after each attack (`TasksApiTests.cs:272-327`), and live (`e2e-run.log:17-20`).
- [x] **Passwords are hashed; no plaintext in logs or responses.** `PasswordHasherAdapter.cs:12-45`
  (PBKDF2 via `PasswordHasher<User>`); no plaintext or hash is stored outside the `PasswordHash`
  column. `LoginAudit.cs:12-13` logs the email **domain only**. `ProblemDetailsExceptionHandler.cs:44-48`
  logs method and path only. Grep over `project/src/**` for secret-shaped assignments returns
  nothing; the only key in the tree is the **test** fixture `CrudWorksApiFactory.cs:16-17`,
  explicitly labelled "fixed, non-secret key for tests only".
- [x] **Secrets are not in source.** `appsettings.json` carries no signing key and no credential;
  the connection string is the documented SQLite default (`appsettings.json:9-11`, ADR-0003:30-33).
  `project/.gitignore:9,21-25` excludes `*.db`, `.env*`, `appsettings.*.local.json`, `secrets.json`.
  `docs/qa/logs/api-server.log` contains only the launch banner — no token, no password.
- [x] **Input validation is present at the boundary.** `AuthValidation.cs:20-57` and
  `TaskValidation.cs:20-73` run in the Application layer, before any repository call
  (`TaskService.cs:33` and `:53` both precede the lookup); the domain re-validates
  (`TaskItem.cs:96-123`). Field keys are the camelCase JSON names and match ADR-0004 §5.
- [x] **CORS is restricted.** `Program.cs:71-79` — `WithOrigins(allowedOrigin)` from
  `Cors:AllowedOrigin` (default `http://localhost:5173`, `appsettings.json:12-14`), explicit
  methods, explicit headers, **no `AllowCredentials`, no `AllowAnyOrigin`**. `UseCors` is
  registered at `Program.cs:88`, before `UseAuthentication`/`UseAuthorization` (`:89-90`), so the
  header is present on preflight **and** on the actual response. Asserted by three tests
  (`project/tests/CrudWorks.Api.Tests/Pipeline/ApiPipelineTests.cs:83-127`): the allowed-origin
  test asserts the **exact** origin string (a `*` would fail it), the unknown-origin test asserts
  the header is **absent**, and the third covers a real (non-preflight) `GET`.
- [x] **Error responses do not leak internals.** The `500` branch returns the fixed title
  `"An unexpected error occurred."` (`ProblemDetailsExceptionHandler.cs:41-49`); the body is
  asserted to contain neither the exception message nor the exception type
  (`ApiPipelineTests.cs:60-81`). Every validation message is a fixed safe string; the only
  interpolated messages are built from constants (`TaskItem.cs:100-102`, `:117-120`).
  `traceId` is the request trace identifier (`ApiProblemDetails.cs:23`), documented in ADR-0004 §1.

---

## Test checks

- [x] **Unit tests cover business rules.** Domain 26 methods / 37 cases, Application 34 methods /
  57 cases (counted statically from the `[Fact]`/`[Theory]` attributes — matches
  `docs/qa/test-plan.md:360-362`). Invariants (`TaskItemTests.cs`), validation and ownership
  decisions (`TaskServiceTests.cs`, `AuthServiceTests.cs`) are covered at the Application layer
  with NSubstitute and no database.
- [x] **Integration tests cover persistence and API behaviour.** `TaskRepositoryTests.cs` /
  `UserRepositoryTests.cs` run against a real SQLite file/in-memory connection with migrations
  applied; `CrudWorksApiFactory.cs:19-32` gives each API factory its own temp SQLite file
  (`Pooling=False`, deleted on dispose) and injects the key and connection string.
  **No shared mutable database**: every fixture owns its own store.
- [x] **Edge cases and failure paths are covered.** 400 (all four task fields, all three auth
  fields, malformed JSON), 401 (no token / malformed token / expired token / non-GUID subject),
  404 (missing **and** foreign), 409 (duplicate email, case- and space-insensitive), 204 with an
  empty body, `Location` header, list ordering, `dueDate: null` clearing, `status` required on
  update, numeric and lowercase status rejected (`TasksApiTests.cs:141-171`, `:197-232`, `:379-394`).
- [x] **Tests assert behavior, not implementation.** Status codes, body members, storage effects
  and the owner's surviving row — not private fields or call order. The two mutations used to
  obtain a RED were **behaviour-preserving inversions** (CORS wildcard, `exception.Message` in the
  500 title — `docs/ai/014:99-103`), which is the right way to prove a test bites.
- [x] **No test depends on shared mutable state.** Per-factory / per-test SQLite; `sessionStorage`
  is cleared per test in the frontend (`project/frontend/src/test/setup.ts`).
- [x] **No skipped tests.** Grep for `Skip\s*=|Skip\(|Assert\.Skip` over `project/tests/**` →
  **no matches**. The recorded wave-2 run agrees: `Ignorado: 0` for each of the four projects —
  37 / 57 / 25 / 46, 165 total, 0 failed, 0 skipped (`docs/qa/test-plan.md:281-284`;
  per-project: `docs/ai/013:141`, `docs/ai/014:144`).

---

## Frontend checks

- [x] **TypeScript is strict; no `any`.** Grep over `project/frontend/src/**` for
  `: any\b|as any\b|console\.` → **no matches**. `npm run build` runs `tsc -b` first
  (`project/frontend/package.json:8`).
- [x] **Loading, empty, error, and success states exist.** `TaskListPage.tsx:70-93` (loading
  `role="status"`, error with retry, empty state with a create action, rows); `TaskFormPage.tsx:135-170`
  (loading, 404 not-found state, error with retry). Covered by
  `shows a loading state while the tasks are pending`, `shows the empty state …`,
  `shows an error state with retry and recovers after refetching`,
  `reports a network failure with a connection-specific message`.
- [x] **Responsive at mobile, tablet, and desktop.** Real-browser measurement at 360 / 768 / 1280
  with `scrollWidth === innerWidth` on the list **and** the form (`docs/ai/015:129-131`).
  Honest caveat recorded by the implementer: the viewport was emulated via
  `Emulation.setDeviceMetricsOverride`, not a physical device (`docs/ai/015:237-239`).
- [x] **Forms are accessible and keyboard-friendly.** Every control has `<label htmlFor>`; errors
  are `role="alert"` with `aria-describedby` + `aria-invalid` (`TaskFormPage.tsx:192-259`); the
  dialog is `role="dialog" aria-modal="true"` with `aria-labelledby`/`aria-describedby`, focus
  moved in on open, Tab trapped, Escape closes, focus returned to the trigger
  (`ConfirmDialog.tsx:37-113`). Status is conveyed as **text**, not colour alone
  (`components/statusLabels.ts`; `TaskListPage.test.tsx:289`). 18 keyboard/accessibility tests
  plus a real-Chromium keyboard walkthrough with real `Input.dispatchKeyEvent` key events
  (`docs/ai/015:104-132`).
- [x] **No console errors or warnings.** Zero warnings/errors/uncaught exceptions/failed requests
  during the whole demo flow in the real Chromium session (`docs/ai/015:134-141`). The one `error`
  line in the capture is the browser's network log for the deliberately invalid token of the
  `401-reaction` step, not an application error — correctly stated rather than hidden.
- [x] **Auth state is centralised; components do not read tokens ad hoc.** `auth/session.ts:51-79`
  is the only module that touches `sessionStorage`; components read `useAuth()` only.
  The 401 reaction clears the session exactly once with no retry loop across three layers
  (see "Risk areas" §F below).
- [x] **No invented endpoints or fields.** The client uses exactly the nine ADR-0004 routes; the
  `Task` type mirrors the resource one-for-one (`project/frontend/src/lib/tasks.ts:8-24`).

---

## DoD walkthrough (`docs/02-definition-of-done.md`)

### §1 Architecture — **confirmed**, all seven items

| Item | Verdict | Evidence |
| --- | --- | --- |
| Four backend projects | Confirmed | `project/src/CrudWorks.{Domain,Application,Infrastructure,Api}` |
| Project references prove the dependency rule | Confirmed | the four `.csproj` files quoted above |
| No EF/ASP.NET/HTTP types in `Domain`/`Application` | Confirmed | zero source matches (§ Architecture checks) |
| Thin controllers, business rules in `Application` | Confirmed | `TasksController.cs:14-58`, `TaskService.cs:33,53` |
| Repository interfaces in `Application`, EF in `Infrastructure` | Confirmed | `ITaskRepository.cs`, `TaskRepository.cs` |
| Composition root in `Api` | Confirmed | `Program.cs:59-79` |
| ≥3 ADRs | Confirmed | `docs/adr/0001`–`0005` (5). *Caveat: ADR-0001 status is still "proposed" — Minor above.* |
| Architecture diagram in the README | Confirmed | `project/README.md:141-149` (layer diagram) and `docs/adr/0001:18-24` (mermaid) |

### §2 Backend and API — **confirmed**, all eight items

All endpoints in `docs/00-brief.md:114-122` (9 routes) exist and are pinned by API tests.
Status codes 200/201/204/400/401/404/409 are all asserted; **no 403 is produced anywhere**
(`TasksApiTests.cs:272-327` asserts 404 for foreign ids). Validation errors are
`application/problem+json` with a field-level `errors` map (`ApiPipelineTests.cs:35-58`).
`CancellationToken` flows through every async call (`TasksController.cs:17,24,32,46,54` →
`TaskService.cs:16-71` → repository). OpenAPI is served in Development only
(`Program.cs:92-95`; `OpenApiDocument_InDevelopment_IsServed`). Health returns
`{"status":"Healthy","totalDurationMs":…}` (`Program.cs:103-114`; `GetHealth_ReturnsTheContractShape`).
No `async void` / `.Result` / `.Wait()` (grep: zero matches). Build passes with warnings as
errors (`docs/qa/clean-run-report.md:122-127`).

### §3 Data and persistence — **confirmed**, all nine items

SQLite + EF Core, PostgreSQL documented as the swap (ADR-0003:26-44, `:89-98`).
`Users` + `Tasks` with PK, FK and ≥2 business fields. Unique index on normalized email
(`UserConfiguration.cs`; `Register_WithSeededDemoEmailInOtherCaseAndSpaces_Returns409`,
`SaveChangesAsync_WithDuplicateEmail_ThrowsConflictAndKeepsOneUser`). `IX_Tasks_UserId` verified
by `TasksTable_HasTheUserIdIndex`; `DueDate` deliberately unindexed and documented. Migrations
committed and applied from a clean database (`docs/qa/clean-run-report.md:49-79`). Seed: one demo
user + 5 tasks across 3 statuses (`DatabaseSeeder.cs:74-87`; `SeededDemoUser_HasFiveTasksAcrossAllStatusesNewestFirst`).
Timestamps UTC (value converter + `DateTimeKind.Utc` reattach, asserted at
`project/tests/CrudWorks.Api.Tests/Tasks/TasksApiTests.cs:414`). No raw SQL anywhere
(grep: no `ExecuteSqlRaw`/`FromSqlRaw`).

### §4 Authentication and authorization — **confirmed**, all nine items

Registration validates uniqueness and strength; passwords hashed with a vetted hasher; login
returns a 60-minute JWT with `sub`/`email`/`name`/`jti`; `/api/auth/me` is `[Authorize]`;
task endpoints are `[Authorize]`; **IDOR → 404 for read, update and delete**; anonymous only on
register/login/health; the signing key comes from configuration and fails fast outside
Development; CORS is restricted; the `sessionStorage` trade-off is documented (ADR-0002:46-50).

### §5 Business logic and validation — **confirmed**, all seven items

Title 3–200, description ≤2000, status one of three case-sensitive names, due date `yyyy-MM-dd`
date-only in UTC. Ownership is enforced **in the Application layer and again in the repository
query** — not in the controller. Validation is independent of ASP.NET Core and EF Core
(`TaskValidation.cs` uses only `System.Globalization` and the Domain enum).
Domain invariants live in the factory and `Update()`, never in setters
(`TaskItem.cs:40-55` are all `private set`).

### §6 Tests — **confirmed**, all ten items

Including "At least one end-to-end smoke test covers register → login → create → edit → delete":
`docs/qa/e2e-api-check.js` covers the full path live (checks A1→B8) and the frontend was
exercised against the live API in a real browser (`docs/ai/015:104-152`).
Coverage on Domain + Application is **348/348 = 100.00%** against an 80% floor
(`docs/qa/coverage-summary.md:30`). "TDD evidence exists" — the `docs/ai/` red/green record is
honest about its own limits: where the behaviour already existed, the RED was obtained by a
**temporary mutation with a byte-identical restore**, and that is stated plainly rather than
dressed up as a missing-implementation RED (`docs/ai/013:85-137`, `docs/ai/014:92-138`).

### §7 Frontend — **confirmed**, all ten items

Build, strict TS with no `any`, all five flows working, all four list states, labels/inline
validation/keyboard, 360/768/1280, clean console, `VITE_API_BASE_URL` env var
(`project/frontend/src/lib/api.ts:1`), centralised auth state, no dead code — except the one
unused dependency noted above (Minor).

### §8 Documentation and demo — **confirmed** except one item

Root README covers prerequisites, setup, migrations, run, test and demo credentials
(`README.md`, `project/README.md:22-109`); architecture diagram and structure present;
curl example at `project/README.md:131-137`; `docs/user-story.md`, `docs/adr/`, `docs/ai/` and
`docs/presentation.md` all exist. "A clean clone can reach a running app by following the README"
— the reset procedure is documented twice and reproduced mechanically
(`docs/qa/tools/api-ctl.js reset`, `docs/qa/clean-run-report.md:49-62`). "The demo script passes
end to end" — the API half is proven live (37/37); the UI half is proven by the real-browser
walkthrough (`docs/ai/015`).

### §9 GenAI evidence — **confirmed**, all six items

Prompt summaries plus delegation briefs, representative outputs, validation steps
(build/tests/manual/security), corrections and rejections with reasons, edge cases and auth
decisions, and an explicit distinction between AI-generated and human-reviewed code. Fourteen
of the sixteen entries carry a `## Corrections` table; the two that do not are the review and
adjudication entries (`docs/ai/005`, `006`, `010`, `011`).

### §10 Presentation readiness — **confirmed** except the two open items

Outline fits 10–15 minutes; the six required Q&A topics are all present
(`docs/presentation.md:168-189`); the demo script and its fallback are written
(`docs/presentation.md:22-47`, `:191-194`). *Note: the traceability matrix says nine Q&A
entries; there are eight — Minor above.*

### §11 Final gate — **confirmed** except this item

All five build/lint/test gates are recorded green on the current tree — but **from two
different runs**: the backend gates were re-measured in wave 2 (`docs/qa/test-plan.md:264-284`,
`docs/ai/013:180-192`) and the frontend gates in wave 3 (`docs/ai/015:86-102`). The backend
build itself is also recorded in the wave-1 report (`docs/qa/clean-run-report.md:122-127`).
The clean-reset procedure works from a clean database (`docs/qa/clean-run-report.md:49-79`).
The traceability matrix has evidence for every row — with the caveat that one row's *content*
is stale (Major above).

### The four deliberately open items — **all four should stay open; one owner needs correcting**

| Item | Is leaving it open correct? | Owner correct? |
| --- | --- | --- |
| The repository is initialized with a clean commit history | **Yes.** `AGENTS.md` §12 forbids committing without explicit owner approval, and §11 assigns the approval to the owner. The reviewer must not `git init`. Note the risk is already mitigated: `project/.gitignore` and `project/frontend/.gitignore` exclude `bin/`, `obj/`, `node_modules/`, `dist/`, `TestResults/`, `coverage/`, `*.db` and `.env*`, so the first commit will be clean once approved. The red→green record living in `docs/ai/` is an honest substitute, not an equivalent. | **Yes** — Orchestrator (needs explicit approval). |
| The demo is rehearsed at least twice | **Yes.** `docs/presentation.md` §2 is a written script with timings not yet recorded. The API half of the script is proven; the rehearsal itself is a human activity that has not happened. | **Yes** — Orchestrator, executing with the owner. |
| No browser console warnings during the demo | **Yes, and correctly narrowed.** The console *was* observed clean in a real Chromium session over the whole demo flow (`docs/ai/015:134-141`); what remains open is the observation **during the rehearsal**. | **Owner needs correcting.** The DoD assigns this to the Orchestrator, but the **contradictory statement** that the console was "never observed" lives in `docs/qa/test-plan.md:390,408,453`, which the **QA engineer** owns. That contradiction must be corrected by QA (Major above); the rehearsal-time observation stays with the Orchestrator. |
| The reviewer has no open blockers | **Yes, and this document closes it.** The verdict carries 0 blockers, 0 majors that touch `project/**`, and both majors are documentation-only with named owners. | **Yes** — Reviewer. |

### "Verified but not fully provable here"

The accessibility row (AC-003-10 "Partially satisfied") is honest and correctly labelled:
the keyboard half is fully verified (18 automated tests + a real-browser walkthrough), the
screen-reader half is not verifiable without assistive technology, and the ARIA markup a
screen reader depends on is asserted by machine (`docs/ai/015:201`, `:224-240`). The DoD is
right to record this rather than hide it. **Confirmed as written.**

---

## Documentation truth — spot-checks (16 claims)

Every number below was checked against the code, the captured output, or the source file.

| # | Claim | Where | Verdict |
| --- | --- | --- | --- |
| 1 | 165 backend tests (Domain 37 / Application 57 / Infrastructure 25 / Api 46) | `README.md:24`, `project/README.md:78-79`, `docs/02-definition-of-done.md:12-13`, `docs/presentation.md:93-94` | **True.** Consistent across five artifacts and three captured runs. Static method counts corroborate: Domain 26 methods (`UserTests.cs` 8 + `TaskItemTests.cs` 18), Application 34 (`AuthServiceTests.cs` 14 + `TaskServiceTests.cs` 20) — matches `docs/qa/test-plan.md:360-362`. |
| 2 | 69 frontend tests, 10 files | `README.md:25`, `project/README.md:79`, `docs/02-definition-of-done.md:13`, `docs/presentation.md:94` | **True.** Static count: 68 `it(...)` declarations across 10 files, of which one is `it.each` with 2 rows (`project/frontend/src/pages/LoginPage.test.tsx:60-63`) → **69 executed cases**. Recorded output: 10 files / 69 tests (`docs/ai/015:93-94`). |
| 3 | 234 automated tests (165 + 69) | `docs/presentation.md:124` | **True.** 165 + 69 = 234. |
| 4 | 37/37 API-level E2E checks | `README.md:26`, `project/README.md:100`, `docs/02-definition-of-done.md:13`, `docs/qa/test-plan.md:56` | **True.** The script contains exactly 37 `record(...)` calls (A1–A4, B1–B8, C1–C7, D1–D6, E1–E6, F1–F3, G1–G2, H1); the log shows 37 `PASS` lines and `SUMMARY 37/37 checks passed` (`docs/qa/logs/e2e-run.log:40-41`), repeated in `docs/qa/logs/e2e-run-wave2.log:39-40`. |
| 5 | Domain + Application = 348/348 = 100.00% | `docs/qa/coverage-summary.md:30`, `project/README.md:89-90`, `docs/presentation.md:96-97` | **True as recorded.** 112 + 236 = 348 ✓. *Not independently re-measured by me (no shell).* |
| 6 | Api = 364/613 = 59.38% | `docs/qa/coverage-summary.md:19` | **Arithmetically true** (364 ÷ 613 = 59.380%). 229 + 135 = 364 ✓ and 229 + 384 = 613 ✓ — the hand-written/generated split is internally consistent. |
| 7 | Hand-written API = 229/229 = 100.00% | `docs/qa/coverage-summary.md:164`, `docs/ai/014:210` | **Arithmetically true.** 38+12+13+22+33+80+5+24+2 = 229 ✓; and there are exactly **9** hand-written files under `project/src/CrudWorks.Api/` (Program, AuthenticationSetup, AuthController, TasksController, ApiProblemDetails, ProblemDetailsExceptionHandler, HttpCurrentUser, JwtSigningKey, LoginAudit) ✓. |
| 8 | Infrastructure = 432/453 = 95.36% | `docs/qa/coverage-summary.md:22` | **Arithmetically true** (432 ÷ 453 = 95.364%). 21 uncovered lines named line-by-line ✓. |
| 9 | "The Cobertura XML files … are gitignored (`project/.gitignore`: `TestResults/`, `coverage/`)" | `docs/qa/coverage-summary.md:202-203` | **True.** `project/.gitignore:7-8` contains `TestResults/` and `coverage/`. |
| 10 | 18 uncovered Infrastructure lines = design-time factory 6 + two `Down` bodies 8 + seeder 5 + `UnitOfWork` 2 | `docs/qa/coverage-summary.md:230-239` | **True** (6+4+4+2+3+2 = 21). The *old* description of these same lines still sitting in `docs/04-requirement-traceability.md:117` is **false** — see Major 2. |
| 11 | "nine likely questions" in `docs/presentation.md` §9 | `docs/04-requirement-traceability.md:30` | **FALSE — eight** (`docs/presentation.md:168,171,173,176,179,182,185,187`). |
| 12 | "13 evidence entries" | `docs/04-requirement-traceability.md:36` | **FALSE — 16** (`docs/ai/000`…`015`). |
| 13 | The browser console "was never observed" | `docs/qa/test-plan.md:390,408,453` | **FALSE.** It was observed clean in a real Chromium session (`docs/ai/015:134-141`); `docs/02-definition-of-done.md:24` states this correctly. |
| 14 | ".NET SDK 10.0.404", API on `http://localhost:5080`, C# 13 | `project/README.md:17,24,33` | SDK and port **true** (`project/global.json:3`, `Properties/launchSettings.json:8`). **C# 13 is false** — `net10.0` + `LangVersion=latest` (`project/Directory.Build.props:6`) under SDK 10.0.404 is C# 14. |
| 15 | The demo has 5 tasks, 2 Todo / 2 InProgress / 1 Done | `docs/presentation.md:33-34`, `docs/qa/clean-run-report.md:74` | **True.** `DatabaseSeeder.cs:74-87` produces exactly InProgress, Done, Todo, InProgress, Todo → 2/2/1 ✓; asserted by `SeededDemoUser_HasFiveTasksAcrossAllStatusesNewestFirst` and by check A3. |
| 16 | Demo credentials `demo@crudworks.local` / `Demo123!`, seeded in Development only | `project/README.md:7-8`, `docs/presentation.md:7` | **True.** `DatabaseSeeder.cs:10-14`; the seeder runs only inside `if (app.Environment.IsDevelopment())` (`Program.cs:92-100`). |

**Summary:** 13 of 16 claims are true; 3 are false (numbers 11, 12, 13), plus one false
toolchain claim (14) and one false gap description (Major 2). All the false ones are in
orchestrator/QA-owned **documentation**, none in `project/**` or in the code.

---

## Risk areas — the specific questions asked

### A. Is the ownership/IDOR guarantee still airtight end to end? — **Yes**

Traced, token to query:

1. **Token `sub`** — `project/src/CrudWorks.Infrastructure/Security/JwtTokenService.cs:26`
   writes `sub = user.Id`; the token carries `notBefore`/`expires` (`:36-37`).
2. **Validation** — `project/src/CrudWorks.Api/Authentication/AuthenticationSetup.cs:19-30`
   validates issuer, audience, lifetime and signature, and sets `MapInboundClaims = false`
   (`:18`) so `sub` survives as the raw claim rather than being remapped.
3. **Resolution** — `project/src/CrudWorks.Api/Security/HttpCurrentUser.cs:13-14` reads
   `FindFirst(AuthClaimNames.Subject)` and `Guid.TryParse`s it; an unparsable subject yields
   `null`, which becomes a `401`, never a `500` (`TasksController.cs:61-62`;
   `Me_WithAValidTokenWhoseSubjectIsNotAGuid_Returns401`).
4. **Application** — the owner id is threaded explicitly into every use case
   (`TaskService.cs:18, 24, 35, 55, 68`) and is never read from the request.
5. **Repository** — the owner filter is **in the SQL predicate**:
   `TaskRepository.cs:13` (`task.UserId == userId`) for the list, and
   `TaskRepository.cs:24` (`task.Id == id && task.UserId == userId`) for the single fetch.
   A foreign task is therefore never loaded into the Application layer at all — there is no
   intermediate state in which a foreign task exists in memory and a later check forgets to run.
6. **Indistinguishability** — a foreign id and a missing id both produce `null` from the
   repository, both throw `NotFoundException("Task not found.")` (`TaskService.cs:14, 73-75`),
   and both map to `404` with the same title (`ProblemDetailsExceptionHandler.cs:37-39`).
   Verified in code and by test: `Get_ForAnotherUsersTask_Returns404AndTheRowStillExistsForTheOwner`
   vs `Get_WithAMissingTask_Returns404ProblemDetails` (`TasksApiTests.cs:272-290`, `:329-341`)
   assert the identical title; the live body carries no task member
   (`docs/qa/logs/e2e-run.log:17`), and the leak detector that checks this has its own
   6-case self-test that still catches a real leak (`docs/qa/e2e-api-check.test.js`).
7. **The domain cannot reassign the owner** — `TaskItem.UserId` has a private setter and
   `Update()` does not accept an owner (`TaskItem.cs:43, 87-94`); the factory rejects an empty
   owner (`TaskItem.cs:65-68`).

**One behaviour worth stating out loud rather than hiding:** `UpdateAsync` validates the body
*before* the ownership lookup (`TaskService.cs:53` then `:55`), so an **invalid** body against a
**foreign** id returns `400` rather than `404`. That is body-derived and resource-blind — the
same `400` is returned for an invalid body against a *missing* id — so it leaks nothing about
existence. It is already adjudicated and recorded (`docs/ai/013:200`). **Not a finding.**

### B. Can a client influence the owner of a task? — **No path exists**

- `CreateTaskRequest.cs:8` = `record(string? Title, string? Description, string? Status, string? DueDate)`.
  `UpdateTaskRequest.cs:8` = the same four fields. **No id, no owner, no timestamps, no role.**
- The JSON binder ignores unknown members, so a client-sent `userId` is dropped before it
  reaches any code; the owner is `RequireUserId()` (`TasksController.cs:35, 48, 56`) — the token,
  always.
- Proven at the API boundary: `Create_WithAUserIdInTheBody_IsIgnoredAndTheCallerRemainsTheOwner`
  (`TasksApiTests.cs:173-195`) — a foreign `userId` in the body leaves the task owned by the
  caller; and live check G1 (`docs/qa/logs/e2e-run.log:36`).
- The frontend cannot even express an owner: `TaskInput` is `{title, description, status, dueDate}`
  (`project/frontend/src/lib/tasks.ts:19-24`) and `toInput` maps only those
  (`project/frontend/src/pages/TaskFormPage.tsx:44-51`).
- The database enforces it too: FK `Tasks.UserId → Users.Id`, verified by
  `TasksTable_EnforcesTheForeignKeyToUsers`.

### C. JWT handling — any weakness?

- **Key resolution per environment.** `JwtSigningKey.Resolve` (`JwtSigningKey.cs:16-31`):
  a configured key wins (`:18-21`); it must be base64 and decode to ≥32 bytes
  (`:33-52`, two distinct failure messages). **Outside Development a missing key throws**
  (`:23-27`), and the throw is forced at startup by resolving the singleton eagerly
  (`Program.cs:84`) — the host refuses to boot rather than starting with a weak or absent key.
  In Development a 64-byte ephemeral per-process key is generated and a warning is logged
  (`:29`; `Program.cs:47-53`). Tested: `NonDevelopment_WithoutAConfiguredSigningKey_FailsTheStartup`,
  `AConfiguredSigningKeyThatIsInvalid_FailsTheStartup` (`docs/ai/014:72-76`).
  **Residual, already documented:** in Development a token minted before a restart stops
  validating after it (ADR-0002:31-34; `project/README.md:37-39`). That is a demo
  inconvenience, stated in three places rather than hidden.
- **Expiry.** 60 minutes (`JwtOptions.cs:15`), `notBefore`/`expires` set
  (`JwtTokenService.cs:36-37`), `ValidateLifetime = true` with a 30-second clock skew
  (`AuthenticationSetup.cs:25, 29`). An expired token is rejected — pinned by
  `Me_WithAnExpiredToken_Returns401`.
- **Claim contents.** `sub`, `email`, `name`, `jti` (`JwtTokenService.cs:24-30`); issuer and
  audience are both validated (`AuthenticationSetup.cs:21-24`). No roles, no scopes, no
  `password` or `hash` claim. `jti` is present but unused — no revocation store, which ADR-0002
  lists under "Negative / trade-offs" as a deliberate demo limitation.
- **What happens when the key is missing outside Development:** startup fails with
  `InvalidOperationException("Jwt:SigningKey is required outside Development. Provide it through
  user-secrets or the Jwt__SigningKey environment variable.")` (`JwtSigningKey.cs:25-26`).
  The message names the mechanism and contains **no key material**.
- **Known, accepted simplifications** (documented, not defects): one symmetric key with no
  rotation and no `kid`/JWKS; no refresh, no revocation, no logout-everywhere
  (ADR-0002 §"Negative" and §"Production path").

### D. Are the error responses safe? — **Yes**

- **The 500 path.** `ProblemDetailsExceptionHandler.cs:41-49` — default branch → `500`, title
  `"An unexpected error occurred."`, exception logged server-side with **method and path only**.
  The body is asserted to contain neither the message nor the type name
  (`ApiPipelineTests.cs:79-80`), and the mutation RED in `docs/ai/014:101-103` proves the test
  fails if the title ever becomes `exception.Message`.
- **Validation messages.** All seven are fixed strings (`TaskValidation.cs:13-16`,
  `AuthValidation.cs:12-15`); none interpolates user input. The only interpolated messages in
  the codebase are the domain's, and they interpolate **constants**
  (`TaskItem.cs:100-102`, `:117-120`).
- **The `type` member** is a fixed RFC 9110 URL (`ApiProblemDetails.cs:33-40`).
- **`traceId`** is the request trace identifier (`ApiProblemDetails.cs:23`) — an operator
  correlation id, documented in ADR-0004 §1, not an internal identifier.
- **Residual (Nit, not raised as a defect):** a framework-generated `400` (malformed JSON) is
  an MVC `ValidationProblemDetails` — it carries `errors` but not the `traceId` member that
  `ApiProblemDetails.Create` adds, so two different `400` bodies exist for the same status code.
  Both are RFC 7807 and neither leaks anything. Worth a sentence in the ADR if the panel asks.

### E. Is the CORS configuration actually restrictive, and does it hold for the real request path?

**Yes, and it is asserted on the real path.** `Program.cs:71-79`: `WithOrigins(allowedOrigin)`
from `Cors:AllowedOrigin` (default `http://localhost:5173`), explicit methods
`GET/POST/PUT/DELETE/OPTIONS`, explicit headers `Content-Type`/`Authorization`, **no**
`AllowCredentials`, **no** `AllowAnyOrigin`. `UseCors` is registered at `:88`, before
`UseAuthentication`/`UseAuthorization` (`:89-90`) — the correct order, so the header is emitted
on preflight **and** on the real response, including on a `401`.

The tests are stronger than "the header exists": the allowed-origin test asserts the **exact
origin string** (`ApiPipelineTests.cs:94` — a `*` would fail it), the unknown-origin test asserts
the header is **absent** (`:113`), and a third test covers a **non-preflight** `GET`
(`:116-127`). The mutation RED (policy changed to `AllowAnyOrigin()` — `docs/ai/014:99-100`)
shows the preflight tests fail under a wildcard; a presence-only assertion would not have
caught it (`docs/ai/014:219`).

**One honest caveat the panel should hear rather than infer:** the demo browser path does
**not** exercise CORS. The Vite dev server proxies `/api` to the API
(`project/frontend/vite.config.ts:8-13`), so the browser sees a same-origin request. CORS is
proven by API-level tests, not by the demo. That is fine — it is how the demo is built — but it
should be stated rather than implied.

### F. Does the frontend keep the security-relevant invariants? — **Yes**

- **Owner from the token, not the form.** The form has no owner field to send:
  `TaskInput` = `{title, description, status, dueDate}` (`lib/tasks.ts:19-24`); `toInput` maps
  only those four (`pages/TaskFormPage.tsx:44-51`); `createTask`/`updateTask` post exactly that
  (`lib/tasks.ts:76-88`). There is no code path from form state to an owner, in either
  direction.
- **The 401 reaction clears the session exactly once, with no retry loop.** Three layers, each
  independently verified:
  1. the global query client refuses to retry a `401` (`App.tsx:13-14`);
  2. `notifyUnauthorized` fires **only while a session still exists**, so the second and third
     401 of the same burst are no-ops (`lib/tasks.ts:44-50`);
  3. `handleUnauthorized` clears the stored session and nulls the React session
     (`auth/AuthProvider.tsx:58-61`), registered and unregistered once per provider lifetime
     (`:65-68`).
  Tested at the `lib/tasks.ts` boundary (`calls the injected handler once and rethrows the
  ApiError`, `does not call the handler when no session exists`, `calls the handler once across
  repeated failed attempts instead of looping` — `lib/tasks.test.ts:120-163`) and at the app
  level (`clears the session and returns to login once when the list returns 401` —
  `pages/TaskListPage.test.tsx:270-288`).
- **One storage owner.** `auth/session.ts:51-79` is the only module that touches
  `sessionStorage`; the trade-off is documented (ADR-0002:46-50, ADR-0005:88-92).
- **No untrusted HTML.** Grep for `dangerouslySetInnerHTML|innerHTML` over `project/**` →
  only third-party `node_modules` type definitions. React escapes by default.

### G. Is there any secret, token, password, or connection string in the source? — **No**

Grep over the workspace for secret-shaped assignments returns only `'token-abc'` in the
frontend **test** files and third-party `node_modules` fixtures. `appsettings.json` has no
signing key and no credential. The only key in the tree is the **test** signing key in
`CrudWorksApiFactory.cs:16-17`, labelled "fixed, non-secret key for tests only". The demo
password `Demo123!` is a documented development credential (`DatabaseSeeder.cs:13-14`;
`project/README.md:7-8`), seeded only in Development. The only connection string is the
documented SQLite default. `docs/qa/logs/api-server.log` contains only the launch banner.

### H. Does anything violate the dependency rule or leak EF/ASP.NET types? — **No**

See "Architecture checks" above: verified from the `.csproj` files, and a source-level grep of
`Domain`/`Application` for `Microsoft.EntityFrameworkCore|Microsoft.AspNetCore|Microsoft.Data.Sqlite|HttpClient|DbContext`
returns **zero** matches outside `obj/` build artifacts.

### I. Dead code, debug artifacts, unused dependencies, and the `docs/qa/tools/` question

- **No `console.*`, no `any`** in `project/frontend/src/**` (grep: zero).
- **No `.Result`, `.Wait()`, `async void`, `GetAwaiter().GetResult()`** in `project/src/**` or
  `project/tests/**` (grep: zero).
- **No `TODO`/`FIXME`/`HACK`/`XXX`** in `project/src/**` (grep: zero).
- **No leftover artifacts:** no `*.orig/*.rej/*.bak`; the throwaway CDP script directory
  `node_modules/.cdp-check/` (`docs/ai/015:215-217`) is gone.
- **Unused dependency:** `@vitest/coverage-v8` (Minor above). One unexplained duplicate:
  `Microsoft.EntityFrameworkCore.Design` in the Api project.
- **`docs/qa/tools/` — genuinely useful, not clutter.** All three tools are cited by the release
  evidence and each carries a header explaining why it exists:
  `api-ctl.js` is the reproducible clean-reset harness that made the wave-1 clean run possible
  (`docs/qa/clean-run-report.md:52`; `project/README.md:107-109`);
  `coverage-report.js` is the aggregator that merges the four Cobertura files, writes the
  tracked summary and **self-verifies its own parser** (`docs/qa/coverage-summary.md:8`, `:209`);
  `inspect-coverage.js` is the per-file reproduction command cited for the uncovered-line tables
  (`docs/qa/coverage-summary.md:295-297`). The only complaint is its "ad-hoc" label (Nit above).
  **Verdict: keep all three.**

---

## Confirmation pass — 2026-11-12

**Verdict: Approve with comments** — 0 blockers, 0 majors, 1 minor, 1 nit still open.
(Previous verdict: *Approve with comments — 0 blockers, 2 majors, 6 minors, 5 nits*;
both majors and five of the six minors and all but one of the nits are now closed.)

### What I verified

**Major 1 — `docs/qa/test-plan.md` stale statements (closed).**
Re-read the whole plan (830 lines) and grepped for the stale phrases. None of the
following survives: "the console was never observed", "51 cases / 9 files" as the
current figure, "not re-run in wave 2", or "was never observed". The plan now
carries the wave-4 numbers throughout: `10 files / 69 cases` (lines 13, 77, 98-99,
191, 306, 361, 395-397, 487, 502, 515, 650-656), totals `165 + 69 + 37 + 6 = 277`
(lines 518, 775), and the console row now reads "observed clean in a real Chromium
session" (lines 546, 590, 642-647). The AC-003-07 and AC-003-10 rows
(lines 246, 249) are honest: the keyboard half is fully verified, the screen-reader
half is "not verifiable here" and is "not claimed as fully satisfied". The
`docs/ai/015-frontend-accessibility-pass.md` filename is cited correctly in all 6
places (lines 12, 35, 315, 361, 487, 734). Matches `docs/ai/015` (the authoritative
record) and the measured 10 files / 69 cases. **Closed.**

**Major 2 — `docs/04-requirement-traceability.md:118` known-gaps row (closed).**
The Infrastructure row now reads "`UnitOfWork.cs:34-35`, the design-time `DbContext`
factory body, the two migration `Down` rollback bodies, and 5 `DatabaseSeeder`
branches" — i.e. the corrected list, matching `docs/qa/coverage-summary.md:230-239`
and `docs/ai/013`. The stale "migration `Up` methods / `PasswordHasherAdapter`
fallback / 3 seeder branches" description is gone. **Closed.**

**Minor findings (5 of 6 closed; 1 re-opened as drift):**
- `docs/04-requirement-traceability.md:30` — "nine" → "eight" likely questions.
  Verified: row now says "eight" and `docs/presentation.md` §9 has 8 entries
  (`:168,171,173,176,179,182,185,187`). **Closed.**
- `specs/README.md:109-111` — SPEC-001/002/003 now "Implemented" (was "Approved").
  **Closed.**
- `docs/adr/0001-clean-architecture.md:3` — status now "accepted" (was "proposed").
  **Closed.**
- `project/README.md:17` — now "C# 14" (was "C# 13"). **Closed.**
- `project/frontend/package.json` — `@vitest/coverage-v8` removed (grep: no matches).
  **Closed** (see `docs/ai/018`).
- `docs/04-requirement-traceability.md:26` (R-20) / `:36` (R-30) and
  `docs/02-definition-of-done.md:14` — **still open as drift.** R-20 cites
  `docs/ai/000`–`016` and R-30 says "17 evidence entries", and the DoD evidence
  index says `000`–`016`, but `docs/ai/` now holds **19** entries (`000`–`018`,
  confirmed by glob). The fix that closed the original minor (000–015 / 16) was
  correct at the time; entries `016`, `017`, `018` were added afterwards and the
  range/count were not re-bumped. **Open Minor — owner: orchestrator.** Closes by
  bumping R-20 → `docs/ai/000`–`018`, R-30 → "19 evidence entries", and
  `docs/02-definition-of-done.md:14` → `docs/ai/000`–`018`, then re-checking
  after any further entry.

**Nit findings (6 of 7 closed; 1 residual):**
- `docs/qa/test-plan.md` filename citation → now `015-frontend-accessibility-pass.md`.
  **Closed.**
- `docs/qa/e2e-api-check.js:127-186` — cleanup by `Id COLLATE NOCASE AND Email`,
  owned tasks removed first, `process.exit(3)` on failure (line 482), exercised for
  real (documented in `docs/qa/test-plan.md:424-431`). **Closed.**
- `docs/adr/0001-clean-architecture.md:60-74` folder map — verified against the
  real tree (`project/src/CrudWorks.Api/` has `AuthController.cs` and
  `TasksController.cs` only; no `HealthController.cs`; `ErrorHandling/`,
  `Authentication/`, `Security/` folders exist as mapped). **Closed.**
- `docs/qa/tools/inspect-coverage.js:2` — header now "Per-file coverage inspector".
  **Closed.**
- `project/frontend/src/pages/RegisterPage.tsx:28-29` — regex now `/\p{L}/u` and
  `/\p{Nd}/u` (was `/[A-Za-z]/` + `/[0-9]/`). **Closed.**
- `README.md:28` — "Three independent reviews" (was "Two"). **Closed.**
- `README.md:125` — repo map `tools/` comment lists `api-ctl.js, coverage-report.js,
  db-state.js` but omits `docs/qa/tools/inspect-coverage.js`, which exists and is
  cited by `docs/qa/coverage-summary.md:295-297`. **Open Nit — owner: orchestrator.**
  Closes by adding `inspect-coverage.js` to the comment.

**Documented limitations (verified, not defects):**
- `docs/ai/015-frontend-accessibility-pass.md:201` records AC-003-10 as
  "partially satisfied" (keyboard half verified; screen-reader half not verifiable
  here); `docs/qa/test-plan.md:249` and `:547` say the same; the DoD
  (`docs/02-definition-of-done.md:35`) says "Partially satisfied" and does **not**
  claim full satisfaction. The limitation is recorded honestly in all three places.
  **Confirmed as written.**

### DoD item "The reviewer has no open blockers"

**Yes — this item can now be checked.** Owner: reviewer (me). There are 0
blockers and 0 majors remaining. The two majors from the prior verdict are closed
and verified above; the only open items are 1 Minor (AI evidence range/count drift)
and 1 Nit (README omits `inspect-coverage.js`), neither of which is a blocker.
The item stays unchecked in `docs/02-definition-of-done.md:168` until the
orchestrator flips it; I do not edit that file (owner: orchestrator per
`AGENTS.md` §11).

### Open findings the orchestrator still needs to act on

1. **Minor** — `docs/04-requirement-traceability.md:26,36` and
   `docs/02-definition-of-done.md:14`: AI evidence range/count stale by 2 entries
   (says 000–016 / 17; actual is 000–018 / 19). Owner: orchestrator. Closes by
   bumping to `000`–`018` / 19.
2. **Nit** — `README.md:125`: `tools/` comment omits `inspect-coverage.js`.
   Owner: orchestrator. Closes by adding it to the list.

Fact: all verification above is from direct file reads on 2026-11-12; no shell
commands were run in this pass (none required — the findings are documentation-only).
Assumption: entries `016`–`018` are GenAI evidence entries counted by R-30; if the
orchestrator intends to exclude the meta entries (`017`, `018`) from the count,
the correct figure would be 17 (000–016) and only the DoD line would need no change
— but then R-20 would still need to stop at `016`, which it already does. Either
way the two artifacts must agree with each other and with `docs/ai/`; they currently
do not agree with the README, which already lists `000`–`018`.
Open question: should the orchestrator bump the count to 19 or define a stable
rule for which entries count, so the number stops drifting as entries are added?

## GenAI narrative assessment

**Verdict: the corrections are real, and the narrative is honest. This is the strongest part of
the deliverable.**

Evidence for that judgement, not for the story:

1. **The corrections are specific, falsifiable, and name a mechanism.** 14 of the 16 entries
   carry a `## Corrections` table whose rows name the wrong thing, the change, and the reason:
   an absolute `Location` header (`docs/ai/003:44`), SQLite returning `DateTimeKind.Unspecified`
   (`:45`), a repository that saved implicitly against ADR-0001 (`:43`), an enum-typed request
   DTO that would have lost the ADR-0004 error key (`docs/ai/007:62`), `DateTime?` leaking a
   timestamp into the JSON (`:63`), a `STATUS_LABELS` export that tripped the oxlint
   fast-refresh rule (`docs/ai/008:157`), ref callbacks assigned during render (`:158`),
   scaffolding that was dead code (`:159`).
2. **The candidate caught the AI's *typical* failure modes, and the pattern is coherent:**
   framework/default semantics (implicit save, `AsNoTracking`, `Enum.TryParse` accepting
   integer strings, `DateTime` kind, absolute URLs, SQLite type mapping) and **contract-vs-code
   contradictions** (ADR-0004 §5 vs §1 on `userId` → `docs/ai/009`; ADR-0004 §4 vs the
   implementation on case-sensitive status). The presentation's summary at
   `docs/presentation.md:128-143` lists nine of these with the same specificity as the logs.
3. **The candidate corrected *its own tooling*, and said so.** The QA script asserted that a
   `404` body had no `status` member — but RFC 7807 always serialises one, so the check could
   never pass. The fix discriminates by type and value and has a **self-test proving it still
   catches a real leak** (`docs/qa/clean-run-report.md:93-117`, `docs/qa/e2e-api-check.test.js`,
   6/6). The first `coverage-report.js` reported 100% everywhere because zero-hit lines were
   never inserted into the line map — recorded, not quietly rewritten (`docs/ai/012:162`).
   This is the strongest possible evidence of critical thinking about AI output, because the
   tool that would have embarrassed them was the one that was wrong.
4. **Two review findings were themselves bugs in the test, not the product** — the missing
   `userId` pin and the `dueDate: null` pin. The second one **exposed a real defect**: the
   repository read with `AsNoTracking`, so every `PUT` mutated a detached entity and the `200`
   was in-memory only — a silent no-op on the update path (`docs/ai/011:29-38`). That is a
   genuine bug found by a test the review asked for, and it is recorded as such.
5. **The RED evidence is unusually candid.** Where the behaviour already existed, the entries
   say so and explain that the RED was obtained by a **temporary mutation with a
   byte-identical restore (SHA-256 verified)**, explicitly calling it "a mutation-based RED, not
   a 'write the test, watch it fail against missing code' RED" (`docs/ai/013:85-95`). They also
   record that the first restore reused a stale DLL and 20 tests failed until the files were
   re-stamped — i.e. that "restored" proves nothing until rebuilt (`docs/ai/013:199`,
   `docs/ai/014:223`). That is exactly the level of honesty the criterion asks for.
6. **The AI's own wrong claims were corrected in place, with the settlement shown.** The
   `Up`-vs-`Down` migration error and the `PasswordHasherAdapter` "gap" were published in two
   entries, then corrected **in those same entries** with the line numbers and the command that
   settled it (`docs/ai/013:205-215`, `docs/ai/014:225-237`), and QA repeated the correction in
   its own artifact (`docs/qa/coverage-summary.md:245-264`).
7. **Rejections are recorded with reasons** (`docs/ai/015:211-222`): no Playwright/new
   dependency, no skip link, no API-contract change, no localised date formatting. ADR-level
   rejections (MediatR/CQRS/AutoMapper/generic repository) are in ADR-0001 §"Alternatives".

**Where the narrative is weaker than it looks (and it is the same defect as the two majors):**
the *corrections did not propagate to every artifact*. The `Up`/`Down` correction reached
`docs/ai/013`, `docs/ai/014` and `docs/qa/coverage-summary.md` but not
`docs/04-requirement-traceability.md:117`. The wave-3 frontend pass reached `docs/ai/015`, the
DoD and both READMEs but not `docs/qa/test-plan.md`. A panel reading the deliverables
sequentially will see the same fact asserted and denied within two files of each other. The
GenAI **process** is sound; the **bookkeeping** is not. Fixing the two majors is the highest-value
hour available before Friday.

**One thing I did not find:** an AI output that was recorded and left uncorrected when it should
have been. Every correction I could trace was either applied or explicitly adjudicated
(FIX / ACCEPT) in `docs/ai/006` and `docs/ai/011`.

---

## What I could not verify

- **Any command.** The reviewer session has no shell permission, so I did not re-run
  `dotnet build/test`, `npm run lint/test/build`, or `node docs/qa/e2e-api-check.js`. Every
  command result in this review is quoted from the captured output in `docs/qa/` and `docs/ai/`
  and is marked **Fact (recorded)**. The recorded outputs are internally consistent
  (counts reconcile across `docs/qa/clean-run-report.md`, `docs/qa/coverage-summary.md`,
  `docs/qa/test-plan.md`, `docs/ai/013`, `docs/ai/014`, `docs/ai/015`), and the code I read
  does not contradict them.
- **The coverage percentages themselves** (the Cobertura files) — I verified their arithmetic
  and internal consistency, and the method/case counts that feed them, but not the measurement
  itself.
- **The screen-reader half of AC-003-10** — no assistive technology is available; the
  implementer says so explicitly (`docs/ai/015:201`, `:224-240`) and the DoD records it as
  "Partially satisfied". I concur with that label.
- **The demo rehearsal** — it has not happened; that is the point of the open item.
- **The browser console during the rehearsal** — not observed, by definition.

---

## Summary

- **What is good (and is evidence, not praise):**
  - The ownership seam is structurally correct, not just tested: the owner filter lives in the
    SQL predicate (`TaskRepository.cs:24`), a foreign task never enters the Application layer,
    and foreign vs missing are indistinguishable in status, title and body
    (`TaskService.cs:14`, `:73-75`; `TasksApiTests.cs:272-341`; `docs/qa/logs/e2e-run.log:17`).
  - The dependency rule is enforced by project references and verified from the files.
  - JWT handling fails safe: no key outside Development means **no startup**, not a weaker token
    (`JwtSigningKey.cs:23-27`; `Program.cs:84`).
  - Error bodies leak nothing, and the `500` branch is proven by a mutation RED that would fail
    if the title ever became `exception.Message`.
  - The frontend keeps exactly two security invariants — owner from the token, and a 401 that
    clears the session once — and both are tested at their own boundary.
  - Every blocker and major from the two prior reviews is verifiably closed
    (timing side-channel → `PasswordHasherAdapter.cs:19-45`; SPEC-001 §8 logging →
    `LoginAudit.cs:12-13`; numeric status → `TaskValidation.cs:78-84`; the `userId` and
    `dueDate: null` pins → `TasksApiTests.cs:173-195`, `:197-232`; CORS breadth →
    `Program.cs:77-78`; `UnitOfWork` keying → `UnitOfWork.cs:21-25`; the return-path guard
    test → `LoginPage.test.tsx:60-89`; the overlong-email case → `AuthServiceTests.cs:69`).
  - The GenAI record is genuine: real framework-level corrections, the candidate's own tooling
    found to be wrong and fixed with a self-test, a real product bug found by a review-requested
    test, and an honest disclosure that two REDs were mutation-based.

- **What must change before delivery (both are documentation, both are one-line edits):**
  1. `docs/qa/test-plan.md` — refresh the frontend numbers (10 files / 69 tests) and correct the
     three places that say the browser console was never observed.
  2. `docs/04-requirement-traceability.md:117` — replace the stale Infrastructure gap list with
     the corrected one already written in `docs/qa/coverage-summary.md:230-239`.

  After those two, this review's "The reviewer has no open blockers" item can be ticked with no
  caveat.

- **What can wait:** the traceability nits (R-24 count, R-30 count), the specs index status, the
  ADR-0001 status, the `C# 13` wording, the unused `@vitest/coverage-v8`, the broken
  `docs/ai/015` filename in the test plan, the E2E script's leftover probe user, the ADR-0001
  folder map, and the RegisterPage password regex.

---

## If there were more time before Friday — in priority order

1. **Reconcile the evidence (highest value per minute).** One pass over `docs/qa/test-plan.md`,
   `docs/04-requirement-traceability.md`, `specs/README.md` and `docs/adr/0001` fixing the two
   majors and the four stale-number minors. The exercise scores *critical thinking*, and the
   strongest thing this delivery has — a correction made and documented — is currently
   contradicted by two neighbouring artifacts. This costs under an hour and removes the single
   biggest attack surface in the deliverable.
2. **Run the full gate once, on one tree, and freeze the numbers.** `dotnet build
   CrudWorks.slnx -warnaserror`, `dotnet test`, `npm run lint`, `npm run test`, `npm run build`,
   `node docs/qa/e2e-api-check.js`, `node --test docs/qa/e2e-api-check.test.js` — from a
   **deleted** database, in one sitting, with the output pasted into one dated block. Right now
   the backend numbers are wave-2 and the frontend numbers are wave-3, which is exactly the
   split that produced the majors. A single captured run closes the DoD §11 gate honestly and
   lets the DoD's "Counts measured" line cite one date.
3. **Rehearse the demo twice, with the console visible, and record the timings.** It is the only
   open item that needs a human, it closes two of the four open DoD items, and it is the thing
   the panel actually watches. The script is already written (`docs/presentation.md` §2); it needs
   a clock, not a rewrite.
4. **Close the two behavioural Infrastructure gaps** named by QA (`DatabaseSeeder.cs:36-37` —
   the "database already has a user" path — and `UnitOfWork.cs:34-35` — the non-SQLite
   inner-exception path). Four lines of test for the two gaps that are about behaviour rather
   than tooling; QA explicitly recommended these two if the timebox allowed
   (`docs/qa/coverage-summary.md:292-306`).
5. **Ask the owner for `git init` and commit the history.** The red→green record currently lives
   only in `docs/ai/`; a real commit history is what R-03 and half of R-26 ask for, and the
   `.gitignore` files are already in place, so the approval is the only thing missing. This is
   the single highest-value *unblockable-by-me* item.
6. **Add a CI pipeline** (QA's open QA-05): `dotnet build -warnaserror` + `dotnet test` +
   `npm run lint/test/build` on every push. It converts every local evidence claim into a
   reproducible one and closes the traceability rows that are only "Manual" today.
7. **One security hardening that would earn its place in the Q&A:** rate limiting on
   `POST /api/auth/login` plus a failed-attempt counter. ADR-0002 already names it as absent
   (`:81`); implementing it would close the one item the panel will most likely probe in a
   security review, and it is small.
8. **Add the two missing contract pins that are cheap and would silence the panel:** an API test
   asserting a `400` on malformed JSON carries the same `type`/`traceId` shape as an application
   `400`, and a test that the CORS policy rejects a *credentialed* cross-origin request
   (`Access-Control-Allow-Credentials` must be absent). Both are one test each and both close
   the two residual nits in this review.
9. **If time is still left: a real browser E2E.** The gap that costs the most credibility is
   the one the panel can probe: no automated browser test of the critical flow, and no automated
   accessibility audit. A single Playwright spec (login → create → edit → delete) plus
   `axe-core` would convert the "Manual" rows of the traceability matrix into automated ones.
   It needs a dependency, which `docs/ai/015:213-217` deliberately avoided — so this is a
   decision to take, not a task to start.

---

*No file under `project/**`, `docs/ai/**`, `docs/qa/**`, `specs/**`, or any README was modified by
this review. The only artifact written is this file, `docs/reviews/final-review.md`
(AGENTS.md §11).*
