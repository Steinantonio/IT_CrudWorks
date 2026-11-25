# PLAN-008 — Task list pagination implementation plan

- **Spec:** `specs/008-pagination/spec.md`
- **Status:** implemented
- **Owner:** architect
- **Last updated:** 2026-11-25

## 1. Architecture impact

| Layer / area | Change |
| --- | --- |
| Domain | No change; `TaskItem` keeps its fields. |
| Application | New `TaskPageCursor` and `TaskPage`; `ITaskService.ListAsync` takes a cursor and an `int take`; `ITaskRepository` replaces `ListByOwnerAsync` with `ListPageAsync`; page-size validation stays in Application. |
| Infrastructure | Keyset query in `TaskRepository`; composite index in `TaskConfiguration`; migration `AddTaskPaginationIndex`. |
| Api | `GET /api/tasks` binds `take`/`cursor`, decodes the opaque cursor, returns the envelope, documents `400`; a small cursor codec lives in `CrudWorks.Api` (HTTP contract concern). |
| OpenAPI | Query parameters and `TaskPageResponse` appear from the controller signature/`ProducesResponseType`; contract tests pin them. |
| Frontend | `listTasksPage` returns `TaskPageResponse`; `TaskListPage` moves to `useInfiniteQuery`; “Load more”, end-of-list, cursor-`400` reset, counts note; tests updated. |
| Generated types | Regenerate `src/api/generated/openapi.d.ts` from the Development document. |
| CI / deployment | No change; CI remains deferred. The migration runs on startup in Development and through the existing deployment flag. |

## 2. Data model and migration

No new columns. Replace the single-column index with a composite:

- `IX_Tasks_UserId_CreatedAtUtc_Id` on `(UserId, CreatedAtUtc, Id)`.
- Migration `AddTaskPaginationIndex`: drop `IX_Tasks_UserId`, create the composite.
- The composite covers the `UserId` filter and the `(CreatedAtUtc DESC, Id DESC)`
  order, so the page is an index range scan plus a small sort/tie-break.

## 3. Contract

ADR-0006 is the source of truth. The only route that changes is
`GET /api/tasks`; it returns `{ items, nextCursor }` and accepts `take` and
`cursor`. All other task routes keep their current request/response shapes.

## 4. Implementation shape

### Application

- `TaskPageCursor(DateTime CreatedAtUtc, Guid Id)`.
- `TaskPage(IReadOnlyList<TaskResponse> Items, TaskPageCursor? NextCursor)`.
- `ITaskRepository.ListPageAsync(Guid userId, TaskPageCursor? cursor, int take, CancellationToken)`
  returns at most `take + 1` tasks.
- `TaskService.ListAsync(Guid userId, TaskPageCursor? cursor, int take, CancellationToken)`:
  validates `take` (`1..100`), fetches `take + 1`, trims to `take`, and builds
  `nextCursor` from the last returned row. The default `10` is the controller
  parameter default (`TaskService.DefaultPageSize`).
- Validation errors use the existing `ValidationException` with keys `take` or
  `cursor`; the API maps them to problem+json through the existing handler.

### Api

- `TaskPageResponse(IReadOnlyList<TaskResponse> Items, string? NextCursor)` lives
  in `CrudWorks.Api.Pagination`; the service returns `TaskPage` and the controller
  encodes the cursor into the response.
- `TaskCursorCodec` encodes/decodes `{ v, c, i }` as base64url JSON.
- Decoding rejects unsupported versions, malformed/missing dates, and
  malformed/missing/empty GUIDs with a `cursor` validation error.
- Controller signature:

```csharp
[HttpGet]
[ProducesResponseType<TaskPageResponse>(StatusCodes.Status200OK)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
public async Task<ActionResult<TaskPageResponse>> List(
    [FromQuery] string? cursor,
    CancellationToken cancellationToken,
    [FromQuery] int take = TaskService.DefaultPageSize)
```

- The OpenAPI operation transformer normalizes `take` to `integer` with
  `default: 10` (the framework otherwise describes it as `integer | string`).

- `ProducesResponseType` keeps `401`/`500` coverage from the existing contract
  transformer.

### Infrastructure

- LINQ keyset predicate with `OrderByDescending(CreatedAtUtc).ThenByDescending(Id)`.
- If EF Core SQLite cannot translate the GUID tie-breaker, use a parameterized
  `FromSqlInterpolated` with the same predicate and order; cover it with the same
  integration test.
- `AsNoTracking()` remains.

### Frontend

- `listTasksPage(token, { cursor?, take? })` builds the query string and returns
  `TaskPageResponse` from generated types.
- `useInfiniteQuery` with `initialPageParam: undefined`,
  `getNextPageParam: (last) => last.nextCursor ?? undefined`.
- `TaskListPage` flattens `data.pages` for rows, counts, and the client filter.
- “Load more” button while `hasNextPage`; disabled while `isFetchingNextPage`;
  live-region loading text; end-of-list message.
- A generation counter in the query key resets the infinite query when a cursor
  request returns `400`; the reset happens once, and a first-page `400` is an
  error.
- Counts/filter note while `hasNextPage`.

## 5. Test strategy

| Layer | What is proven | Where |
| --- | --- | --- |
| Application | `take` bounds/default, trimming, `nextCursor`, empty page, invalid cursor mapping | `CrudWorks.Application.Tests/Tasks` with a fake repository |
| Infrastructure | Keyset query over 12+ rows, stable order, no duplicates/gaps, composite index | `CrudWorks.Infrastructure.Tests/Persistence` against SQLite |
| API | Default 10, next page, cap 100, `400` for `take`/cursor, ownership, empty envelope | `CrudWorks.Api.Tests/Tasks` |
| OpenAPI | Query params, `TaskPageResponse`, `400` documented, generated types deterministic | `OpenApiContractTests` + regeneration script |
| Frontend | First page, Load more, end-of-list, loading/error, cursor-`400` reset, counts note | `TaskListPage.test.tsx` with the fetch mock |
| Performance | 10,000-row smoke, first and cursor page latency | Local script, evidence in `docs/ai/` |

## 6. Task order

The executable list is in `tasks.md`; implementation is gated on ADR-0006 and
SPEC-008 approval.

1. Accept ADR-0006 and SPEC-008; resolve the five open questions.
2. Backend application types, service, and repository interface (tests first).
3. Infrastructure query, index, and migration (tests first).
4. API cursor codec, controller, and `400` mapping (tests first).
5. OpenAPI contract tests and regenerated types.
6. Frontend infinite query and UI states (tests first).
7. Full verification, performance smoke, docs, and AI evidence.
8. Independent review.

## 7. Risks and fallbacks

| Risk | Impact | Fallback |
| --- | --- | --- |
| EF Core SQLite cannot translate the GUID tie-breaker. | Query fails at runtime or returns unstable pages. | Parameterized `FromSqlInterpolated` with the same predicate/order; test the exact-tie case. |
| Breaking envelope breaks the frontend or generated types. | List page fails to render. | Update API, OpenAPI, generated types, and frontend in the same wave; contract tests gate the shape. |
| Cursor `400` reset loops. | Repeated requests or a stuck page. | Reset only when the failing page carried a cursor; a first-page `400` is an error. |
| Counts/filter look wrong when only part of the list is loaded. | Misleading UI. | Show the “loaded tasks” note while `hasNextPage`; server-side filtering is a future ADR. |
| Migration drops the old index on an existing database. | Startup migration failure or slow list. | Test the migration on a copy of the demo database; the composite covers the old `UserId` prefix. |
| Performance target missed on a large seed. | Weak scale story. | Record the actual p95 and the query plan; the target is a demo goal, not a release gate. |
| Test volume grows across API/contract/frontend. | Longer verification. | Keep one focused test per criterion; reuse the existing factories and fetch mock. |

## 8. Definition of done

- [x] ADR-0006 and SPEC-008 are approved before implementation.
- [x] Every acceptance criterion has a test or a named manual check.
- [x] The envelope, `take`, cursor, and `400` paths match ADR-0006 exactly.
- [x] The composite index and migration are present and verified.
- [x] OpenAPI documents the new contract; generated types are deterministic.
- [x] The frontend pages with a keyboard-accessible “Load more”, end-of-list,
      cursor-`400` reset, and the loaded-tasks note.
- [x] Backend, frontend, lint, and build pass with captured output.
- [x] The 10,000-row smoke result is recorded (reproducible tool: p95 2.00/4.33 ms; index plan confirmed).
- [x] No auth, ownership, security-header, or unrelated route change is present.
- [x] AI evidence entry exists and an independent review has no blocker. (First review found one blocker and several majors; fixes applied. Follow-up review: Approve with comments, no blockers/majors; remaining nits fixed.)
