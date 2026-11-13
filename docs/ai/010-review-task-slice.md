# AI Evidence Log

## Entry 010

- **Date:** 2026-11-12
- **Agent:** reviewer
- **Task:** Review the task slice — SPEC-002 backend (T-002-01…11) and SPEC-003 frontend (T-003-07…11)
- **Tool/model:** OpenCode subagent (GLM-5.3-Flash Free)

## Prompt (summary)

Review-only pass over the supplied change set (no git repo; files read directly).
Check spec conformance against SPEC-002 ACs and SPEC-003 AC-003-05…08, the
dependency rule, ownership/IDOR (owner from `sub`, foreign ≡ missing → 404 "Task
not found."), ADR-0004 as clarified by `docs/ai/009` (userId ignored, status
case-sensitive, exact messages, yyyy-MM-dd), data layer (migration ↔ model, UTC,
seed idempotency, UnitOfWork keyed violation mapping), frontend contract usage,
401 exactly-once without a retry loop, delete-dialog a11y, PUT full replacement,
400→input mapping, and test quality. Adjudicate the seeder timestamp bypass and
the required-on-update status path. Do not re-run builds/tests (evidence already
captured in `docs/ai/007` and `docs/ai/008`); write only
`docs/reviews/review-task-slice.md` and this entry.

## What I read

`AGENTS.md`, `specs/constitution.md`, `specs/002-task-crud/{spec,tasks}.md`,
`specs/003-web-client/{spec,tasks}.md`, ADR-0002/0004/0005, `docs/ai/009`,
`docs/02-definition-of-done.md`, `docs/reviews/review-auth-slice.md`,
`docs/ai/007` and `008`, `docs/04` (grep), then the change set: `TaskItem`,
`TaskItemStatus`, all of `Application/Tasks/**`, `TaskRepository`,
`TaskConfiguration`, `UnitOfWork`, `DatabaseSeeder`, `CrudWorksDbContext`, the
`AddTasks` migration + snapshot, `TasksController`, `Program.cs`, `ProblemDetails`
handler, `TasksApiTests` (382 lines), `TaskServiceTests`, `TaskItemTests`,
`TaskRepositoryTests`, and the frontend change set (`lib/api.ts`, `lib/tasks.ts`
+ test, both task pages + tests, `ConfirmDialog`, `StatusBadge`, `ErrorState`,
`AppRoutes`, `App`, `AuthProvider`, `session.ts`, test helpers) plus project
reference files and targeted greps (framework usings, `any`/`console.*`,
`userId`, `/api/health`).

## Findings count by severity

- **Blocker: 0**
- **Major: 1** — `TaskValidation.TryParseStatus` accepts numeric status strings
  (`"1"` → `InProgress` via `Enum.TryParse` integer parsing), returning 200/201
  where ADR-0004 pins 400; untested case.
- **Minor: 3** — no end-to-end pin that a client-supplied `userId` is ignored;
  no test for `PUT` `dueDate: null` clearing a set due date; traceability rows
  R-15/R-16 stale for the delivered task UI.
- **Nit: 2** — `specs/003/tasks.md` note contradicts the real-API run already
  recorded in `docs/ai/008`; no API-level raw-JSON case for `"todo"` → 400
  (covered at the Application layer).

Adjudications: seeder `Entry(...).CurrentValue` timestamp bypass **accepted as a
recorded demo shortcut** (domain has no timestamp setter by design; keep it out
of production paths); required-on-update status **confirmed implemented and
tested** at both the Application and API layers.

## Validation

- [x] Every finding carries `file:line`, impact, and a concrete fix.
- [x] No builds/tests re-run (per orchestrator instruction); build/test/lint
  claims taken from `docs/ai/007`/`008` and flagged as accepted evidence. The
  numeric-status behavior is asserted from documented .NET `Enum.TryParse`
  semantics and is made moot by the recommended fix + test.
- [x] Regression check on the auth-slice review: the `UnitOfWork` unique-violation
  mapping is now keyed on `Users.Email` — fixed, not regressed; CORS and the
  timing-side-channel adjudications are outside this change set and were not
  re-litigated.
- [x] No code, tests, or docs other than the two review artifacts were modified.

## Verdict

**Request changes** (1 major, 0 blockers). Full report:
`docs/reviews/review-task-slice.md`.
