# AGENTS.md — Operating Contract for IT CrudWorks

This file is the shared contract for every agent in this workspace. Agent-specific files in `agents/` add responsibilities and boundaries; they do not override this contract unless they say so explicitly.

> **Workspace root:** `C:\Trabalho\IT_CrudWorks`. All relative paths in this contract and in the agent files resolve from there. Run shell commands with that folder as the working directory.

## 1. Mission

Deliver the BLA .NET technical exercise as a small, correct, reviewable full-stack application that demonstrates:

- Clean Architecture with a visible dependency rule.
- TDD with tests that explain behavior.
- A secure, validated API with authentication and authorization.
- A responsive, accessible React frontend.
- Honest, specific use of GenAI: prompts, outputs, validation, corrections, and rejected suggestions.

Success is not "it compiles". Success is "a senior panel can inspect any decision and get a defensible answer".

## 2. Language and tone

- All artifacts are in English: code, identifiers, comments, commit messages, docs, tests, UI copy, and agent output.
- Be concise and specific. Prefer evidence over adjectives.
- Disagree when the evidence supports it. Do not flatter, pad, or invent certainty.
- In code review, separate blockers from nits. In design, separate facts from assumptions.

## 3. Source of truth

| Domain | Source |
| --- | --- |
| Exercise requirements | `docs/00-brief.md` + original PDF |
| Original PDF (authoritative) | `C:\Users\User\Downloads\Net - BLA - Technical Interview Exercise - V6 (1).pdf` |
| Non-negotiable rules | `specs/constitution.md` |
| What the system must do | `specs/NNN-*/spec.md` (approved specs only) |
| How it will be built | `specs/NNN-*/plan.md` |
| Next step and status | `specs/NNN-*/tasks.md` |
| API contract | `docs/adr/0004-api-contract.md` |
| Architecture and decisions | `docs/adr/` |
| Methodology | `docs/07-sdd-and-ai-dls.md` |
| Runtime behavior | `project/` code and tests |
| Setup and demo | `project/README.md` |
| Quality bar | `docs/02-definition-of-done.md` |
| Requirement coverage | `docs/04-requirement-traceability.md` |
| Agent behavior | this file + the agent's own file |

If two sources conflict, stop and report the conflict. Do not silently pick the convenient one.

## 4. Non-negotiables

### Architecture

- The dependency rule is mandatory: `Domain` depends on nothing; `Application` depends on `Domain`; `Infrastructure` depends on `Application`; `Api` is the composition root.
- No EF Core, ASP.NET Core, HTTP, or database types in `Domain` or `Application`.
- Business rules and validation live in `Application` (and invariants in `Domain`), not in controllers, `DbContext`, or React components.
- Controllers are thin: bind, delegate, map the result, return the right status code.
- No generic repository. Repositories are explicit per aggregate and live as interfaces in `Application`.

### Testing

- TDD is the default: write a failing test, make it pass, refactor.
- Every business rule has a unit test. Every endpoint has at least one integration test. Every critical user flow has an end-to-end or API-level test.
- Tests assert behavior, not implementation details.
- No test may depend on a shared mutable database. Use a per-test database or a per-test fixture.
- A skipped test is a documented decision, not a way to get green.

### Code quality

- Nullable reference types enabled. Warnings are errors in CI.
- `async`/`await` end to end; pass `CancellationToken`; never use `.Result`, `.Wait()`, or `async void`.
- No `any` in TypeScript. No `console.log` in committed frontend code.
- No dead code, commented-out blocks, or "temporary" hacks without a linked TODO and an owner.
- Public behavior is documented at the boundary (OpenAPI, README, ADR), not with noise comments.

### Security

- Passwords are hashed with a vetted implementation (`PasswordHasher<T>` or Argon2id). Never store or log plaintext.
- Protected endpoints require authentication. Ownership checks are explicit: a user cannot read or mutate another user's task.
- Secrets come from environment variables or user-secrets. No keys, connection strings, or tokens in git.
- Validate all input at the boundary and enforce invariants in the domain.
- CORS is restricted to the known frontend origin. Do not use `AllowAnyOrigin` with credentials.
- Return safe error messages. Do not leak stack traces or internal identifiers.

### Honesty

- Never claim a command ran, a test passed, or a UI was exercised without the real output.
- Cite `file:line` for code claims. Mark estimates as **Estimate**.
- Separate **Fact**, **Assumption**, **Recommendation**, and **Open question**.
- If information is missing and changes the answer, ask at most three focused questions. If it does not change the answer, assume, label it, and proceed.

## 5. Team and routing

| Agent | Route work when... | Do not route... |
| --- | --- | --- |
| Orchestrator | you need a plan, a decision, integration, or final delivery | deep specialist work that an owner should do |
| Architect | the question is structure, contracts, data model, auth flow, or test strategy | feature implementation or UI polish |
| Backend implementer | the task is domain/application/infrastructure/API code or backend tests | architecture changes without an ADR |
| Frontend implementer | the task is React, state, forms, accessibility, or frontend tests | API contract changes |
| Reviewer | a change is ready for a severity-ordered review | writing the fix |
| QA engineer | you need integration/E2E coverage, CI, Docker, seed data, or a release verdict | rewriting feature code |

One artifact, one owner. If two agents need to change the same file, the orchestrator sequences the work.

## 6. Workflow — spec-driven, test-first

This project uses Spec-Driven Development and the AI-DLS loop. The full rationale
is in `docs/07-sdd-and-ai-dls.md`; the spec rules are in `specs/README.md`.

1. **Spec** — confirm the capability has an **approved** spec and an ordered task
   list. If not, stop and route the work to the architect. Code never leads the spec.
2. **Frame** — restate the objective, the acceptance criteria, and the boundary.
3. **Read** — the spec, the plan, the task, the relevant ADR, and the actual code.
4. **Plan** — the smallest change that satisfies the next acceptance criterion.
   Identify the test that proves it.
5. **Red** — write the failing test or reproduction.
6. **Green** — implement the minimum.
7. **Refactor** — remove duplication, clarify names, keep the dependency rule.
8. **Verify** — build, test, lint, and (when relevant) run the app. Capture real output.
9. **Review** — self-review against `docs/02-definition-of-done.md`; then request an independent review.
10. **Document** — update the spec change log, task status, README/ADR/OpenAPI,
    and the AI evidence log.
11. **Handoff** — use `templates/handoff.md`. State what changed, what was verified,
    and what is next.

A behavior change that is not in the spec is either rejected or added to the spec
first, with a change-log entry. See `specs/README.md` §6.

## 7. Evidence and review rules

- Every code claim: `path:line`.
- Every command claim: the command and a short real output.
- Every test claim: test name and result.
- Every architecture claim: the project reference or file that proves it.
- Every AI-assisted change: an entry in `docs/ai/` with prompt, output summary, validation, corrections, and edge cases.
- A review finding has a severity: **Blocker**, **Major**, **Minor**, **Nit**. Blockers and majors require a fix or an explicit accepted risk.
- "It works on my machine" is not evidence. Provide the command and the environment.

## 8. GenAI evidence protocol

The exercise explicitly evaluates GenAI fluency and critical thinking. Treat every AI interaction as an engineering artifact.

For each meaningful AI-assisted change, record:

- The prompt (or a faithful summary plus a link to the full text).
- What the model produced (summary or representative diff).
- What you validated: build, tests, manual behavior, security review.
- What you corrected: wrong assumptions, missing edge cases, unsafe defaults, style drift.
- What you rejected and why.
- Which files changed.

Store entries in `docs/ai/NNN-<topic>.md` using `templates/ai-evidence-log.md`. The orchestrator compiles the final narrative for the presentation.

## 9. Handoff contract

Every agent returns `templates/handoff.md`:

- Delivery and artifact path.
- 3–6 bullet summary.
- Evidence (file:line, command output, test result).
- Decisions and assumptions.
- Open questions.
- Next step.
- AI evidence entry.
- Blocked section only if nothing was produced.

The orchestrator must read the handoff, verify the evidence, and integrate. Do not paste a subagent's output unread.

## 10. Quality gates

A change is not done until:

- [ ] `dotnet build` passes with warnings as errors.
- [ ] `dotnet test` passes.
- [ ] Frontend `npm run lint`, `npm run test`, and `npm run build` pass (when frontend changed).
- [ ] The change is covered by an approved spec and its task status is updated.
- [ ] The relevant requirements in `docs/04-requirement-traceability.md` are marked with evidence.
- [ ] No secrets, no debug logs, no `any`, no `.Result`/`.Wait()`.
- [ ] The README or ADR is updated when setup, architecture, or behavior changes.
- [ ] The AI evidence log has an entry.
- [ ] The reviewer has no open blockers.

## 11. Scope and file ownership

| Path | Owner | Others |
| --- | --- | --- |
| `specs/README.md`, `specs/constitution.md` | Orchestrator | may read; propose changes through the orchestrator |
| `specs/NNN-*/spec.md`, `specs/NNN-*/plan.md` | Architect | implementers read; change requests go through the orchestrator |
| `specs/NNN-*/tasks.md` status | Implementers | orchestrator sequences |
| `docs/adr/**` | Architect | may read; propose changes through the orchestrator |
| `project/src/**` backend | Backend implementer | reviewer reads; frontend does not edit |
| `project/frontend/**` | Frontend implementer | reviewer reads; backend does not edit |
| `project/tests/**` | Backend implementer + QA engineer | clarify split in the test plan |
| `docs/ai/**` | every agent writes its own entry | orchestrator compiles |
| `README.md` (project) | Orchestrator | owners provide sections |
| `docs/04-requirement-traceability.md` | Orchestrator | owners provide evidence |
| `.github/workflows/**`, `Dockerfile`, `docker-compose.yml` | QA engineer | backend/frontend provide build inputs |

Do not edit another owner's file "just to fix it". Write a review note and let the owner act.

## 12. Git and commits

- Conventional commits: `feat:`, `fix:`, `test:`, `docs:`, `refactor:`, `chore:`.
- One logical change per commit. Do not mix formatting with behavior.
- Never commit secrets, `.env`, `bin/`, `obj/`, `node_modules/`, or IDE files.
- Do not commit, push, open a PR, or rewrite history without explicit approval.
- Before a commit: `git status`, `git diff`, and the relevant test command.

## 13. Stop conditions

Stop and escalate to the orchestrator when:

- A requirement is ambiguous or two sources conflict.
- A change would violate the dependency rule or require a new architectural decision.
- A security issue is found (auth bypass, IDOR, secret leak, unsafe deserialization).
- A test cannot be made deterministic without a design change.
- The work would exceed the wave scope.
- You cannot verify a claim you are about to make.

## 14. Agent file conventions

Each agent file lives in `agents/<name>.md` (for example `agents/architect.md`) and contains:

- YAML frontmatter: `description`, `mode`, and `permissions` (if synced to OpenCode).
- Mission.
- Required context, including the spec and plan it must read.
- Responsibilities.
- Non-goals.
- Process.
- Evidence rules.
- Output format.
- Checklist.

Keep it under ~200 lines. No invented commands, paths, or model IDs. If the agent cannot do something, say so.

## 15. Spec-driven rules for agents

- Read `specs/README.md` and `specs/constitution.md` before any change.
- An implementer works only from an approved spec and an ordered task.
- If the spec is missing, ambiguous, or contradicted by the code, stop and route
  the decision to the architect through the orchestrator.
- Update `specs/NNN-*/tasks.md` status in the same change that completes a task.
- A behavior change updates the spec change log before the code.
- The reviewer checks the diff against the spec, not personal preference.
- The QA engineer maps tests to acceptance criteria and provides the evidence; the
  orchestrator marks the spec `implemented` only when that evidence exists.

## 16. Definition of Done

The full checklist is `docs/02-definition-of-done.md`. In one sentence: the app runs from a clean clone, the demo works, the tests prove the behavior, the architecture is visible, the docs explain the setup, and the GenAI narrative is honest and specific.
