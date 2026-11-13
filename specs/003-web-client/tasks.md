# TASKS-003 — Web client

- **Spec:** `specs/003-web-client/spec.md`
- **Plan:** `specs/003-web-client/plan.md`
- **Status:** implemented
- **Last updated:** 2026-11-12

## Tasks

| ID | Task | Owner | Depends on | Test / evidence | Status |
| --- | --- | --- | --- | --- | --- |
| T-003-01 | Router shell, providers, not-found route | Frontend | — | `App.test.tsx` | done |
| T-003-02 | API client with ProblemDetails mapping | Frontend | — | typed client review | done |
| T-003-03 | Session module and `AuthProvider` | Frontend | T-003-02 | session test | done |
| T-003-04 | `RequireAuth` with return path | Frontend | T-003-03 | guard test | done |
| T-003-05 | Login page with 401 handling | Frontend | T-003-03 | login test | done |
| T-003-06 | Register page with 409 handling | Frontend | T-003-03 | register test | done |
| T-003-07 | Task list: loading, empty, error, data | Frontend | T-002-08 | `TaskListPage.test.tsx` | done |
| T-003-08 | Task form: create and edit | Frontend | T-002-08 | `TaskFormPage.test.tsx` | done |
| T-003-09 | Server `400` field errors mapped to inputs | Frontend | T-003-08 | `TaskFormPage.test.tsx` | done |
| T-003-10 | Delete confirmation dialog | Frontend | T-003-07 | delete tests in `TaskListPage.test.tsx` | done |
| T-003-11 | Global `401` → clear session → login | Frontend | T-003-03 | `tasks.test.ts` + `TaskListPage.test.tsx` | done |
| T-003-12 | Responsive + accessibility pass | Frontend | T-003-07..10 | manual 360/768/1280 | done |
| T-003-13 | Lint, test, build evidence | Frontend | all | command output | done |
| T-003-14 | Auth centering, demo access, task list/form polish | Frontend | T-003-07..13 | `LoginPage.test.tsx`, `TaskListPage.test.tsx`, real-Chromium 360/768/1280 (`docs/ai/020`) | done |

## Notes

- T-003-01 and T-003-02 are the scaffold delivered before the first feature slice.
- Sequencing decision (orchestrator, 2026-11-12): T-003-07…T-003-10 may be built against the approved ADR-0004 contract with `fetch` mocked, in parallel with the SPEC-002 backend; the real-API wiring and the AC-003-05…07 evidence run happen after T-002-08 lands. The earlier "wait for the implemented contract" note is relaxed for the mock phase only.
- T-003-11 must include the "clears exactly once / no retry loop" test at the `lib/api.ts` boundary (AC-003-08), not only the context-level probe.
- T-003-07…T-003-11 implemented 2026-11-12 against the ADR-0004 contract with mocked `fetch` (mock phase per the sequencing decision above). Evidence: `TaskListPage.test.tsx`, `TaskFormPage.test.tsx`, `lib/tasks.test.ts`. Real-API smoke pass completed 2026-11-12 (list/create/delete against the live backend, viewports logged) — recorded in `docs/ai/008-task-ui-slice.md`, closing the AC-003-05…07 run for this slice.
- T-003-12 and T-003-13 completed 2026-11-12. The keyboard half of the
  accessibility pass is machine-verified: 18 new tests using real
  `userEvent.tab()` / `userEvent.keyboard()`, plus a full keyboard walkthrough
  in a real Chromium browser driven over the DevTools Protocol (real key
  events, against the live API). Four real defects were found and fixed:
  ambiguous `Delete` button names, focus lost on every client-side route
  change, focus lost after deleting a row, and the browser's default focus
  ring around the whole content area. Viewports at 360/768/1280 were
  re-measured in the browser with no horizontal overflow on the list and the
  form. Evidence: `docs/ai/015-frontend-accessibility-pass.md`.
- **AC-003-10 is only partially satisfied.** The keyboard half is fully
  verified; the screen-reader half is not verifiable in this environment
  (no assistive technology available). The ARIA markup a screen reader depends
  on is asserted by machine, but no NVDA/JAWS/VoiceOver run was performed.
  This limitation is recorded, not hidden.
- T-003-13 evidence: `npm run lint` exit 0, `npm run test` → 11 files / 80
  tests passed, `npm run build` → 185 modules.
- T-003-14 (2026-11-13) completed the owner-requested UI polish: centered auth
  composition via `AuthLayout`, one-click demo access, formatted due dates,
  summary counts, and the Done-task overdue fix. Evidence: `docs/ai/020`; measured
  at 360/768/1280 with no horizontal overflow and a clean console.
