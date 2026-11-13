# 06 — Session Checkpoint

- **Date:** 2026-11-12 (updated after the wave-1 clean run and the closing passes)
- **Purpose:** durable context so a fresh session can resume without re-reading the whole conversation.
- **Deadline:** Friday 2026-11-13 (two working days).
- **Read this first, then** `AGENTS.md`, `specs/README.md`, and `docs/01-wave-plan.md`.

## 1. Objective

Deliver the BLA .NET technical interview exercise as a small, correct, reviewable
full-stack application, using **Spec-Driven Development (SDD)** and an **AI-Driven
Development Life Cycle (AI-DLS)** with a six-agent team:

- A full-stack CRUD task manager.
- .NET 10 API and data layer, Clean Architecture, TDD.
- User registration, login, ownership-scoped tasks.
- Responsive React frontend.
- Seeded demo data and credentials.
- README, ADRs, specs, and an honest GenAI prompt/output/validation narrative.
- A 10–15 minute presentation and code-review Q&A.

Everything lives under `C:\Trabalho\IT_CrudWorks`. All artifacts are in English.

## 2. Hard constraints

- The exercise PDF is the source of truth: `C:\Users\User\Downloads\Net - BLA - Technical Interview Exercise - V6 (1).pdf`.
- English only: code, docs, commits, UI copy, agent output.
- Lean team: 6 agents, one owner per artifact.
- **Spec first:** no implementation without an approved spec and an ordered task.
- No secrets in git. No unverified claims.
- Deadline Friday; cut from the nice-to-have list, never from the dependency rule,
  auth/IDOR, core tests, demo, README, or GenAI narrative.

## 3. Decisions

| ID | Decision | Status | Evidence |
| --- | --- | --- | --- |
| D-01 | Task manager domain: users own tasks | Accepted | `specs/000-product/spec.md` |
| D-02 | Four projects: Domain, Application, Infrastructure, Api | Accepted | `docs/adr/0001-clean-architecture.md` |
| D-03 | JWT bearer + `PasswordHasher<User>`; IDOR returns 404 | Accepted | `docs/adr/0002-auth-strategy.md` |
| D-04 | React + TypeScript + Vite, TanStack Query, RHF + Zod | Accepted | `docs/adr/0005-frontend-architecture.md` |
| D-05 | xUnit + NSubstitute + `WebApplicationFactory` | Accepted | `docs/03-stack-baseline.md` |
| D-06 | No MediatR, CQRS, AutoMapper, generic repository, microservices | Accepted | `AGENTS.md` §4 |
| D-07 | Team: orchestrator, architect, backend, frontend, reviewer, QA | Accepted | `agents/README.md` |
| D-08 | SQLite for the demo, EF Core, PostgreSQL as the documented production swap | Accepted | `docs/adr/0003-persistence-and-test-strategy.md` |
| D-09 | SDD + AI-DLS as the operating method | Accepted | `docs/07-sdd-and-ai-dls.md`, `specs/README.md` |
| D-10 | Toolchain: .NET SDK 10.0.404, Node 26.13.1, npm 11.20.0 | Done | `docs/ai/001-environment-setup.md` |
| D-11 | OpenCode sync via `scripts/sync-agents.ps1` | Done | 6 agents synced and hash-matched |
| D-12 | API contract fixed before implementation | Accepted | `docs/adr/0004-api-contract.md` |

## 4. Completed work

### Wave 0 — Brief and preferences

- PDF extracted into 30 requirement IDs (`R-01`…`R-30`).
- Domain, stack baseline, language, location, team size, and Friday deadline confirmed.

### Wave 1 — Foundation

- `README.md`, `AGENTS.md`.
- Requirements, wave plan, DoD, stack baseline, traceability, smoke tests.
- Nine reusable templates.

### Wave 2 — Core agents

- `agents/orchestrator.md`, `architect.md`, `backend-implementer.md`.
- ADR-0001 Clean Architecture, ADR-0002 Auth.

### Wave 3 — Delivery agents

- `agents/frontend-implementer.md`, `reviewer.md`, `qa-engineer.md`.
- ADR-0003 persistence and test strategy (SQLite).
- AI evidence entries 000 and 001.

### Wave 4 — Sync

- Six agents validated (frontmatter, mode, workspace-prefixed permissions).
- Canonical and synced files hash-match.
- Live smoke tests are folded into the first feature slice.

### Wave 5 — Scaffold and SDD (current)

- Backend solution `project/CrudWorks.slnx` with four source and four test projects.
- `global.json` pins SDK 10.0.404; `Directory.Build.props` enables nullable and
  warnings-as-errors.
- Minimal API: controllers, OpenAPI, ProblemDetails, CORS, health at `/api/health`,
  `public partial class Program`.
- NuGet packages pinned to **10.0.15**.
- React 19 + TypeScript + Vite 8 shell: router, TanStack Query, typed API client,
  Home and NotFound pages.
- Vitest 5 + React Testing Library harness.
- SDD layer: `specs/README.md`, `specs/constitution.md`, SPEC-000…003, plans, task
  lists, templates.
- ADR-0004 API contract, ADR-0005 frontend architecture.
- `docs/07-sdd-and-ai-dls.md` and AI evidence entries 002–004.

### Wave 6 — Authentication slice (current)

- **Backend SPEC-001 complete** (T-001-01…T-001-12): `User`, `AuthService`,
  validation, repository/UoW, EF migration, `PasswordHasher<User>` adapter, JWT
  service, seed demo user, `AuthController`, ProblemDetails mapping.
- **Frontend auth slice complete** (T-003-01…T-003-06): session module,
  `AuthProvider`, `RequireAuth`, login/register pages, 401/409 handling, layout.
- **Review (docs/reviews/review-auth-slice.md):** verdict "Request changes" —
  0 blockers, 1 major (login timing side-channel), 4 minors, 3 nits. All
  findings resolved: constant-time verification (dummy hash on the unknown-email
  path), `ILoginAudit` domain-only logging, return-path negative test with a
  mutation check, overlong-email case, CORS tightened to ADR-0004 §6, `null!`
  accepted and recorded in ADR-0002.
- **Verified after fixes:** build 0 warnings/0 errors; **58 backend tests**
  (15/19/13/11) and **23 frontend tests** pass.
- AI evidence: `docs/ai/003` (backend + review follow-up), `docs/ai/004`
  (frontend + review follow-up), `docs/ai/005` (review), `docs/ai/006`
  (adjudications).
- **Wave 7 running:** SPEC-002 backend (task CRUD + IDOR tests) and the SPEC-003
  task UI (list/form/delete + global 401) in parallel.

### Wave 8 — Clean run and closing passes (2026-11-12)

- **QA clean run from a deleted database.** The API was stopped, `crudworks.db`
  (+ `-shm`/`-wal`) was removed, and the API restarted so migrations and the
  seeder rebuilt the schema and the demo data with no manual step. Health 200.
- **A QA-script false positive was found and fixed.** The end-to-end script's
  C4 check asserted that a `404` body carried no `status` member, but RFC 7807
  `ProblemDetails` always serializes one — the check could never pass. The API
  was correct; the check was wrong. The fix discriminates the numeric
  `ProblemDetails.status` from the task resource's string-enum `status` and is
  proven bidirectionally by `docs/qa/e2e-api-check.test.js` (6 tests).
- **A second QA-script defect was found and fixed:** the script was not
  idempotent (it left the ownership-probe task behind, so a re-run failed four
  count checks). It now cleans up and asserts the cleanup (new check G2).
- **Measured evidence:** 143 backend tests, 51 frontend tests, 37/37 live
  API-level end-to-end checks, build with warnings as errors clean, lint clean,
  production build green. Line coverage on Domain + Application: **96.55%**
  (Domain 100.00%, Application 94.92%) against the 80% DoD floor.
- **Specs closed:** SPEC-001 and SPEC-002 moved from `approved` to
  `implemented` (per `specs/README.md` §2). SPEC-003 closes with the frontend
  accessibility pass.
- **Documentation reconciled:** `docs/04-requirement-traceability.md` (all 30
  rows carry evidence), `docs/02-definition-of-done.md` (86 checked, 5 open
  items with named owners), `docs/presentation.md` (stale counts and
  "TO UPDATE" markers replaced with the measured numbers), root and
  `project/README.md` (setup, coverage, E2E check, accurate status).
- **AI evidence:** `docs/ai/012-qa-clean-run.md`, `docs/ai/013`, `docs/ai/014`,
  `docs/ai/015`, `docs/ai/016`, `docs/ai/017`, `docs/ai/018`.

### Wave 9 — Coverage and critical-path closure (2026-11-12)

- **QA-03 closed.** The 12 uncovered `Application` lines were confirmed by
  reading the two source files: `AuthValidation.cs:47-49` (invalid email on
  login) and `TaskValidation.cs:52-54` / `:57-59` / `:68-70` (invalid title,
  description and due date on update). 10 new unit cases cover them.
  `CrudWorks.Application` went from 94.92% (224/236) to **100.00% (236/236)**;
  Domain + Application combined is now **348/348 = 100.00%** against the 80%
  DoD floor.
- **No production code changed.** The behaviour was already correct — only
  untested. The two source files the backend touched were restored
  byte-identically, proven by SHA-256 (re-verified independently by the
  orchestrator against the hashes published in `docs/ai/013`).
- **QA-04 closed for every critical path.** 12 new API cases cover the RFC 7807
  contract, the generic `500` branch, CORS, the health body, `JwtSigningKey`
  resolution in every branch, a non-GUID `sub`, and the OpenAPI document.
  `CrudWorks.Api` hand-written sources are **229/229 = 100.00%**; the assembly
  total is 364/613 (59.38%) because the denominator is dominated by
  compiler-generated OpenAPI output.
- **Two factual errors were found in the backend's own evidence entries** by
  the QA engineer, and corrected by their author after independent
  verification: the uncovered migration lines are the **`Down` (rollback)
  bodies**, not the `Up` methods; and `PasswordHasherAdapter` was already at
  100%, so it was never an open gap. The orchestrator confirmed both by
  reading the code.
- **Measured:** 165 backend tests (37/57/25/46), 0 failures, build with
  warnings as errors clean.
- **Test plan written:** `docs/qa/test-plan.md` maps every requirement and
  acceptance criterion to the real test names read from the source.
- **AI evidence:** `docs/ai/013`, `docs/ai/014`.

## 5. Current state

| Item | State |
| --- | --- |
| Documentation | Foundation, ADRs 0001–0005, AI entries 000–014, QA artifacts under `docs/qa/` (clean-run report, coverage summary, test plan, tooling) |
| Specs | 000 product, 001 auth (**implemented**), 002 task CRUD (**implemented**), 003 web client (approved; closing) |
| Agents | 6 defined, validated, synced, spec-aware |
| Backend | SPEC-001 and SPEC-002 implemented; 165 tests pass; Domain + Application coverage 100%; critical API paths covered |
| Frontend | Scaffold + auth slice + task UI; task UI tests pass; accessibility/responsive pass closing |
| Review | Auth-slice and task-slice reviews closed, all blockers and majors resolved; the coverage entries corrected after a factual review |
| Git | No repository initialized; nothing committed (needs explicit owner approval) |
| Next | The final review has run and returned **Approve with comments** (0 blockers, 2 majors now closed, 6 minors, 5 nits); the DoD item "The reviewer has no open blockers" is now checked. What remains is the owner's decision: (1) `git init` and the commit sequence, (2) the demo rehearsal with timings, (3) the browser console observed during that rehearsal. |

## 6. Environment (verified 2026-11-11)

| Tool | Version | State |
| --- | --- | --- |
| .NET SDK | 10.0.404 (also 9.0.101, 9.0.201) | Ready; pinned in `project/global.json` |
| Node.js | 26.13.1 LTS | Ready; prepend `C:\Program Files\nodejs` to PATH per shell |
| npm | 11.20.0 | Ready after `RemoteSigned` for CurrentUser |
| Git | 2.49.0.windows.1 | Ready |
| SQLite | via EF Core provider | No install or service needed |
| Docker / PostgreSQL | not installed | Not required for the chosen path |

## 7. Database decision

Criterion: **"it must be something that's easy to recreate."**

Decision: **SQLite** with EF Core.

- Reset: stop the API, delete `crudworks.db`, start the API; migrations and seed
  recreate everything.
- Tests use SQLite per-test databases; Docker and Testcontainers are not on the
  critical path.
- PostgreSQL remains a documented provider swap in ADR-0003.
- Trade-off: SQLite is not the production target; provider behavior and
  concurrency must be validated if promoted.

## 8. SDD and AI-DLS in one paragraph

The chain is **Brief → Constitution → Spec → Plan → Tasks → Red test →
Implementation → Validation → Evidence → Learning**. Each phase has an artifact
and an owner: the architect writes specs and plans; the orchestrator sequences and
approves; implementers do red/green; the reviewer checks the diff against the
spec; QA maps acceptance criteria to evidence. An agent works only from an
approved spec, and a behavior change updates the spec change log first. Full
details: `docs/07-sdd-and-ai-dls.md`.

## 9. Next actions

1. Close the frontend accessibility and responsive pass (SPEC-003, T-003-12 and
   T-003-13) and move SPEC-003 to `implemented`.
2. Run the **final review** of the whole change set against
   `docs/02-definition-of-done.md`, with the evidence already captured. The
   verdict must have no open blockers.
3. **Initialize git and propose the commit sequence.** `AGENTS.md` §12 forbids
   committing without the owner's explicit approval; the red→green history is
   currently recorded in `docs/ai/` instead of in commits. This is the only
   outstanding item that needs a decision from the owner.
4. Rehearse the demo twice and record the timings (`docs/presentation.md` §2).
5. Verify the browser console is clean during the demo — the only DoD item that
   no automated check in this repository can close.

Closed and no longer action items:

- QA-03 (the 12 uncovered `Application` lines) — closed in wave 9;
  `CrudWorks.Application` is at 100.00%.
- QA-04 (the critical `CrudWorks.Api` paths) — closed in wave 9; every
  hand-written API line is covered.
- The formal test plan — `docs/qa/test-plan.md`.
- The traceability matrix, the DoD checklist, the presentation and both READMEs
  — reconciled with the measured numbers in wave 8.

## 10. How to orchestrate

1. Restart OpenCode or start a new session so it reloads `.opencode/agents/`.
2. Select `it_crudworks/orchestrator` as the primary agent.
3. Give it an objective with a spec and task range, not an implementation.
4. Invoke `it_crudworks/architect`, `backend-implementer`,
   `frontend-implementer`, `reviewer`, and `qa-engineer` as subagents.
5. After editing an agent file, run `powershell -File scripts/sync-agents.ps1`.

The canonical files are in `agents/`; `.opencode/` holds generated copies.

## 11. File map

```text
IT_CrudWorks/
├── README.md
├── AGENTS.md
├── specs/
│   ├── README.md, constitution.md
│   ├── 000-product/spec.md
│   ├── 001-auth/{spec,plan,tasks}.md
│   ├── 002-task-crud/{spec,plan,tasks}.md
│   ├── 003-web-client/{spec,plan,tasks}.md
│   └── _template/{spec,plan,tasks}.md
├── docs/
│   ├── 00-brief.md … 07-sdd-and-ai-dls.md
│   ├── adr/0001…0005
│   └── ai/000…002
├── templates/                    # 9 reusable templates
├── scripts/sync-agents.ps1
├── project/
│   ├── CrudWorks.slnx, global.json, Directory.Build.props
│   ├── src/                      # Domain, Application, Infrastructure, Api
│   ├── tests/                    # four xUnit projects
│   └── frontend/                 # React app, Vitest, build
└── agents/                       # canonical agent contracts
```

## 12. Resume protocol

1. Read this file, then `AGENTS.md` and `specs/README.md`.
2. Check `docs/01-wave-plan.md` and the task lists in `specs/NNN-*/tasks.md`.
3. Check `docs/04-requirement-traceability.md` for coverage.
4. Verify the environment with `dotnet --list-sdks`, `node -v`, `npm -v`.
5. Run `dotnet build CrudWorks.slnx -warnaserror`, `dotnet test`, and the frontend
   `lint`/`test`/`build` before changing code.
6. Continue from §9; do not redo completed waves.
7. Update this file at the end of every wave.
