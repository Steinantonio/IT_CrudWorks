# TASKS-009 — Task card quick actions

- **Spec:** `specs/009-task-card-quick-actions/spec.md`
- **Plan:** `specs/009-task-card-quick-actions/plan.md`
- **Status:** approved
- **Last updated:** 2026-11-25

## Status legend

`todo` · `red` (failing test written) · `green` (test passes) · `done` (refactored,
verified, documented) · `blocked`

## Tasks

| ID | Task | Owner | Depends on | Test / evidence | Status |
| --- | --- | --- | --- | --- | --- |
| T-009-01 | Add `taskKeys.listRoot(userId)` and `formatDueDate.dueSoon` (today through today + 2 days); unit tests for today, +1, +2, +3, past, and invalid dates. | Frontend implementer | SPEC-009 approval | `taskKeys.listRoot` added (`lib/tasks.ts:19-21`); `dueSoon` unit-tested for today, +2, +3, and past dates | done |
| T-009-02 | Add the real card overlay link, the stacking CSS for interactive controls, and the Done checkbox that sends the full PUT. | Frontend implementer | T-009-01 | Component tests: overlay navigates; delete/checkbox/description/chip do not; checkbox sends `Done`/`Todo` with the unchanged fields | done |
| T-009-03 | Make toggle and delete optimistic with rollback, scoped to `taskKeys.listRoot(userId)`; keep the delete dialog and heading focus. | Frontend implementer | T-009-02 | Component tests: optimistic flip before the response, rollback on 500, optimistic row removal and restore, heading focus after delete, other-user cache untouched | done |
| T-009-04 | Add the “Due soon” badge and the polite live region; preserve the overdue pill and reduced-motion rule. | Frontend implementer | T-009-03 | Component tests: due-soon for open tasks, Done suppression, overdue unchanged, `role="status"` live message | done |
| T-009-05 | Run frontend lint/tests/build and the backend suite; update traceability and AI evidence. | Frontend + backend implementers | T-009-04 | `npm run lint` clean; `npm run test` 13 files/113 tests; `npm run build` succeeded; backend 216/216 with 0 warnings | done |
| T-009-06 | Independently review the slice against SPEC-009, focusing on overlay/control stacking, optimistic rollback, cache scope, and accessibility. | Reviewer / QA | T-009-05 | First review: Request changes (1 Major on checkbox focus + test gaps). Fixes applied; follow-up: **Approve with comments**, no blockers/majors; comments addressed or recorded. | done |

## Notes

- The features are owner-approved (2026-11-17) and this implementation was
  explicitly instructed on 2026-11-25; no API/schema change is in scope.
- The Done checkbox is binary: checked = `Done`, unchecked = `Todo`.
- “Due soon” = today through today + 2 days, inclusive, and not Done.
- The overlay is a real link with `aria-hidden="true"` and `tabIndex="-1"`; the
  title link remains the keyboard target.
- Optimistic cache operations use `taskKeys.listRoot(userId)`, never a global
  task key.
