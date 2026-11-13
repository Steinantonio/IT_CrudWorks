# AI Evidence — 015: Frontend accessibility and responsive pass

- **Date:** 2026-11-12
- **Agent:** frontend implementer (subagent session), transcribed by the orchestrator
- **Task:** Close the missing keyboard half of T-003-12, run a systematic
  accessibility audit against SPEC-003 and DoD §7, fix what the audit found
  inside the approved contract, and produce the T-003-13 gate evidence.
- **Tool/model:** OpenCode subagent (MiniMax M3.1 Flash Free), driving the
  installed Google Chrome over the DevTools Protocol.

## Prompt summary

The orchestrator delegated the final frontend pass with these constraints:

- The task UI was already implemented and tested (T-003-07…T-003-11 are
  `done`); what remained was T-003-12 (responsive + accessibility) and
  T-003-13 (lint/test/build evidence).
- Structural viewport checks at 360/768/1280 already existed in
  `docs/ai/008`. The missing half was the **keyboard pass** and a systematic
  accessibility check.
- Write automated tests with real keyboard interaction (`userEvent.tab()` /
  `userEvent.keyboard()`), matching the existing house style; check first
  whether `@testing-library/user-event` is actually installed and say so if it
  is not.
- Fix what the audit found, within the approved contract: no redesign, no
  component library, **no new dependencies**, no API-contract change.
- Verify against the real backend and state plainly what could NOT be
  verified.
- Do not write `docs/ai/**` — the orchestrator transcribes the report.

## Output summary

### Fixes applied (all inside the contract, no dependency added)

| # | Finding | Fix | Location |
| --- | --- | --- | --- |
| 1 | **Minor — ambiguous accessible names.** The five `Delete` buttons shared the name "Delete". | Per-task `aria-label`, mirroring the existing `Edit {title}` links. | `src/pages/TaskListPage.tsx:120` |
| 2 | **Minor — focus lost on every client-side navigation.** After the auth-guard redirect, after login, and after create/edit/delete, focus stayed on an element that no longer existed, so a screen reader announced nothing. | Move focus to the `main` landmark on a path change; `main` is focusable via `tabIndex={-1}`. | `src/components/Layout.tsx:9-22` |
| 3 | **Minor — focus lost after deleting a row.** The dialog cleanup restored focus to the trigger, but the trigger was removed with the row. | Focus the list heading instead. | `src/pages/TaskListPage.tsx:39-40,55` |
| 4 | **Minor — inconsistent focus indicator.** The focused landmark used the browser default `1px auto rgb(16,16,16)` ring around the entire content area. | One CSS rule → `2px solid var(--accent)`, `outline-offset: -2px` so nothing is clipped. | `src/index.css:74-77` |

### Checked and already correct (no change needed)

Every form control has a `<label for>`; field errors are `role="alert"` and
linked with `aria-describedby` + `aria-invalid`; the form-level error is
`role="alert"`; the task list is a semantic `<ul>/<li>`; the dialog is
`role="dialog" aria-modal="true"` with `aria-labelledby`/`aria-describedby`;
**status is conveyed by text, not by colour alone** (`StatusBadge` renders
"Todo" / "In progress" / "Done"); the landmarks are `header` + `main`; the
heading order is one `h1` per page (`h2` only inside the dialog); the
destructive action is visually distinct and confirmed.

### Reported, not fixed (product-behaviour decisions or browser-native)

- **Nit** — `input[type="date"]` is a native multi-segment widget: Tab moves
  between the day/month/year segments before leaving the field. Browser-native.
- **Nit** — each row exposes two tab stops for the same destination (title
  link and `Edit` link). Kept: the title link is the primary affordance and
  `Edit` carries the explicit action name.
- **Nit** — the dialog does not close on a backdrop click and the background is
  not `inert`. Changing this is a product-behaviour decision.
- **Recommendation (not implemented)** — a skip link is not required: WCAG
  2.4.1 is satisfied by the `header`/`main` landmarks plus the heading
  structure. If the header grows, add `#main-content`.
- **Manual colour-contrast computation** (relative-luminance maths, not a
  tool): `--muted #5c6875` on white ≈ 5.7:1, `--danger #a12b2b` ≈ 7.3:1,
  `--accent #1f6f5c` ≈ 6.0:1, white on `--accent` ≈ 6.0:1 — all ≥ AA for
  normal text.

## Validation

- [x] Lint: `npm run lint` → exit 0, no findings. The linter was proven to
      actually scan `src/` by temporarily adding a probe file with `debugger`
      and an unused variable (reported), then deleting it.
- [x] Tests: `npm run test` → **10 files, 69 tests passed** (was 9 files / 51
      tests). **+18 tests**, all keyboard/accessibility.
- [x] Build: `npm run build` → 183 modules transformed, built in ~175 ms.
- [x] Manual behavior — **real Chromium over the DevTools Protocol**, real key
      events (`Input.dispatchKeyEvent`), no synthetic DOM events, against the
      live API on 5080 through the Vite dev server on 5173.
- [x] Security review: the 401 reaction clears the session once, returns to
      `/login`, and does not loop.

### Commands and real output

```text
=== npm run lint ===
> oxlint
LINT_EXIT=0

=== npm run test ===
 Test Files  10 passed (10)
      Tests  69 passed (69)
   Duration  6.84s

=== npm run build ===
✓ 183 modules transformed.
dist/assets/index-DlHxzzUH.css    6.28 kB │ gzip:  1.75 kB
dist/assets/index-RhuRUHPU.js   430.57 kB │ gzip: 132.45 kB
✓ built in 177ms
BUILD_EXIT=0
```

### Real-browser walkthrough (headless Chrome, DevTools Protocol)

```text
STEP guard-redirect: {"url":"/login","heading":"Sign in","landmarks":["header","main"],
  "labelledInputs":[{"id":"login-email","label":"Email"},{"id":"login-password","label":"Password"}],
  "focusOutline":"solid 2px rgb(31, 111, 92)"}

STEP login-submit-with-enter: {"focusBeforePassword":{"tag":"input","id":"login-password"},
  "url":"/tasks/new","heading":"New task","activeAfterNavigation":{"tag":"main",...},"sessionStored":true}

STEP task-list-structure: {"listTag":"UL","rowCount":5,"headingOrder":["H1:Tasks"],
  "statusBadges":[{"text":"Todo","ariaHidden":null},{"text":"In progress",...},{"text":"Done",...}],
  "deleteButtonNames":["Delete Send the weekly status update","Delete Review the API contract",...]}

STEP delete-dialog-keyboard: {"focusRing":{"outlineStyle":"solid","outlineWidth":"2px"},
  "dialog":{"role":"dialog","ariaModal":"true","title":"Delete task","active":"Cancel","activeInDialog":true},
  "tabSequence":["Delete task","Cancel","Delete task","Cancel"],
  "afterEscape":{"dialogPresent":false,"active":{"tag":"button","ariaLabel":"Delete Send the weekly status update"}}}

STEP invalid-form-announcement: {"errorRole":"alert",
  "errorText":"Title must be between 3 and 200 characters.",
  "ariaDescribedBy":"task-title-error","ariaInvalid":"true","focusedElement":"task-title/INPUT"}

STEP 401-reaction: {"url":"/login","heading":"Sign in","sessionCleared":true,"activeElement":"MAIN"}

STEP viewports: 360  -> list {innerWidth:360,scrollWidth:360,horizontalOverflow:false}
                      768  -> list {innerWidth:768,scrollWidth:768,horizontalOverflow:false}
                      1280 -> list {innerWidth:1280,scrollWidth:1280,horizontalOverflow:false}
```

### Browser console

During the whole demo flow, without the deliberate invalid-token step, the
browser console contained **only** `debug` (Vite HMR) and `info` (React
DevTools notice) entries — **zero warnings, zero errors, zero uncaught
exceptions, zero failed requests**. The one `error` line in the final capture
is the browser's network log for the deliberately invalid token of the
`401-reaction` step, not an application error.

### HTTP-level verification of the dev-server proxy (also real)

```text
LOGIN=200 EMAIL=demo@crudworks.local DISPLAY=Demo User
TASKS=200 COUNT=5
ANON=401
```

Final state check after the browser run: `FINAL_TASK_COUNT=5` — the probe task
created during the test was deleted again.

### Code hygiene

```text
=== console.log / console.* in src ===   (no matches)
=== 'any' type usage in src ===          (no matches)
```

## New tests (18)

`src/components/ConfirmDialog.test.tsx` (new file): exposes the dialog
semantics with a title and a description; moves the focus to the safe action
when it opens; keeps the focus inside the dialog while tabbing; closes with
Escape and returns the focus to the trigger; confirms with the keyboard and
returns the focus to the trigger.

`src/pages/TaskListPage.test.tsx`: conveys the task status as text and not
only with colour; reaches the row actions with the keyboard and opens the edit
form with Enter; reaches the delete action with the keyboard and opens the
dialog with Enter; returns the focus to the delete trigger when the dialog is
dismissed with Escape; moves the focus to the list heading after a task is
deleted.

`src/pages/TaskFormPage.test.tsx`: walks the form controls with Tab in the
visual order; moves the focus to the first invalid field and announces the
message; submits with Enter from the title field; cancels with the keyboard
from the cancel link.

`src/pages/LoginPage.test.tsx`: walks the form controls with Tab in the visual
order; returns to the route the auth guard interrupted and keeps the focus on
the page.

`src/pages/RegisterPage.test.tsx`: walks the form controls with Tab in the
visual order; submits with Enter from the password field.

## Acceptance criteria

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-003-01 guard protects task routes | satisfied | `RequireAuth.test.tsx`; browser `guard-redirect` → `/login`, no task request sent |
| AC-003-02 login stores session, returns to target | satisfied | browser `login-submit-with-enter` → landed on `/tasks/new` (the interrupted route) |
| AC-003-03 invalid login explained | satisfied | `LoginPage.test.tsx` (401 message, email kept, button re-enabled) |
| AC-003-04 registration validates | satisfied | `RegisterPage.test.tsx` (field-level + 409) |
| AC-003-05 list has four states | satisfied | `TaskListPage.test.tsx` (loading / empty / error+retry / rows) |
| AC-003-06 create and edit share one form | satisfied | `TaskFormPage.test.tsx`; browser `create-with-keyboard`, `edit-with-keyboard` |
| AC-003-07 delete asks for confirmation | satisfied | `TaskListPage.test.tsx`; browser `delete-dialog-keyboard` (no DELETE before confirm) |
| AC-003-08 expired session returns to login | satisfied | browser `401-reaction` → `/login`, session cleared, no retry loop |
| AC-003-09 responsive | satisfied | browser `viewports`: `scrollWidth === innerWidth` on list **and** form at all three widths |
| AC-003-10 accessibility | **partially satisfied** | The keyboard half is fully verified. The screen-reader half is **not verifiable in this environment**: no assistive technology is available. The markup that a screen reader depends on (`<label for>`, `role="alert"`, `aria-describedby`, `aria-invalid`, `role="dialog"`, `aria-modal`, a labelled title) is asserted by machine, but no NVDA/JAWS/VoiceOver run was performed. |

## Corrections

| What the AI did | What was wrong | What changed | Why |
| --- | --- | --- | --- |
| A focus effect that ran on every render | React effects are order-dependent; skipping the first run would break the auth-guard redirect case | Rewritten to compare the previous path with the current one (`Layout.tsx:9-22`) | A focus effect that fires on mount steals focus from the first field on the login page |
| The `Delete` `aria-label` change | Two existing assertions expected the bare name "Delete" | The two assertions were updated deliberately, not weakened | The old assertions encoded the ambiguity that was the defect |
| The default focus ring was considered acceptable | It surrounded the entire content area in the browser's default 1px black | Replaced with the app's own accent outline | Found only by looking at a screenshot, not by reading the code |

## Rejected

- **Adding Playwright or any test dependency** — `docs/03-stack-baseline.md` §2
  forbids new dependencies, and the project has no Playwright. A throwaway
  Node script drove Chrome over the DevTools Protocol instead; it lived in
  `node_modules/.cdp-check/`, was deleted afterwards, and added nothing to the
  repository.
- **Adding a skip link** — the landmarks already satisfy WCAG 2.4.1.
- **Changing the API contract or the product behaviour** — reported as open
  questions instead.
- **Localised date rendering** ("Due 2026-11-14" → a formatted date) — a UI-copy
  change beyond this pass.

## What could NOT be verified

- **The in-app browser was not connected to this session**, so the Review pane,
  interactive screenshots through it, and the built-in Lighthouse accessibility
  audit were unavailable. Headless Chrome over CDP is a real browser engine,
  but it is not the same tool.
- **No automated accessibility audit (axe / Lighthouse)** was run; the audit is
  structural (DOM/ARIA assertions) plus manual review.
- **No real screen reader** (NVDA / JAWS / VoiceOver) and no braille output.
  "Errors are announced" is verified only through the ARIA markup and the
  browser's live-region semantics.
- **Only Chromium**: no Safari, Firefox, Edge, mobile, or a physical device.
  Windows High Contrast / forced-colors mode was not tested.
- **No touch/pointer testing** on a real device; the 360/768/1280 checks used
  `Emulation.setDeviceMetricsOverride` (real layout and measurements, emulated
  viewport).
- **Contrast was computed by hand**, not measured by a contrast engine.

## Files affected

- `project/frontend/src/components/Layout.tsx` — route-change focus handling.
- `project/frontend/src/components/ConfirmDialog.test.tsx` — **new**, 5 tests.
- `project/frontend/src/pages/TaskListPage.tsx` — delete `aria-label`, focus
  after delete, `tabIndex={-1}`.
- `project/frontend/src/pages/TaskFormPage.tsx` — `tabIndex={-1}` on `main`.
- `project/frontend/src/pages/LoginPage.tsx`, `RegisterPage.tsx`,
  `NotFoundPage.tsx` — `tabIndex={-1}` on `main`.
- `project/frontend/src/index.css` — the focused-landmark outline.
- `project/frontend/src/pages/TaskListPage.test.tsx` — +5 tests, 2 assertions
  updated.
- `project/frontend/src/pages/TaskFormPage.test.tsx` — +4 tests.
- `project/frontend/src/pages/LoginPage.test.tsx` — +2 tests.
- `project/frontend/src/pages/RegisterPage.test.tsx` — +2 tests.

No file outside `project/frontend/**` was modified. No backend file was
touched. No dependency was added. No commit was made.

## Open questions

- Should the delete confirmation also close on a backdrop click, and should the
  background become `inert` while the dialog is open? Both are product-behaviour
  changes.
- Should the duplicated destination per row (title link vs `Edit` link) be
  reduced to one tab stop? That would change the visual design.
- The `dueDate` renders as a raw ISO date. Localised formatting is a UI-copy
  change beyond this pass.
