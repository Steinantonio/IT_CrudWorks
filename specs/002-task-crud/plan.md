# PLAN-002 — Task CRUD implementation plan

- **Spec:** `specs/002-task-crud/spec.md`
- **Status:** approved
- **Owner:** architect
- **Last updated:** 2026-11-11

## 1. Architecture impact

| Layer | Change |
| --- | --- |
| Domain | `TaskItem`, `TaskItemStatus`, invariant methods (`Update`, `EnsureOwnedBy`). |
| Application | `ITaskRepository`, `ITaskService`, request/response DTOs, validation, ownership scoping. |
| Infrastructure | `TaskRepository`, EF configuration, migration. |
| Api | `TasksController`, current-user accessor, status-code mapping. |
| Frontend | Task list and form per SPEC-003. |

## 2. Data model

`Tasks`

| Column | Type | Constraint |
| --- | --- | --- |
| `Id` | TEXT (Guid) | PK |
| `UserId` | TEXT (Guid) | FK → `Users.Id`, required, indexed |
| `Title` | TEXT | required, max 200 |
| `Description` | TEXT | nullable, max 2000 |
| `Status` | TEXT | required, one of `Todo`, `InProgress`, `Done` |
| `DueDate` | TEXT (DateOnly) | nullable |
| `CreatedAtUtc` | TEXT (DateTime) | required |
| `UpdatedAtUtc` | TEXT (DateTime) | required |

Index: `IX_Tasks_UserId`. The list query filters by `UserId` and orders by
`CreatedAtUtc` descending. No pagination in this timebox.

## 3. Contract

`docs/adr/0004-api-contract.md` §4. The body never contains `userId`; the server
derives it from the token. Ownership violations return `404`.

## 4. Test strategy

| Layer | What is proven | Where |
| --- | --- | --- |
| Domain unit | Title/description invariants, status transitions, `Update` timestamps | `tests/CrudWorks.Domain.Tests` |
| Application unit | List filters by owner; get/update/delete return null or throw on foreign ids; validation | `tests/CrudWorks.Application.Tests` |
| Infrastructure integration | Mappings, FK, index, ordering, persistence across contexts | `tests/CrudWorks.Infrastructure.Tests` |
| API integration | Every status code, `Location`, `400` field errors, IDOR for read/update/delete | `tests/CrudWorks.Api.Tests` |
| Frontend component | List states, form validation/submit, delete confirmation | `project/frontend/src/**/*.test.tsx` |

## 5. Task order

1. Domain `TaskItem` + tests.
2. Application `ITaskRepository`, DTOs, validation, `TaskService` + tests.
3. EF configuration, repository, migration.
4. API controller and current-user accessor.
5. API tests including the three IDOR cases.
6. Seed tasks covering all three statuses.
7. Frontend list/form per SPEC-003.

## 6. Risks and fallbacks

| Risk | Impact | Fallback |
| --- | --- | --- |
| Ownership check only in controller | IDOR risk | Scope every query by user id in the repository/service; test it |
| `DateOnly` serialization drift | Wrong due dates | One contract format `yyyy-MM-dd`, tested at the API |
| Status stored as int | Unreadable data | Store as string, parse strictly |
| List ordering untested | Demo looks random | Assert descending `createdAtUtc` in an integration test |

## 7. Definition of done

- [ ] Every acceptance criterion in SPEC-002 has a test.
- [ ] IDOR is tested for read, update, and delete.
- [ ] Build and tests pass; no warnings.
- [ ] Seed covers Todo, InProgress, and Done.
- [ ] AI evidence entry exists.
- [ ] Reviewer has no open blockers.
