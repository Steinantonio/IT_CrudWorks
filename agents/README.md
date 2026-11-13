# Agents — Roster and Conventions

All six agent definitions live here. This file is the contract for how they are written, routed, and kept from stepping on each other. Run `scripts/sync-agents.ps1` after any edit to make the copies in `.opencode/agents/it_crudworks/` current.

## 1. Why a lean team

The exercise is small enough to be built by one engineer, but it has six distinct kinds of risk: architecture, backend behavior, frontend behavior, security, testing, and delivery. The team mirrors those risks, not an org chart.

- One owner per artifact.
- No agent whose only job is to restate another agent.
- No "coordinator of coordinators".
- The orchestrator is the only primary agent; everyone else is a subagent.

## 2. Roster

| File | OpenCode ID (when synced) | Mode | Core question | Owns | Does not do |
| --- | --- | --- | --- | --- | --- |
| `orchestrator.md` | `it_crudworks/orchestrator` | primary | What is the smallest correct next step? | plan, delegation, integration, final delivery docs | deep specialist work; unverified claims |
| `architect.md` | `it_crudworks/architect` | subagent | What is the right shape, and why? | ADRs, API contracts, data model, auth flow, test strategy | production code, UI, release decisions |
| `backend-implementer.md` | `it_crudworks/backend-implementer` | subagent | Does the backend behave correctly? | Domain, Application, Infrastructure, API, backend tests | architecture changes without an ADR; frontend code |
| `frontend-implementer.md` | `it_crudworks/frontend-implementer` | subagent | Is the UI correct, responsive, and clean? | React app, state, forms, accessibility, frontend tests | API contract changes; backend code |
| `reviewer.md` | `it_crudworks/reviewer` | subagent | What would break in review? | review reports, severity-ordered findings, release verdict | editing the fix; approving without evidence |
| `qa-engineer.md` | `it_crudworks/qa-engineer` | subagent | How do we prove it works? | integration/E2E tests, CI, Docker, seed/demo, release checklist | rewriting feature code; deciding product scope |

## 3. Handoff flow

```
Orchestrator
  ├── Architect ──> ADR / contract / test strategy
  ├── Backend implementer ──> backend code + backend tests
  ├── Frontend implementer ──> UI + frontend tests
  ├── Reviewer ──> findings + verdict
  └── QA engineer ──> integration/E2E + CI + release evidence
```

Rules:

- The orchestrator writes the delegation brief and reads the handoff.
- The architect decides the shape; the implementers do not change it unilaterally.
- The reviewer does not edit code. Findings go back to the owner.
- QA does not rewrite feature code. Bugs go back to the owner.
- The orchestrator integrates and updates the traceability matrix.

## 4. Agent file anatomy

Each file has:

```yaml
---
description: <1–3 sentences: what it does, when to use it, what it does not do>
mode: primary | subagent
permissions:
  - action: edit
    resource: "*"
    effect: deny
  - action: edit
    resource: "<allowed path>"
    effect: allow
  - action: shell
    resource: "<allowed command pattern>"
    effect: allow
  - action: subagent
    resource: "*"
    effect: deny
---
```

The body must contain:

1. **Mission** — one paragraph.
2. **Required context** — files to read before acting.
3. **Responsibilities** — what it owns.
4. **Non-goals** — what it must not do.
5. **Process** — ordered steps.
6. **Evidence rules** — how to cite and what not to invent.
7. **Output format** — use `templates/handoff.md`, plus role-specific sections.
8. **Checklist** — self-review before returning.

Keep it under ~200 lines. A long prompt is not a better prompt; a clear boundary is.

## 5. Permission design

The principle is deny-by-default, then allow the smallest zone that lets the agent do its job.

| Agent | Edit | Shell | Subagent |
| --- | --- | --- | --- |
| Orchestrator | all project/docs paths; deny `.git`, secrets | allow; ask before destructive commands | allow |
| Architect | `docs/adr/**`, `docs/ai/**`, `docs/user-story.md` | deny or read-only checks | deny |
| Backend implementer | `project/src/**`, `project/tests/**`, `docs/ai/**` | allow build/test/EF commands | deny |
| Frontend implementer | `project/frontend/**`, `docs/ai/**` | allow npm/vitest/build commands | deny |
| Reviewer | `docs/reviews/**`, `docs/ai/**` | allow build/test commands; deny destructive | deny |
| QA engineer | `project/tests/**`, `project/e2e/**`, `.github/**`, `Dockerfile`, `docker-compose.yml`, `docs/qa/**`, `docs/ai/**` | allow build/test/docker commands | deny |

The frontmatter uses workspace-prefixed resources (`IT_CrudWorks/...`) because the OpenCode workspace root is `C:\Trabalho`, not `IT_CrudWorks/`.

## 6. OpenCode callability

The canonical files live in `IT_CrudWorks/agents/`. OpenCode discovers agents under
`<workspace>/.opencode/agents/`, so a sync script copies the six definitions to
`C:\Trabalho\.opencode\agents\it_crudworks\`:

```powershell
powershell -File scripts/sync-agents.ps1
```

OpenCode then exposes:

- `it_crudworks/orchestrator` (primary)
- `it_crudworks/architect`
- `it_crudworks/backend-implementer`
- `it_crudworks/frontend-implementer`
- `it_crudworks/reviewer`
- `it_crudworks/qa-engineer`

Rules:

- Restart OpenCode or start a new session after syncing; agent discovery happens at load time.
- Edit the canonical files in `agents/`, never the copies in `.opencode/`.
- Re-run the script after every agent change; drift is otherwise silent.
- A junction was rejected because `README.md` would be exposed as an agent file.
- Template mode (pasting the file into a `general` subagent) remains a fallback only.

## 7. Naming and versioning

- File names are lowercase kebab-case, no numeric prefix: `backend-implementer.md`, not `03-backend.md`. The order is this README's job.
- Each file ends with a `Last reviewed: YYYY-MM-DD` line and a trigger for the next review.
- When an agent's scope changes, update this README and the agent file in the same change.
- Do not fix an agent by making it broader. Fix the boundary or add a specialist only when a real task proves the need.
