# 04 — Requirement Traceability

Every requirement from `docs/00-brief.md` has an owner, a deliverable, and a verification method. A row is only "Done" when the evidence exists.

| ID | Requirement | Deliverable | Verification | Owner | Status |
| --- | --- | --- | --- | --- | --- |
| R-01 | .NET C# web app with API and data layer | `project/` solution | Build + run + README | Orchestrator | Done — solution builds with warnings as errors; API runs from a clean database (health 200); setup in `project/README.md` |
| R-02 | Clean Architecture | Four projects + ADR-0001 | Project references + review | Architect | Done — references wired (`Domain` → none, `Application` → `Domain`, `Infrastructure` → `Application`, `Api` → both); verified independently in `docs/reviews/review-task-slice.md` |
| R-03 | TDD | Tests written before/with code | Red/green records + AI log + review | Backend implementer | Done — red→green recorded per slice in `docs/ai/003`, `007`, `011`, `013`, `014`; no git history yet (see R-26 note) |
| R-04 | Informal user story | `docs/user-story.md` | Review + presentation | Orchestrator | Done — `docs/user-story.md`, accepted, with plain-language acceptance criteria |
| R-05 | CRUD via API | Task endpoints | API integration tests | Backend implementer | Done — `GET/POST/PUT/DELETE /api/tasks` implemented and tested; 37/37 live E2E checks (`docs/qa/clean-run-report.md` §"Wave 5") |
| R-06 | User creation, login, persisted user | Auth endpoints + Users table | API tests + DB check | Backend implementer | Done — register/login/me implemented and tested; `Users` table with unique normalized email |
| R-07 | Two tables/objects | `Users`, `Tasks` migrations | Migration + schema review | Backend implementer | Done — `InitialCreate` + `AddTasks` migrations; FK and `IX_Tasks_UserId` enforced by tests |
| R-08 | PK + at least two fields | Task entity | Domain test + migration | Backend implementer | Done — `TaskItem` with PK, owner FK, title/description/status/dueDate; domain tests; `Domain` coverage 100% |
| R-09 | Correct verbs, params, returns | Controllers + OpenAPI | API tests + OpenAPI review | Backend implementer | Done — task + auth routes pinned by API tests; `Location`, 204, and every status code verified |
| R-10 | Second API with auth and anonymous endpoints | Auth controller + `[Authorize]`/`[AllowAnonymous]` | API tests | Backend implementer | Done — register/login anonymous, `/me` authorized; 401 ProblemDetails tested |
| R-11 | Data access layer with CRUD | Repository interfaces + EF implementations | Integration tests | Backend implementer | Done — `ITaskRepository`/`IUserRepository` ports; EF repos integration-tested against real SQLite |
| R-12 | Business logic independent of data/API | Application services + validators | Project references + unit tests | Architect | Done — `AuthService`/`TaskService` + validators in Application; unit-tested with substitutes; no EF/HTTP types |
| R-13 | Unit tests for data, business, API | Test projects | `dotnet test` + coverage | QA engineer | Done — 166 backend tests (37/57/25/47) and 80 frontend tests; coverage 100% on Domain + Application (`docs/qa/coverage-summary.md`) |
| R-14 | Frontend framework integrated | React app | Build + demo | Frontend implementer | Done — React 19 + Vite 8; auth UI exercised against the real API |
| R-15 | Responsive and user-friendly | UI + responsive layout | Manual viewport check + tests | Frontend implementer | Done — auth screens use a centered `AuthLayout` (measured at 360/768/1280 with no horizontal overflow); task list/form polished; keyboard/accessibility assertions in the component suite plus a real-Chromium walkthrough (`docs/ai/015`, `docs/ai/020`) |
| R-16 | CRUD in the UI | Task pages/forms | E2E or component tests | Frontend implementer | Done — list/form/delete implemented with 80 frontend tests + a real-browser CRUD walkthrough against the live API (`docs/ai/015`, `docs/ai/020`) |
| R-17 | Structured components/state | Feature folders + TanStack Query | Code review | Frontend implementer | Done — feature-first structure per ADR-0005; one API client, one auth module, TanStack Query for server state |
| R-18 | README with setup/docs | Root README | Clean-clone walkthrough | Orchestrator | Done — root + `project/README.md` with setup, reset, tests, coverage, E2E check and API summary |
| R-19 | Seeded data/credentials | Seed + README | Clean-clone demo | QA engineer | Done — clean-database reset verified: migrations + seed recreate the demo user and 5 story-shaped tasks across all statuses, all with descriptions and due dates (`docs/ai/019`) |
| R-20 | GenAI prompt written | `docs/ai/000`–`018` | Review | Orchestrator | Done — prompt summaries, delegation briefs and per-slice records through entry 018 |
| R-21 | Output code shown | `docs/ai/003`, `004`, `007`, `008`, `020` | Review | Orchestrator | Done — output summaries and representative diffs recorded per slice; `docs/ai/020` includes the delivered `AuthLayout`/seed code samples and the before/after corrections |
| R-22 | Validation/corrections/edge cases | `docs/ai/*` + review notes | Review + presentation | Reviewer | Done — corrections and edge cases recorded per slice, including the QA-script false positive in entry 012 and the byte-identical restore proof in entry 013 |
| R-23 | Presentation of story/design/architecture/demo | `docs/presentation.md` | Rehearsal | Orchestrator | Done — outline, demo script and Q&A sheet; counts reconciled with the measured evidence |
| R-24 | Code review Q&A readiness | Q&A sheet | Mock review | Reviewer | Done — eight likely questions with defensible answers in `docs/presentation.md` §9 |
| R-25 | Clean Architecture criterion | ADR-0001 + code | Reviewer checklist | Architect | Done — dependency rule verified from the project files in the task-slice review |
| R-26 | Testing/TDD criterion | Test plan + coverage | `dotnet test` + report | QA engineer | Done — 166 backend + 80 frontend tests measured together on the current tree with 37/37 live E2E checks (289 automated checks, all green); coverage report with the exact uncovered lines; red/green evidence in `docs/ai/`; formal test plan in `docs/qa/test-plan.md` |
| R-27 | Code quality criterion | Lint/build gates | `-warnaserror` + review | Reviewer | Done — 0 warnings/0 errors with `-warnaserror`; oxlint clean; both slice reviews resolved (1 major fixed each) |
| R-28 | Functionality criterion | Demo | Demo run + console check | QA engineer | Done — clean-database run: register → login → create → edit → delete all pass; 37/37 live E2E checks |
| R-29 | Presentation criterion | Outline + rehearsal | Timed rehearsal | Orchestrator | Done — outline fits the 10–15 minute budget; script and fallback recorded |
| R-30 | GenAI fluency criterion | AI evidence + narrative | Reviewer + presentation | Orchestrator | Done — 19 evidence entries with prompt, output, validation, corrections and rejected suggestions |

## Status legend

- **Planned:** not started.
- **In progress:** owner is working.
- **Done:** evidence exists and is linked.
- **Blocked:** an open question or dependency prevents progress.

## Current scaffold evidence

Captured 2026-11-11, before the first feature slice:

| Check | Command | Result |
| --- | --- | --- |
| Backend build | `dotnet build CrudWorks.slnx -warnaserror` | 0 warnings, 0 errors |
| Backend tests | `dotnet test CrudWorks.slnx` | 1 passed, 0 failed (`HealthTests.GetHealth_ReturnsOk`) |
| Frontend lint | `npm run lint` | clean |
| Frontend tests | `npm run test` | 2 passed (`App shell`, `NotFoundPage`) |
| Frontend build | `npm run build` | 71 modules, `dist/` produced |

The feature requirements (R-05, R-06, R-07, R-09…R-12, R-16, R-19) remain
**Planned** until SPEC-001 and SPEC-002 are implemented and tested. The specs are
in `specs/001-auth/` and `specs/002-task-crud/`.

## Auth slice evidence

Captured 2026-11-11 after SPEC-001 (backend) and the SPEC-003 auth tasks
(T-003-01…T-003-06):

| Check | Command | Result |
| --- | --- | --- |
| Backend build | `dotnet build CrudWorks.slnx -warnaserror` | 0 warnings, 0 errors |
| Backend tests | `dotnet test CrudWorks.slnx` | 52 passed, 0 failed (15 Domain, 15 Application, 11 Infrastructure, 11 Api) |
| Frontend lint | `npm run lint` | clean |
| Frontend tests | `npm run test` | 6 files, 21 tests passed |
| Frontend build | `npm run build` | 177 modules, `dist/` produced |
| Live API | Development run, temp SQLite | register `201` + `Location: /api/auth/me`; duplicate `409`; invalid `400` with fields; login `200`; unknown/wrong `401` identical; `/me` `200` with token, `401` without |
| Live UI | Vite + real API | demo login, reload, logout, duplicate `409`, fresh registration, wrong password `401` |
| Viewports | 360 / 768 / 1280 | login and register: no horizontal scroll; primary action visible |

Full command output and corrections: `docs/ai/003-auth-slice.md` and
`docs/ai/004-auth-ui-slice.md`. SPEC-002 (task CRUD) is implemented and tested
(`docs/ai/007-task-backend-slice.md`); the task UI (SPEC-003 T-003-07…) is
implemented with a real-API smoke run (`docs/ai/008-task-ui-slice.md`).

## Clean-run evidence (wave 1, 2026-11-12)

Captured from a **deleted and recreated** SQLite database: the API was stopped,
`crudworks.db` (+ `-shm`/`-wal`) was removed, and the API was restarted so the
migrations and the seeder rebuilt the schema and the demo data with no manual
step.

| Check | Command | Result |
| --- | --- | --- |
| Backend build | `dotnet build CrudWorks.slnx -warnaserror` | 0 warnings, 0 errors |
| Backend tests | `dotnet test CrudWorks.slnx` | 166 passed, 0 failed, 0 skipped (Domain 37, Application 57, Infrastructure 25, Api 47) |
| Gate provenance | one full gate, wave 4 | backend + frontend + live API run back to back on one tree; tree pinned by a SHA-256 fingerprint over its 116 source files |
| Coverage | `dotnet test CrudWorks.slnx --collect:"XPlat Code Coverage"` + `node docs/qa/tools/coverage-report.js --verify --write` | Domain 100.00% (112/112), Application 100.00% (236/236), **combined 348/348 = 100.00%** vs. the 80% floor — `docs/qa/coverage-summary.md` |
| Frontend lint | `npm run lint` | clean (oxlint, exit 0) |
| Frontend tests | `npm run test` | 11 files, 80 tests passed |
| Frontend build | `npm run build` | 183 modules, `dist/` produced |
| Live API E2E | `node docs/qa/e2e-api-check.js` | **37/37 checks passed**, exit 0; idempotent across two consecutive runs — `docs/qa/clean-run-report.md` §"Wave 5" |
| Leak-detector self-test | `node --test docs/qa/e2e-api-check.test.js` | 6 tests, 6 pass (proves a real leak is still detected) |
| Clean reset | `node docs/qa/tools/api-ctl.js reset` | PID stopped, 3 database files deleted, health 200 `{"status":"Healthy"}` |
| IDOR seams | part of the 37 checks | foreign task read/update/delete → 404 (C4/C5/C6); anonymous on every task route → 401 (E1–E5); client-supplied `userId` ignored (G1) |
| Validation matrix | part of the 37 checks | short title, overlong title, overlong description, invalid due date, `status` `"1"`, `status` `"todo"` → 400 with the contract message and the right `errors` key (D1–D6) |

The wave-1 run measured 143 backend tests; the wave-2 coverage pass added 22
(10 Application cases for the QA-03 branches, 12 API cases for the critical
paths), giving 165. The production code was not changed — the two source files
the backend touched were restored byte-identically, proven by SHA-256
(`docs/ai/013`). Wave 5 added one API test for the routing-level `404`
(`ProblemDetails_ForAnUnknownRoute_Returns404AsProblemJson`), bringing the
current figure to **166**.

Full report: `docs/qa/clean-run-report.md`. Test plan: `docs/qa/test-plan.md`.
Evidence entries: `docs/ai/012`, `docs/ai/013`, `docs/ai/014`.

### Known gaps, with owners

| Gap | Severity | Owner | Where |
| --- | --- | --- | --- |
| No git repository, so the red→green history is recorded in `docs/ai/` instead of in commit history | Major (process) | Orchestrator (needs explicit approval) | R-03, R-26 |
| 21 uncovered Infrastructure lines (`UnitOfWork.cs:34-35`, the design-time `DbContext` factory body, the two migration `Down` rollback bodies, and 5 `DatabaseSeeder` branches) | Minor | Backend implementer | `docs/ai/013`, `docs/qa/coverage-summary.md` |
| `CrudWorks.Api` line coverage is 59.38% — the hand-written code is fully covered, the denominator is dominated by compiler-generated OpenAPI output | Minor | Backend implementer | QA-04 in `docs/ai/014` |
| No CI pipeline (`.github/workflows/`) — the evidence above is local | Minor | QA engineer | QA-05 in `docs/ai/012` |
| No screen-reader verification and no automated axe/Lighthouse audit; only Chromium was exercised, and the contrast was computed by hand | Minor | Frontend implementer | `docs/ai/015` |

## Evidence rules

- Link to a file, test name, command output, or ADR. Do not write "done" without a link.
- The orchestrator updates the status at the end of each wave.
- The reviewer checks the matrix before the final verdict.
