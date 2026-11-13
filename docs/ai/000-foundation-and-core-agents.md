# AI Evidence — Waves 1–2: Foundation and Core Agents

- **Date:** 2026-11-11
- **Agent:** orchestrator (main session)
- **Task:** turn the BLA exercise PDF into a wave-based agent playbook; create the foundation and the three core agents.
- **Tool/model:** OpenCode session (DeepSeek V4.1 Flash Free)

## Prompt

The user asked, in Portuguese, for a folder `IT_CrudWorks` containing specialized agents for a modern .NET microservice-style CRUD exercise, with prompt templates, summaries, and optimizations, built in waves rather than all at once. Preferences confirmed: English-only artifacts, everything under `IT_CrudWorks/`, lean 5–6 agent team, .NET 10 + PostgreSQL baseline. The exercise PDF was attached and summarized before design.

## Output summary

- `README.md`, `AGENTS.md`, 9 templates, 5 foundation docs, and `agents/README.md`.
- `agents/orchestrator.md`, `agents/architect.md`, `agents/backend-implementer.md`.
- `docs/adr/0001-clean-architecture.md`, `docs/adr/0002-auth-strategy.md`.
- `docs/05-agent-smoke-tests.md` with six boundary tests.
- The wave plan was recompressed when the user confirmed a Friday 2026-11-13 deadline.

## Validation

- [x] The PDF was read and converted to a structured brief with 30 requirement IDs.
- [x] Every agent file was checked for mission, non-goals, permissions, process,
      evidence rules, output format, and checklist.
- [x] Agent ownership zones were checked for overlap.
- [x] The stack baseline records the real environment gaps.
- [x] No application code was generated; no build/test claim was made.
- [ ] Build/test validation is not applicable yet; the project does not exist.

## Corrections

| What the first draft suggested | What was wrong | What changed | Why |
| --- | --- | --- | --- |
| Wave plan with a multi-day optimization phase | Deadline is Friday, two working days | Compressed to a must-have/cut list and a 30-minute smoke test | The exercise matters more than agent polish |
| Numeric agent file names (`01-orchestrator.md`) | The naming convention says kebab-case without numbers | Renamed to `orchestrator.md`, `architect.md`, `backend-implementer.md` | Stable OpenCode IDs and no ordering noise |
| Backend prompt allowed `TaskStatus` / `Task` | Collides with `System.Threading.Tasks` types | Explicitly required `TaskItemStatus` and `TaskItem` | Avoids real C# ambiguity and reviewer friction |
| Backend prompt was ~270 lines | Too long for a system prompt | Trimmed to ~240 lines; kept boundaries and evidence rules | Clarity over volume |
| No AI evidence log existed | The contract requires one per meaningful change | Added this entry | Practice what the playbook preaches |

## Edge cases considered

- The model cannot read PDFs directly; the text was extracted with a temporary
  PdfPig console app and summarized, with the PDF marked as authoritative.
- .NET 10 SDK, Node.js, and Docker are not installed on the machine; the stack
  baseline records the gap and the Wave 5 mitigation.
- The exercise says "ASP.NET MVC, Web API" and also asks for a frontend framework;
  the chosen reading is Web API controllers + React SPA, documented in ADR-0001.
- The GenAI section can reuse the real task API scaffold, so the prompt/output
  evidence is authentic rather than a separate toy.

## Auth and validation decisions

- JWT bearer, 60-minute expiry, `PasswordHasher<User>`, ownership checks return
  404 to avoid leaking existence, CORS restricted to the frontend origin.
- The `sessionStorage` trade-off is documented; production hardening is listed in
  ADR-0002.

## Files affected

- `README.md`, `AGENTS.md`
- `docs/00-brief.md`, `docs/01-wave-plan.md`, `docs/02-definition-of-done.md`,
  `docs/03-stack-baseline.md`, `docs/04-requirement-traceability.md`,
  `docs/05-agent-smoke-tests.md`
- `docs/adr/0001-clean-architecture.md`, `docs/adr/0002-auth-strategy.md`
- `templates/*.md`
- `agents/README.md`, `agents/orchestrator.md`, `agents/architect.md`,
  `agents/backend-implementer.md`

## Final decision

Accepted as the Wave 1–2 baseline, pending user review of the core agents. The
application build starts in Wave 5, after the delivery agents and the smoke test.
