# Presentation — CrudWorks

- **Format:** 10–15 minutes + Q&A
- **Status:** ready — not yet rehearsed; evidence refreshed to the wave-5 gate
  (`docs/qa/clean-run-report.md` §"Wave 5", `docs/ai/020`).
- **Owner:** orchestrator
- **Seed credentials:** `demo@crudworks.local` / `Demo123!` (development only)

## 1. Hook and user story (2 min)

> "I keep my work in notes, chat messages, and a paper pad. Every Monday I spend
> more time reconstructing what I promised than doing it. I do not need a project
> suite; I need one private place to write down what I owe and close it when it is
> done."

- The user: a busy professional with a small, private task list.
- The promise: register, sign in, manage only your own tasks, from phone or laptop.
- The constraint that makes it interesting: **ownership is a security invariant**,
  not a UI filter.
- Full story: `docs/user-story.md`.

## 2. Demo (4 min)

Run from a clean database. Show, in this order:

1. **Signed out** — open `/`; the guard redirects to `/login` and sends no task
   request.
2. **Register** — create a fresh user; show the safe duplicate-email message by
   trying `demo@crudworks.local` once.
3. **Log in** — sign in with the seeded demo account, using the **Use demo
   account** button on the login screen (it fills `demo@crudworks.local` /
   `Demo123!`); point out the header with the display name and the logout action.
4. **Task list** — show the seeded tasks and the empty/loading/error states.
   The clean run shows 5 seeded tasks, newest first, with the status badge
   (`2 Todo / 2 InProgress / 1 Done`).
5. **Create** — title, description, status, due date; validation messages appear
   inline; the new task is visible immediately.
6. **Edit** — change title and status; show that `createdAtUtc` stays and
   `updatedAtUtc` changes.
7. **Delete** — confirmation dialog; cancel once, then confirm; the row disappears.
8. **Ownership** — with a second user, request the first user's task id; show the
   `404`. The API never confirms that the resource exists.
9. **Logged out** — reload a protected route; redirect to `/login`.
10. **Operational evidence** — `GET /api/health` returns `200`; OpenAPI is exposed
    in Development; the SQLite file is recreated by migrations + seed.

**Fallback:** if the live demo fails, show the API tests and the captured command
output. Never pretend a step ran.

## 3. Architecture (3 min)

- Four projects, one dependency direction:
  `Domain ← Application ← Infrastructure`, with `Api` as the composition root.
- Why Clean Architecture for a small app: the panel can point at any behavior and
  see where it lives; the Application layer is testable without a database or HTTP;
  the dependency rule is enforced by project references, not convention.
- Data model: `Users` (unique normalized email, password hash) and `Tasks`
  (owner FK, title, description, status, due date, timestamps), with an index on
  `Tasks.UserId`.
- Auth flow: register → validate → PBKDF2 hash → persist; login → verify → JWT
  (HMAC-SHA256, 60 min, `sub` = user id); protected request → validate token →
  resolve the user from `sub` → scope the query by owner.
- Deliberate exclusions: MediatR, CQRS, AutoMapper, generic repository,
  microservices. Each would add indirection without serving this story.

## 4. Clean Architecture walkthrough (2 min)

Show these four files, in this order:

1. `project/src/CrudWorks.Api/Controllers/AuthController.cs` — a thin controller:
   binds, delegates, maps the result.
2. `project/src/CrudWorks.Application/Auth/AuthService.cs` — validation,
   normalization, hashing, duplicate check, generic `401`.
3. `project/src/CrudWorks.Application/Users/IUserRepository.cs` — the port lives in
   Application; the EF implementation lives in Infrastructure.
4. `project/src/CrudWorks.Domain/Users/User.cs` — the invariant (normalized email,
   required fields) is enforced in the entity, not the controller.

Then show the project references and note that Domain has no framework packages.

## 5. TDD and test evidence (2 min)

- One red → green example: the session module test failed because `./session` did
  not exist, then passed after the module was implemented; the backend has the same
  pattern recorded in `docs/ai/003-auth-slice.md`.
- Pyramid:
  - Domain: invariants and normalization.
  - Application: use cases with substitutes (no database).
  - Infrastructure: real per-test SQLite, unique index, migration, JWT round-trip.
  - API: `WebApplicationFactory` with a temp SQLite file; status codes, ProblemDetails,
    `Location`, IDOR.
  - Frontend: Vitest + RTL with `fetch` mocked at the boundary; guard, forms,
    session expiry, 401 seam.
- Real counts, measured back to back on one tree: **166 backend tests**
  (Domain 37, Application 57, Infrastructure 25, Api 47), **80 frontend
  tests**, **37/37 API-level end-to-end checks** against a freshly seeded
  database, and **6/6** self-tests of the IDOR leak detector — **289 automated
  checks, all green**. 0 warnings with `-warnaserror`. Line coverage on
  Domain + Application: **100%** (Domain 112/112, Application 236/236)
  against an 80% floor.
- What is not tested: visual regressions, load, real email delivery (out of scope),
  screen readers (no assistive technology available), and the automated
  axe/Lighthouse audit. The browser console was observed clean in a real
  Chromium session during the whole demo flow.

## 6. SDD and AI-DLS (2 min)

- The chain: Brief → Constitution → Spec → Plan → Tasks → Red test → Implementation
  → Validation → Evidence → Learning.
- Artifacts in the repository: `specs/constitution.md`, `specs/001-auth/`,
  `specs/002-task-crud/`, `specs/003-web-client/`, `docs/adr/0004-api-contract.md`,
  `docs/07-sdd-and-ai-dls.md`.
- Each phase has an owner and a gate: the architect writes the spec; the orchestrator
  approves and sequences; implementers do red/green; the reviewer checks the diff
  against the spec; QA maps acceptance criteria to evidence.
- Rule: **no implementation from a draft spec**, and a behavior change updates the
  spec change log first.

## 7. GenAI narrative (2 min)

Be specific; do not say "the AI wrote it".

- **Prompt:** a delegation brief naming the spec, task IDs, contract, security
  rules, and the exact evidence required.
- **Output:** Domain/Application/Infrastructure/API code and tests; React auth
  module, pages, and tests.
- **Validated:** build with warnings as errors, **246 automated tests** (166
  backend + 80 frontend), a **37/37** live API end-to-end run from a clean
  database, a real-browser keyboard walkthrough against the live API, and two
  reviews against the contract.
- **Corrected (real examples):**
  - Package versions resolved to a newer target framework; pinned to `10.0.15`.
  - The repository saved implicitly; added `IUnitOfWork.SaveChangesAsync`.
  - SQLite returned `Unspecified` timestamps; added a UTC value converter.
  - `Location` was absolute; changed to the relative `/api/auth/me`.
  - Zod trimmed the email after validation; moved the trim before the check.
  - A test rendered `App` inside a second router; tested the page directly instead.
  - The task repository read with `AsNoTracking`, so every `PUT` mutated a
    detached entity and persisted nothing — the `200` was in-memory only. A
    review pin caught it; the write path was corrected.
  - `Enum.TryParse` accepted `"status": "1"`, bypassing the contract's
    three-name rule; replaced with an explicit case-sensitive whitelist.
  - My own QA script asserted that a `404` body had no `status` member — but
    RFC 7807 always serializes one, so the check could never pass. The API was
    correct; the check was wrong. The fix discriminates by type and value and
    is proven to still catch a real leak.
- **Rejected:** MediatR/CQRS, AutoMapper, a generic repository, Tailwind/component
  library, storing the token in `localStorage`, and inventing task fields not in
  ADR-0004.
- **Not delegated:** scope, security trade-offs, the final verdict, and the decision
  to cut features.

## 8. Trade-offs and lessons (1 min)

- **SQLite** for a one-command reproducible demo; PostgreSQL is the documented
  production swap (ADR-0003). Cost: provider-specific behavior must be revalidated.
- **JWT in `sessionStorage`** for a cross-origin SPA demo; XSS exposure is real and
  documented. Production: httpOnly/Secure/SameSite cookie + refresh rotation + CSRF.
- **No pagination/search** because the demo dataset is small; adding them would
  expand the contract and the test surface without serving the story.
- **No refresh tokens or revocation**; 60-minute tokens are enough for the demo and
  the limitation is explicit.
- Lesson: the spec caught scope creep earlier than code review would have; the AI
  was fastest at scaffolding and tests, and wrong most often at framework details
  and defaults.

## 9. Q&A preparation

### Likely questions and short answers

- **Why Clean Architecture for a small app?** Not for scale; for reviewability and
  testability. The dependency rule makes the business rules independent of EF and
  HTTP, and the panel can locate any behavior quickly.
- **Why not MediatR/CQRS?** Five use cases do not need a dispatcher or separate
  read/write models. The indirection would obscure the flow and add packages.
- **Why JWT in `sessionStorage`?** It is a deliberate demo trade-off across origins;
  the XSS risk and the production cookie alternative are in ADR-0002. The app never
  injects untrusted HTML.
- **How is IDOR prevented?** The owner id comes only from the validated `sub` claim;
  every query is scoped by it; a foreign id returns `404`, not `403`; tests cover
  read, update, and delete.
- **What did the AI get wrong?** Framework/version details, implicit-save
  repository semantics, UTC handling in SQLite, and validation order. Each is
  recorded with the correction in `docs/ai/`.
- **Are the tests meaningful?** They assert observable behavior: status codes,
  response bodies, ownership, ordering, session clearing. The API tests use a real
  HTTP pipeline and a real SQLite database, not mocks of the thing under test.
- **What would you do with more time?** Refresh-token rotation, rate limiting,
  structured audit logging, PostgreSQL, Playwright E2E, and pagination.
- **How would you deploy it?** Containerize the API, serve the built SPA from a CDN
  or static host, PostgreSQL, secrets from the platform, HTTPS only, and a CI gate
  on build + tests + lint.

### Demo fallback

- API tests and captured command output are the evidence. If the UI fails, do not
  improvise a success claim; show the failing state and the test that covers it.

## 10. Closing line

> "The application is small on purpose. The process is the point: a spec that
> predicts the behavior, tests that prove it, an architecture that makes the
> boundaries visible, and an honest record of where the AI helped and where it was
> wrong."
