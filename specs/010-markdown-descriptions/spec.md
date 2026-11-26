# SPEC-010 — Markdown task descriptions

- **Status:** implemented
- **Owner:** architect
- **Approved by:** project owner (2026-11-17 backlog; 2026-11-26 instruction)
- **Requirements:** R-15, R-16
- **ADRs:** 0004, 0008
- **Last updated:** 2026-11-26

## 1. Problem

Descriptions are plain text, so users cannot add emphasis, lists, or links to a
task. Rendering user-authored markdown in the browser is also the project's
clearest XSS surface. The slice adds formatting without ever trusting the stored
string.

## 2. Scope

**In scope**

- Render task descriptions as GitHub-flavoured markdown on the card.
- Sanitize the rendered HTML with an allowlist; strip raw HTML and unsafe URLs.
- Keep the editor plain text; the stored value and `PUT` payload are unchanged.
- Unit tests with XSS payloads.

**Out of scope**

- Server-side rendering or markdown storage.
- A rich-text/WYSIWYG editor, preview pane, or toolbar.
- Markdown in titles, comments, or any other field.
- Image uploads or embedded media (images are not allowed).
- API, schema, or contract changes.

## 3. Glossary

| Term | Meaning |
| --- | --- |
| Markdown | The user's plain-text description, parsed to HTML at render time. |
| Sanitizer | DOMPurify, which removes anything outside the allowlist. |
| Allowlist | The exact tags, attributes, and URL schemes that may survive. |

## 4. User stories

| ID | Story |
| --- | --- |
| US-010-01 | As a user, I want bold, lists, and links in a description, so it is easier to scan. |
| US-010-02 | As a user, I want my description to stay editable as plain text. |
| US-010-03 | As a maintainer, I want a proven XSS boundary, so user content cannot run code. |

## 5. Acceptance criteria

### AC-010-01 — Safe markdown renders

- **Given** a description with `**bold**`, `_italic_`, a list, inline code, and an
  `https` link
- **When** the card renders
- **Then** the matching HTML elements appear and the link keeps its `href`.

### AC-010-02 — Raw HTML is stripped

- **Given** a description containing `<script>`, `<iframe>`, or an element with an
  `onerror` handler
- **When** the card renders
- **Then** the element is absent and no handler attribute survives.

### AC-010-03 — Unsafe URLs are blocked

- **Given** a markdown link to `javascript:` or `data:`
- **When** the card renders
- **Then** the anchor has no `href` and the visible text remains.

### AC-010-04 — The editor and the contract are unchanged

- **Given** the inline editor
- **When** the user saves
- **Then** the same `PUT /api/tasks/{id}` payload is sent with the raw text, and
  the stored value is not transformed.

### AC-010-05 — No regression

- **Given** the change set
- **When** the suites run
- **Then** no API, DTO, OpenAPI, or migration change exists and the backend suite
  is green.

## 6. Rules and validation

| Rule | Constraint |
| --- | --- |
| Tags | Only `p br strong em del code pre ul ol li blockquote a h1-h6 hr` plus GFM table tags. |
| Attributes | Only `href` and `title`; `data-*`, `aria-*`, `class`, `id`, and `style` are dropped. |
| URLs | `http(s)`, `mailto`, `#anchor`, and same-site `/path`; `//host` and raw `/\host` lose `href`, as does anything else. |
| Raw HTML | Never trusted; anything outside the allowlist is removed even if `marked` passes it through. |
| Images/media | Not allowed in this slice. |
| Storage | The description stays the user's raw text; parsing happens at render. |

## 7. API contract

No change. The description is still a string of at most 2000 characters in
`TaskResponse` and `UpdateTaskRequest` (ADR-0004).

## 8. Non-functional requirements

- **Security:** allowlist sanitization on every render; no `innerHTML` without
  DOMPurify; no `dangerouslySetInnerHTML` anywhere else.
- **Performance:** the parser and sanitizer live in the lazy task-list chunk.
- **Accessibility:** rendered content is ordinary HTML; links keep their text.
- **Compatibility:** existing plain-text descriptions render as paragraphs.

## 9. Dependencies and assumptions

- **Fact:** descriptions were rendered as one `<p>` (`TaskDescriptionEditor.tsx`).
- **Decision:** `marked` + `dompurify`; both are well maintained and small enough
  for a lazy route.
- **Assumption:** GFM is enough for the demo; no plugins.

## 10. Change log

| Date | Change | Reason | Approved by |
| --- | --- | --- | --- |
| 2026-11-26 | Implemented markdown rendering with an allowlist sanitizer and XSS tests. | Owner-approved backlog (4.2 / G14); ADR-0008 records the decision. | Project owner |
