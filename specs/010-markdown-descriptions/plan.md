# PLAN-010 — Markdown task descriptions

- **Spec:** `specs/010-markdown-descriptions/spec.md`
- **Status:** implemented
- **Owner:** architect
- **Last updated:** 2026-11-26

## 1. Architecture impact

Frontend-only. No Domain, Application, Infrastructure, Api, database, or CI
change.

| Area | Change |
| --- | --- |
| Component | New `Markdown` component: `marked` → DOMPurify → `dangerouslySetInnerHTML`. |
| Card | `TaskDescriptionEditor` renders the description through `Markdown`. |
| Editor | Unchanged: plain textarea, same `PUT` payload. |
| Dependencies | `marked`, `dompurify` (runtime). |
| Tests | Six unit tests covering safe markdown and XSS payloads. |

## 2. Implementation shape

- `marked.use({ gfm: true, breaks: true })` once at module load.
- Sanitize with `ALLOWED_TAGS`, `ALLOWED_ATTR: ['href', 'title']`,
  `ALLOW_DATA_ATTR: false`, `ALLOW_ARIA_ATTR: false`, and an
  `ALLOWED_URI_REGEXP` that only permits `http(s)`, `mailto`, `#`, and same-site
  `/path` URLs (rejecting `//host` and raw `/\host`).
- Memoize the sanitized HTML per description with `useMemo`.
- No `target="_blank"`, so no `window.opener` surface.

## 3. Test strategy

| Case | Expected |
| --- | --- |
| Plain text | One `<p>` with the text |
| Bold, italic, list, link | Matching elements; `href` kept |
| `<script>` | Removed; text not executed |
| `<img onerror>`, `<iframe>` | Removed; no handler attribute |
| `javascript:` link | Anchor without `href`, text kept |
| `data:` link | Anchor without `href`, no script element |

## 4. Risks

| Risk | Mitigation |
| --- | --- |
| A sanitizer misconfiguration lets a payload through | Explicit allowlist + payload tests; DOMPurify defaults still apply |
| Bundle growth on the list route | Measured: lazy chunk grows ~84 kB raw / ~28 kB gzip; recorded in ADR-0008 |
| Markdown changes existing plain-text rendering | Tests assert the paragraph and the description text still render |

## 5. Definition of done

- [x] Every acceptance criterion has a test.
- [x] Raw HTML, event handlers, and unsafe URLs are covered by tests.
- [x] The editor and the API payload are unchanged.
- [x] Lint, tests, and build pass.
- [x] ADR-0008 records the decision and the cost.
