# AI Evidence Log

## Entry 037

- **Date:** 2026-11-25
- **Agent:** orchestrator / GPT-6 Luna
- **Task:** Implement and verify Wave 5 (keyset task pagination, ADR-0006 / SPEC-008).
- **Tool/model:** GPT-6 Luna (OpenCode); .NET SDK 10.0.404; Node v26.13.1.

## Prompt

Faithful summary: the owner accepted ADR-0006 and approved SPEC-008 with the
recommended options: “Load more” button (auto-load optional), loaded-tasks note,
10,000 tasks/user and p95 ≤ 100 ms, `{items,nextCursor}` envelope, and a
versioned cursor without time-based expiry. Implement T-008-02..07, test first,
then verify T-008-08. No CI change.

## Output summary

- Application: `TaskPageCursor`, `TaskPage`, `ITaskRepository.ListPageAsync`,
  `ITaskService.ListAsync(userId, cursor, take)`, page-size validation (1–100),
  `nextCursor` from the last returned row.
- Infrastructure: keyset query on `(CreatedAtUtc DESC, Id DESC)` with the
  `Guid` tie-breaker; composite index `IX_Tasks_UserId_CreatedAtUtc_Id`; migration
  `AddTaskPaginationIndex` replacing `IX_Tasks_UserId`.
- Api: `TaskCursorCodec` (base64url JSON `{v,c,i}`), `TaskPageResponse`,
  controller query binding, `400` problem+json for invalid `take`/cursor, and an
  OpenAPI transformer that normalizes `take` to `integer` with `default: 10`.
- Frontend: `listTasksPage`, `CursorExpiredError`, `useInfiniteQuery`,
  “Load more”, end-of-list, cursor-`400` reset, loaded-tasks note, tests.
- Docs: ADR-0006 accepted, ADR-0004 §4 updated, SPEC-008/PLAN-008/TASKS-008,
  README, traceability.

## Validation

- [x] Backend: `dotnet test CrudWorks.slnx --verbosity minimal` →
  Domain 37, Application 62, Infrastructure 28, Architecture 6, Api 83
  (**216 passed, 0 failed**).
- [x] Build: `dotnet build CrudWorks.slnx --verbosity minimal` → **0 warnings,
  0 errors**.
- [x] Frontend: `npm run lint` clean; `npm run test` → **13 files, 104/104
  passed**; `npm run build` succeeded.
- [x] OpenAPI: `TaskList_DocumentsPaginationParametersAndEnvelope` passes;
  `take` is `integer` with `minimum: 1`, `maximum: 100`, `default: 10`;
  `TaskPageResponse` requires `items`/`nextCursor`; `nextCursor` is nullable
  string.
- [x] Generated types: two runs produced identical SHA-256
  `B7D7C4EE9B4E39DE336ED175368971F6939D04F901F1458C32021E1BC0CDF153`:

  ```text
  first=B7D7C4EE9B4E39DE336ED175368971F6939D04F901F1458C32021E1BC0CDF153
  second=B7D7C4EE9B4E39DE336ED175368971F6939D04F901F1458C32021E1BC0CDF153
  deterministic=True
  ```
- [x] Performance smoke, reproducible:
  `dotnet run --project project/tools/pagination-smoke` →
  `rows=10000 first_p95_ms=2.00 cursor_p95_ms=4.33` and
  `plan=SEARCH Tasks USING INDEX IX_Tasks_UserId_CreatedAtUtc_Id (UserId=? AND CreatedAtUtc<?)`.
  (Repeated runs stayed below 10 ms; all are far below the 100 ms target.)
- [x] Live API E2E: `node docs/qa/e2e-api-check.js` twice on the same database →
  **`SUMMARY 40/40 checks passed`**, exit 0 both times; cleanup restored
  1 user / 5 tasks each time.
- [x] Leak-detector self-test: `node --test docs/qa/e2e-api-check.test.js` →
  **6 tests, 6 pass**.
- [x] Ownership: API test uses a cursor built from another user’s page and
  confirms no foreign task is returned; the infrastructure test adds a stranger
  task older than the cursor.
- [x] No auth, security-header, CORS, or unrelated route change.
- [x] No commit or push was performed.

## Corrections

| What was considered | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| `int? take` on the controller | ASP.NET Core described it as `integer \| string` with a pattern, so generated TypeScript got `number \| string` | Used `int take = TaskService.DefaultPageSize` and normalized the OpenAPI parameter to `integer` with `default: 10` | A page size is an integer; the generated client should not accept a string |
| `Guid.CompareTo` in the keyset predicate | It was unclear whether EF Core SQLite would translate the tie-breaker | Kept the LINQ predicate and added a same-timestamp test; it translated and the test passes | Prove the behavior instead of assuming a provider limitation |
| Test mocks returned arrays for `/api/tasks` | The list is now an envelope; `flatMap(page => page.items)` crashed on arrays | Updated TaskListPage/TaskFormPage/tasks tests to return `{items,nextCursor}` | Keep the tests on the real contract |
| Status counts/filter over all tasks | With pagination, the summary only sees loaded pages | Added “Counts and filters include loaded tasks” while `hasNextPage` | Honest UI without expanding the wave into server-side filtering |

## Edge cases considered

- Two tasks with the same `createdAtUtc`: `id DESC` breaks the tie; tested.
- Cursor from another user: the `UserId` filter still applies; tested.
- Malformed cursor, unsupported version, non-integer/out-of-range `take`: `400`
  problem+json with the field error; tested.
- Empty list: `{items:[],nextCursor:null}`; tested.
- Cursor `400` on the client: the list discards pages and refetches the first
  page once; a first-page `400` is an error, not a loop; tested.
- Page size 100 accepted, 101 rejected; default 10; tested.
- Existing CRUD/auth/security-header tests remain green.

## Auth and validation decisions

- No authentication, ownership, CORS, security-header, or persistence rule
  changed.
- The cursor is opaque but unsigned; it is not an authorization token. Ownership
  is always the `UserId` filter.
- `take`/cursor validation reuses `ValidationException` → problem+json.
- No CI workflow or commit was added.

## Files affected

- `project/src/CrudWorks.Application/Tasks/TaskPageCursor.cs`
- `project/src/CrudWorks.Application/Tasks/TaskPage.cs`
- `project/src/CrudWorks.Application/Tasks/ITaskRepository.cs`
- `project/src/CrudWorks.Application/Tasks/ITaskService.cs`
- `project/src/CrudWorks.Application/Tasks/TaskService.cs`
- `project/src/CrudWorks.Infrastructure/Persistence/TaskRepository.cs`
- `project/src/CrudWorks.Infrastructure/Persistence/Configurations/TaskConfiguration.cs`
- `project/src/CrudWorks.Infrastructure/Persistence/Migrations/20261125044557_AddTaskPaginationIndex.cs`
- `project/src/CrudWorks.Api/Pagination/TaskCursorCodec.cs`
- `project/src/CrudWorks.Api/Pagination/TaskPageResponse.cs`
- `project/src/CrudWorks.Api/Controllers/TasksController.cs`
- `project/src/CrudWorks.Api/OpenApi/ContractOpenApiTransformers.cs`
- `project/tests/CrudWorks.Application.Tests/Tasks/TaskServiceTests.cs`
- `project/tests/CrudWorks.Infrastructure.Tests/Persistence/TaskRepositoryTests.cs`
- `project/tests/CrudWorks.Api.Tests/Tasks/TasksApiTests.cs`
- `project/tests/CrudWorks.Api.Tests/Pipeline/DatabaseStartupTests.cs`
- `project/tests/CrudWorks.Api.Tests/Pipeline/OpenApiContractTests.cs`
- `project/frontend/src/lib/tasks.ts`
- `project/frontend/src/pages/TaskListPage.tsx`
- `project/frontend/src/index.css`
- `project/frontend/src/api/generated/openapi.d.ts`
- `project/frontend/src/pages/TaskListPage.test.tsx`
- `project/frontend/src/pages/TaskFormPage.test.tsx`
- `project/frontend/src/lib/tasks.test.ts`
- `docs/adr/0004-api-contract.md`
- `docs/adr/0006-pagination.md`
- `specs/008-pagination/{spec,plan,tasks}.md`
- `project/README.md`
- `docs/04-requirement-traceability.md`
- `specs/README.md`

## Independent review and fixes

The first read-only review returned **Block**. Findings and fixes:

| Severity | Finding | Fix |
| --- | --- | --- |
| Blocker | The E2E script parsed the old array; the 37/37 evidence was stale | Updated `e2e-api-check.js` to `{items,nextCursor}`; added A5–A7; 40/40 twice with baseline cleanup |
| Major | The 10k smoke was not reproducible | Added `project/tools/pagination-smoke` and recorded the command/output |
| Major | The plan claimed an independent review that had not happened | Unchecked the DoD item; T-008-08 stays `green` pending the follow-up; this section records the first review |
| Major | A cursor with a missing `i` was accepted as `Guid.Empty` | `[JsonRequired]`, nullable `Guid?`, reject null/empty; malformed payload tests |
| Minor | `take=1`/`100` and `errors.take` for `abc` were untested | Added the theory and the key assertion |
| Minor | The foreign-cursor test was tautological | Added a stranger task older than the cursor |
| Minor | The index test checked only the name | `PRAGMA index_info` asserts columns/order |
| Minor | Frontend first-page 400/loading untested; retry policy | Added tests, `shouldRetryQuery`, and the `isFetchNextPageError` branch |
| Minor | Stale root README/DoD/traceability/test plan | Updated counts, contract, index, and the SPEC-008 gate |
| Nit | ADR said “clamped” | “Validated; values outside return 400” |
| Nit | OpenAPI `take` lacked bounds | Added `minimum: 1`, `maximum: 100` |

## Follow-up review

The follow-up review returned **Approve with comments** (no blockers or majors).
The remaining minors/nits were then fixed: traceability R-26 and the remaining
`37` references, the DoD reviewer row, the E2E header comments, the gate label,
the smoke tool's temp-file cleanup, the infrastructure test name, and two
frontend tests (next-page failure retry and first-page failure after a cursor
reset).

## Final decision

**Implemented; approved with comments after the follow-up review.** Evidence:
216/216 backend tests, 104/104 frontend tests, 40/40 E2E twice, 6/6 self-test,
deterministic generated types, and a reproducible 10k smoke. Commit approval is
pending; no commit was made.
