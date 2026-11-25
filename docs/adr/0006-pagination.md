# ADR-0006: Task list pagination

- **Status:** accepted
- **Date:** 2026-11-25
- **Decision owner:** architect
- **Related requirements:** R-05, R-09, R-16, R-17
- **Related specs:** `specs/002-task-crud/spec.md`, `specs/008-pagination/spec.md`
- **Supersedes:** ADR-0004 §4 “No pagination” and the §4 non-goal that deferred it

## Context

ADR-0004 deliberately returned every task owned by the caller, newest first, and
stated that adding paging would need a new ADR. The approved gap decisions G1–G4
close that gap: cursor is the default, the page size is 10, the server caps a
request at 100, and sorting stays client-side. The demo dataset is small, but the
portfolio needs a defensible answer to “what happens with 10,000 tasks?”.

Today `GET /api/tasks` returns a bare array (`TasksController.cs:14-18`), the
repository orders only by `CreatedAtUtc` (`TaskRepository.cs:9-16`), and the only
index is `IX_Tasks_UserId` (`TaskConfiguration.cs:24-26`). There is no stable
tie-breaker, no cap, and no way to fetch the next page without loading everything.

## Decision

Use **keyset (cursor) pagination** on `GET /api/tasks`:

- ordering is fixed: `createdAtUtc DESC, id DESC`;
- `take` defaults to 10 and is validated to 1–100 (values outside return `400`);
- the cursor is an opaque, versioned, unsigned token carrying the last row’s
  `(createdAtUtc, id)`;
- the response is an envelope `{ "items": [...], "nextCursor": "..." | null }`;
- an invalid cursor or `take` returns `400 application/problem+json`;
- a composite index `(UserId, CreatedAtUtc, Id)` supports the query;
- the frontend uses `useInfiniteQuery` with a “Load more” control.

No offset/page-number mode is implemented. No server-side sort or status filter is
added in this wave. The cursor is not an authorization token; ownership is always
enforced by the `UserId` filter.

## 1. Contract

### `GET /api/tasks`

- Auth: bearer; the owner is the token’s `sub` claim.
- Query:
  - `take` (integer, optional): page size, default `10`, valid `1..100`.
  - `cursor` (string, optional): opaque continuation token from a previous
    response.
- `200`:

```json
{
  "items": [
    {
      "id": "5a0f6a6e-1a3f-4d6f-9c2e-8b7d4f1a2c33",
      "title": "Write the proposal",
      "description": "Draft and review the proposal.",
      "status": "InProgress",
      "dueDate": "2026-11-14",
      "createdAtUtc": "2026-11-11T18:30:00Z",
      "updatedAtUtc": "2026-11-11T18:45:00Z"
    }
  ],
  "nextCursor": "eyJ2IjoxLCJjIjoiMjAyNi0xMS0xMVQxODozMDowMFoiLCJpIjoiNWEwZi4uLiJ9"
}
```

- `nextCursor` is `null` on the last page.
- Empty list → `{ "items": [], "nextCursor": null }`, never `404`.
- `400` for an invalid `take` (`errors.take`) or cursor (`errors.cursor`).
- `401` without a valid token; `500` unexpected failure.
- All other task routes keep their current contract.

The response shape is a **breaking change** from the bare array; the frontend,
generated types, and contract tests change in the same wave.

## 2. Ordering and cursor

- Sort key: `(CreatedAtUtc DESC, Id DESC)`. The id breaks ties so two tasks created
  in the same tick cannot swap between pages.
- Cursor payload (JSON, then base64url without padding):

```json
{ "v": 1, "c": "2026-11-11T18:30:00.0000000Z", "i": "5a0f6a6e-1a3f-4d6f-9c2e-8b7d4f1a2c33" }
```

- `v` is the cursor format version; an unsupported version is `400`.
- The cursor is **not signed or encrypted**. It encodes only the caller’s own sort
  key; a forged cursor can move the caller within their own list, never into
  another user’s data. Ownership is a server-side `WHERE UserId = @userId`.
- No time-based expiry: the cursor stays valid while its key is meaningful. A
  deleted row simply means the next page starts after that key. A future format
  change bumps `v` and invalidates old tokens with `400`.
- Keyset predicate:

```sql
WHERE UserId = @userId
  AND (CreatedAtUtc < @c OR (CreatedAtUtc = @c AND Id < @i))
ORDER BY CreatedAtUtc DESC, Id DESC
LIMIT @take + 1
```

- Fetch `take + 1` rows to decide `nextCursor` without a count query; the service
  trims to `take` and encodes the last returned row.

## 3. Index and persistence

- Replace `IX_Tasks_UserId` with `IX_Tasks_UserId_CreatedAtUtc_Id` on
  `(UserId, CreatedAtUtc, Id)`. The composite covers the filter and both sort
  columns.
- Migration `AddTaskPaginationIndex`: drop the old index, create the composite.
- `ITaskRepository.ListPageAsync(userId, cursor, take)` returns at most `take + 1`
  rows; the service owns trimming, `nextCursor`, and `take` validation.
- Provider note: EF Core SQLite may not translate a `Guid` tie-breaker. The
  implementation must prove the predicate with a test; if LINQ cannot translate
  it, use a parameterized `FromSqlInterpolated` with the same predicate and order.
  The behavior is the contract, not the LINQ shape.

## 4. Client behavior

- `useInfiniteQuery` keyed by user id; `getNextPageParam` returns
  `lastPage.nextCursor ?? undefined`.
- A “Load more” button appears while `hasNextPage`, is disabled while
  `isFetchingNextPage`, and announces loading through a live region. Automatic
  loading with `IntersectionObserver` is optional; the button remains the
  accessible control.
- On `400` for a request that carried a cursor, the client discards the loaded
  pages and refetches from the first page once. A `400` on the first page is shown
  as an error, not retried in a loop.
- Status counts and filters are computed over loaded pages. While `hasNextPage`,
  the UI states that counts/filters include loaded tasks. Server-side filtering is
  a separate ADR.
- Create, update, and delete invalidate the infinite query (all pages).

## 5. Scale target

Target for the demo: **10,000 tasks for one user, 100,000 total**, with **p95 ≤
100 ms** for a 10-row page on a local SQLite file. A reproducible tool seeds
10,000 rows and measures the first and a cursor page:

```powershell
dotnet run --project project/tools/pagination-smoke
```

Reference run (2026-11-25): p95 **2.00 ms** first page / **4.33 ms** cursor page
(repeated runs stay well below 10 ms);
`plan=SEARCH Tasks USING INDEX IX_Tasks_UserId_CreatedAtUtc_Id (UserId=? AND CreatedAtUtc<?)`.
This is a portfolio target, not a production SLA: SQLite and a single instance
are the deployment reality. The measurement is recorded, not enforced in CI (CI
is deferred).

## 6. Alternatives considered

| Option | Why not |
| --- | --- |
| Offset/page numbers | `OFFSET` degrades on large offsets and duplicates/skips under concurrent inserts; the UI does not need numbered pages (G1). |
| Server-side title sort | G2 keeps sorting client-side; a title cursor would need another index and contract. |
| Signed/encrypted cursor | No security gain: the cursor only encodes the caller’s own key, and ownership is enforced by the `UserId` filter. |
| Time-based cursor expiry | No server state to expire; versioning covers format changes. |
| `X-Next-Cursor` header | Invisible to generated types and easy to drop by proxies; the envelope is explicit and typed. |
| Return all rows and page in the client | Defeats the purpose; memory and latency grow with the dataset. |
| Server-side status filter now | New contract, index, and tests; not required by G1–G4. |

## 7. Consequences

- `GET /api/tasks` changes from an array to an envelope; this is a breaking
  contract change handled inside one wave (API, OpenAPI, generated types,
  frontend, tests).
- ADR-0004 §4 and its non-goals are updated; its change log records the
  supersession.
- The composite index replaces the single-column index; a migration is required.
- The cursor is opaque but not secret; documentation must not present it as an
  authorization token.
- Status counts and filters are honest but limited to loaded pages until a
  server-side filter ADR exists.
- No CI change; the Render deployment continues as-is.

## 8. Validation plan

| Layer | What is proven |
| --- | --- |
| Application | `take` bounds, trimming, `nextCursor`, empty page, invalid cursor. |
| Infrastructure | Keyset query over 12+ rows, stable order, no duplicates/gaps, composite index present. |
| API | Default 10, next page, cap 100, `400` for invalid `take`/cursor, ownership, empty envelope. |
| OpenAPI | Query parameters, envelope schema, `400` documented; generated TypeScript regenerated. |
| Frontend | First page, Load more, end-of-list, loading/error, cursor-`400` reset, counts note. |
| Performance | 10,000-row smoke with recorded first/cursor page latency. |

## 9. Change log

| Date | Change | Why |
| --- | --- | --- |
| 2026-11-25 | Proposed; supersedes ADR-0004 §4 “No pagination” and the §4 non-goal. | Approved gap decisions G1–G4; the portfolio needs a defensible scale story. |
| 2026-11-25 | Accepted and implemented in SPEC-008. | Owner approved the envelope, 10/page, cap 100, unsigned versioned cursor, and the composite index. |

## Links

- `docs/adr/0004-api-contract.md` §4
- `docs/adr/0003-persistence-and-test-strategy.md`
- `docs/improvement-plan.md` gaps G1–G4
- `specs/008-pagination/spec.md`
