# SPEC-009 — Task card quick actions

- **Status:** implemented
- **Owner:** architect
- **Approved by:** project owner (2026-11-17 feature sign-off; 2026-11-25 implementation instruction)
- **Requirements:** R-14, R-15, R-16, R-17
- **ADRs:** 0004, 0005
- **Last updated:** 2026-11-25

## 1. Problem

The task list is readable, but a card only navigates through its title link,
changing status requires the full edit form, delete waits for the server, and a
near due date has no distinct signal. This slice makes the card itself the edit
target, adds a Done checkbox with an optimistic update, makes delete optimistic,
and adds a due-soon badge and mutation announcements — all frontend-only.

## 2. Scope

**In scope**

- 4.1 Whole-card overlay link to the edit page; interactive controls stay above it.
- 4.3 Done checkbox on the card, reusing the existing full-replacement `PUT`.
- 4.4 Optimistic status toggle and delete with rollback on failure, scoped to the
  signed-in user's cache.
- 4.6 Due-soon badge (due within two days, not Done); the existing overdue pill
  remains.
- 4.9 `aria-live` announcement for toggle/delete outcomes, focus after delete,
  and respect for `prefers-reduced-motion` (the global rule already exists).
- Component and unit tests; lint/build.

**Out of scope**

- Markdown (4.2), search (4.5), sorting (4.7), undo/soft delete (4.8), dark mode
  (4.10), tags/priority (4.11), bulk actions (4.12).
- API, schema, contract, auth, or pagination changes; no new endpoint.
- CI/CD.

## 3. Actors and glossary

| Term | Meaning |
| --- | --- |
| Overlay link | An absolutely positioned link covering the card; `aria-hidden` and `tabIndex=-1`. |
| Optimistic update | The cache shows the expected result before the server confirms it; a failure rolls back. |
| Due soon | A due date from today through two days ahead, inclusive. |

## 4. User stories

| ID | Story |
| --- | --- |
| US-009-01 | As a user, I want to click anywhere on a card to edit it, without losing the buttons inside it. |
| US-009-02 | As a user, I want to mark a task done or reopen it from the list, with instant feedback. |
| US-009-03 | As a user, I want delete and status changes to feel instant but roll back safely when the server fails. |
| US-009-04 | As a user, I want a clear “Due soon” signal before a deadline passes. |
| US-009-05 | As a keyboard or screen-reader user, I want one card target, working controls, and announced results. |

## 5. Acceptance criteria

### AC-009-01 — The card body opens the editor

- **Given** a task card
- **When** the user clicks the overlay covering the card body
- **Then** the app navigates to `/tasks/{id}/edit`.
- **And** clicking Delete, the Done checkbox, the description toggle/editor, or
  the ID chip does not navigate.
- **And** the overlay has `aria-hidden="true"` and `tabIndex="-1"`; the title link
  remains the keyboard target.

### AC-009-02 — Done checkbox uses the existing contract

- **Given** a task with any status
- **When** the user checks the Done box
- **Then** a `PUT /api/tasks/{id}` is sent with the unchanged title, description,
  and due date, and `status: "Done"`.
- **When** the user unchecks it
- **Then** the same request is sent with `status: "Todo"`.
- **And** the status badge reflects the new status.

### AC-009-03 — Status toggle is optimistic and rolls back

- **Given** a task and a pending server response
- **When** the user toggles Done
- **Then** the badge and checkbox update before the response.
- **When** the request fails
- **Then** the previous status returns and a safe error is shown.
- **When** it succeeds
- **Then** the optimistic state remains and the list refetches in the background.

### AC-009-04 — Delete is optimistic and rolls back

- **Given** the delete confirmation
- **When** the user confirms
- **Then** the row disappears before the `204`.
- **When** the request fails
- **Then** the row returns and a safe error is shown.
- **When** it succeeds
- **Then** the row stays gone and focus moves to the list heading.

### AC-009-05 — Due-soon and overdue signals

- **Given** a task due today or within the next two days and not Done
- **Then** the card shows a “Due soon” badge.
- **Given** a past due date and not Done
- **Then** the existing red “Overdue · date” pill is shown.
- **Given** a Done task
- **Then** neither overdue nor due-soon styling is shown.

### AC-009-06 — Accessible feedback

- **Given** a successful toggle or delete
- **Then** a polite live region announces the result.
- **Given** a failure
- **Then** the error uses `role="alert"`.
- **And** the global `prefers-reduced-motion` rule covers the new transitions.

### AC-009-07 — Scope is preserved

- **Given** the change set
- **When** the suites run
- **Then** no API route, DTO, OpenAPI metadata, auth rule, pagination behavior, or
  database code changed; the existing backend suite remains green.

### AC-009-08 — Optimistic updates are user-scoped

- **Given** cached task lists for more than one user
- **When** a toggle or delete patches the cache
- **Then** only the signed-in user's list prefix is read or written.

## 6. Rules and validation

| Rule | Constraint | Behavior on violation |
| --- | --- | --- |
| Card overlay | One real link per card, `aria-hidden`, `tabIndex=-1`; controls above it. | Test fails. |
| Toggle | Checked = `Done`; unchecked = `Todo`; full PUT with current fields. | Test fails. |
| Optimistic rollback | Snapshot before mutate; restore on error. | Test fails. |
| Cache scope | `taskKeys.listRoot(userId)` for get/set/cancel/invalidate. | Review rejects a global patch. |
| Due soon | Today through today + 2 days, inclusive, and not Done. | Unit/component test fails. |
| Accessibility | Live region for results; `role="alert"` for errors; overlay hidden from AT. | Test/review fails. |

## 7. API contract

No change. The toggle reuses `PUT /api/tasks/{id}` (ADR-0004) and delete reuses
`DELETE /api/tasks/{id}`. No new fields or status codes.

## 8. Non-functional requirements

- **Performance:** optimistic UI removes the round-trip wait; the background
  refetch reconciles with the server.
- **Security:** no new input; ownership remains server-side. The optimistic cache
  is scoped by the signed-in user.
- **Accessibility:** one card target, labelled checkbox, live announcements,
  reduced-motion support.
- **Observability:** no new logging or telemetry.

## 9. Dependencies and assumptions

- **Fact:** the card title is a `Link` and the row has Edit/Delete actions
  (`TaskListPage.tsx:171-220`); the description editor owns its own `PUT`
  (`TaskDescriptionEditor.tsx:41-59`).
- **Fact:** `formatDueDate` returns `{ date, overdue }` (`lib/dates.ts:1-6,43-55`);
  the list already renders an overdue pill (`TaskListPage.tsx:190-195`).
- **Fact:** the list is an infinite query keyed by user and generation
  (`TaskListPage.tsx:51-57`); the global reduced-motion rule exists
  (`index.css:1103-1111`).
- **Decision:** the Done checkbox is binary (`Done` ↔ `Todo`); an InProgress task
  becomes Done when checked and Todo when unchecked.
- **Decision:** “due soon” means today through two days ahead, inclusive.

## 10. Open questions

None. The owner approved the features on 2026-11-17 and instructed this
implementation on 2026-11-25.

## 11. Change log

| Date | Change | Reason | Approved by |
| --- | --- | --- | --- |
| 2026-11-25 | Initial approved spec for the cheap product slice (4.1, 4.3, 4.4, 4.6, 4.9). | Owner-approved backlog; frontend-only, zero schema. | Project owner |
| 2026-11-25 | Implemented; first review requested changes (keyboard focus on the Done checkbox plus test gaps); fixes applied; follow-up review approved with comments (no blockers/majors). | Keep the slice honest before commit: 216/216 backend, 113/113 frontend, lint/build green; E2E not rerun (no API change). | Project owner + independent review |
