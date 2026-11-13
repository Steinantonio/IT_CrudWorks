# AI Evidence — 008: Task UI Slice

- **Date:** 2026-11-12
- **Agent:** frontend (subagent session)
- **Task:** SPEC-003 tasks T-003-07…T-003-11 — task list with four states, shared
  create/edit form, server `400` field-error mapping, delete confirmation
  dialog, global `401` → clear session → `/login`.
- **Tool/model:** OpenCode session (GLM-5.3-Flash Free)

## Prompt

The orchestrator delegated the task UI slice with these constraints:

- Read `AGENTS.md`, SPEC-003 (`spec.md` §5 AC-003-05…08, `plan.md`, `tasks.md`),
  ADR-0004 §4–§5, ADR-0005, and `docs/ai/004-auth-ui-slice.md`.
- Work only under `project/frontend/`, `docs/ai/`, and the status column of
  `specs/003-web-client/tasks.md`. The backend task slice (T-002-08) was
  implemented in parallel; per the orchestrator's sequencing note the slice may
  be built with `fetch` mocked, with the real-API evidence run afterwards.
- New `src/lib/tasks.ts`: typed `Task`, literal-union status, list/get/create/
  update/remove on `apiRequest`; protected calls catch `ApiError` `401` and call
  an injected unauthorized handler exactly once before rethrowing (module-level
  setter configured in the provider).
- Query keys `['tasks']` / `['tasks', id]`; mutations invalidate both.
- `TaskListPage` as the protected home: loading, empty (CTA), error (Retry),
  data (title, status badge, due date, truncated description, Edit/Delete);
  delete behind an accessible confirmation dialog (Escape closes, focus in/out);
  failed delete keeps the row with a safe message.
- `TaskFormPage` shared by `/tasks/new` and `/tasks/:id/edit`: RHF + Zod
  mirroring ADR-0004 §5; PUT full replacement; server 400 mapped to inputs,
  unknown keys to a form-level message; 404 edit → safe not-found state.
- Global 401: clears `crudworks.session` and redirects once; no retry loop;
  tested at the `lib/tasks.ts` boundary.
- Replace the interim health page as home; update tests that asserted
  "API healthy". Keep `/api/health` used by nothing.
- No `any`, no `console.log`, no invented endpoints/fields. Verify with real
  `npm run lint`, `npm run test`, `npm run build` output.

## Output summary

New modules:

- `src/lib/tasks.ts` — `Task`, `TaskStatus` (`'Todo' | 'InProgress' | 'Done'`),
  `TaskInput`, `taskKeys` (`all`, `detail(id)`), and the five API functions.
  Every call goes through a private `taskRequest` wrapper that catches
  `ApiError 401`, calls the injected handler only while a session still exists
  (read via `auth/session.readSession`), and rethrows.
- `src/components/StatusBadge.tsx` — status pill with distinct classes.
- `src/components/statusLabels.ts` — display labels shared with the form select
  (separate file to keep fast-refresh clean).
- `src/components/ErrorState.tsx` — alert panel with optional Retry button.
- `src/components/ConfirmDialog.tsx` — modal dialog: focus moves to the first
  control on open, Escape closes, Tab wraps inside, focus returns to the
  trigger on close.
- `src/pages/TaskListPage.tsx` — four states, toolbar, row actions, delete
  mutation, action-error alert.
- `src/pages/TaskFormPage.tsx` — create/edit in one component; edit loads
  `['tasks', id]` and resets the form when the data arrives; 400 field mapping
  mirrors `RegisterPage`; 404 → "Task not found" state with a link back.

Changed modules:

- `src/AppRoutes.tsx` — `/` → `TaskListPage`, `/tasks/new` and
  `/tasks/:id/edit` → `TaskFormPage`, both behind `RequireAuth`.
- `src/App.tsx` — query retry function skips `401` (no retry of unauthorized
  calls); `src/pages/HomePage.tsx` deleted (the interim health page is gone;
  the health endpoint is used by nothing).
- `src/auth/AuthProvider.tsx` — registers the context's `handleUnauthorized`
  into `lib/tasks` via a mount/unmount effect.
- `src/test/renderApp.tsx` — accepts an optional `QueryClient` so a test can
  exercise production-like retry.
- `src/index.css` — task list, badges, dialog, empty/error states, form actions.

Tests: 30 new/updated in 3 new files; `App.test.tsx` and `LoginPage.test.tsx`
now assert the task-list heading instead of "API healthy".

## Validation

- [x] `npm run lint` → clean (one warning round fixed: unused import,
      fast-refresh export, ref-in-render, unused type import).
- [x] `npm run test` → 9 files / 51 tests passed (was 6 files / 23 tests).
- [x] `npm run build` → `tsc -b && vite build`, 183 modules, built in 329 ms.
- [x] Real-API smoke (Development, port 5080, T-002-08 already implemented):
      login as the demo user → list renders the 5 seeded tasks (createdAtUtc
      desc) → create via the form → row appears first → delete dialog Cancel
      and Escape both close without DELETE → confirm deletes → row count back
      to 5 → browser console clean throughout.
- [x] Contract fidelity: only the five ADR-0004 task routes are called; no new
      fields; `description`/`dueDate` are sent as `null` when empty.
- [x] No `any`, no `console.*` (grep over `src/` finds nothing).

### Commands and real output

| Command | Result |
| --- | --- |
| `npm run lint` | exit 0, no findings (after fixes) |
| `npm run test` | 9 files passed, 51 tests passed, ~6 s |
| `npm run build` | 183 modules transformed, `✓ built in 329ms`, 430.22 kB JS |
| `dotnet run --project src\CrudWorks.Api --launch-profile http` | listening on `http://localhost:5080` |
| `GET /api/tasks` with the demo token (PowerShell) | `200`, 5 tasks, desc order by `createdAtUtc` |
| Browser, real API | login → `/` renders 5 rows; create → `POST /api/tasks` 201 → row first in list; dialog Cancel/Escape → no DELETE; confirm → row removed |
| Browser console during the whole flow | no errors |

### Test inventory (new/updated)

- `src/lib/tasks.test.ts` (8): bearer token + list; get by id; create POST
  payload; update PUT payload; DELETE resolves without body; 401 → injected
  handler once + session cleared + `ApiError` rethrown; handler not called when
  no session; handler called once across three repeated failed attempts.
- `src/pages/TaskListPage.test.tsx` (10): loading; rows in order with fields,
  badge, due date, Edit/Delete; empty state + CTA; error + Retry recovers;
  network failure message; Cancel keeps row and sends no DELETE; confirm sends
  DELETE and removes the row; Escape closes without deleting; failed delete
  keeps row + safe message; `401` with a retrying client clears the session,
  lands on `/login`, and does not loop (fetch calls ≤ initial + one retry).
- `src/pages/TaskFormPage.test.tsx` (10): client validation before request;
  title error linked to the input; create POST payload + navigate back;
  defaults (`Todo`, `null` optionals); Cancel without submitting; edit loads
  values and PUTs the replacement; 404 edit shows the not-found state; server
  400 mapped to matching inputs (`toHaveAccessibleDescription`); unknown error
  key → form-level message; save-time 404 → safe message.
- `src/App.test.tsx`, `src/pages/LoginPage.test.tsx`: home-route assertions
  moved from "API healthy" to the task-list heading.

### Viewport checks (structural, iframe pattern from docs/ai/004)

Real layout viewports via same-origin iframes in the logged-in tab
(`window.innerWidth` matched the frame width). Measured
`documentElement.scrollWidth` vs `innerWidth` plus action rect edges.

| Page | Width | `scrollWidth` / `innerWidth` | Rightmost action | Result |
| --- | --- | --- | --- | --- |
| `/` (5 rows) | 360 | 345 / 360 | Delete right edge 312 | no overflow, actions visible |
| `/` | 768 | 768 / 768 | Delete right edge 735 | no overflow |
| `/` | 1280 | 1280 / 1280 | Delete right edge 1135 | no overflow |
| `/tasks/new` | 360 | 360 / 360 | Cancel right edge 252 | no overflow |
| `/tasks/new` | 768 | 768 / 768 | Cancel right edge 252 | no overflow |
| `/tasks/new` | 1280 | 1280 / 1280 | Cancel right edge 364 | no overflow |
| `/tasks/{id}/edit` | 360 | 360 / 360 | Cancel right edge 269 | no overflow, form prefilled |
| `/tasks/{id}/edit` | 768 | 768 / 768 | Cancel right edge 269 | no overflow |
| `/tasks/{id}/edit` | 1280 | 1280 / 1280 | Cancel right edge 381 | no overflow |

- **Limitation:** the desktop browser window was not visible, so a screenshot
  and a real keyboard traversal could not be captured; dialog Escape and the
  dialog open/close flow were exercised through browser clicks/keys above, and
  the full keyboard pass remains T-003-12.

## Corrections

| What was first suggested | What was wrong | What changed | Why |
| --- | --- | --- | --- |
| Calling the handler on every 401 | A retried query would clear the session twice | Handler is called only while `readSession()` reports a session; App also skips retrying 401s | "Clears exactly once / no retry loop" (AC-003-08) survives a retrying client |
| `toMatchObject<ApiError>` | Build error: `message` is required on `ApiError` | `Partial<ApiError>` | Typed matcher without inventing fields |
| Testing the handler with a bare `vi.fn()` | The spy did not clear the session, so three attempts called it three times | Spy mirrors production (`vi.fn(() => clearSession())`) | The test now exercises the real no-session guard |
| `getByRole('button', { name: 'Delete' })` in list tests | Matched both rows | Row-scoped lookup helper (`rowFor`/`deleteButtonFor`) | Deterministic selection with two rows |
| Cancel test asserting "fetch not called" | Navigating home legitimately fetches the list | Assert no mutation request, allow the list GET | Tests behavior, not request absence |
| `STATUS_LABELS` exported from `StatusBadge.tsx` | oxlint fast-refresh warning (component file exporting a constant) | Moved to `components/statusLabels.ts` | Clean lint, same single source |
| Ref callbacks assigned during render (`onXRef.current = …`) | oxlint react-refs warning | Assign inside a plain `useEffect` | Refs stay out of the render path |
| Accidental `FormLocation`/`toFormValues` scaffolding | Dead code | Removed before commit | No dead code |

## Edge cases considered

- Empty optional fields are sent as `null` (`PUT` clears them per ADR-0004).
- `dueDate` accepts only `yyyy-MM-dd` (or empty); other input is rejected
  client-side with the contract message.
- A delete that fails (500) closes the dialog, keeps the row, and shows a
  form-level alert; the next Delete click clears the stale alert.
- An edit whose GET or PUT returns 404 (foreign/missing task) shows a safe
  not-found state; a save-time 404 keeps the form with a safe message.
- Unknown `ProblemDetails.errors` keys produce the generic form-level message.
- The delete dialog restores focus to the previously focused element on close.
- `encodeURIComponent` guards the id path segment.

## Auth and validation decisions

- The unauthorized handler is injected once through a module-level setter
  (`setUnauthorizedHandler` in `lib/tasks.ts`); `AuthProvider` registers its
  `handleUnauthorized` on mount and unregisters on unmount
  (`AuthProvider.tsx:65-68`). The lib module keeps no auth state and never
  touches storage directly — it reads only through `auth/session.readSession`
  to decide whether a notification is still meaningful.
- `['tasks']` prefix-matches `['tasks', id]`, so list invalidation covers
  detail keys; edit/delete still invalidate the detail key explicitly.
- Client validation mirrors ADR-0004 §5 exactly (messages included); the API
  stays authoritative — server 400 messages replace client messages.
- `RequireAuth` already guaranteed the session, but the pages still narrow
  `session` defensively instead of asserting.

## Files affected

- `project/frontend/src/lib/tasks.ts`, `project/frontend/src/lib/tasks.test.ts`
- `project/frontend/src/components/StatusBadge.tsx`,
  `project/frontend/src/components/statusLabels.ts`,
  `project/frontend/src/components/ErrorState.tsx`,
  `project/frontend/src/components/ConfirmDialog.tsx`
- `project/frontend/src/pages/TaskListPage.tsx`,
  `project/frontend/src/pages/TaskListPage.test.tsx`,
  `project/frontend/src/pages/TaskFormPage.tsx`,
  `project/frontend/src/pages/TaskFormPage.test.tsx`
- `project/frontend/src/AppRoutes.tsx`, `project/frontend/src/App.tsx`,
  `project/frontend/src/auth/AuthProvider.tsx`,
  `project/frontend/src/test/renderApp.tsx`, `project/frontend/src/index.css`
- `project/frontend/src/pages/HomePage.tsx` (deleted)
- `project/frontend/src/App.test.tsx`,
  `project/frontend/src/pages/LoginPage.test.tsx` (home-route assertions)
- `specs/003-web-client/tasks.md` (status for T-003-07…T-003-11 + note)
- `docs/ai/008-task-ui-slice.md`

## Open questions

- None blocking. Remaining for the final pass (T-003-12, T-003-13): the real
  keyboard-only traversal, screenshots of the three widths, and the final
  lint/test/build evidence run. Real-API evidence for AC-003-05…07 was partially
  exercised here and should be repeated after T-002-08 review closes.
