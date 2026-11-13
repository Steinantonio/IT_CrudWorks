# AI Evidence — 001: Environment Setup and Database Decision

- **Date:** 2026-11-11
- **Agent:** orchestrator (main session)
- **Task:** persist the session context, install the missing .NET and Node toolchain, and resolve the database choice using the user's reproducibility criterion.
- **Tool/model:** OpenCode session (DeepSeek V4.1 Flash Free)

## Prompt

The user asked, in Portuguese, to first summarize the work and update the context
window, then proceed with the approved installations. For the database, the exact
criterion was: **"it must be something that's easy to recreate."** The original
exercise PDF was confirmed as an additional context source:
`C:\Users\User\Downloads\Net - BLA - Technical Interview Exercise - V6 (1).pdf`.

## Output summary

- Created `docs/06-session-checkpoint.md` so a fresh session can resume without
  the full conversation.
- Installed .NET SDK **10.0.404** and Node.js LTS **26.13.1** via `winget`.
- Fixed the PowerShell execution policy so `npm` runs.
- Wrote ADR-0003: SQLite with EF Core for the demo, PostgreSQL as the documented
  production swap.
- Wrote the three delivery agents and synced all six agents to OpenCode.

## Commands and real results

| Command | Result |
| --- | --- |
| `winget install --id Microsoft.DotNet.SDK.10 --exact --silent` | `Instalado com êxito`; version 10.0.404 |
| `winget install --id OpenJS.NodeJS.LTS --exact --silent` | `Instalado com êxito`; version 26.13.1 |
| `dotnet --list-sdks` | `9.0.101`, `9.0.201`, `10.0.404` |
| `node -v` | `v26.13.1` |
| `npm -v` (after the policy fix) | `11.20.0` |
| `Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned -Force` | succeeded; `npm.ps1` loads |
| `powershell -File scripts/sync-agents.ps1` | `Synced 6 agents to C:\Trabalho\.opencode\agents\it_crudworks` |
| Frontmatter validation | 6/6 files start with `---`, have a `mode`, and close the frontmatter |

## Validation

- [x] `dotnet --list-sdks` shows 10.0.404.
- [x] `node -v` and `npm -v` return real versions.
- [x] Git was already installed (`2.49.0.windows.1`).
- [x] The six synced agent files parse as frontmatter-delimited Markdown.
- [x] Every edit/shell permission resource is workspace-prefixed
      (`IT_CrudWorks/...`), matching the OpenCode workspace root `C:\Trabalho`.
- [ ] Application build and tests: not applicable yet; the project does not exist.

## Corrections

| What was first suggested | What was wrong | What changed | Why |
| --- | --- | --- | --- |
| PostgreSQL as the runtime database | Needs an install, a service, and credentials; not "easy to recreate" | SQLite file via EF Core, with PostgreSQL documented as the production swap | Matches the user's explicit criterion and the Friday deadline |
| Run `npm -v` directly | PowerShell blocked `npm.ps1` under the execution policy | Set `RemoteSigned` for `CurrentUser` | Node is unusable otherwise in this shell |
| Agent permissions such as `project/**` | The OpenCode workspace root is `C:\Trabalho`, so those paths never match `IT_CrudWorks/project/**` | Prefixed every resource with `IT_CrudWorks/` | Makes the permissions real instead of decorative |
| A junction to `IT_CrudWorks/agents` | It would expose `README.md` as an agent file | A sync script copies only the six agent definitions | Avoids discovery errors and silent drift |

## Edge cases considered

- Multiple .NET SDKs coexist (9.0.101, 9.0.201, 10.0.404). The project will pin
  10.0.404 in `global.json` so the build does not fall back to .NET 9.
- Node 26.13.1 is newer than the 22/24 LTS assumed in the stack baseline. The
  baseline is updated to "Node.js LTS (installed 26.13.1)".
- The database reset is a file deletion; the seed runs only when the database is
  empty. This is documented in ADR-0003 and must be verified from a clean state.
- SQLite is not the production target; the swap path is documented instead of
  hidden.

## Files affected

- `docs/06-session-checkpoint.md`
- `docs/adr/0003-persistence-and-test-strategy.md`
- `docs/ai/001-environment-setup.md`
- `agents/frontend-implementer.md`, `agents/reviewer.md`, `agents/qa-engineer.md`
- `scripts/sync-agents.ps1`
- `README.md`, `AGENTS.md`, `docs/00-brief.md`, `docs/01-wave-plan.md`,
  `docs/02-definition-of-done.md`, `docs/03-stack-baseline.md`,
  `docs/05-agent-smoke-tests.md`, `templates/test-plan.md`, `agents/README.md`

## Final decision

Accepted. The toolchain is installed and verified, the database decision is
recorded, and all six agents are callable after an OpenCode reload. The next step
is the Wave 4 smoke test, then the Wave 5 scaffold.
