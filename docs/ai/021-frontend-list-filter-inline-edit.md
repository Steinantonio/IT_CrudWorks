# AI Evidence Log

## Entry 021

- **Date:** 2026-11-13
- **Agent:** frontend (it_crudworks/frontend-implementer)
- **Task:** Task list page — clickable status filter cards, inline description editing, icons and task ID on each card
- **Tool/model:** OpenCode subagent (GLM-5.3)

## Prompt

The user asked for three UX improvements on the task list page: (1) the three status summary cards become clickable filters that toggle the visible list by status; (2) the description can be edited inline on each card (full-replace PUT, empty→null, 2000-char validation); (3) each card shows small icons and the task's short ID. Constraints: no `any`, no `console.log`, oxlint clean, keep 80 existing tests green, English copy only, do not touch backend/docs/specs.

## Output summary

Created three components and rewrote the page: `TaskDescriptionEditor.tsx` (inline edit with full-replace PUT, 2000-char inline error, Save/Cancel, `aria-busy`), `TaskIdChip.tsx` (`#XXXXXXXX` chip with full-GUID tooltip and feature-guarded `navigator.clipboard` copy), `icons.tsx` (three stroke SVGs, `aria-hidden`), and updated `TaskListPage.tsx` (filter state `statusFilter`, `visibleTasks`, `aria-pressed` cards with explicit `aria-label="Filter by <label> (<count>)"`, zero-match inline note, `aria-busy` while saving). CSS updated for the pressed state and new classes; media queries kept responsive (360/768/1280, no horizontal scroll).

## Validation

- [x] Build: `npm run build` → 188 modules transformed, built in 302 ms
- [x] Unit tests: `npm run test` → 11 files, 88 tests passed (was 80; +8)
- [x] Lint: `npm run lint` → exit 0, no findings
- [x] Manual behavior: real browser run against the live API — Done card filters to one row, counts stay 1/1/1, `aria-pressed="true"`; second click clears to all 3 rows. Inline edit saved → `PUT /api/tasks/{id}` 200 with body `{"title":...,"description":...,"status":"InProgress","dueDate":"2026-11-20"}` and the card refetched with the new text. Console errors: 0.
- [x] Security review: unchanged — owner comes from the token; `userId` never sent from the client.

## Corrections

| What the AI suggested | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| Cards as plain `dl`/divs with click handlers | No accessible role/state for a toggle | Made each card a real `<button>` with `aria-pressed` and an explicit `aria-label` (visible inline text collapsed to "Todo1" in accessible-name computation) | Screen-reader correctness and testability |
| Clipboard stub defined before `userEvent.setup()` | `userEvent` replaces `navigator.clipboard` on setup, silently dropping the stub | Install the clipboard stub **after** `setup()` | Discovered with a temporary probe test |
| Editor saving via partial PATCH | API contract is full-replace PUT (ADR-0004) | Send `{ title, description, status, dueDate }` with only description changed; trim and map empty→null | Matches backend semantics |

## Edge cases considered

- Empty/whitespace description → `null` (matches backend trimming).
- Over-2000-char input → inline error + disabled Save, no request sent.
- Filter active with zero matches → inline note, not the empty-state block (reserved for a truly empty account).
- Counts always computed from the full list, never the filtered subset.
- `navigator.clipboard` feature-guarded; falls back silently when unavailable.

## Files affected

- `project/frontend/src/pages/TaskListPage.tsx`
- `project/frontend/src/components/TaskDescriptionEditor.tsx` (new)
- `project/frontend/src/components/TaskIdChip.tsx` (new)
- `project/frontend/src/components/icons.tsx` (new)
- `project/frontend/src/index.css`
- `project/frontend/src/pages/TaskListPage.test.tsx`

## Final decision

Accepted. All gates green; behavior verified against the real API with network capture.
