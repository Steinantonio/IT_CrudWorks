# 02 — Definition of Done

A requirement is done only when the evidence exists. "It should work" is not evidence.

- **Last updated:** 2026-11-12, after the wave-1 clean run, the wave-2 coverage
  and accessibility passes, the wave-3 closing passes, and the wave-4 full gate
  on one tree.
- **Evidence index:** `docs/qa/clean-run-report.md` (the wave-5 full gate on the
  current tree, which is the authoritative release evidence),
  `docs/qa/coverage-summary.md` (coverage per file),
  `docs/qa/test-plan.md` (requirement-to-test map),
  `docs/qa/logs/` (captured command output),
  `docs/reviews/final-review.md` (the final review verdict),
  `docs/ai/000`-`docs/ai/020` (GenAI evidence per slice).
- **Counts measured on the current tree (2026-11-25):** 216 backend tests
  (Domain 37, Application 62, Infrastructure 28, Architecture 6, Api 83),
  104 frontend tests, 40/40 API-level E2E checks, 6/6 leak-detector self-tests —
  **366 automated checks, all green**. Domain + Application line coverage was
  100.00% at the last measured gate (floor: 80%); the pagination slice adds unit
  tests but was not re-measured for coverage in this wave. Earlier wave figures
  remain in the historical sections of `docs/qa/` and are superseded by this
  measurement.

## Open items

Three items are intentionally unchecked. Each names the owner and what closes
it. The reviewer gate is closed: the wave-4 final review and the SPEC-008
follow-up both ended without blockers; the follow-up returned **Approve with
comments** (nits only, recorded in `docs/ai/037`).

| Item | Why it is open | Owner | Closes when |
| --- | --- | --- | --- |
| The repository is initialized with a clean commit history | `AGENTS.md` §12 forbids committing without explicit owner approval, and no git repository exists yet. The red→green history is recorded in `docs/ai/` instead. | Orchestrator (needs explicit approval) | The owner approves `git init` and the commit sequence. |
| The demo is rehearsed at least twice | The demo script is written (`docs/presentation.md` §2) but has not been timed end to end. | Orchestrator | The owner rehearses the script twice and records the timings. |
| No browser console warnings during the demo | The console was observed clean in a real Chromium session during the whole demo flow (`docs/ai/015`), but the **rehearsal** itself has not happened yet, so the final check during the live demo is still open. | Orchestrator | The rehearsal happens with the browser console visible and clean. |

### Verified but not fully provable here

| Item | State | Note |
| --- | --- | --- |
| Accessibility (AC-003-10) | Partially satisfied | The keyboard half is fully verified — 18 automated tests plus a real-browser keyboard walkthrough. The screen-reader half cannot be verified in this environment (no assistive technology available); the ARIA markup it depends on is asserted by machine. Recorded in `docs/ai/015`, not hidden. |

## 1. Architecture

- [x] Solution has four backend projects: `Domain`, `Application`, `Infrastructure`, `Api`.
- [x] Project references prove the dependency rule:
  - `Domain` → none
  - `Application` → `Domain`
  - `Infrastructure` → `Application`
  - `Api` → `Application` + `Infrastructure`
- [x] No EF Core, ASP.NET Core, HTTP, or database types in `Domain` or `Application`.
- [x] Controllers are thin; business decisions live in `Application`.
- [x] Repository interfaces live in `Application`; EF Core implementations live in `Infrastructure`.
- [x] The composition root is in `Api`.
- [x] At least three ADRs exist: Clean Architecture, auth strategy, persistence/testing strategy.
- [x] An architecture diagram is in the README.

## 2. Backend and API

- [x] All endpoints in `docs/00-brief.md` are implemented.
- [x] Correct status codes: 200, 201, 204, 400, 401, 404, 409.
- [x] Validation errors use `ProblemDetails` with field-level details.
- [x] `CancellationToken` is passed through async calls.
- [x] OpenAPI JSON is generated and exposed in Development (no UI package; polish is out of scope).
- [x] Health endpoint returns `Healthy` during the demo; the current endpoint has
      no registered database-readiness probe (`Program.cs:27,147-159`).
- [x] No `async void`, `.Result`, `.Wait()`, or fire-and-forget tasks.
- [x] Build passes with warnings as errors.

## 3. Data and persistence

- [x] SQLite is the runtime database; EF Core is the ORM; PostgreSQL is the documented production swap.
- [x] At least two tables: `Users` and `Tasks`.
- [x] `Tasks` has a PK, a FK to `Users`, and at least two business fields.
- [x] Unique index on normalized email.
- [x] Composite index on `Tasks (UserId, CreatedAtUtc, Id)` for keyset pagination; `DueDate` is not indexed at demo scale (documented).
- [x] Migrations are committed and apply from a clean database.
- [x] Seed data includes a demo user and at least five tasks with different statuses.
- [x] Timestamps are stored in UTC.
- [x] No raw SQL unless justified and parameterized.

## 4. Authentication and authorization

- [x] Registration validates email uniqueness and password strength.
- [x] Passwords are hashed with a vetted hasher; plaintext is never stored or logged.
- [x] Login returns a JWT with a short expiry and the required claims.
- [x] `GET /api/auth/me` requires authentication.
- [x] Task endpoints require authentication.
- [x] A user cannot read, update, or delete another user's task (IDOR test).
- [x] Anonymous access is explicitly allowed only on register, login, and health.
- [x] JWT signing key comes from configuration/user-secrets, not source.
- [x] CORS allows only the frontend origin.
- [x] Token storage trade-off is documented in an ADR.

## 5. Business logic and validation

- [x] Title is required and 3–200 characters.
- [x] Description is optional and at most 2000 characters.
- [x] Status must be a valid enum value.
- [x] Due date, when present, is handled consistently (UTC/date-only).
- [x] Ownership is enforced in the application layer, not only in the controller.
- [x] Validation is independent of ASP.NET Core and EF Core.
- [x] Domain invariants are enforced in constructors/factories, not setters.

## 6. Tests

- [x] Domain unit tests cover invariants and status behavior.
- [x] Application unit tests cover validators and use cases with fakes.
- [x] Infrastructure integration tests cover the repository against a real SQLite database (per-test file or shared in-memory connection).
- [x] API integration tests cover every endpoint, including 401/404/409 (no 403 for task ownership).
- [x] Frontend component tests cover the task form, list, and auth guard.
- [x] At least one end-to-end smoke test covers register → login → create → edit → delete.
- [x] Coverage is at least 80% line coverage on `Domain` + `Application`; critical API paths are covered.
- [x] `dotnet test` passes from a clean clone.
- [x] `npm run test` passes from a clean clone.
- [x] TDD evidence exists in the AI/engineering log (red → green → refactor).

## 7. Frontend

- [x] React + TypeScript + Vite app builds with `npm run build`.
- [x] TypeScript is strict; no `any`.
- [x] Login/register, task list, create, edit, and delete flows work.
- [x] Loading, empty, error, and success states exist.
- [x] Forms have labels, inline validation, and keyboard support.
- [x] Responsive at 360 px, 768 px, and 1280 px.
- [x] No console errors or warnings in the browser.
- [x] API base URL comes from an environment variable.
- [x] Auth state is centralized; components obtain the token from the auth
  context, never from storage.
- [x] No dead code or unused dependencies.

## 8. Documentation and demo

- [x] Root `README.md` explains prerequisites, setup, migrations, run, test, and demo credentials.
- [x] README includes the architecture diagram and the project structure.
- [x] README includes curl/HTTP examples for the API.
- [x] `docs/user-story.md` contains the informal user story and acceptance criteria.
- [x] `docs/adr/` contains the key decisions.
- [x] `docs/ai/` contains the GenAI evidence.
- [x] `docs/presentation.md` contains the outline and Q&A prep.
- [ ] The repository is initialized with a clean commit history (requires explicit owner approval; not done without it) or the working tree is presentation-ready.
- [x] A clean clone can reach a running app by following the README.
- [x] The demo script passes end to end.

## 9. GenAI evidence

- [x] The main prompt is saved verbatim, or a faithful summary plus the full delegation brief is saved.
- [x] A representative output or diff is saved.
- [x] The validation steps are described (build, tests, manual, security).
- [x] Corrections and rejected suggestions are described with reasons.
- [x] Edge cases, auth, and validation decisions are explained.
- [x] The narrative distinguishes AI-generated code from human-reviewed code.

## 10. Presentation readiness

- [x] The presentation fits 10–15 minutes.
- [x] The user story, architecture, demo, TDD evidence, and GenAI workflow are covered.
- [ ] The demo is rehearsed at least twice.
- [x] The Q&A sheet covers likely questions:
  - Why Clean Architecture for a small app?
  - Why not MediatR/CQRS?
  - Why JWT in sessionStorage? What would change in production?
  - How do you prevent IDOR?
  - What did the AI get wrong, and how did you catch it?
  - What would you do with more time?
- [ ] No browser console warnings during the demo.

## 11. Final gate

- [x] `dotnet build -warnaserror`
- [x] `dotnet test`
- [x] `npm run lint`
- [x] `npm run test`
- [x] `npm run build`
- [x] The documented clean-reset procedure works from a clean clone.
- [x] The reviewer has no open blockers.
- [x] The requirement traceability matrix has evidence for every row.
