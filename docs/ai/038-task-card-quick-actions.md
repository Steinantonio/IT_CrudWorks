# AI Evidence Log

## Entry 038

- **Date:** 2026-11-25
- **Agent:** orchestrator (GPT-6 Luna)
- **Task:** Implement and verify SPEC-009, the cheap product slice (4.1 card
  click, 4.3 Done checkbox, 4.4 optimistic updates, 4.6 due-soon badge, 4.9
  accessibility).
- **Tool/model:** GPT-6 Luna (OpenCode); Node v26.13.1; .NET SDK 10.0.404.

## Prompt

Faithful summary: after the pagination wave was committed and pushed, implement
the owner-approved cheap product slice — whole-card click target, Done checkbox,
optimistic status/delete, due-soon badge, and accessibility — with a spec first,
tests, and no API or schema change. Then run the gates and request an independent
review before committing.

## Output summary

- Created `specs/009-task-card-quick-actions/{spec,plan,tasks}.md` (approved).
- `taskKeys.listRoot(userId)` added so optimistic patches never touch another
  user's cache.
- `formatDueDate` now returns `dueSoon` (today through today + 2 days,
  inclusive) alongside `date` and `overdue`; unit tests updated.
- `TaskListPage`: real overlay link (`aria-hidden`, `tabIndex=-1`), Done checkbox
  that sends the full `PUT`, optimistic toggle and delete with snapshot/rollback,
  due-soon badge, polite live region, heading focus after delete, and an
  `aria-busy` row while a toggle is pending.
- CSS: overlay and control stacking (`z-index`), due-soon tokens/badge,
  `.sr-only`, and the existing reduced-motion rule covers the transitions.
- Component tests added for every acceptance criterion; two pre-existing
  second-page fixtures were given unique ids (they reused `tasks[1].id` and
  produced a React duplicate-key warning).

## Validation

- [x] Backend: `dotnet build CrudWorks.slnx --verbosity minimal` → 0 warnings,
  0 errors; `dotnet test CrudWorks.slnx --verbosity minimal` → **216 passed**
  (Domain 37, Application 62, Infrastructure 28, Architecture 6, Api 83).
- [x] Frontend: `npm run lint` clean; `npm run test` → **13 files, 113 tests
  passed** (104 before the slice); `npm run build` succeeded.
- [x] Scope: `git status` shows only frontend source/tests and docs; no API,
  DTO, OpenAPI, migration, or CI change.
- [x] E2E/self-test: not rerun (frontend-only); the 40/40 and 6/6 figures are
  carried from the SPEC-008 gate on the same API.
- [x] No commit or push was performed before the review.

## Corrections

| What was considered | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| Query the live region with `getByRole('status')` | The page has several `role="status"` elements (loading, counts, live region), so the query was ambiguous | Assert the message element and its `role` | One unambiguous assertion per behavior |
| Seed the "other user" cache with `createTestQueryClient` | `gcTime: 0` in the test client garbage-collects an unobserved query immediately, so the assertion read `undefined` | Build a client with `gcTime: Infinity` for that test | Prove the scope claim instead of a timing artifact |
| Keep `{ ...tasks[1], title: 'Third task' }` in pagination tests | The second page reused the first page's id, producing a React duplicate-key warning | Gave the fixture a unique id | Clean test output; duplicate keys are unsupported |
| Disable the Done checkbox while its toggle is pending | Disabling a focused control moves focus to `<body>` in Chrome/Firefox, so a keyboard user lost their place on every toggle (review Major) | Keep the checkbox enabled and ignore a second toggle while one is in flight; only the other rows' checkboxes are disabled | Preserve focus and still serialize mutations |
| Restore the whole list snapshot on any failure | Two concurrent toggles could roll each other back | Serialize toggles (one in flight), so a snapshot restore cannot clobber another task | Removes the race the review flagged as a minor |
| Always label the checkbox `Mark X as done` | On a Done task the name contradicted the checked state and did not match the visible "Done" label | State-aware label (`as not done` when checked) | WCAG 2.5.3 label-in-name and a truthful state |
| Announce only "Task deleted." | The toggle announcement named the task; delete did not | Delete mutation now carries the task and announces `“title” deleted.` | Consistent, useful feedback |
| Compare the other user's cache with the inserted object | An in-place mutation would pass the equality check | Compare against `structuredClone` of the seeded data | The test proves immutability, not identity |

## Edge cases considered

- Overlay must not steal focus: `aria-hidden="true"`, `tabIndex={-1}`; the title
  link stays the keyboard target.
- Controls above the overlay: actions, checkbox, description toggle/editor, and
  ID chip are `position: relative; z-index: 2`.
- Toggle is binary: checked → `Done`, unchecked → `Todo`; the full `PUT` keeps
  title, description, and due date unchanged.
- Rollback restores the exact cache snapshots under `taskKeys.listRoot(userId)`;
  a failure also clears the success message.
- Delete removes the row optimistically; a failure restores it and closes the
  dialog; success moves focus to the list heading.
- Due soon is suppressed for Done tasks, and the overdue pill keeps its existing
  precedence.
- Live region is polite; errors stay `role="alert"`.
- Reduced motion is already global; the new transitions inherit it.

## Auth and validation decisions

- No auth, ownership, validation, or contract change. The optimistic cache is
  scoped by the signed-in user id; ownership remains a server-side filter.
- No new endpoint; the toggle reuses `PUT /api/tasks/{id}` and delete reuses
  `DELETE /api/tasks/{id}`.

## Files affected

- `specs/009-task-card-quick-actions/{spec,plan,tasks}.md`
- `project/frontend/src/lib/tasks.ts`
- `project/frontend/src/lib/dates.ts`
- `project/frontend/src/lib/dates.test.ts`
- `project/frontend/src/pages/TaskListPage.tsx`
- `project/frontend/src/pages/TaskListPage.test.tsx`
- `project/frontend/src/index.css`
- `specs/README.md`
- `docs/04-requirement-traceability.md`
- `docs/02-definition-of-done.md`
- `docs/qa/test-plan.md`
- `docs/qa/clean-run-report.md`
- `docs/presentation.md`
- `README.md`, `project/README.md`
- `docs/ai/038-task-card-quick-actions.md`

## Independent review and fixes

The first read-only review returned **Request changes**:

| Severity | Finding | Fix |
| --- | --- | --- |
| Major | Disabling the focused Done checkbox drops keyboard focus to `<body>` on every toggle | Kept the checkbox enabled; ignore a second toggle while one is in flight; other rows' checkboxes are disabled so mutations stay serialized. Tests assert the pending checkbox is not disabled and the announcement still appears |
| Minor | AC clauses without assertions: badge after toggle, background refetch, delete announcement | Added all three: `.status-badge` text, a second `GET /api/tasks`, and `“title” deleted.` in the live region |
| Minor | The control-vs-overlay stacking test is jsdom-only | Documented in the clean-run report that stacking is CSS-inspected, not browser-tested; no browser test was added |
| Minor | Concurrent toggles could roll each other back | Serialized toggles; a snapshot restore can no longer clobber another task |
| Minor | Delete focus is lost while the dialog stays open | Recorded as an accepted-risk/limitation in the clean-run report; the failure path now moves focus to the heading after the dialog unmounts (asserted), the success path does so after the refetch |
| Minor | Checkbox label contradicted the checked state | State-aware `aria-label` |
| Minor | Cache-scope test compared the same object reference | Compares against `structuredClone` |
| Minor | "374 on the current tree" implied a fresh E2E | Presentation, DoD, and test plan state that the E2E was not rerun (API unchanged) |
| Nit | Redundant `aria-live` next to `role="status"` | Removed the redundant attribute |
| Nit | Title link painted under the overlay | Raised `.task-title` with the other interactive controls |

## Follow-up review

The follow-up returned **Approve with comments** (no blockers/majors). The
remaining comments were closed or recorded:

- Focus when a filtered row leaves the active filter: moves to the heading
  (tested).
- Delete failure: focus moves to the heading after the dialog unmounts (tested).
- Toggle serialization: the other row's checkbox is disabled and a second click
  sends no second `PUT` (tested).
- Cross-mutation rollback (delete/description vs toggle) is recorded as an
  accepted risk in `docs/qa/clean-run-report.md` §10.
- Evidence labels split the current-tree suites (216 backend + 113 frontend)
  from the carried E2E/self-test (40 + 6), and the docs no longer claim strict
  TypeScript (`tsconfig.app.json` does not enable it).

## Final decision

**Implemented; review approved with comments.** Evidence: 216/216 backend,
113/113 frontend, lint and build green; E2E not rerun because the API is
unchanged. No commit was made.
