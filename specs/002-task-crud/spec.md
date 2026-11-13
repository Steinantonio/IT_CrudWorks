# SPEC-002 — Task CRUD and ownership

- **Status:** implemented
- **Owner:** architect
- **Approved by:** orchestrator
- **Requirements:** R-05, R-07, R-08, R-09, R-11, R-12
- **ADRs:** 0001, 0003, 0004
- **Last updated:** 2026-11-11

## 1. Problem

The core product is a task list. A user must be able to create, read, update, and
delete their own tasks, and must never see or change another user's task. The rule
has to hold at the API, not only in the UI.

## 2. Scope

**In scope**

- `TaskItem` aggregate with title, description, status, due date, timestamps, owner.
- CRUD endpoints under `/api/tasks`.
- Ownership scoping in the Application layer.
- Validation and safe error responses.
- EF Core repository and SQLite persistence.

**Out of scope**

- Sharing, assigning, or transferring tasks.
- Pagination, search, filtering, or bulk operations.
- Subtasks, comments, attachments, reminders, recurrence.
- Soft delete and audit history.
- Reordering or drag-and-drop.

## 3. Actors

| Actor | Goal |
| --- | --- |
| Authenticated user | Manage only their own tasks. |
| Anonymous caller | Receive `401` on every task route. |

## 4. User stories

| ID | Story |
| --- | --- |
| US-002-01 | As a user, I want to create a task with a title, description, status, and due date. |
| US-002-02 | As a user, I want to list my tasks, newest first, so that recent work is visible. |
| US-002-03 | As a user, I want to open and edit one of my tasks, so that it stays accurate. |
| US-002-04 | As a user, I want to delete a task I no longer need. |
| US-002-05 | As a user, I want other users' tasks to be invisible, so that my data stays private. |

## 5. Acceptance criteria

### AC-002-01 — Create returns a task and a location

- **Given** an authenticated user
- **When** they post a valid task
- **Then** the API returns `201`, a `Location` header for `/api/tasks/{id}`, and
  the created task with server-generated `id` and timestamps
- **And** the task is stored with the caller's user id, never a client-supplied one.

### AC-002-02 — List returns only the caller's tasks

- **Given** user A owns two tasks and user B owns one
- **When** A calls `GET /api/tasks`
- **Then** the response is `200` with exactly A's two tasks
- **And** the tasks are ordered by `createdAtUtc` descending.

### AC-002-03 — Read is ownership-scoped

- **Given** user A owns task T and user B is authenticated
- **When** B calls `GET /api/tasks/{T}`
- **Then** the API returns `404`
- **And** no task data appears in the body.

### AC-002-04 — Update is ownership-scoped

- **Given** user A owns task T and user B is authenticated
- **When** B calls `PUT /api/tasks/{T}` with a valid body
- **Then** the API returns `404`
- **And** T is unchanged.

### AC-002-05 — Delete is ownership-scoped

- **Given** user A owns task T and user B is authenticated
- **When** B calls `DELETE /api/tasks/{T}`
- **Then** the API returns `404`
- **And** T still exists for A.

### AC-002-06 — Valid update replaces the editable fields

- **Given** user A owns task T
- **When** A calls `PUT /api/tasks/{T}` with a valid body
- **Then** the API returns `200` with the updated task
- **And** `updatedAtUtc` is later than or equal to `createdAtUtc`
- **And** the owner and `createdAtUtc` do not change.

### AC-002-07 — Delete removes the task

- **Given** user A owns task T
- **When** A calls `DELETE /api/tasks/{T}`
- **Then** the API returns `204` with no body
- **And** a subsequent `GET` returns `404`.

### AC-002-08 — Anonymous access is rejected

- **Given** no bearer token
- **When** any `/api/tasks` route is called
- **Then** the API returns `401`
- **And** no data is read or written.

### AC-002-09 — Validation rejects bad input

- **Given** an authenticated user
- **When** the title is shorter than 3 or longer than 200 characters, the
  description exceeds 2000 characters, the status is unknown, or the due date is
  invalid
- **Then** the API returns `400` with field-level `errors`
- **And** no write occurs.

### AC-002-10 — Missing task is a 404

- **Given** an authenticated user
- **When** they request, update, or delete a task id that does not exist
- **Then** the API returns `404` with a safe ProblemDetails body.

## 6. Rules and validation

| Field | Rule | Violation |
| --- | --- | --- |
| `title` | Required, trimmed, 3–200 characters | `400` |
| `description` | Optional, trimmed, max 2000; empty becomes `null` | `400` |
| `status` | `Todo`, `InProgress`, or `Done`; create defaults to `Todo`; update requires it | `400` |
| `dueDate` | Optional ISO `yyyy-MM-dd` | `400` |
| `userId` | Server-owned; never accepted from the client | `400` if supplied |
| `id` | Server-generated GUID | — |
| `createdAtUtc` | Set on creation, never changes | — |
| `updatedAtUtc` | Set on every update | — |

**Update semantics:** `PUT` replaces the editable fields. A `null` description or
due date clears the value. `status` is required on update so a missing field cannot
silently reset it; on create, a missing status means `Todo`.

## 7. API contract

Owned by `docs/adr/0004-api-contract.md`. Summary:

| Method | Route | Auth | Success | Errors |
| --- | --- | --- | --- | --- |
| GET | `/api/tasks` | bearer | `200` + array | `401` |
| GET | `/api/tasks/{id}` | bearer | `200` + task | `401`, `404` |
| POST | `/api/tasks` | bearer | `201` + task + `Location` | `400`, `401` |
| PUT | `/api/tasks/{id}` | bearer | `200` + task | `400`, `401`, `404` |
| DELETE | `/api/tasks/{id}` | bearer | `204` | `401`, `404` |

There is no `403` for ownership: the API must not confirm that another user's task
exists. Pagination and filtering are deliberately absent; the demo dataset is small.

## 8. Non-functional requirements

- **Security:** ownership is enforced in the Application layer; the controller never
  trusts a user id from the request body.
- **Data:** unique `Id`; indexed `UserId`; FK enforced; status stored as a string.
- **Performance:** one indexed query per list; no N+1.
- **Testability:** Domain invariants, Application service behavior, EF mappings, and
  API status codes each have a test.

## 9. Dependencies and assumptions

- SPEC-001 provides the authenticated user id.
- The repository interface lives in `Application`; EF Core lives in `Infrastructure`.
- SQLite is the runtime database (ADR-0003).

## 10. Open questions

None. Filtering and pagination are recorded as out of scope, not forgotten.

## 11. Change log

| Date | Change | Reason | Approved by |
| --- | --- | --- | --- |
| 2026-11-11 | Initial spec | Core CRUD and ownership | Orchestrator |
| 2026-11-12 | Status moved from `approved` to `implemented` | Every acceptance criterion has automated coverage, the independent review has no open blockers, and the clean-database run proves the live behaviour | Orchestrator |
