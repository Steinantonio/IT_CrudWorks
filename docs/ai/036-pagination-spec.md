# AI Evidence Log

## Entry 036

- **Date:** 2026-11-25
- **Agent:** orchestrator (GPT-6 Luna)
- **Task:** Draft ADR-0006, SPEC-008, PLAN-008, and TASKS-008 for Wave 5
  (task list pagination, gaps G1–G4).
- **Tool/model:** GPT-6 Luna (OpenCode).

## Prompt

Faithful summary: the owner chose pagination as the next wave after the security
headers. Prepare the ADR and the spec/plan/tasks first, keeping them in draft; no
implementation until the ADR is accepted and the spec/tasks are approved. The
approved gap decisions are: cursor default, 10 per page, server cap 100,
client-side sort only, opaque versioned cursor, invalid/old → `400` with a client
refetch from the start.

## Output summary

- Created `docs/adr/0006-pagination.md` (proposed): keyset pagination ordered by
  `(createdAtUtc DESC, id DESC)`, `take` default 10 / cap 100, opaque base64url
  cursor `{v,c,i}`, `{items,nextCursor}` envelope, `400` for invalid input,
  composite index, scale target, alternatives, consequences, validation plan.
- Created `specs/008-pagination/spec.md` with testable criteria for the first
  page, next page, last page, `take` bounds, invalid cursor, ownership, empty
  envelope, OpenAPI/generated types, frontend paging, index/performance, and
  regressions.
- Created `PLAN-008` and `TASKS-008` with the layer-by-layer implementation shape,
  test strategy, risks, ordered tasks, and independent review.
- Added SPEC-008 to the spec inventory and Wave 5 to the Track 5 table as draft.
- No code, package, migration, test, or CI file was changed.

## Validation

- [ ] Build: not run — documentation-only task.
- [ ] Tests: not run — documentation-only task.
- [x] Source review: current list returns a bare array
  (`TasksController.cs:14-18`), orders only by `CreatedAtUtc`
  (`TaskRepository.cs:9-16`), indexes only `UserId`
  (`TaskConfiguration.cs:24-26`); the frontend uses `useQuery`/`listTasks`
  (`TaskListPage.tsx:49-53`, `lib/tasks.ts:60-62`); the contract test expects
  `200,401,500` (`OpenApiContractTests.cs:204`).
- [x] Scope review: Wave 5 changes only `GET /api/tasks`, its index/migration,
  OpenAPI/generated types, and the list page. Auth, ownership, security headers,
  other routes, and CI are untouched.

## Corrections

| What was considered | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| Keep the bare array and add `X-Next-Cursor` | The cursor would be invisible to generated types and easy to drop by proxies; the client would need a second channel | Chose `{ items, nextCursor }` | One typed contract; OpenAPI and TypeScript see the same shape |
| Sign the cursor | It would add key management and verification cost for no security gain: the cursor only encodes the caller’s own sort key | Kept it opaque but unsigned; ownership stays the `UserId` filter | Simpler and still safe; a forged cursor cannot cross users |
| Use `OFFSET` for page numbers | Degrades on large offsets and duplicates/skips under concurrent inserts; no UI demand | Keyset with `(createdAtUtc, id)` | Stable pages and an index range scan |
| Assume EF Core translates the GUID tie-breaker | SQLite may not translate `Guid` comparison in LINQ | Added a parameterized SQL fallback to the plan and a required test | The contract is the behavior; the provider decides the query shape |
| Keep counts/filter silently over loaded pages | With pagination, the summary would look complete while only part of the list is loaded | Require a “loaded tasks” note while `hasNextPage`; server-side filtering is a future ADR | Honest UI without expanding this wave |

## Edge cases considered

- Two tasks with the same `createdAtUtc`: the `id DESC` tie-breaker keeps pages
  stable; the query must be tested for translation.
- A cursor row deleted before the next page: the page starts after the key; no
  duplicate or crash.
- A cursor from another user’s task: the `UserId` filter still returns only the
  caller’s tasks.
- Empty list: `{ items: [], nextCursor: null }`, never `404`.
- Invalid cursor/`take`: `400` problem+json with `cursor`/`take` errors, never
  `500`.
- Cursor `400` on the client: discard pages and refetch the first page once; a
  first-page `400` is an error, not a retry loop.
- Page size 100: accepted; 101: `400`; omitted: 10.
- No server-side sort/filter: client-side behavior is documented, not implied.

## Auth and validation decisions

- No auth, ownership, security-header, or route change.
- The cursor is not an authorization token; the repository always filters by the
  token’s `sub`.
- `take` and cursor validation live in the Application/API boundary and reuse the
  existing `ValidationException` → problem+json mapping.
- No CI workflow or commit was added.

## Files affected

- `docs/adr/0006-pagination.md`
- `specs/008-pagination/spec.md`
- `specs/008-pagination/plan.md`
- `specs/008-pagination/tasks.md`
- `specs/README.md`
- `docs/improvement-plan.md`
- `docs/ai/036-pagination-spec.md`

## Final decision

**Draft ready for owner review.** ADR-0006 is `proposed`; SPEC-008 is `draft`.
Implementation is not authorized until the owner accepts the ADR, approves the
spec/tasks, and resolves the five open questions (Load more vs auto-load,
counts/filter note, scale target, envelope vs header, cursor expiry).
