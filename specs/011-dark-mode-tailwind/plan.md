# PLAN-011 — Dark mode and the Tailwind v4 pilot

- **Spec:** `specs/011-dark-mode-tailwind/spec.md`
- **Status:** implemented
- **Owner:** architect
- **Last updated:** 2026-11-26

## 1. Architecture impact

Frontend-only.

| Area | Change |
| --- | --- |
| Theme | `src/theme/colorMode.ts` (read/store/apply/system) + `ColorModeToggle`. |
| Header | `Layout` renders the switch for every route. |
| Tokens | `[data-color-mode='dark']` overrides in `index.css`. |
| Build | `@tailwindcss/vite` plugin; `src/tailwind.css` with theme + utilities only. |
| Layering | `index.css` wrapped in `@layer legacy`; layer order declared once. |
| Pilot | `LoginPage` form converted to utilities, including `dark:` variants. |
| Boot | `/theme-init.js` sets the attribute before the stylesheet paints; `main.tsx` applies it again before `createRoot`. |

## 2. Implementation shape

### Color mode

- `readStoredMode` accepts only `light`/`dark`; a corrupted value falls back.
- `systemMode` uses `matchMedia('(prefers-color-scheme: dark)')` when available,
  otherwise light.
- `initialMode` = stored ?? system.
- `applyMode` sets `data-color-mode` on `<html>`; `storeMode` writes the key and
  swallows storage errors.

### Toggle

- Labelled button (`Switch to dark mode` / `Switch to light mode`), `aria-pressed`.
- Applies the mode in an effect and persists on click.

### Tailwind

- `src/tailwind.css` declares `@layer legacy, theme, base, components, utilities`
  and imports only `tailwindcss/theme.css` and `tailwindcss/utilities.css`.
- No preflight: the legacy stylesheet keeps owning base elements.
- `@custom-variant dark` targets `[data-color-mode='dark']` and descendants.

### Pilot

- Login form, demo block, fields, submit, and the register link use utilities.
- Existing labels, `aria-*`, disabled state, and roles are unchanged.

## 3. Test strategy

| Case | Where |
| --- | --- |
| Read/store/apply/system/initial mode | `theme/colorMode.test.ts` |
| Toggle flips, persists, starts from storage | `components/ColorModeToggle.test.tsx` |
| Login behavior unchanged | existing `LoginPage` tests |
| Build + measured delta | `npm run build` output recorded in ADR-0005 |

## 4. Risks

| Risk | Mitigation |
| --- | --- |
| Preflight resets the legacy UI | Preflight is not imported |
| Utilities cannot override legacy rules | Legacy rules wrapped in a layer declared before `utilities` |
| Flash of light theme | Mode applied before React renders |
| Hardcoded light colors in dark mode | Dark tokens cover the palette; remaining literals are status accents (recorded) |
| Bundle growth | Only the pilot page and its chunk pay for utilities; measured |

## 5. Definition of done

- [x] Toggle, persistence, system default, and boot behavior tested.
- [x] Dark tokens defined once.
- [x] Legacy CSS in a layer; utilities win where used.
- [x] Login page converted with no behavior/accessibility regression.
- [x] Build passes and the delta is recorded in ADR-0005.
