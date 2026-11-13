# AI Evidence — 004: Authentication UI Slice

- **Date:** 2026-11-11
- **Agent:** frontend (subagent session)
- **Task:** SPEC-003 tasks T-003-03…T-003-06 — session module, `AuthProvider`,
  `RequireAuth`, login page, register page, router extraction, layout.
- **Tool/model:** OpenCode session (DeepSeek V4.1 Flash Free)

## Prompt

The orchestrator asked the frontend subagent to implement the authentication UI
slice of the existing Vite/React client, with these constraints:

- Read `AGENTS.md`, the SDD rules, SPEC-003, PLAN-003, ADR-0002/0004/0005, the
  definition of done, and the existing `project/frontend/src/**`.
- Work only under `project/frontend/`, `docs/ai/`, and the status column of
  `specs/003-web-client/tasks.md`. Do not touch backend code.
- Session type `{ accessToken, expiresAtUtc, user }`, stored in `sessionStorage`
  under `crudworks.session`; only the session module may read or write storage.
- `AuthProvider` exposes `session`, `user`, `isAuthenticated`, `login`,
  `register`, `logout`, and a single idempotent `handleUnauthorized`.
- Login and register use React Hook Form + Zod, real labels, `aria-describedby`,
  `role="alert"`, disabled submit while pending, and preserve input on failure.
- Map `401` login to a safe message, `409` register to a duplicate-email message,
  and `400` ProblemDetails field errors to the matching inputs.
- Extract routes into `AppRoutes.tsx`; `RequireAuth` redirects with `state.from`.
- Mock `fetch` at the boundary in tests; no real network in tests.
- No `any`, no `console.log`, no invented endpoints, responsive at 360/768/1280.

## Output summary

New modules:

- `src/auth/session.ts` — `Session`/`SessionUser` types, `readSession`,
  `writeSession`, `clearSession`, runtime shape guard, expiry check. Malformed or
  expired data is removed on read.
- `src/auth/AuthProvider.tsx` + `src/auth/authContext.ts` — session state seeded
  from storage; `login` posts to `/api/auth/login` and persists the response;
  `register` posts to `/api/auth/register` and returns the created user without
  faking a token; `logout` and `handleUnauthorized` clear storage and state.
- `src/auth/RequireAuth.tsx` — redirects to `/login` with
  `state.from = pathname + search` before rendering children.
- `src/pages/LoginPage.tsx` — RHF + Zod, 401 message, return-path navigation.
- `src/pages/RegisterPage.tsx` — RHF + Zod mirroring ADR-0004, 409 message, 400
  field-error mapping, success redirect to `/login` with a notice and email.
- `src/components/Layout.tsx` — header with brand, display name, and logout;
  logout clears the session and navigates (the provider stays router-agnostic).
- `src/AppRoutes.tsx` — public `/login`, `/register`, protected `/` (existing
  health page for now), catch-all not found. `App.tsx` keeps the providers and
  `BrowserRouter`.

Tests: 21 tests in 6 files, `fetch` stubbed through a shared helper.

## Validation

- [x] Build: `npm run build` → `tsc -b && vite build`, 177 modules,
      `✓ built in 200ms`; `dist/assets/index-t0qChZAe.js` 418.85 kB (gzip 129.77 kB).
- [x] Unit/component tests: `npm run test` → 6 files passed, 21 tests passed.
- [x] Lint: `npm run lint` → exit 0, no findings.
- [x] Manual behavior against the real API (Development, port 5080):
      login with the seeded demo user, reload persistence, logout, duplicate
      registration, fresh registration, login as the new user, wrong password.
- [x] Security review: token only in `sessionStorage` via `session.ts`;
      register stores no token; no secrets or credentials in source; errors use
      fixed safe messages; no `any`; no `console.*`.
- [x] Architecture: one API client (`lib/api.ts`), one auth module, router-agnostic
      provider, no new endpoint or field outside ADR-0004.

### Commands and real output

| Command | Result |
| --- | --- |
| `npx vitest run src/auth/session.test.ts` (red) | 1 file failed: module `./session` did not exist |
| `npx vitest run src/auth/session.test.ts` (green) | 1 file passed, 7 tests passed |
| `npx vitest run src/auth` | 3 files passed, 9 tests passed |
| `npm run test` | 6 files passed, 21 tests passed |
| `npm run lint` | exit 0, no output |
| `npm run build` | 177 modules transformed, built in 200ms |
| `dotnet run --project src\CrudWorks.Api --launch-profile http` | listening on `http://localhost:5080`; migrations applied; demo user seeded |
| Browser, real API | `POST /api/auth/login` 200, path `/`, header `Demo User`, `GET /api/health` healthy |
| Browser, reload | path `/`, header `Demo User`, `crudworks.session` present |
| Browser, logout | path `/login`, header removed, `crudworks.session` null |
| Browser, duplicate register | `[role=alert]` "That email is already registered.", email preserved |
| Browser, fresh register | path `/login`, `[role=status]` "Your account is ready. Sign in to continue.", email prefilled, no session stored |
| Browser, wrong password | `[role=alert]` "Invalid email or password.", email preserved, submit enabled, no session |

The fresh registration created the user
`reviewer.1794450448636@crudworks.local` in the gitignored Development SQLite
database; it is disposable demo data.

### Viewport checks

Same-origin iframes gave each document a real 360/768/1280 layout viewport
(`window.innerWidth` matched the frame width). Measured with
`document.documentElement.scrollWidth` and element rects.

| Page | Width | `scrollWidth` / `innerWidth` | Panel right edge | Result |
| --- | --- | --- | --- | --- |
| `/login` | 360 | 360 / 360 | 344 | no overflow, button visible |
| `/login` | 768 | 768 / 768 | 432 | no overflow |
| `/login` | 1280 | 1280 / 1280 | 544 | no overflow |
| `/register` | 360 | 360 / 360 | 344 | no overflow, button visible |
| `/register` | 768 | 768 / 768 | 432 | no overflow |
| `/register` | 1280 | 1280 / 1280 | 544 | no overflow |

### Accessibility checks

- Labels render as real `<label>` elements; the browser accessibility tree shows
  textboxes named "Email", "Password", "Display name".
- Field errors set `aria-invalid` and `aria-describedby`; the password hint is
  always linked (`register-password-hint`), and the error id is appended when the
  field is invalid.
- Focus order follows DOM order with no `tabindex` overrides: brand link → email →
  password → submit → secondary link.
- `:focus-visible` rules exist for links, inputs, and buttons; a focused email
  input computes `2px solid rgb(31, 111, 92)`.
- **Limitation:** the desktop browser window was not visible, so real Tab key
  traversal and screenshots could not be exercised; the check above is
  structural plus computed styles. `keyboard '{Enter}'` submission is covered by
  a component test (`submits with Enter from the password field`).

## Corrections

| What was first suggested | What was wrong | What changed | Why |
| --- | --- | --- | --- |
| `z.email(...).trim()` | Zod validates the email before the trim transform, so a valid address with spaces fails | `z.string().trim().pipe(z.email(...))` | Trim first, then validate; verified with a Node probe |
| Error id `register-displayName-error` | Mixed casing next to kebab-case ids | `register-display-name-error` | Consistent, predictable ids |
| Password hint without an id | The hint was visible but not programmatically linked | Added `register-password-hint` to `aria-describedby` | Screen readers should hear the rule before submitting |
| Tab title `frontend` | The product name was missing from the browser tab | `<title>CrudWorks</title>` | Visible polish, no behavior change |
| Password-less `register` returning a token | Would fake an auth state the API does not return | Register returns the user; the page navigates to `/login` with a notice | ADR-0004: register returns the user, not a token |
| Clearing the session on any 401 | A login with wrong credentials is a `401`, but there is no session to expire | Login `401` only shows the message; `handleUnauthorized` is for protected calls | Avoid logging out on a failed sign-in |
| Claiming the viewport check with `window.resizeTo` | Blocked for windows not opened by script; no device-emulation tool | Same-origin iframes at exact widths | Produce real layout measurements instead of an unsupported claim |

## Edge cases considered

- Missing, malformed, or non-object session JSON is cleared on read; an invalid
  or past `expiresAtUtc` is treated as expired and cleared.
- Storage access that throws (for example, blocked storage) degrades to `null`
  rather than crashing the app.
- `state.from` is accepted only when it starts with `/` and not `//`, which blocks
  open-redirect style values.
- The form keeps the email after a `401` or `409`; submit is disabled while the
  request is pending, preventing double submission.
- A network failure (`TypeError` from `fetch`) shows a connection-specific
  message instead of blaming the credentials, and keeps the input.
- Unknown `ProblemDetails.errors` keys are ignored; if none match a register
  field, a safe form-level message is shown instead.
- A register success stores no token; the session is created only by login.
- `handleUnauthorized` is idempotent: repeated calls clear storage and produce no
  additional state change.

## Auth and validation decisions

- `src/auth/session.ts` is the only module that touches `sessionStorage`;
  components use the context.
- `AuthProvider` does not import the router. `Layout` performs the logout
  navigation, and `RequireAuth` performs the unauthenticated redirect.
- Client validation mirrors ADR-0004 (display name 2–100, email, password 8–128
  with a letter and a digit) but the API remains authoritative; server `400`
  messages replace the client message on the matching input.
- The server field keys were verified in
  `project/src/CrudWorks.Application/Auth/AuthValidation.cs:26,31,36`
  (`email`, `displayName`, `password`), which matches the client mapping.
- The token is sent only through `apiRequest`'s `token` option; it is never logged
  or placed in a URL.

## Files affected

- `project/frontend/src/auth/session.ts`
- `project/frontend/src/auth/session.test.ts`
- `project/frontend/src/auth/authContext.ts`
- `project/frontend/src/auth/AuthProvider.tsx`
- `project/frontend/src/auth/AuthProvider.test.tsx`
- `project/frontend/src/auth/RequireAuth.tsx`
- `project/frontend/src/auth/RequireAuth.test.tsx`
- `project/frontend/src/components/Layout.tsx`
- `project/frontend/src/pages/LoginPage.tsx`
- `project/frontend/src/pages/LoginPage.test.tsx`
- `project/frontend/src/pages/RegisterPage.tsx`
- `project/frontend/src/pages/RegisterPage.test.tsx`
- `project/frontend/src/AppRoutes.tsx`
- `project/frontend/src/App.tsx`
- `project/frontend/src/App.test.tsx`
- `project/frontend/src/index.css`
- `project/frontend/src/test/setup.ts`
- `project/frontend/src/test/http.ts`
- `project/frontend/src/test/renderApp.tsx`
- `project/frontend/index.html`
- `specs/003-web-client/tasks.md` (status for T-003-03…T-003-06)
- `docs/ai/004-auth-ui-slice.md`

## Review follow-up (2026-11-12)

- **Source finding:** `docs/reviews/review-auth-slice.md`, Minor —
  `project/frontend/src/pages/LoginPage.tsx:29-32`: the `state.from` guard
  rejects `//host` and non-internal values, but no test exercised the
  rejection branch.
- **Change:** added a parameterized negative test to
  `project/frontend/src/pages/LoginPage.test.tsx` —
  `ignores an external return path (%s) and lands on the home route` with the
  cases `//evil.example` (protocol-relative) and `https://evil.example`
  (absolute URL). It renders the app at `/login` with
  `location.state.from` set to the external value, submits valid credentials
  against a mocked successful login, and asserts the app lands on the
  protected home route (health status + `Demo User` header, no
  "Page not found" heading), not the external path.
- **Mutation check (red/green evidence):** temporarily removing the
  `!path.startsWith('//')` condition from `asInternalPath` made the
  protocol-relative case fail (`Unable to find an element with the text:
  /api healthy/i` — the app left the home route); the condition was restored
  immediately and the file is byte-identical to the reviewed version
  (`LoginPage.tsx:31`). No production code changed.
- **Validation:** `npm run lint` → clean; `npm run test` → 6 files / 23 tests
  passed (was 21; +2 parameterized cases); `npm run build` → 177 modules,
  `✓ built in 227ms`.
- **Open question:** none.

## Final decision

Accepted. The auth slice is green and exercised against the real Development API.
Follow-ups are explicitly out of this slice: T-003-07…T-003-10 (task list and
form), T-003-11 (wire `handleUnauthorized` into protected requests), T-003-12
(real keyboard and full responsive pass, including the authenticated header), and
T-003-13 (final evidence run).

## Delegation brief (verbatim)

The orchestrator sent this prompt to the frontend implementer subagent:

```text
Implement the authentication UI slice for SPEC-003 (web client) in the existing IT CrudWorks frontend.

Workspace root: C:\Trabalho\IT_CrudWorks. Work only under project/frontend/, docs/ai/, and the status column of specs/003-web-client/tasks.md.

READ FIRST:
- AGENTS.md
- specs/README.md and specs/constitution.md
- specs/003-web-client/spec.md, plan.md, tasks.md
- docs/adr/0002-auth-strategy.md, 0004-api-contract.md, 0005-frontend-architecture.md
- docs/02-definition-of-done.md (frontend section)
- project/frontend/src/** and package.json

CURRENT FRONTEND FACTS (verified):
- Vite 8 + React 19 + TypeScript 7.
- Installed: react-router-dom 7, @tanstack/react-query 5, react-hook-form 7, zod 4, @hookform/resolvers 7.
- Installed dev: vitest 5, @testing-library/react, @testing-library/jest-dom, @testing-library/user-event, jsdom.
- Scripts: dev, build (tsc -b && vite build), lint (oxlint), test (vitest run), test:watch.
- Current files: src/App.tsx (QueryClientProvider + BrowserRouter + Routes), src/pages/HomePage.tsx (health check), src/pages/NotFoundPage.tsx, src/lib/api.ts (apiRequest + ApiError), src/App.test.tsx (2 passing tests), src/index.css.
- Vite dev server proxies /api to http://localhost:5080.
- Lint, tests, and build are green before you start.

SCOPE: tasks T-003-03 through T-003-06 (session/AuthProvider, RequireAuth, Login page, Register page). Do NOT build task list/form yet, and do NOT touch backend code.

REQUIRED DESIGN:
1. Routing:
   - Extract the route tree into `src/AppRoutes.tsx` so tests can render it inside MemoryRouter.
   - `App.tsx` keeps QueryClientProvider + BrowserRouter + AppRoutes.
   - Routes: `/login` and `/register` public; `/` protected (keep the existing health/welcome content as the interim protected home); `*` NotFound.
   - `RequireAuth` redirects unauthenticated users to `/login` with `state.from` (the attempted path) and no task request.
2. Session module (`src/auth/session.ts`):
   - Type `Session { accessToken: string; expiresAtUtc: string; user: { id: string; email: string; displayName: string } }`.
   - Storage key `crudworks.session` in sessionStorage.
   - `readSession`, `writeSession`, `clearSession`. Handle malformed JSON safely and expired tokens (compare expiresAtUtc to now) by clearing.
   - Only this module reads/writes storage.
3. AuthProvider (`src/auth/AuthProvider.tsx`):
   - Exposes `session`, `user`, `isAuthenticated`, `login(email,password)`, `register(displayName,email,password)`, `logout()`.
   - login calls POST /api/auth/login through `apiRequest`, stores the session, returns.
   - register calls POST /api/auth/register, then automatically logs in? Keep it explicit: register returns the user; the Register page navigates to /login with a success message. Do not fake a token.
   - logout clears the session and navigates to /login (use a callback or let pages handle navigation; document the choice).
   - Provide a single `handleUnauthorized()` that clears the session exactly once; protected pages/hooks call it on ApiError 401.
4. Login page:
   - React Hook Form + Zod: email valid, password required.
   - Labels, `aria-describedby` for errors, `role="alert"` for form-level errors.
   - Loading state, disabled submit while pending, keep email on failure.
   - 401 -> "Invalid email or password." (from the API or a safe fallback).
   - On success navigate to `location.state.from` or `/`.
5. Register page:
   - RHF + Zod: displayName 2–100, email valid, password 8–128 with at least one letter and one digit.
   - 409 -> safe "That email is already registered." message.
   - 400 field errors from ProblemDetails.errors mapped to inputs; keep input on failure.
   - On success navigate to /login with a success message (state or query param).
6. Layout:
   - Simple header/nav showing "CrudWorks", the user's display name, and a logout button when authenticated.
   - Consistent with the existing calm light style; extend index.css with form/button/alert classes. No Tailwind, no component library.
7. 401 handling:
   - A protected request that returns 401 clears the session and redirects to /login once; no retry loop.
   - Keep the mechanism small and centralized.

TESTS (must be real and pass):
- Update/replace App.test.tsx as needed; keep the not-found test.
- `RequireAuth` redirects an unauthenticated user and preserves the target path.
- Login success stores the session and navigates to the target.
- Login 401 shows the message and keeps the email.
- Register 409 shows the duplicate-email message.
- Session module: expired/malformed session is cleared.
- Mock `fetch` at the boundary; no real network. Use MemoryRouter and a fresh QueryClient per test.
- Tests must not leak sessionStorage between cases (clear in beforeEach).

VERIFY AND CAPTURE REAL OUTPUT:
- `npm run lint` -> clean.
- `npm run test` -> all tests pass; report counts.
- `npm run build` -> production build succeeds.
- If a command fails, fix it; do not claim success without output.

DOCS:
- Update the status column of specs/003-web-client/tasks.md for T-003-03..06.
- Write docs/ai/004-auth-ui-slice.md using templates/ai-evidence-log.md: prompt summary, output, commands with real results, corrections, edge cases, files.
- Do not edit specs/003-web-client/spec.md or plan.md; report spec problems instead.

RULES:
- No `any`, no `console.log`, no secrets, no dead code.
- Do not invent endpoints or fields; use ADR-0004 exactly.
- Do not touch project/src/** or project/tests/**.
- No commits, pushes, resets, or clean.
- Responsive at 360/768/1280: forms must be usable and not overflow.

RETURN a handoff with: files created/changed, exact commands and short real output, test names/results, decisions/assumptions, blockers, and the next task. Be specific and honest.
```

