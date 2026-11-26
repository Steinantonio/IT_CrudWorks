# TASKS-010 — Markdown task descriptions

- **Spec:** `specs/010-markdown-descriptions/spec.md`
- **Plan:** `specs/010-markdown-descriptions/plan.md`
- **Status:** implemented
- **Last updated:** 2026-11-26

## Tasks

| ID | Task | Owner | Depends on | Test / evidence | Status |
| --- | --- | --- | --- | --- | --- |
| T-010-01 | Add the `Markdown` component (marked + DOMPurify allowlist) and its unit tests. | Frontend implementer | ADR-0008 | `Markdown.test.tsx`: 6/6 pass | done |
| T-010-02 | Render task descriptions through `Markdown`, keeping the editor and `PUT` payload unchanged. | Frontend implementer | T-010-01 | Existing list/editor tests still pass | done |
| T-010-03 | Run lint/tests/build, update the ADR, traceability, and AI evidence. | Frontend implementer | T-010-02 | Frontend suite 16 files / 134 tests; build green | done |

## Notes

- The description remains raw text in the API and in the database.
- Images and embedded media are intentionally not allowed.
- The sanitizer is the security boundary; tests treat it as such.
