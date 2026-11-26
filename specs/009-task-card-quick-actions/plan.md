# PLAN-009 — Task card quick actions

- **Spec:** `specs/009-task-card-quick-actions/spec.md`
- **Status:** approved
- **Owner:** architect
- **Last updated:** 2026-11-25

## 1. Architecture impact

Frontend-only. No Domain, Application, Infrastructure, Api, database, or CI
change.

| Area | Change |
| --- | --- |
| Task list | Real overlay link per row; Done checkbox; optimistic toggle/delete; due-soon badge; live status message. |
| Task cache | `taskKeys.listRoot(userId)` used for optimistic get/set/cancel/invalidate; `taskKeys.all` remains for broad invalidation. |
| Dates | `formatDueDate` adds `dueSoon` (today through today + 2 days). |
| CSS | Overlay, control stacking, due-soon badge, visually hidden live region; existing reduced-motion rule covers transitions. |
| Tests | Component tests for navigation, controls, optimistic success/rollback, due-soon, live region; unit test for `dueSoon`. |
| API | No change; `PUT`/`DELETE` are reused. |

## 2. Data model

None. No schema or migration.

## 3. Contract

No route, payload, status code, or OpenAPI change. The toggle sends the existing
full-replacement `PUT`; delete sends the existing `DELETE`.

## 4. Implementation shape

### Overlay link

- Render a real `<Link className="task-card-overlay" to={/tasks/{id}/edit}
  aria-hidden="true" tabIndex={-1} />` inside the row.
- `.task-row { position: relative }`; the overlay is `position: absolute; inset: 0;
  z-index: 1`.
- `.task-actions`, `.task-status-toggle`, `.task-description-toggle`,
  `.task-description-editor`, and `.task-id-chip` are `position: relative;
  z-index: 2` so their clicks do not navigate.
- The title link remains the keyboard target; the overlay is not tabbable.

### Done checkbox

- A labelled checkbox in the actions area: checked when `status === 'Done'`.
- `onChange` calls a mutation with `{ task, status: checked ? 'Done' : 'Todo' }`.
- The mutation sends `updateTask` with the task's current title, description,
  status, and due date (full replacement).
- The checkbox is disabled for the task whose toggle is pending.

### Optimistic updates

- `onMutate`: cancel the user's list queries, snapshot `getQueriesData` under
  `taskKeys.listRoot(userId)`, patch the infinite pages (`withTaskStatus` or
  `withoutTask`), and clear the previous error.
- `onError`: restore every snapshot and show a safe message.
- `onSuccess`: set a polite status message and invalidate the user's list.
- Delete keeps the existing dialog and moves focus to the heading after success.

### Due-soon badge

- `formatDueDate` returns `dueSoon` for dates from today through today + 2 days.
- The card shows a “Due soon” badge when `dueSoon && status !== 'Done'`.
- The existing overdue pill and text stay unchanged.

### Accessibility

- A visually hidden `<p role="status" aria-live="polite">` announces “marked as
  done”, “moved back to Todo”, and “task deleted”.
- Errors keep `role="alert"`.
- The overlay is `aria-hidden`/`tabIndex=-1`; the checkbox has an accessible label.
- The global `prefers-reduced-motion` rule covers the new transitions.

## 5. Test strategy

| Layer | What is proven | Where |
| --- | --- | --- |
| Unit | `dueSoon` for today, +1, +2, +3, past, and invalid dates | `lib/dates.test.ts` |
| Component | Overlay navigates; delete/checkbox/description/chip do not; overlay is hidden/tabIndex -1 | `TaskListPage.test.tsx` |
| Component | Checkbox sends the full PUT and updates the badge; optimistic state before response; rollback on 500 | `TaskListPage.test.tsx` |
| Component | Delete row disappears optimistically; rollback on 500; focus after success | `TaskListPage.test.tsx` |
| Component | Due-soon badge; overdue unchanged; Done suppresses both | `TaskListPage.test.tsx` |
| Regression | Existing 216 backend tests and the frontend suite (113 after this slice), lint, build | Full gate |

## 6. Task order

1. Add `taskKeys.listRoot` and `formatDueDate.dueSoon` (unit tests first).
2. Add the overlay link, stacking CSS, and Done checkbox.
3. Add optimistic toggle and delete with rollback.
4. Add the due-soon badge and live region.
5. Add/update component tests; run lint/test/build.
6. Independent review and evidence.

## 7. Risks and fallbacks

| Risk | Impact | Fallback |
| --- | --- | --- |
| Overlay swallows control clicks | Delete/edit/checkbox stop working | Raise controls with `z-index`; test each control. |
| Optimistic patch leaks across users | Wrong user sees another cache | Scope every get/set/invalidate to `taskKeys.listRoot(userId)`; login/logout clears the cache. |
| Rollback overwrites a newer refetch | Stale data after failure | Snapshot/restore the exact keys returned by `getQueriesData`; invalidate after success. |
| Checkbox loses InProgress state | Unexpected status change | Documented decision: checked = Done, unchecked = Todo. |
| Due-soon timezone drift | Badge off by one day | Reuse `formatDueDate`'s local-calendar parsing; test with a fixed `today`. |

## 8. Definition of done

- [x] Every acceptance criterion has a test or a named manual check.
- [x] Overlay navigates; controls do not; keyboard target unchanged.
- [x] Toggle/delete are optimistic and roll back on failure.
- [x] Cache patches are scoped to the signed-in user.
- [x] Due-soon and overdue signals are correct; Done suppresses both.
- [x] Live announcements and focus behavior are verified.
- [x] No API/schema/OpenAPI/CI change.
- [x] Frontend lint/tests/build and the backend suite pass with captured output.
- [x] AI evidence entry exists and a review has no blocker. (First review requested changes: one Major on keyboard focus plus test gaps; fixes applied; follow-up returned Approve with comments, no blockers/majors.)
