# ADR-0004: API Contract

- **Status:** accepted
- **Date:** 2026-11-11
- **Decision owner:** architect
- **Related requirements:** R-05, R-09, R-10, R-16, R-17
- **Related specs:** `specs/001-auth/spec.md`, `specs/002-task-crud/spec.md`,
  `specs/003-web-client/spec.md`

## Context

The backend and the frontend are developed in parallel. Without a written
contract, the client invents fields, the controller returns inconsistent status
codes, and the review has nothing to check against. The contract must be small
enough to implement by Friday and precise enough to test.

## Decision

Use JSON over HTTP under `/api`, `application/json`, problem details for errors,
strings for enums and dates, and JWT bearer authentication. This document is the
single source of truth for routes, payloads, and status codes.

## 1. Conventions

| Topic | Rule |
| --- | --- |
| Base path | `/api` |
| Content type | `application/json; charset=utf-8` |
| Authentication | `Authorization: Bearer <token>` |
| IDs | UUID v4 as strings |
| Timestamps | ISO 8601 UTC with `Z` (`2026-11-11T18:30:00Z`) |
| Dates | `yyyy-MM-dd` (`2026-11-14`) |
| Enums | Strings: `Todo`, `InProgress`, `Done` |
| Casing | `camelCase` |
| Unknown fields | Ignored by the JSON binder — including a client-supplied `userId`; the owner is always derived from the token's `sub` claim, never from the body |
| Nulls | Serialized as `null`, never omitted for task fields |

### Errors

All errors use RFC 7807 `application/problem+json`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "title": ["Title must be between 3 and 200 characters."]
  },
  "traceId": "00-..."
}
```

- `errors` is present only for validation failures.
- Messages are safe to display; no stack traces, SQL, or internal ids.
- `401` uses the title `Invalid email or password` on login, and
  `Authentication required` elsewhere.

| Status | Meaning in this API |
| --- | --- |
| `200` | Read or update succeeded |
| `201` | Resource created; `Location` points to it |
| `204` | Deleted; no body |
| `400` | Validation failed; `errors` maps field → messages |
| `401` | Missing/invalid token, or invalid credentials |
| `404` | Resource does not exist **or is not owned by the caller** |
| `409` | Email already registered |
| `500` | Unexpected failure; generic problem details only |

No `403` is used for task ownership. The API must not confirm that another user's
task exists.

## 2. Health

### `GET /api/health`

- Auth: anonymous.
- `200`:

```json
{ "status": "Healthy", "totalDurationMs": 1.42 }
```

## 3. Authentication

### `POST /api/auth/register`

- Auth: anonymous.
- Request:

```json
{
  "email": "demo@crudworks.local",
  "displayName": "Demo User",
  "password": "Demo123!"
}
```

- `201 Created`, `Location: /api/auth/me`:

```json
{
  "id": "0b7f0f8e-6c2f-4a6f-9f2a-1f5a1e3c9d10",
  "email": "demo@crudworks.local",
  "displayName": "Demo User",
  "createdAtUtc": "2026-11-11T18:30:00Z"
}
```

- `400` validation; `409` email already registered.

### `POST /api/auth/login`

- Auth: anonymous.
- Request:

```json
{ "email": "demo@crudworks.local", "password": "Demo123!" }
```

- `200`:

```json
{
  "accessToken": "<jwt>",
  "expiresAtUtc": "2026-11-11T19:30:00Z",
  "user": {
    "id": "0b7f0f8e-6c2f-4a6f-9f2a-1f5a1e3c9d10",
    "email": "demo@crudworks.local",
    "displayName": "Demo User"
  }
}
```

- `400` malformed body; `401` invalid credentials with a generic message.

### `GET /api/auth/me`

- Auth: bearer.
- `200` user summary (same shape as the register response).
- `401` missing or invalid token.

## 4. Tasks

Every route requires a valid bearer token. The owner is always the `sub` claim.
No request body may contain `userId`; a client-supplied `userId` is ignored
(no request model binds it), never trusted. Status names are parsed
case-sensitively: `"todo"` is not a valid `status` and returns `400`.

### Task resource

```json
{
  "id": "5a0f6a6e-1a3f-4d6f-9c2e-8b7d4f1a2c33",
  "title": "Write the proposal",
  "description": "Draft and review the proposal.",
  "status": "InProgress",
  "dueDate": "2026-11-14",
  "createdAtUtc": "2026-11-11T18:30:00Z",
  "updatedAtUtc": "2026-11-11T18:45:00Z"
}
```

### `GET /api/tasks`

- `200` array of the caller's tasks, ordered by `createdAtUtc` descending.
- Empty list → `200 []`, never `404`.
- No pagination or filtering in this timebox.

### `GET /api/tasks/{id}`

- `200` the task; `404` if missing or owned by another user.

### `POST /api/tasks`

- Request:

```json
{
  "title": "Write the proposal",
  "description": "Draft and review the proposal.",
  "status": "Todo",
  "dueDate": "2026-11-14"
}
```

- `title` required; `description`, `status`, and `dueDate` optional.
- Missing `status` defaults to `Todo`.
- `201 Created`, `Location: /api/tasks/{id}`, body = task resource.
- `400` validation; `401` no token.

### `PUT /api/tasks/{id}`

- Full replacement of the editable fields.
- Request: same shape as create, but `status` is required.
- `null` description or `dueDate` clears the value.
- `200` updated task; `400` validation; `401`; `404` missing or not owned.
- The owner and `createdAtUtc` never change; `updatedAtUtc` is refreshed.

### `DELETE /api/tasks/{id}`

- `204` no body; `401`; `404` missing or not owned.

### Non-goals

- No pagination, filtering, or sorting parameters: the demo dataset is small and
  `GET /api/tasks` returns every task owned by the caller, newest first. Adding
  paging is a contract change and would need a new ADR.

## 5. Validation matrix

| Field | Rule | Message |
| --- | --- | --- |
| `email` | Required, valid, max 256 | `Email must be a valid address.` |
| `displayName` | Required, 2–100 after trim | `Display name must be between 2 and 100 characters.` |
| `password` | 8–128, at least one letter and one digit | `Password must be 8–128 characters and contain a letter and a digit.` |
| `title` | Required, 3–200 after trim | `Title must be between 3 and 200 characters.` |
| `description` | Max 2000 after trim | `Description must be at most 2000 characters.` |
| `status` | One of the three names; required on update | `Status must be Todo, InProgress, or Done.` |
| `dueDate` | Valid ISO date or null | `Due date must be a valid date.` |
| `userId` | Ignored (no request model binds it); the owner always comes from the `sub` claim, never from the body | — |

The frontend mirrors these rules but the API is authoritative.

## 6. CORS and development

- Allowed origin: `http://localhost:5173` (`Cors:AllowedOrigin`).
- Methods: `GET`, `POST`, `PUT`, `DELETE`, `OPTIONS`.
- Headers: `Content-Type`, `Authorization`.
- Credentials are not allowed; the token travels in the header.

## 7. Test coverage required by this contract

- Register `201`, duplicate `409`, invalid `400`.
- Login `200`, wrong password `401`, unknown email `401` (same body).
- `me` `200` with token, `401` without.
- Task create `201` + `Location`; list `200`; get `200`; update `200`; delete
  `204`; get-after-delete `404`.
- No token → `401` on every task route.
- IDOR → `404` on read, update, and delete.
- Invalid payload → `400` with the field in `errors`.
- List ordering is descending by `createdAtUtc`.

## 8. Alternatives considered

| Option | Why not |
| --- | --- |
| PATCH with partial merge | More states to test; PUT is simpler and the client always sends the full form |
| `userId` in the task body | Invites IDOR and contradicts the ownership model |
| Integer enums | Unreadable data and brittle migrations |
| Offset pagination now | Not needed for the demo; adds contract surface and tests |
| GraphQL | Overkill for five routes and two days |

## 9. Consequences

- The frontend can be built against this document before the API is complete.
- Controllers stay thin; the contract is testable route by route.
- Changing a field or status code requires a spec change log entry and a contract
  update, not a silent client change.

## Links

- `specs/001-auth/spec.md`, `specs/002-task-crud/spec.md`, `specs/003-web-client/spec.md`
- `docs/adr/0002-auth-strategy.md`
- `docs/adr/0003-persistence-and-test-strategy.md`
- `docs/04-requirement-traceability.md`

## Change log

| Date | Change | Why |
| --- | --- | --- |
| 2026-11-12 | Clarified §1/§4/§5: a client-supplied `userId` is **ignored** (request DTOs contain no `userId` property), not rejected with `400`; the owner is always the token's `sub` claim. Added: status names are parsed case-sensitively (`"todo"` → `400`). | Ownership is a server invariant; returning `400` on the extra field would only confirm the field is read, contradicting the ownership model. Matches the implemented contract (see `docs/ai/007-task-backend-slice.md`). |
