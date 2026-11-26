# TASKS-011 — Dark mode and the Tailwind v4 pilot

- **Spec:** `specs/011-dark-mode-tailwind/spec.md`
- **Plan:** `specs/011-dark-mode-tailwind/plan.md`
- **Status:** implemented
- **Last updated:** 2026-11-26

## Tasks

| ID | Task | Owner | Depends on | Test / evidence | Status |
| --- | --- | --- | --- | --- | --- |
| T-011-01 | Add the color-mode module (read/store/apply/system/initial) and tests. | Frontend implementer | SPEC-011 approval | `colorMode.test.ts`: 5/5 pass | done |
| T-011-02 | Add the header toggle and apply the mode before render. | Frontend implementer | T-011-01 | `ColorModeToggle.test.tsx`: 2/2 pass; no-flash boot in `main.tsx` | done |
| T-011-03 | Add dark token overrides and wrap the legacy stylesheet in a layer. | Frontend implementer | T-011-01 | `index.css` dark block; layer order declared in `tailwind.css` | done |
| T-011-04 | Install Tailwind v4 (no preflight) and convert the login page as the pilot. | Frontend implementer | T-011-03 | Login tests pass; production build green; delta recorded | done |
| T-011-05 | Update ADR-0005, traceability, counts, and AI evidence. | Frontend implementer | T-011-04 | ADR addendum; frontend 134 tests; docs updated | done |

## Notes

- The pilot is deliberately one page; the rest of the app keeps the hand-written
  CSS in the legacy layer.
- Tailwind's preflight is not imported, so existing element styles are intact.
- `dark:` is tied to the app switch, which itself resolves the OS preference.
