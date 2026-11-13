# TASKS-002 — Task CRUD

- **Spec:** `specs/002-task-crud/spec.md`
- **Plan:** `specs/002-task-crud/plan.md`
- **Status:** implemented
- **Last updated:** 2026-11-12

## Tasks

| ID | Task | Owner | Depends on | Test / evidence | Status |
| --- | --- | --- | --- | --- | --- |
| T-002-01 | `TaskItem` aggregate + `TaskItemStatus` | Backend | — | `TaskItemTests` | done |
| T-002-02 | Title/description/status/due-date invariants | Backend | T-002-01 | `TaskItemTests.Create_WithTitleShorterThanThreeCharacters_ThrowsDomainException`, `Create_WithDescriptionLongerThanTwoThousandCharacters_ThrowsDomainException` | done |
| T-002-03 | `ITaskRepository` + `TaskService` list scoped by owner | Backend | T-002-01 | `TaskServiceTests.ListAsync_DelegatesToTheRepositoryScopedByTheCaller` | done |
| T-002-04 | Create assigns owner and timestamps | Backend | T-002-03 | `TaskServiceTests.CreateAsync_AssignsTheCallerUserIdAndServerTimestamps` | done |
| T-002-05 | Get/update/delete return `404` for foreign or missing ids | Backend | T-002-03 | `TaskServiceTests.GetAsync_WithAnotherUsersTask_ThrowsNotFoundWithoutLeakingExistence` | done |
| T-002-06 | EF `TaskConfiguration` + migration + index | Backend | T-002-01 | `TaskRepositoryTests.Migration_AppliesAddTasks`, `TasksTable_HasTheUserIdIndex` | done |
| T-002-07 | `TaskRepository` with ordering and tracking | Backend | T-002-06 | `TaskRepositoryTests.ListByOwnerAsync_OrdersByCreatedAtUtcDescending` | done |
| T-002-08 | `TasksController` with `Location` and status codes | Backend | T-002-03..07 | `TasksApiTests.Create_WithValidPayload_Returns201WithLocationAndTaskResource` | done |
| T-002-09 | Current-user accessor from the JWT subject | Backend | T-001-11 | `TasksApiTests.EveryTaskRoute_WithoutAToken_Returns401` | done |
| T-002-10 | IDOR tests: read, update, delete | Backend/QA | T-002-08 | `TasksApiTests.Get_ForAnotherUsersTask_Returns404AndTheRowStillExistsForTheOwner` (update/delete variants too) | done |
| T-002-11 | Seed five tasks across all statuses | Backend | T-002-06 | `TaskRepositoryTests.Seeder_WithEmptyTasks_SeedsFiveTasksAcrossAllStatusesIdempotently` | done |
| T-002-12 | Task list with four states | Frontend | T-002-08 | `TaskListPage.test.tsx` | done (tracked as T-003-07) |
| T-002-13 | Create/edit form with server error mapping | Frontend | T-002-08 | `TaskFormPage.test.tsx` | done (tracked as T-003-08/09) |
| T-002-14 | Delete confirmation | Frontend | T-002-12 | delete tests in `TaskListPage.test.tsx` | done (tracked as T-003-10) |

## Notes

- T-002-10 is the security seam; do not defer it.
- The list ordering assertion belongs in the repository and API tests.
- Seed data is shared with TASKS-001; one seeder, one demo user.
- Review note (auth-slice review, nit): the UnitOfWork unique-constraint mapping currently reports every violation as "Email already registered.". When the Tasks schema lands, key the mapping on the table/constraint name so future unique indexes are not misreported.
- Frontend note: T-003-11 must include the "clears exactly once / no retry loop" test at the `lib/api.ts` boundary (AC-003-08), not only the context-level probe.
- The three frontend tasks (T-002-12…T-002-14) duplicate T-003-07…T-003-10 in
  `specs/003-web-client/tasks.md`, which own the frontend work. They are marked
  `done` with a cross-reference rather than left as duplicates.
- T-002-10 (the IDOR seam) is closed and independently verified: the live
  end-to-end run proves read, update and delete by a second user all return
  `404` (`docs/qa/logs/e2e-run.log`, checks C4/C5/C6), and the row still
  belongs to its owner (C7).
- The spec moves to `implemented` (see `specs/README.md` §2): all tasks are done,
  the independent review (`docs/reviews/review-task-slice.md`) had no open
  blockers and its one major finding is fixed, and the clean-database run
  proves the live behaviour.
