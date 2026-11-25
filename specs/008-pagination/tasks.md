# TASKS-008 — Task list pagination

- **Spec:** `specs/008-pagination/spec.md`
- **Plan:** `specs/008-pagination/plan.md`
- **Status:** implemented
- **Last updated:** 2026-11-25

## Status legend

`todo` · `red` (failing test written) · `green` (test passes) · `done` (refactored,
verified, documented) · `blocked`

## Tasks

| ID | Task | Owner | Depends on | Test / evidence | Status |
| --- | --- | --- | --- | --- | --- |
| T-008-01 | Accept ADR-0006 and SPEC-008; resolve the five open questions (Load more vs auto-load, counts/filter note, scale target, envelope vs header, cursor expiry). | Project owner + architect | — | ADR-0006 `accepted`; SPEC-008 `approved`; open-question resolutions recorded | done |
| T-008-02 | Add `TaskPageCursor`, `TaskPageResponse`, `ITaskRepository.ListPageAsync`, and `ITaskService.ListAsync(userId, cursor, take)`; validate `take` (1–100, default 10) and build `nextCursor` from the last row. | Backend implementer | T-008-01 | `TaskPageCursor`/`TaskPage` added; Application tests cover bounds, trimming, next cursor, exact page; Application suite 62/62. | done |
| T-008-03 | Implement the keyset query in `TaskRepository`; replace `IX_Tasks_UserId` with `IX_Tasks_UserId_CreatedAtUtc_Id`; add the `AddTaskPaginationIndex` migration. | Backend implementer | T-008-02 | Infrastructure tests cover 12 rows, cursor page, foreign cursor, same-timestamp tie-breaker, composite index; Infrastructure suite 28/28; migration applies. | done |
| T-008-04 | Add `TaskCursorCodec` (base64url JSON `{v,c,i}`), bind `cursor`/`take` in `TasksController.List`, return the envelope, and map invalid input to `400` problem+json. | Backend implementer | T-008-03 | API tests cover default 10, next page, invalid `take`/cursor, unsupported version, ownership, empty envelope; Api suite 80/80. | done |
| T-008-05 | Update `OpenApiContractTests` for the query parameters, `TaskPageResponse`, and the `400` response; regenerate `src/api/generated/openapi.d.ts` and verify determinism. | Backend + frontend implementers | T-008-04 | `TaskList_DocumentsPaginationParametersAndEnvelope` passes; two regenerations identical SHA-256 `B7D7C4EE9B4E39DE336ED175368971F6939D04F901F1458C32021E1BC0CDF153`; `take` is `integer` with `default: 10`. | done |
| T-008-06 | Replace `listTasks` with `listTasksPage`; move `TaskListPage` to `useInfiniteQuery`; add “Load more”, end-of-list, loading/error, cursor-`400` reset, and the loaded-tasks note; update the tests. | Frontend implementer | T-008-05 | `npm run lint` clean; `npm run test` 104/104; `npm run build` succeeded; component tests cover first page, load more, end-of-list, cursor reset, counts note, first-page 400, loading state. | done |
| T-008-07 | Run the full backend/frontend suites, the 10,000-row performance smoke, and update ADR-0004 §4, traceability, README/spec docs, and the AI evidence entry. | Backend + frontend implementers | T-008-06 | Backend 216/216; frontend 104/104; E2E 40/40 twice with baseline cleanup; reproducible 10,000-row smoke p95 2.00 ms first / 4.33 ms cursor; query plan uses `IX_Tasks_UserId_CreatedAtUtc_Id`; docs updated. | done |
| T-008-08 | Independently review the change set against ADR-0006/SPEC-008, including the breaking contract, ownership, cursor errors, migration, and frontend reset behavior. | Reviewer / QA | T-008-07 | First review found 1 blocker (stale E2E) and several majors; all fixes applied. Follow-up review: **Approve with comments**, no blockers/majors; remaining nits fixed. | done |

## Notes

- ADR-0006 is accepted and SPEC-008 is implemented; T-008-08 is the only open task.
- The contract change is breaking: `GET /api/tasks` moves from an array to `{ items, nextCursor }`. API, OpenAPI, generated types, frontend, and tests changed in one wave.
- Baseline facts before the wave: bare array (`TasksController.cs:14-18`), order by `CreatedAtUtc` only (`TaskRepository.cs:9-16`), single-column index (`TaskConfiguration.cs:24-26`), frontend `useQuery`/`listTasks` (`TaskListPage.tsx:49-53`, `lib/tasks.ts:60-62`), contract test expected `200,401,500` (`OpenApiContractTests.cs:204`).
- The cursor is opaque but not an authorization token; ownership is always the `UserId` filter. Do not sign it unless a future ADR finds a security reason.
- Status counts and filters cover loaded pages in this wave; server-side filtering is a separate ADR.
- No CI/CD change; the Render deployment keeps working after the migration.
