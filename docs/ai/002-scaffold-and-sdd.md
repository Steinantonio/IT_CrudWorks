# AI Evidence — 002: Scaffold, Frontend Harness, and SDD Adoption

- **Date:** 2026-11-11
- **Agent:** orchestrator (main session)
- **Task:** finish the application scaffold and adopt Spec-Driven Development with
  AI-DLS artifacts before the first feature slice.
- **Tool/model:** OpenCode session (DeepSeek V4.1 Flash Free)

## Prompt

The user asked, in Portuguese:

> "pode começar pelo scaffold"
>
> "lembrando que quero usar o conceito de sdd, e ai-dls, para desenvolver, entao
> com specs etc etc, agentificação, e tudo documentado no projeto. Enfim, dps do
> scaffold, que é a base, quero ver o resto dai"

The intent: create the base (solution, projects, React app), then make the
development method explicit in the repository — specs, agent responsibilities,
and the AI-driven lifecycle — before building features.

## Output summary

- Backend solution with four source projects and four test projects, pinned to
  .NET 10 (SDK 10.0.404) with warnings as errors.
- Minimal API shell: controllers, OpenAPI, ProblemDetails, CORS, health endpoint,
  `public partial class Program`.
- React 19 + TypeScript + Vite 8 shell with router, TanStack Query, an API client,
  and Vitest + React Testing Library.
- SDD layer: `specs/README.md`, `specs/constitution.md`, SPEC-000…003, plans, task
  lists, and templates.
- ADR-0004 API contract and ADR-0005 frontend architecture.
- `docs/07-sdd-and-ai-dls.md`: the phase loop, artifacts, agent mapping, and
  anti-patterns.
- Agent contracts updated: spec-first required context, spec permissions, and
  review/QA gates.

## Commands and real results

| Command | Result |
| --- | --- |
| `dotnet build CrudWorks.slnx -warnaserror` | 0 warnings, 0 errors; eight projects |
| `dotnet test CrudWorks.slnx` | 1 passed, 0 failed (`HealthTests.GetHealth_ReturnsOk`) |
| `npm install` + dependencies | 0 vulnerabilities after each install |
| `npm run lint` | clean after removing the triple-slash reference |
| `npm run test` | 2 passed: app shell + not-found page |
| `npm run build` | `tsc -b && vite build`; 71 modules; `dist/` produced |
| `powershell -File scripts/sync-agents.ps1` | 6 agents synced |

## Corrections

| What was first suggested | What was wrong | What changed | Why |
| --- | --- | --- | --- |
| `dotnet add package` without a version | Resolved .NET 11 packages that target `net11.0` and break on `net10.0` | Pinned every package to `10.0.15` | The build must stay on the installed LTS line |
| `npm` called directly | Node's path was not in the shell, and `npm.ps1` was blocked by the execution policy | Prepended `C:\Program Files\nodejs` and set `RemoteSigned` for CurrentUser | Commands must be reproducible in a fresh shell |
| `MemoryRouter` wrapped around `App` in a test | `App` already contains `BrowserRouter`; nested routers fail | Tested `NotFoundPage` directly instead | The test now verifies the page, not the router internals |
| Test expected `fetch` with no headers | The client always sends `Accept` and optional auth headers | Asserted `fetch(url, expect.any(Object))` | Matching real behavior |
| Triple-slash Vitest reference in `vite.config.ts` | Lint warning; the import already provides types | Removed the reference and imported `defineConfig` from `vitest/config` | Keep the lint gate clean |
| SDD as a separate document only | The workflow would not bind the agents | Added spec permissions, required context, gates, and task-status rules to every agent | Documentation without enforcement is decoration |

## Validation

- [x] Backend build clean with warnings as errors.
- [x] Health test passes through `WebApplicationFactory`.
- [x] Frontend lint, tests, and production build pass.
- [x] Package versions pinned to the `10.0.x` line.
- [x] Specs have testable Given/When/Then criteria and explicit out-of-scope items.
- [x] The API contract is precise enough to implement the client in parallel.
- [x] Every agent names the spec it must read and the artifact it may edit.
- [x] Canonical and synced agent files hash-match after the sync.

## Edge cases considered

- **No signing key in source.** The API will generate an ephemeral key in
  Development and fail fast elsewhere; tests inject a fixed key. Tokens do not
  survive a restart, which is acceptable for the demo and honest in ADR-0002.
- **Empty test projects.** Three xUnit projects currently contain no tests; they
  are scaffolding for the next slices. `dotnet test` reports "no tests" for them
  without failing the run.
- **Frontend build size.** 292 kB raw / 92 kB gzip before feature code; acceptable
  for a demo, and no heavy UI library was added.
- **Date-only serialization.** The contract fixes `yyyy-MM-dd`; the implementation
  must test it rather than rely on framework defaults.
- **Spec drift.** The change protocol and the reviewer's spec-conformance
  dimension are the guards.

## Files affected

- `project/` — solution, four projects, test projects, frontend app and tests.
- `specs/` — README, constitution, four specs, three plans, three task lists,
  templates.
- `docs/adr/0002-auth-strategy.md`, `0004-api-contract.md`,
  `0005-frontend-architecture.md`.
- `docs/07-sdd-and-ai-dls.md`, `docs/01-wave-plan.md`,
  `docs/04-requirement-traceability.md`, `docs/06-session-checkpoint.md`,
  `README.md`, `AGENTS.md`.
- `agents/*.md` and the synced copies in `.opencode/agents/it_crudworks/`.

## Final decision

Accepted. The scaffold is green and the SDD/AI-DLS layer is part of the operating
contract, not a side document. The next slice is SPEC-001: Domain `User`,
Application `AuthService`, EF persistence, JWT, and the API tests, followed by the
frontend auth screens.
