# ADR-0008: Markdown descriptions and the sanitizer boundary

- **Status:** accepted
- **Date:** 2026-11-26
- **Decision owner:** architect
- **Related requirements:** R-15, R-16
- **Related specs:** `specs/010-markdown-descriptions/spec.md`
- **Related ADRs:** 0004 (contract), 0005 (frontend architecture)

## Context

Task descriptions are user-authored strings rendered in the task list. The
approved backlog (4.2 / gap G14) asks for basic formatting, and the same feature
creates the project's clearest XSS surface: a description is stored verbatim and
shown to the same user, but a stored payload must never execute.

The API contract is fixed: `description` is a plain string of at most 2000
characters. Changing the storage format would be a contract change and is out of
scope.

## Decision

Render descriptions as GitHub-flavoured markdown **at render time** with
`marked`, then sanitize the result with **DOMPurify** using an explicit
allowlist. The stored value and the `PUT` payload stay raw text.

Allowlist:

- **Tags:** `p br strong em del code pre ul ol li blockquote a h1-h6 hr` plus GFM
  table tags (`table thead tbody tr th td`).
- **Attributes:** `href`, `title` only; `data-*`, `aria-*`, `class`, `id`, and
  `style` are dropped.
- **URLs:** `http(s)`, `mailto`, `#anchor`, and same-site `/path`. A path that
  starts with `//` or `/\` loses `href`; anything else loses it too.
- **Never:** images, iframes, forms, SVG, `style`, event handlers, or `srcdoc`.

Links open in the same tab (no `target="_blank"`), so there is no
`window.opener` surface.

## Alternatives considered

| Option | Why not |
| --- | --- |
| Store HTML from a rich-text editor | Moves the trust boundary to the client and changes the contract; sanitization would still be required on read |
| Render markdown server-side | Adds a parser dependency and an API response change; the demo has no shared cache |
| `react-markdown` | Safe by default, but it re-renders through React and adds a larger dependency tree for one field |
| A tiny parser (e.g. `snarkdown`) | Cheaper, but no GFM tables/strikethrough and still needs a sanitizer |
| Trust `marked` alone | `marked` passes raw HTML through; that is an XSS hole |

## Consequences

- The description editor remains plain text; users type markdown.
- `marked` + `dompurify` add about **84 kB raw / 28 kB gzip** to the lazy
  `TaskListPage` chunk. The initial entry is unaffected. The cost is acceptable
  for a route that already carries the list; a future optimization can lazy-load
  the renderer if it grows.
- Every render of a description goes through DOMPurify; the component memoizes
  the result per string.
- The XSS tests are part of the security evidence, not decoration.

## Validation

| Case | Expected |
| --- | --- |
| Plain text | One paragraph |
| Bold/italic/list/link | Matching elements, `href` kept |
| `<script>` | Removed |
| `<img onerror>`, `<iframe>`, `<svg onload>`, `<form>`, `<style>` | Removed, no handler attribute |
| `data-*`, `aria-*`, `class`, `id`, `style` | Dropped |
| `javascript:` / `data:` / `//host` / raw `/\host` link | Anchor without `href` |
| GFM table | Rendered with the table tags |
