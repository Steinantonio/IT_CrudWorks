# 01 — Wave Plan

## Principles

- One wave at a time. Each wave ends with a review and explicit approval to continue.
- A wave is done when its exit criteria are met with evidence, not when the files exist.
- No wave may silently expand the scope of the exercise.
- The agent team is built before the application. The application is the first real test of the team.

## Timebox (confirmed)

**Deadline: Friday 2026-11-13. Two working days.**

| Day | Focus | Exit |
| --- | --- | --- |
| Wed 11 | Wave 1 + Wave 2: foundation and core agents | Agents defined; ADR drafts; no app code |
| Thu 12 | Wave 3 + start Wave 5: delivery agents + backend/frontend scaffold | Auth + task CRUD walking skeleton; core tests |
| Fri 13 | Finish Wave 5, Wave 4 smoke test, and rehearsal | DoD met; demo runs; GenAI narrative and presentation ready |

**Rule:** if the critical path slips, cut from the nice-to-have list in `docs/00-brief.md` §7. Never cut the dependency rule, the auth/IDOR checks, the core tests, or the demo.

## Wave 0 — Brief and preferences (complete)

**Goal:** replace assumptions with the actual exercise and the candidate's preferences.

**Deliverables**

- Extracted brief and requirement IDs.
- Language, location, team size, and stack baseline decisions.
- Initial risk list.

**Exit criteria**

- [x] The PDF was read and summarized.
- [x] Preferences were confirmed.
- [x] The domain and stack baseline are proposed.

## Wave 1 — Foundation (approved)

**Goal:** create the shared contract, traceability, quality bar, and templates that every agent will use.

**Deliverables**

- `README.md`, `AGENTS.md`.
- `docs/00-brief.md`, `01-wave-plan.md`, `02-definition-of-done.md`, `03-stack-baseline.md`, `04-requirement-traceability.md`.
- `templates/` for delegation, handoff, wave summary, ADR, user story, test plan, code review, AI evidence, and presentation.
- `agents/README.md` with the roster and file conventions.

**Exit criteria**

- [x] Every hard requirement has an ID and an owner.
- [x] The DoD is testable.
- [x] The stack baseline names the environment gaps.
- [x] Every agent output has a template.
- [x] The user approves the foundation.

## Wave 2 — Core agents (delivered — awaiting approval)

**Goal:** create the three agents that shape and implement the backend.

**Deliverables**

- `agents/orchestrator.md`
- `agents/architect.md`
- `agents/backend-implementer.md`
- `docs/adr/0001-clean-architecture.md` (draft by the architect)
- `docs/adr/0002-auth-strategy.md` (draft)
- `docs/05-agent-smoke-tests.md` with one smoke test per agent.

**Exit criteria**

- [x] Each agent has a clear mission, non-goals, permissions, and output format.
- [x] The orchestrator routing table names one owner per sample feature.
- [x] The architect produces ADRs with alternatives and consequences.
- [x] The backend implementer defines the TDD slices and names the first failing test.
- [x] No agent overlaps another's file ownership.
- [ ] The user approves the core team.

> Smoke-test prompts are defined in `docs/05-agent-smoke-tests.md`. Execution
> happens in Wave 4, when the agents are synced to OpenCode and callable.

## Wave 3 — Delivery agents (delivered)

**Goal:** complete the team with frontend, review, and QA.

**Deliverables**

- `agents/frontend-implementer.md`
- `agents/reviewer.md`
- `agents/qa-engineer.md`
- Refinements to the templates based on real use.
- Refine the delivery-agent smoke tests in `docs/05-agent-smoke-tests.md`.

**Exit criteria**

- [x] Each delivery agent has a mission, non-goals, permissions, evidence rules, and output format.
- [ ] A change can travel from architect → backend/frontend → reviewer → QA. (Wave 4 smoke test)
- [ ] The reviewer produces a severity-ordered review of a supplied sample diff. (Wave 4)
- [ ] The QA engineer produces a test plan mapped to requirements. (Wave 5)
- [ ] The frontend implementer respects the API contract and the design system. (Wave 5)
- [ ] The user approves the full team.

## Wave 4 — Smoke test and sync (complete)

**Goal:** prove the agents are usable, not perfect. This is a smoke test, not a full dry run.

**Deliverables**

- [x] OpenCode sync mechanism (`scripts/sync-agents.ps1`) so the agents are callable.
- [x] Frontmatter and workspace-prefixed permission validation.
- [x] Canonical/synced hash check.
- [ ] One live smoke test per agent (moved into Wave 5 as the first real slice).
- [ ] Fix the top three handoff frictions.

**Exit criteria**

- [x] The six definitions parse and expose the intended modes.
- [x] No agent permission points outside `IT_CrudWorks/`.
- [ ] Each agent returns a usable first output on a real task.
- [ ] The user approves the team for the real exercise.

> The definitions are callable; the live prompts run as part of the first feature
> slice so the smoke test produces real work instead of throwaway output.

## Wave 5 — Build and rehearse the exercise (in progress — Thursday/Friday)

**Goal:** deliver the application and the presentation, spec first.

**Deliverables**

- [x] SDD layer: `specs/README.md`, `specs/constitution.md`, SPEC-000…003,
  plans, task lists, and templates.
- [x] ADR-0004 API contract and ADR-0005 frontend architecture.
- [x] Backend scaffold: `CrudWorks.slnx`, four projects, four test projects,
  health endpoint, warnings as errors.
- [x] Frontend scaffold: Vite 8 + React 19 + TypeScript, router, query client,
  API client, Vitest harness, two passing tests, production build.
- [ ] SPEC-001 implementation: auth endpoints, JWT, seed user.
- [ ] SPEC-002 implementation: task CRUD, ownership, seed tasks.
- [ ] SPEC-003 implementation: auth screens, task list/form, responsive pass.
- [ ] Tests, clean reset, README, AI evidence, and presentation.
- [ ] A rehearsed demo.

**Exit criteria**

- [ ] `docs/02-definition-of-done.md` is fully checked.
- [ ] Every approved spec is `implemented` with evidence.
- [ ] The demo runs from a clean clone.
- [ ] The presentation fits the timebox.
- [ ] The Q&A sheet covers the likely questions.
