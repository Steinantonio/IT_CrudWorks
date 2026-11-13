# IT CrudWorks

**An agent playbook for the BLA .NET technical interview exercise.**

IT CrudWorks is a self-contained workspace for building a full-stack CRUD task manager with .NET 10, SQLite, React, Clean Architecture, TDD, and a documented GenAI workflow. Development follows **Spec-Driven Development (SDD)** and an **AI-Driven Development Life Cycle (AI-DLS)**: specs define behavior, agents own phases, and every claim is backed by a test or a command output. The goal is not just to ship the exercise; it is to make every design decision, test, and AI correction explainable in a code review.

## Status

| Wave | Scope | Status |
| --- | --- | --- |
| 0 | Brief intake and preferences | Complete |
| 1 | Foundation: contract, brief breakdown, traceability, DoD, stack baseline, templates | Approved |
| 2 | Core agents: orchestrator, architect, backend implementer | Approved |
| 3 | Delivery agents: frontend implementer, reviewer, QA engineer | Delivered |
| 4 | Smoke test + OpenCode sync | Complete — six agents synced and validated |
| 5 | Build and rehearse the actual exercise | In progress — auth slice, task CRUD, and task UI are implemented, reviewed, and green; the clean-database run and the closing passes are done |

**Deadline: Friday 2026-11-13.** The plan is compressed for two working days; see `docs/00-brief.md` §7 for the must-have/cut list.

### Where the application stands

| Area | State | Evidence |
| --- | --- | --- |
| Backend | Auth + task CRUD implemented, reviewed, tested | 166 backend tests (Domain 37, Application 57, Infrastructure 25, Api 47) |
| Frontend | Login, register, task list, task form, delete dialog, 401 wiring | 80 frontend tests; keyboard pass and a real-browser walkthrough at 360/768/1280 |
| Live API | 37/37 end-to-end checks green from a clean database | `docs/qa/logs/e2e-run.log` |
| Coverage | 100% line coverage on Domain + Application (floor: 80%) | `docs/qa/coverage-summary.md` |
| Reviews | Three independent reviews (auth slice, task slice, final); every blocker and major resolved | `docs/reviews/` |
| Open | Git repository, demo rehearsal, and the console check during the rehearsal | `docs/02-definition-of-done.md` → "Open items" |

## The exercise in one paragraph

Build a simple web application with a .NET C# API and data layer, following Clean Architecture and TDD. The app must support CRUD on records, user registration and login, authorized and anonymous endpoints, and a responsive frontend. It must ship with seeded data and credentials, a README, an informal user story, and a written account of how GenAI was used, validated, and corrected. The panel will review the code and ask you to defend your decisions.

## Spec-driven development

Behavior is defined before code:

```
Brief → Constitution → Spec → Plan → Tasks → Red test → Implementation → Evidence
```

- `specs/constitution.md` — rules no change may violate.
- `specs/001-auth/` — registration, login, current user.
- `specs/002-task-crud/` — ownership-scoped CRUD.
- `specs/003-web-client/` — routes, states, responsiveness, accessibility.
- `docs/adr/0004-api-contract.md` — the single source of truth for routes and payloads.
- `docs/07-sdd-and-ai-dls.md` — the full method and how agents map to it.

A change starts in the spec. An implementer works only from an approved spec and
an ordered task; the reviewer checks the diff against the spec; QA maps tests to
acceptance criteria. The task lists in `specs/NNN-*/tasks.md` are the working queue.

## Team roster

| Agent | Core question | Owns | Mode | Status |
| --- | --- | --- | --- | --- |
| Orchestrator | What is the smallest correct next step? | plan, delegation, integration, final delivery | primary | Defined |
| Architect | What is the right shape, and why? | ADRs, contracts, data model, test strategy | subagent | Defined |
| Backend implementer | Does the backend behave correctly? | Domain, Application, Infrastructure, API, backend tests | subagent | Defined |
| Frontend implementer | Is the UI correct, responsive, and clean? | React app, state, forms, frontend tests | subagent | Defined |
| Reviewer | What would break in review? | review reports, risk findings, release verdict | subagent | Defined |
| QA engineer | How do we prove it works? | integration/E2E tests, CI, Docker, seed/demo, release checklist | subagent | Defined |

The roster is deliberately lean. One owner per artifact, no duplicate roles. See `agents/README.md` for the full contract and `docs/05-agent-smoke-tests.md` for the boundary tests.

## Repository map

```
IT_CrudWorks/
├── README.md                  # this file
├── AGENTS.md                  # shared operating contract for every agent
├── specs/                     # SDD: what the product must do
│   ├── README.md              # lifecycle, naming, change protocol
│   ├── constitution.md        # non-negotiable rules
│   ├── 000-product/spec.md
│   ├── 001-auth/{spec,plan,tasks}.md
│   ├── 002-task-crud/{spec,plan,tasks}.md
│   ├── 003-web-client/{spec,plan,tasks}.md
│   └── _template/{spec,plan,tasks}.md
├── docs/
│   ├── 00-brief.md            # structured brief, requirement IDs, timebox
│   ├── 01-wave-plan.md        # waves, deliverables, exit criteria
│   ├── 02-definition-of-done.md
│   ├── 03-stack-baseline.md   # stack and environment
│   ├── 04-requirement-traceability.md
│   ├── 05-agent-smoke-tests.md
│   ├── 06-session-checkpoint.md
│   ├── 07-sdd-and-ai-dls.md   # methodology and agent mapping
│   ├── user-story.md          # informal user story (R-04)
│   ├── presentation.md        # outline, demo script, Q&A prep
│   ├── adr/
│   │   ├── 0001-clean-architecture.md
│   │   ├── 0002-auth-strategy.md
│   │   ├── 0003-persistence-and-test-strategy.md
│   │   ├── 0004-api-contract.md
│   │   └── 0005-frontend-architecture.md
│   ├── ai/
│   │   ├── 000-foundation-and-core-agents.md
│   │   ├── 001-environment-setup.md
│   │   ├── 002-scaffold-and-sdd.md
│   │   ├── 003-auth-slice.md
│   │   ├── 004-auth-ui-slice.md
│   │   ├── 005-review-auth-slice.md
│   │   ├── 006-review-decisions.md
│   │   ├── 007-task-backend-slice.md
│   │   ├── 008-task-ui-slice.md
│   │   ├── 009-contract-clarification.md
│   │   ├── 010-review-task-slice.md
│   │   ├── 011-task-review-decisions.md
│   │   ├── 012-qa-clean-run.md
│   │   ├── 013-application-coverage-gaps.md
│   │   ├── 014-api-critical-paths.md
│   │   ├── 015-frontend-accessibility-pass.md
│   │   ├── 016-full-gate-one-tree.md
│   │   ├── 017-final-review-findings-closed.md
│   │   └── 018-frontend-final-fixes.md
│   ├── reviews/             # three independent reviews
│   └── qa/
│       ├── e2e-api-check.js      # 37 API-level end-to-end checks
│       ├── e2e-api-check.test.js # self-test of the IDOR leak detector
│       ├── coverage-summary.md   # per-file coverage against the 80% floor
│       ├── clean-run-report.md   # the clean-database run, end to end
│       ├── test-plan.md          # requirement-to-test map
│       ├── tools/                # api-ctl.js, coverage-report.js, db-state.js, inspect-coverage.js
│       └── logs/                # captured command output
├── templates/                 # 9 reusable templates
├── scripts/
│   └── sync-agents.ps1        # copies agents to .opencode/agents/it_crudworks
├── project/                   # the deliverable application
│   ├── CrudWorks.slnx
│   ├── src/                   # Domain, Application, Infrastructure, Api
│   ├── tests/                 # four xUnit projects
│   └── frontend/              # React + TypeScript + Vite
└── agents/
    ├── README.md              # roster, routing, permissions, conventions
    ├── orchestrator.md        # primary
    ├── architect.md           # subagent
    ├── backend-implementer.md # subagent
    ├── frontend-implementer.md
    ├── reviewer.md
    └── qa-engineer.md
```

The application is green end to end: the backend builds with warnings as errors
and 166 tests pass, the frontend passes lint with 80 tests and the production
build, and 37/37 API-level end-to-end checks pass against a database recreated
from scratch. Setup and demo steps are in `project/README.md`; the evidence
lives in `docs/qa/` and `docs/ai/`.

## How to use this playbook

1. Read `AGENTS.md` before delegating anything.
2. Read `specs/constitution.md` and the relevant approved spec; if the behavior is
   not in the spec, fix the spec first.
3. Read `docs/00-brief.md` and `docs/04-requirement-traceability.md` to anchor the scope.
4. Pick the next task from `specs/NNN-*/tasks.md` and start with
   `templates/delegation-brief.md`.
5. Every agent returns `templates/handoff.md` with evidence, assumptions, and the next step.
6. The orchestrator closes each wave with `templates/wave-summary.md`.
7. Every AI-assisted change gets an entry in `docs/ai/` using `templates/ai-evidence-log.md`. This is not bureaucracy; it is raw material for the GenAI section of the presentation.
8. The reviewer uses `templates/code-review.md`; the final presentation uses `templates/presentation-outline.md`.

## Start orchestrating

The six agents are synced to `.opencode/agents/it_crudworks/` as `it_crudworks/orchestrator`, `it_crudworks/architect`, `it_crudworks/backend-implementer`, `it_crudworks/frontend-implementer`, `it_crudworks/reviewer`, and `it_crudworks/qa-engineer`.

1. Restart OpenCode, or start a new session, so it reloads the agent directory.
2. Select **it_crudworks/orchestrator** as the primary agent.
3. Give it an objective, not an implementation. Example:
   > "Implement SPEC-001. Start with T-001-01 to T-001-05; use the backend
   > implementer, then review the slice against the spec."
4. After editing any agent file, run `powershell -File scripts/sync-agents.ps1`.

The canonical files are in `agents/`; `.opencode/` holds generated copies. Do not edit the copies.

## Key decisions

| Decision | Choice | Status | Why |
| --- | --- | --- | --- |
| Method | SDD + AI-DLS with agent-owned phases | Accepted | Makes behavior reviewable before code and keeps AI output accountable. |
| Domain | Task manager: users own tasks | Accepted | It satisfies the CRUD, auth, and GenAI exercise with one coherent story. |
| Backend | .NET 10, ASP.NET Core Web API, EF Core, SQLite | Accepted | Modern LTS stack, explicit layers, zero-setup database; PostgreSQL is the documented production swap. |
| Frontend | React + TypeScript + Vite | Accepted | Mainstream, easy to review, strong typing and ecosystem. |
| Auth | JWT bearer + ASP.NET Core Identity password hasher | Accepted | Simple to demo and explain; production hardening documented in an ADR. |
| Tests | xUnit, NSubstitute, WebApplicationFactory, SQLite test databases | Accepted | Covers unit, infrastructure, and API layers without Docker or shared state. |
| Language | English for code, docs, commits, and agent output | Accepted | Matches the American interview panel. |
| Location | Everything under `IT_CrudWorks/` | Accepted | Self-contained deliverable; synced to OpenCode with `scripts/sync-agents.ps1`. |

Open decisions and environment gaps are tracked in `docs/03-stack-baseline.md`.

## Source brief

The original exercise PDF is `C:\Users\User\Downloads\Net - BLA - Technical Interview Exercise - V6 (1).pdf`. `docs/00-brief.md` is a structured working summary; if the two ever conflict, the PDF wins.
