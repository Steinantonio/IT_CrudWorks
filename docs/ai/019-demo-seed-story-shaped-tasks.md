# AI Evidence Log

## Entry 019

- **Date:** 2026-11-12
- **Agent:** backend
- **Task:** Reseed the demo tasks with story-shaped content (review finding m1), keeping five tasks, the 2 Todo / 2 InProgress / 1 Done mix, and the idempotent seeding contract.
- **Tool/model:** OpenCode / DeepSeek V4.1 Flash

## Prompt

Summary of the orchestrator prompt: the seeded tasks described the build process
itself ("Write the BLA technical exercise proposal", "Set up the CI pipeline" as
Done although no CI exists) and contradicted the user story of a busy professional
managing their own tasks. Replace the five tasks with realistic example stories,
each with a description and a due date; keep exactly five, keep 2/2/1 statuses,
keep idempotency; update the tests that pin titles, counts, descriptions, due dates
and ordering; do not change schema or migrations; verify with
`dotnet build CrudWorks.slnx -warnaserror` and `dotnet test CrudWorks.slnx`.

## Output summary

- `DatabaseSeeder.SeedDemoTasksAsync` now holds five story-shaped tasks, all with a
  description and a due date, still staggered 30/60/120/180/240 minutes ago so the
  newest-first ordering stays visible.
- The tuple element types tightened from `string? / DateOnly?` to `string / DateOnly`,
  so the compiler now enforces "every demo task has a description and a due date".
- `TasksApiTests.SeededDemoUser_HasFiveTasksAcrossAllStatusesNewestFirst` and
  `TaskRepositoryTests.Seeder_WithEmptyTasks_SeedsFiveTasksAcrossAllStatusesIdempotently`
  now assert the new titles, the 2/2/1 distribution and `All` descriptions/due dates.

## Validation

- [x] Build: `dotnet build CrudWorks.slnx -warnaserror` -> `0 Aviso(s) 0 Erro(s)`
- [x] Unit tests: `dotnet test CrudWorks.slnx` -> Domain 37/37, Application 57/57
- [x] Integration/API tests: Infrastructure 25/25, Api 46/46
- [ ] Manual behavior: not exercised against the running UI; the API-level seed test
      hits the real pipeline (migrations + Development seed) via `WebApplicationFactory`.
- [x] Security review: no auth, ownership, IDOR or secret paths touched; seed content
      is static text with no credentials.
- [x] Architecture review: change confined to Infrastructure (seed) and tests; no
      schema/migration change and no new dependency.

## Corrections

| What the AI suggested | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| Reseed via a new migration or database reset | Not needed and would change schema history | Kept the existing seeder and its empty-table guard | Review asked for content, not schema |
| Assert exact due dates computed in the test | Duplicates the seeder's clock and can flake across UTC midnight | Asserted non-null due dates for every task plus exact titles/statuses | Keep the assertion about behavior, not the wall clock |
| Leave the seed tuple nullable | Allows a future edit to silently ship an empty demo card | Made description and due date non-nullable in the seed tuple | Compile-time guard for the review requirement |

## Edge cases considered

- Seed runs only when `Tasks` is empty, so an existing demo database keeps the old
  rows until it is reset; the seeder itself needed no change to remain idempotent.
- The ordering test relies on distinct staggered timestamps; the 30-minute steps
  remain far apart, so a slow test run cannot reorder them.
- Due dates are relative to `DateTime.UtcNow` at seed time, so the demo always shows
  future work plus one overdue Done item regardless of the demo date.

## Auth and validation decisions

- None. The demo user, password constant and hashing path are untouched.

## Files affected

- `project/src/CrudWorks.Infrastructure/Persistence/Seed/DatabaseSeeder.cs`
- `project/tests/CrudWorks.Api.Tests/Tasks/TasksApiTests.cs`
- `project/tests/CrudWorks.Infrastructure.Tests/Persistence/TaskRepositoryTests.cs`

## Final decision

Accepted. Content-only change, guarded by two seed tests and a clean
warnings-as-errors build.
