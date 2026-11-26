# AI Evidence Log

## Entry 039

- **Date:** 2026-11-26
- **Agent:** orchestrator (GPT-6 Luna)
- **Task:** Close three fronts in one pass: markdown descriptions with a
  sanitizer (4.2 / G14), dark mode plus the Tailwind v4 pilot (4.10 / G22), and
  `strict` TypeScript.
- **Tool/model:** GPT-6 Luna (OpenCode); Node v26.13.1; .NET SDK 10.0.404.

## Prompt

Faithful summary: after SPEC-009 shipped, the owner chose all three remaining
fronts and authorized finishing and pushing. Implement them with the project's
spec-first flow, run the gates, review, commit, and push.

## Output summary

- **Markdown:** new `Markdown` component (`marked` → DOMPurify allowlist), used by
  `TaskDescriptionEditor`; six unit tests including XSS payloads. ADR-0008 and
  SPEC-010 record the decision and the cost.
- **Dark mode:** `src/theme/colorMode.ts` (stored/system/initial/apply/store),
  `ColorModeToggle` in the header, `data-color-mode` applied before `createRoot`,
  and dark token overrides in `index.css`.
- **Tailwind v4:** `@tailwindcss/vite` plugin, `src/tailwind.css` with theme +
  utilities only (no preflight), `index.css` wrapped in `@layer legacy`, and the
  login page converted as the pilot with `dark:` variants.
- **Strict TS:** `strict: true` in `tsconfig.app.json` and `tsconfig.node.json`;
  the code needed no fixes.
- **Docs:** SPEC-010/011, ADR-0008, ADR-0005 addendum, inventory, traceability,
  DoD, test plan, clean-run §11, presentation, and this entry.

## Validation

- [x] Backend: `dotnet build` → 0 warnings/errors; `dotnet test` → **216
  passed** (unchanged).
- [x] Frontend: `npm run lint` clean; `npm run test` → **16 files, 134 tests
  passed** (113 before the slice); `npm run build` succeeded.
- [x] Strict: `npx tsc -b` with `strict: true` → no errors.
- [x] Markdown XSS tests: script, handlers, iframe/svg/form/style, data/aria
  attributes, `javascript:`, `data:`, `//host`, raw `/\host` all neutralized.
- [x] Color mode: 7 module tests (including blocked storage) + 2 toggle tests.
- [x] Measured build delta: CSS 15.38 → 30.55 kB raw (3.91 → 6.73 kB gzip);
  initial entry 293.42 kB; `TaskListPage` 13.84 → 97.71 kB (markdown deps).
- [x] No API, DTO, OpenAPI, migration, or CI change.

## Corrections

| What was considered | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| Import Tailwind with preflight | Preflight resets base elements and would restyle every existing page | Imported only theme + utilities, and wrapped the legacy CSS in `@layer legacy` | Adopt incrementally without a UI rewrite |
| `vi.spyOn(window, 'matchMedia')` in tests | jsdom does not define `matchMedia`, so the spy failed | Stubbed the global and restored it per test | Keep the tests deterministic |
| Dark mode keyed to the OS media query | A manual switch could not override the OS | `dark:` is a custom variant on `data-color-mode`; the app resolves the OS preference once on boot | One source of truth for the palette |
| Put markdown in the initial bundle | It would grow the entry chunk for every route | It lives in the lazy `TaskListPage` chunk; the cost is recorded in ADR-0008 | Keep the initial load flat |
| Claim strict TypeScript without enabling it | The earlier docs claimed strict while the tsconfig did not enable it | Enabled `strict` in both configs and restored the claim | Documentation truth |
| App tokens named `--radius-*`/`--shadow-*` | Tailwind's theme layer defines the same names, so `rounded-lg` on the pilot restyled every legacy panel | Renamed to `--app-radius-*` / `--app-shadow-*` | Keep the pilot local |
| `readStoredMode` default parameter read `window.localStorage` outside `try` | A blocked storage getter crashed boot before `createRoot` | Resolve the storage object inside `try`; tests cover get/set throwing | Non-fatal storage failure |
| Inputs used `outline-none` + a 30% ring | The focus indicator fell below 3:1 | Full-opacity `focus-visible` ring with offset; invalid fields keep a red border | Keyboard/contrast |
| Markdown links sat under the card overlay | Mouse clicks navigated to the editor instead of the link | Raised `.task-description` with the other interactive elements | Links work with mouse and keyboard |
| `ALLOWED_ATTR` alone did not drop `data-*`/`aria-*` | DOMPurify allows them by default, contradicting the documented allowlist | `ALLOW_DATA_ATTR: false`, `ALLOW_ARIA_ATTR: false`; tests added | Enforce the stated boundary |
| `/\host` passed the URL regex | Browsers can read a backslash as a slash | Regex rejects `//` and `/\`; raw-HTML test added | No protocol-relative escape |
| Theme applied only in `main.tsx` | The document could paint one light frame before the module ran | Added same-origin `/theme-init.js` (CSP-safe) before the stylesheet | No flash on a cold load |

## Edge cases considered

- Corrupted/absent stored mode → fall back to the system preference, then light.
- Storage blocked (private mode) → the switch still works for the session.
- No `matchMedia` → light, no crash.
- Stored dark mode → `/theme-init.js` applies it before the stylesheet paints (no
  flash), and `main.tsx` repeats it before React renders.
- Markdown link to `javascript:`/`data:`/`//host`/`/\host` → anchor without
  `href`, text kept.
- Raw HTML in markdown → sanitized against the allowlist (handlers, iframes,
  forms, SVG, and style removed).
- Plain-text descriptions → still render as a paragraph; existing tests pass.
- Legacy pages → unchanged because they are not converted and their CSS remains
  in the legacy layer.
- Dark mode with hardcoded status accents (row borders, due-soon/overdue pills) →
  readable, recorded as the next tokenization step.

## Auth and validation decisions

- No auth, ownership, validation, or contract change.
- The theme is a client-only preference; nothing is sent to the server.
- The markdown sanitizer is the security boundary; no raw `innerHTML` elsewhere.

## Files affected

- `project/frontend/package.json`, `package-lock.json`
- `project/frontend/index.html`, `public/theme-init.js`
- `project/frontend/tsconfig.app.json`, `tsconfig.node.json`
- `project/frontend/vite.config.ts`
- `project/frontend/src/main.tsx`
- `project/frontend/src/index.css`, `src/tailwind.css`
- `project/frontend/src/components/Markdown.tsx`, `Markdown.test.tsx`
- `project/frontend/src/components/TaskDescriptionEditor.tsx`
- `project/frontend/src/theme/colorMode.ts`, `colorMode.test.ts`
- `project/frontend/src/components/ColorModeToggle.tsx`, `ColorModeToggle.test.tsx`
- `project/frontend/src/components/Layout.tsx`
- `project/frontend/src/pages/LoginPage.tsx`
- `specs/010-markdown-descriptions/**`, `specs/011-dark-mode-tailwind/**`
- `specs/README.md`
- `docs/adr/0008-markdown-and-sanitizer.md`, `docs/adr/0005-frontend-architecture.md`
- `docs/04-requirement-traceability.md`, `docs/02-definition-of-done.md`
- `docs/qa/test-plan.md`, `docs/qa/clean-run-report.md`
- `docs/presentation.md`, `README.md`, `project/README.md`
- `docs/ai/039-markdown-dark-tailwind-strict.md`

## Final decision

**Implemented and verified.** The first review returned Request changes with five
majors (Tailwind token collision, blocked-storage crash, weak focus ring,
markdown links under the overlay, incomplete attribute allowlist); all five were
fixed and covered by tests. No blockers remain. 216/216 backend, 134/134
frontend, lint/build green, strict enabled. Commit and push authorized by the
owner.
