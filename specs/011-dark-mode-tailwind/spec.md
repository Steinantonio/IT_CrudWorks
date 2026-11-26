# SPEC-011 — Dark mode and the Tailwind v4 pilot

- **Status:** implemented
- **Owner:** architect
- **Approved by:** project owner (2026-11-17 backlog; 2026-11-26 instruction)
- **Requirements:** R-14, R-15, R-17
- **ADRs:** 0005
- **Last updated:** 2026-11-26

## 1. Problem

The app only has a light palette, and every color is hand-written CSS. The
portfolio needs a defensible answer to "would you adopt a utility framework?"
without rewriting the whole UI. This slice adds a dark palette driven by tokens
and a time-boxed Tailwind v4 pilot on one page.

## 2. Scope

**In scope**

- Light/dark switch in the header, persisted per browser, defaulting to the OS
  preference.
- Dark palette defined once as token overrides on `[data-color-mode='dark']`.
- Apply the mode before the stylesheet paints (`/theme-init.js`) and before React
  renders.
- Tailwind v4 installed with **no preflight**, legacy CSS moved into a `legacy`
  cascade layer, and one page (login) converted to utilities.
- Measured CSS/JS delta and tests.

**Out of scope**

- Converting the rest of the app to Tailwind.
- Theme customization beyond light/dark.
- Server-side theme detection or a theme API.
- Changing any component behavior or the API.

## 3. Glossary

| Term | Meaning |
| --- | --- |
| Color mode | `light` or `dark`, applied as `data-color-mode` on `<html>`. |
| Token | A CSS custom property (`--bg`, `--surface`, ...) consumed by the legacy CSS. |
| Pilot | One page using Tailwind utilities, measured, with the rest unchanged. |
| Legacy layer | The existing stylesheet wrapped in `@layer legacy`, so utilities can override it. |

## 4. User stories

| ID | Story |
| --- | --- |
| US-011-01 | As a user, I want a dark theme that follows my system and remembers my choice. |
| US-011-02 | As a user, I want no flash of the wrong theme on load. |
| US-011-03 | As a maintainer, I want a measured Tailwind pilot, so adoption is a decision, not a leap. |

## 5. Acceptance criteria

### AC-011-01 — The switch works and persists

- **Given** the header
- **When** the user activates the mode switch
- **Then** `<html>` gets `data-color-mode="dark"` (or `light`), the button label
  and `aria-pressed` update, and the choice is stored.

### AC-011-02 — The OS preference is the default

- **Given** no stored choice
- **When** the app loads
- **Then** the mode follows `prefers-color-scheme`.

### AC-011-03 — No flash

- **Given** a stored dark choice
- **When** the app boots
- **Then** `data-color-mode` is set by the same-origin `/theme-init.js` before the
  stylesheet paints, and React renders with the stored mode.

### AC-011-04 — The dark palette covers the app

- **Given** dark mode
- **Then** background, surfaces, text, borders, accents, status colors, and
  shadows come from the dark tokens; no component keeps a light-only literal
  where a token exists.

### AC-011-05 — The pilot does not regress

- **Given** the login page
- **When** it renders in either mode
- **Then** labels, `aria-invalid`, `aria-describedby`, the disabled submit state,
  and keyboard focus remain, and existing login tests pass.

### AC-011-06 — Legacy CSS is not broken by adoption

- **Given** pages that still use the hand-written CSS
- **When** the app builds
- **Then** the legacy rules are in the `legacy` layer, utilities can override
  them, and no page loses its styles.

### AC-011-07 — Cost is measured

- **Given** the production build
- **Then** the CSS and lazy-chunk deltas are recorded, and the decision is
  documented in ADR-0005.

## 6. Rules and validation

| Rule | Constraint |
| --- | --- |
| Attribute | `data-color-mode` on `<html>`, only `light` or `dark`. |
| Storage | `localStorage` key `crudworks-color-mode`; failures are non-fatal. |
| Boot | `/theme-init.js` (same-origin, CSP-safe) sets the attribute before the stylesheet paints; `main.tsx` repeats the resolution before `createRoot`. |
| Tailwind | No preflight; only theme + utilities layers. |
| Legacy | `index.css` wrapped in `@layer legacy`, declared before utilities. |
| Dark variant | `dark:` keyed to `[data-color-mode='dark']`, not the OS query. |

## 7. API contract

No change. The theme is a client preference; nothing is sent to the server.

## 8. Non-functional requirements

- **Performance:** the pilot adds CSS only on the pages that use utilities; the
  Tailwind runtime is build-time.
- **Accessibility:** the switch is a labelled button with `aria-pressed`; the
  palette keeps contrast in both modes.
- **Security:** no new input or storage of sensitive data.
- **Compatibility:** browsers without `matchMedia` fall back to light.

## 9. Dependencies and assumptions

- **Fact:** the palette is entirely CSS variables in `index.css`.
- **Fact:** the header is rendered by `Layout` for every route.
- **Decision:** Tailwind v4 with the Vite plugin, no preflight, pilot on login.
- **Assumption:** one page is enough to judge the developer experience and cost.

## 10. Change log

| Date | Change | Reason | Approved by |
| --- | --- | --- | --- |
| 2026-11-26 | Implemented dark mode and the Tailwind v4 pilot on the login page. | Owner-approved backlog (G22 + 4.10). | Project owner |
