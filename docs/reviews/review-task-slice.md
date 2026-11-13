# Code Review: Task slice (SPEC-002 backend T-002-01…11, SPEC-003 frontend T-003-07…11)

- **Reviewer:** reviewer agent
- **Date:** 2026-11-12
- **Scope reviewed:** backend task CRUD (Domain/Application/Infrastructure/Api + four test projects) and frontend task UI (`lib/tasks.ts`, list/form pages, dialog, 401 wiring), per the supplied change set. No git repo; files read directly.
- **Evidence basis:** line-by-line code and test reading against `specs/002`, `specs/003`, ADR-0002/0004/0005 and `docs/ai/009`. Build/test/lint/build/live-run claims taken from `docs/ai/007-task-backend-slice.md` and `docs/ai/008-task-ui-slice.md`, not re-run, per orchestrator instruction.

## Verdict

**Request changes** — no blockers; one major (numeric status strings bypass the contract validation matrix) must be fixed or explicitly accepted as a recorded risk before the demo. Everything else is minor or nit; the ownership/IDOR core is solid.

## Findings

| Severity | File:line | Finding | Why it matters | Suggested fix |
| --- | --- | --- | --- | --- |
| Major | `project/src/CrudWorks.Application/Tasks/TaskValidation.cs:77-81` | `TryParseStatus` accepts **numeric status strings**: `Enum.TryParse<TaskItemStatus>("1", false, out parsed)` succeeds for integer names, and `Enum.IsDefined` then confirms the mapped member — so `"status": "1"` → `InProgress` (and `"0"`/`"2"` → `Todo`/`Done`) is accepted on create **and** update, returning 200/201 where the contract requires 400. ADR-0004 §4 (`docs/adr/0004-api-contract.md:147-148`) pins status to the three names **case-sensitively**, and §5 (`:213`) says "One of the three names". The status theory (`tests/CrudWorks.Application.Tests/Tasks/TaskServiceTests.cs:117-122`) has lowercase and unknown names but no numeric case, so nothing catches it. | A panel probing the validation matrix with `"status": "1"` gets a created/updated task instead of `400` + `errors.status` — an invented validation behavior and a wrong status code against the approved contract, exactly the kind of contract-drift question this exercise invites. No ownership or data-integrity impact (the enum is stored as its name), which is why this is a major, not a blocker. | In `TryParseStatus`, reject strings containing only digits before `Enum.TryParse` (e.g. `status.Any(char.IsDigit) ? null : …`, or match against an explicit `["Todo","InProgress","Done"]` list). Add one API-level and one Application-level case asserting `"1"` → 400 with the `status` key. Owner: backend. |
| Minor | `project/tests/CrudWorks.Api.Tests/Tasks/TasksApiTests.cs` (missing case) | No end-to-end pin that a **client-supplied `userId` in the body is ignored** (AC-002-01 "stored with the caller's user id, never a client-supplied one"; ADR-0004 §1 as clarified 2026-11-12). The DTOs contain no `userId` property (`CreateTaskRequest.cs:8`, `UpdateTaskRequest.cs:8`), so the binder ignores it — but the only related assertion checks the response body (`TasksApiTests.cs:134-135`), not that a foreign `userId` in the request had no effect. | The clarified ownership contract has no failing test protecting it; a future DTO change that adds `userId` (or a binder behavior change) would silently reopen the IDOR seam the ADR exists to close. | One API test: POST/PUT with `userId: "<another user's id>"` in the body; assert the task is owned by the caller (owner visible via the caller's own list/get). Owner: backend. |
| Minor | `project/tests/CrudWorks.Api.Tests/Tasks/TasksApiTests.cs:112-139` | `PUT` with `dueDate: null` **clearing a previously set due date** is untested (SPEC-002 §6 `specs/002-task-crud/spec.md:139-141`: "A null description or due date clears the value"). Description clearing is covered (`:120-131` and `TaskServiceTests.cs:191-212`); dueDate clearing is not. | The half of the PUT-replacement rule that the edit form exercises when a user removes a due date has no failing test; a regression (e.g. a mapper defaulting nulls) would not be caught. | Extend the API update test: create with a dueDate, PUT with `dueDate = null`, assert the response (and a re-GET) show `dueDate: null`. Owner: backend. |
| Minor | `docs/04-requirement-traceability.md:22-23` | Traceability is stale for the delivered task UI: R-16 "CRUD in the UI" is still **Planned** and R-15 still says "task UI pending", although T-003-07…11 are done with component tests, a real-API smoke run, and viewport measurements (`docs/ai/008-task-ui-slice.md`, `specs/003-web-client/tasks.md:18-22`). | A reviewer following the matrix from the DoD checklist finds dead rows for work that exists; the reverse (rows marked done without evidence) is worse, but either direction erodes the matrix's value at the final gate. | Update R-15/R-16 (and R-05/R-07 status if QA evidence has since landed) with the concrete evidence pointers from `docs/ai/008`. Owner: orchestrator, evidence from frontend. |
| Nit | `specs/003-web-client/tasks.md:31` | The note says the AC-003-05…07 real-API run "remains open", but `docs/ai/008-task-ui-slice.md` already records a real-API smoke against the implemented backend (login → list of 5 → create → delete dialog → delete). The two artifacts disagree on what is open. | Small honesty-of-record drift; the remaining open item is only "repeat after the T-002-08 review closes" and the final T-003-12/13 pass. | Reconcile the note: strike the "remains open" clause or restate exactly what is still owed. Owner: frontend (tasks.md status column). |
| Nit | `project/tests/CrudWorks.Api.Tests/Tasks/TasksApiTests.cs:251-284` | No raw-JSON API-level case for the **case-sensitive** status rule (`"todo"` → 400) that ADR-0004 §4 added on 2026-11-12; the contract behavior is covered at the Application layer (`TaskServiceTests.cs:118-119`) and the API test only uses `"Unknown"`. | The ADR change log calls the case-sensitivity rule out explicitly; a one-line test pins it at the boundary where the panel will probe. | Add a status-input case (`status = "todo"`) to the 400 theory. Owner: backend. |

## Adjudications requested by the orchestrator

1. **Seeder timestamps via `dbContext.Entry(task).Property(...).CurrentValue`** (`project/src/CrudWorks.Infrastructure/Persistence/Seed/DatabaseSeeder.cs:93-96`, same pattern in `TaskRepositoryTests.cs:264-274`). **Accepted risk — keep as-is.** The domain deliberately has no timestamp setter (`TaskItem.cs` — private setters, factory stamps `UtcNow`), the bypass writes only timestamps, no invariant depends on them, the intent is commented (`DatabaseSeeder.cs:93`), and the decision is recorded in `docs/ai/007` corrections. Recommendation: this pattern must stay confined to the seeder and test helpers — never a production write path. Nothing to change now.
2. **Status required on update (missing field must not silently reset it).** **Confirmed working and tested.** The 400 path exists (`TaskValidation.cs:61-65`) and is asserted at the Application layer (`TaskServiceTests.cs:229-247`, no save) and the API layer (`TasksApiTests.cs:286-301`).

## Architecture checks

- [x] Dependency rule respected, verified from the actual project files: `CrudWorks.Domain.csproj` has no references; `CrudWorks.Application.csproj:4` → Domain only; `CrudWorks.Infrastructure.csproj:4` → Application only; `CrudWorks.Api.csproj:19-20` → both. No EF/ASP.NET Core/HTTP types in the new Domain/Application code (grep: only BCL `System.Net.Mail` for the pre-existing email validator; no `Microsoft.*` usings in `Application/Tasks/**` or `Domain/Tasks/**`).
- [x] Business rules live in Application/Domain — `TasksController.cs` binds, delegates, and maps status codes only (`:14-58`); invariants in `TaskItem.Create`/`Update` (`TaskItem.cs:57-123`); validation in `TaskValidation`; mapping in `TaskMapping`.
- [x] Repository interface in Application (`ITaskRepository.cs`), EF implementation in Infrastructure (`TaskRepository.cs`); no generic repository; `IUnitOfWork` owns the save (`TaskService.cs:43,61,70`).
- [x] Composition root in `Api` (`Program.cs:59-67` DI registrations, `:20-22` JSON converters); no MediatR/CQRS/AutoMapper.
- [x] Migration ↔ model consistency: `20261112184118_AddTasks.cs:14-42` (TEXT status/dueDate, FK Restrict `:30-35`, `IX_Tasks_UserId` `:38-41`) matches `TaskConfiguration.cs:12-56` and the snapshot; index and FK enforced by tests (`TaskRepositoryTests.cs:40-78`).
- [x] UTC converters consistent on both timestamp columns with `DateTimeKind.Utc` reattachment (`TaskConfiguration.cs:45-55`); round-trip kind pinned by `TaskRepositoryTests.cs:106-107` and the API seed test (`TasksApiTests.cs:321`).

## Security checks

- [x] `[Authorize]` at controller level on every task route (`TasksController.cs:10`); anonymous only on register/login/health (unchanged from the auth slice).
- [x] Ownership: owner always from the token `sub` (`HttpCurrentUser.cs:9-16` via `AuthClaimNames.Subject`), never from the body — no request DTO contains a user id; the controller passes `RequireUserId()` (`TasksController.cs:61-62`) and the service threads it into every repository call (`TaskService.cs:18,24,35,55,68`). The repository scopes every read by `id && userId` (`TaskRepository.cs:19-22`), so a foreign task is indistinguishable from a missing one.
- [x] Foreign **and** missing ids both → 404 with the identical safe title `"Task not found."` (`TaskService.cs:14,73-75`); no existence leak. IDOR is tested end-to-end for read, update, and delete with the row verified intact for the owner (`TasksApiTests.cs:179-234` — the owner's list/GET is re-fetched after each attack).
- [x] `UnitOfWork` regression check: unique-violation mapping is now keyed on `Users.Email` (`UnitOfWork.cs:21-25,38-42`), so a future Tasks unique index cannot be misreported as an email conflict — the auth-review nit is fixed, not regressed.
- [x] No secrets, tokens, or auth headers in logs; error responses are ProblemDetails only (`ApiProblemDetails.cs`), with the 400 title `One or more validation errors occurred.` (`ValidationException.cs:8`) and field keys `title`/`description`/`status`/`dueDate` camelCase (`TaskValidation.cs:26,31,36,41,63,68`).
- [x] CORS restricted to the configured origin with explicit methods/headers and no credentials (`Program.cs:71-79`) — tightened from the auth-slice state, matching ADR-0004 §6.
- [x] Update validates the body before the ownership check, so an invalid body on a foreign id returns 400 — body-derived, resource-blind; no existence leak (checked `TaskService.cs:48-64`).

## Contract checks (ADR-0004)

- [x] Routes and methods exactly the five task routes; `{id:guid}` constraints; no extra endpoints.
- [x] `201` + `Location: /api/tasks/{id}` (`TasksController.cs:36`; pinned `TasksApiTests.cs:28`), `200`, `204` with empty body (`:150-151`), `401` on all five routes without a token (`:155-177`), empty list → `200 []` (`:79-87`).
- [x] `status` serialized as exact strings and `dueDate` as `yyyy-MM-dd`, pinned by raw-JSON assertions (`TasksApiTests.cs:50-55`); nulls serialized, never omitted (`:105-108`).
- [x] Exact validation messages and keys for all four fields (`TasksApiTests.cs:279-283`) plus boundary acceptance at 3/200 title and 2000 description (`TaskItemTests.cs:95-121`).
- [x] Status default `Todo` on create, required on update (see adjudication 2); `userId` ignored per the clarified contract — see the Minor finding for the missing end-to-end pin.
- [x] **Major finding:** numeric status strings are accepted where the contract pins the three names case-sensitively — see the table.

## Test checks

- [x] Behavior-level tests at every layer; IDOR variants verify state, not implementation; per-test/per-fixture SQLite (`CrudWorksApiFactory` + `TaskRepositoryTests.cs:17-38` per-test connection), no shared mutable database.
- [x] Failure paths: validation (all fields, boundaries, empty/whitespace), 401, 404 (foreign + missing), delete-then-get 404, 400-on-update-missing-status, seeder idempotency (run twice, count asserted `TaskRepositoryTests.cs:219-262`).
- [x] Determinism acceptable: the list-ordering test separates two creates with a 20 ms delay (`TasksApiTests.cs:65-66`) — .NET 6+ `DateTime.UtcNow` uses high-resolution interpolation, so this is not a flake risk at demo scale.
- [ ] Gaps worth naming: numeric status case missing (Major above); client-`userId`-ignored end-to-end; `dueDate` null clearing; lowercase-status API-level pin (Nits/Minors above).

## Frontend checks

- [x] One API layer: every task call goes through `lib/tasks.ts` → `apiRequest` (`lib/api.ts` owns base URL, auth header, JSON, ProblemDetails → `ApiError`); no invented endpoints — grep confirms only the five ADR-0004 task routes plus the auth routes.
- [x] 401 handled exactly once with no retry loop, verified across the three layers: (1) `taskRequest` notifies only while `readSession()` still reports a session and always rethrows (`lib/tasks.ts:44-66`); (2) `AuthProvider` clears and nulls the session in one idempotent handler registered/unregistered per provider lifetime (`AuthProvider.tsx:58-68`); (3) the App query client skips retrying `401`s (`App.tsx:13-14`). Tested at the `lib/tasks.ts` boundary (`tasks.test.ts:120-163`, including the no-session guard and the once-across-three-attempts case) and at the app level with a retrying client asserting fetch ≤ 2 calls (`TaskListPage.test.tsx:252-269`).
- [x] Delete dialog: DELETE sent only after confirm; Cancel and Escape close without any mutation request (`TaskListPage.test.tsx:148-167,201-221`); focus moves in on open, Tab wraps, focus returns to the trigger on close (`ConfirmDialog.tsx:37-81`); failed delete keeps the row with a safe alert (`:223-250`); confirm defaults focus to Cancel, not the destructive button.
- [x] Form mirrors §5/ADR-0004: shared create/edit, PUT full replacement with `null` clearing empties (`TaskFormPage.tsx:44-51`; payload pinned by `TaskFormPage.test.tsx:212-217` and the null-defaults test `:124-154`), server 400 mapped to matching inputs with unknown keys falling back to a form-level message (`:108-124`; tested `:236-305`), 404 → safe not-found state on load (`:146-158`; tested `:224-234`) and a safe message on save (`:103-105`; tested `:307-331`).
- [x] List has all four states with retry and a connection-specific error path (`TaskListPage.tsx:64-87`; tested `:60-146`).
- [x] No `any`, no `console.*` (grep over `src/`: the only `\bany\b` hit is `expect.any(Headers)` in a test); labels/`aria-invalid`/`aria-describedby`/`role="alert"`/`role="status"` used consistently; ids are `encodeURIComponent`-guarded (`lib/tasks.ts:73,84,91`).
- [x] Components never touch storage directly; `tasks.test.ts` reads `sessionStorage` only to assert the clear, and `tasks.ts` reads session state via `auth/session.readSession` — the single storage owner from the auth review is preserved.

## What I verified vs. could not verify

**Verified by reading code, tests, specs, and ADRs** (every claim has a `file:line` above): dependency rule from project files; ownership/IDOR path end to end including the 404-shape equivalence of foreign and missing ids; contract shapes (Location, status codes, raw JSON, messages, keys, nulls, empty list); the UnitOfWork keyed mapping; migration/model/snapshot consistency; seeder idempotency and its timestamp bypass; frontend contract usage, 401 exactly-once logic across `tasks.ts`/`AuthProvider`/`App`, dialog a11y, form 400 mapping and PUT-replacement payloads; test-to-AC mapping for SPEC-002 AC-002-01…10 and SPEC-003 AC-003-05…08.

**Not independently verified (accepted from recorded evidence per orchestrator instruction):** `dotnet build -warnaserror` 0/0, 136 backend tests, `npm run lint/test/build` green (51 tests), the live-API smoke runs, and the viewport measurements — all documented with command output in `docs/ai/007` and `docs/ai/008`; nothing in the code contradicts them. The numeric-status behavior is asserted from documented .NET `Enum.TryParse` semantics (integer strings parse), not from a run; the fix and its test make the question moot.

**Open questions for the orchestrator:**

1. Accept the numeric-status finding as a **fix** (recommended — a ~3-line guard plus one test) or record it as an accepted risk in ADR-0004 before Friday. Decision owner: orchestrator/architect.
2. R-15/R-16 traceability rows need updating before the final gate; who supplies the evidence row text — frontend or orchestrator?

## Summary

- **Good:** the ownership seam is genuinely tight (server-derived owner, scoped repository, indistinguishable 404s, IDOR tests that verify the owner's data survived); the contract is pinned by raw-JSON tests rather than types alone; the 401 exactly-once mechanism is layered and tested at its boundary; the seeder and UnitOfWork carry-over items from the auth review were addressed, not regressed.
- **Must change before merge/demo:** the numeric status validation bypass (fix or record as an accepted risk with an ADR change-log entry).
- **Can wait:** the three missing test pins (client `userId`, `dueDate` clearing, lowercase status at the API boundary), the traceability row refresh, and the tasks.md note reconciliation.
