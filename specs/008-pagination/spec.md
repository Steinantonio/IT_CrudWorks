# SPEC-008 — Task list pagination

- **Status:** implemented
- **Owner:** architect
- **Approved by:** project owner
- **Requirements:** R-05, R-09, R-16, R-17
- **ADRs:** 0004, 0006
- **Last updated:** 2026-11-25

## 1. Problem

`GET /api/tasks` returns every task owned by the caller as a bare array
(`TasksController.cs:14-18`), ordered only by `createdAtUtc`
(`TaskRepository.cs:9-16`). The demo dataset is small, but the API has no page
size cap, no stable tie-breaker, and no way to continue after the first page. ADR-0006
adopts keyset pagination so the portfolio can answer “what happens with 10,000
tasks?” without loading the whole list.

## 2. Scope

**In scope**

- `GET /api/tasks` keyset pagination with `take` and an opaque `cursor`.
- Envelope `{ items, nextCursor }`; `nextCursor` null on the last page.
- Default page size 10; server cap 100; `400` for invalid `take`/cursor.
- Composite index `(UserId, CreatedAtUtc, Id)` and its migration.
- OpenAPI metadata and regenerated TypeScript declarations.
- Frontend `useInfiniteQuery`, “Load more”, end-of-list, cursor-`400` reset, and
  an honest note that counts/filters cover loaded pages.
- Backend, contract, and frontend tests; a 10,000-row performance smoke.
- ADR-0004 §4 update and AI evidence.

**Out of scope**

- Offset or numbered pages; server-side sort, search, or status filtering.
- Pagination of any other endpoint.
- Signed/encrypted cursors or time-based expiry.
- Auth, ownership rules, task fields, validation messages, CORS, or security
  headers.
- Soft delete, undo, or bulk actions.
- CI/CD changes; CI remains deferred by ADR-0007.

## 3. Actors and glossary

| Term | Meaning |
| --- | --- |
| Page | Up to `take` tasks returned in one response. |
| Cursor | Opaque base64url token carrying the last row’s `(createdAtUtc, id)`. |
| Keyset | Query that seeks after a key instead of counting/skipping rows. |
| `nextCursor` | Token for the next page, or `null` when the list is exhausted. |

## 4. User stories

| ID | Story |
| --- | --- |
| US-008-01 | As a user with many tasks, I want the newest 10 first and a way to load more, so the page stays fast. |
| US-008-02 | As a user, I want the list to stay stable while I page, so no task is duplicated or skipped. |
| US-008-03 | As a maintainer, I want one contract and one index for pagination, so the API stays predictable. |

## 5. Acceptance criteria

### AC-008-01 — First page defaults to 10

- **Given** a user with 12 tasks
- **When** they call `GET /api/tasks` without query parameters
- **Then** the response is `200` with exactly 10 `items`, newest first, and a
  non-null `nextCursor`.

### AC-008-02 — The cursor returns the next page without duplicates or gaps

- **Given** the `nextCursor` from AC-008-01
- **When** the user calls `GET /api/tasks?cursor=<token>`
- **Then** the response contains the remaining 2 tasks, no task id appears twice
  across the two pages, and `nextCursor` is `null`.

### AC-008-03 — The last page is explicit

- **Given** a user with 10 or fewer tasks
- **When** they call `GET /api/tasks`
- **Then** `items` contains all of them and `nextCursor` is `null`.

### AC-008-04 — `take` is bounded

- **Given** any authenticated user
- **When** they call `GET /api/tasks?take=1` and `take=100`
- **Then** both return `200` with at most the requested number of items.
- **When** they call `take=0`, `take=101`, or `take=abc`
- **Then** the API returns `400 application/problem+json` with an `errors.take`
  entry and no task data.

### AC-008-05 — An invalid cursor is a client error

- **Given** any authenticated user
- **When** they call `GET /api/tasks?cursor=not-a-token`, a token with an
  unsupported `v`, or a token with a malformed id/date
- **Then** the API returns `400 application/problem+json` with an `errors.cursor`
  entry, never `500`, and no task data.

### AC-008-06 — Pagination never crosses ownership

- **Given** user A and user B each own tasks
- **When** A pages through `GET /api/tasks`, including with a cursor built from
  one of B’s tasks
- **Then** every returned task belongs to A; B’s titles and ids never appear.

### AC-008-07 — The empty list keeps the envelope

- **Given** a user with no tasks
- **When** they call `GET /api/tasks`
- **Then** the response is `200` with `{ "items": [], "nextCursor": null }`.

### AC-008-08 — OpenAPI and generated types describe the new contract

- **Given** the Development OpenAPI document
- **When** `GET /api/tasks` is inspected
- **Then** it documents `take` and `cursor` query parameters, the
  `TaskPageResponse` envelope (`items` array of `TaskResponse`, nullable
  `nextCursor`), and the `400` problem+json response.
- **And** `src/api/generated/openapi.d.ts` is regenerated deterministically and
  the frontend consumes `TaskPageResponse` from it.

### AC-008-09 — The frontend pages accessibly

- **Given** more than 10 tasks
- **When** the list renders
- **Then** it shows the first 10, a “Load more” button, and a live-region loading
  message while the next page is fetched.
- **When** the user activates “Load more”
- **Then** the next 10 append in order and the button disappears when
  `nextCursor` is null.
- **When** a cursor request returns `400`
- **Then** the client discards the loaded pages and refetches the first page once;
  a `400` on the first page is shown as an error.
- **And** while `hasNextPage`, the status summary/filter states that counts cover
  loaded tasks.

### AC-008-10 — The query is indexed and measured

- **Given** the migrated database
- **When** the schema is inspected
- **Then** `IX_Tasks_UserId_CreatedAtUtc_Id` exists on `(UserId, CreatedAtUtc, Id)`
  and `IX_Tasks_UserId` is gone.
- **And** a smoke run with 10,000 tasks records first-page and cursor-page latency
  against the ADR-0006 target (p95 ≤ 100 ms, local SQLite).

### AC-008-11 — Existing behavior is preserved

- **Given** the change set
- **When** the full suites run
- **Then** create/get/update/delete, auth, ownership, validation, security
  headers, and the nine operations remain unchanged except for the documented
  `GET /api/tasks` contract.

## 6. Rules and validation

| Field / rule | Constraint | Behavior on violation |
| --- | --- | --- |
| `take` | Integer, default 10, `1..100` | `400` with `errors.take` |
| `cursor` | Opaque base64url, version 1, valid UTC date and GUID | `400` with `errors.cursor` |
| Ordering | Fixed `createdAtUtc DESC, id DESC`; no sort parameter | Unknown query keys ignored by the binder |
| Envelope | `items` array + nullable `nextCursor` | Contract tests fail |
| Ownership | Always `WHERE UserId = @userId`; cursor is not an auth token | Foreign tasks never returned |
| Page size | Server cap 100 regardless of client input | `400` above 100 |
| Frontend reset | Cursor `400` → refetch first page once | A second failure is shown as an error |

## 7. API contract

ADR-0006 is the source of truth. Summary:

| Method | Route | Auth | Success | Errors |
| --- | --- | --- | --- | --- |
| GET | `/api/tasks?take=&cursor=` | Bearer | `200` `TaskPageResponse` | `400`, `401`, `500` |

All other task routes are unchanged.

## 8. Non-functional requirements

- **Performance:** keyset query uses the composite index; 10,000-row smoke with a
  recorded p95 target. No count query per page.
- **Security:** ownership is enforced server-side; the cursor is not signed and
  must never be treated as authorization. No task data leaks through errors.
- **Accessibility:** “Load more” is a real button, disabled while loading, with a
  live-region message; end-of-list is announced.
- **Observability:** no new logging or telemetry; existing error handling applies.
- **Compatibility:** breaking `GET /api/tasks` change handled in one wave; the
  Render deployment keeps working after the migration.

## 9. Dependencies and assumptions

- **Fact:** the current list returns a bare array and ADR-0004 §4 explicitly
  deferred pagination (`docs/adr/0004-api-contract.md:164-168,204-208`).
- **Fact:** the repository orders only by `CreatedAtUtc`
  (`TaskRepository.cs:9-16`); the only index is `IX_Tasks_UserId`
  (`TaskConfiguration.cs:24-26`).
- **Fact:** the frontend uses `useQuery`/`listTasks`
  (`TaskListPage.tsx:49-53`, `lib/tasks.ts:60-62`) and its tests return arrays
  (`TaskListPage.test.tsx:88-124`); API tests do the same
  (`TasksApiTests.cs:59-87`).
- **Fact:** the OpenAPI contract test currently expects `200,401,500` for
  `GET /api/tasks` (`OpenApiContractTests.cs:204`).
- **Assumption:** EF Core SQLite may not translate the `Guid` tie-breaker; a
  parameterized SQL fallback is acceptable and must be tested.
- **Assumption:** 10,000 tasks for one user is a sufficient demo scale target.
- **Decision:** the envelope is preferred over a custom header; the cursor is
  unsigned and versioned.

## 10. Open questions

| Question | Owner | Deadline | Resolution |
| --- | --- | --- | --- |
| Should the UI use only a “Load more” button, or also auto-load with `IntersectionObserver`? | Project owner | Resolved 2026-11-25 | **Resolved:** button is required; auto-load is optional and not part of this wave. |
| Should the status counts/filter stay visible with a “loaded tasks” note, or be removed until server-side filtering exists? | Project owner | Resolved 2026-11-25 | **Resolved:** keep them with the note while `hasNextPage`; server-side filtering is a future ADR. |
| Is the 10,000 tasks/user and p95 ≤ 100 ms target right for the demo? | Project owner | Resolved 2026-11-25 | **Resolved:** yes; reproducible tool measured 2.00 ms first page and 4.33 ms cursor page (p95, local SQLite; repeated runs stay well below 10 ms). |
| Is the breaking envelope acceptable, or should the next cursor travel in a header? | Project owner | Resolved 2026-11-25 | **Resolved:** envelope; it is typed, visible in OpenAPI, and hard to drop. |
| Should cursors expire by time, or only by version? | Project owner | Resolved 2026-11-25 | **Resolved:** version only; no server state to expire, and old keys remain meaningful. |

## 11. Change log

| Date | Change | Reason | Approved by |
| --- | --- | --- | --- |
| 2026-11-25 | Initial draft for keyset pagination: envelope, `take`/`cursor`, composite index, frontend infinite query. | Prepare ADR-0006 and gaps G1–G4 for separate spec review; no implementation is authorized by this draft. | — |
| 2026-11-25 | Approved and implemented; first review fixes applied; follow-up review approved with comments. | ADR-0006 accepted; 216/216 backend tests, 104/104 frontend tests; E2E 40/40 twice with baseline cleanup; generated types deterministic (`B7D7C4EE9B4E39DE336ED175368971F6939D04F901F1458C32021E1BC0CDF153`); reproducible 10,000-row smoke p95 2.00/4.33 ms and index plan confirmed. | Project owner + independent review |
