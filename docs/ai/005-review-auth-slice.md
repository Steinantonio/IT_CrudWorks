# AI Evidence — 005: Review of the Authentication Slice

- **Date:** 2026-11-12
- **Agent:** reviewer
- **Task:** Severity-ordered review of SPEC-001 (backend, all tasks) and SPEC-003 T-003-01…T-003-06.
- **Tool/model:** OpenCode subagent (GLM-5.3-Flash Free)

## Prompt summary

The orchestrator asked for a severity-ordered review of the authentication slice
(backend SPEC-001 all tasks, frontend SPEC-003 T-003-01…06) against the specs,
ADR-0001/0002/0004/0005, the contract, and the DoD, with `file:line` evidence.
Command evidence (build/test/live runs) was pre-verified and documented in
`docs/ai/003` and `004`; the reviewer was told not to re-run builds/tests and to
spend effort reading code. Explicit adjudications requested: the
`PasswordHasherAdapter` `null!`, the SPEC-001 §8 failed-login logging rule, and
T-003-11 being only a seam. Outputs: `docs/reviews/review-auth-slice.md` and this
entry. Read-only on the change set.

## What I read

`AGENTS.md`, `specs/constitution.md`, `specs/README.md`, `specs/001-auth/{spec,plan,tasks}.md`,
`specs/003-web-client/{spec,tasks}.md`, `docs/adr/0001/0002/0004/0005`,
`docs/02-definition-of-done.md`, `docs/04-requirement-traceability.md`,
`templates/code-review.md`, `docs/ai/003` and `004`; then the full change set:
Domain `User.cs`, all `Application/Auth` + `Common` + `Users` files, all
`Infrastructure` persistence/security files including the migration and seed, all
`Api` files (`Program.cs`, `AuthenticationSetup`, `AuthController`, error
handling, `HttpCurrentUser`, `JwtSigningKey`, appsettings), all four backend test
projects, all eight `csproj` files, and the complete frontend auth change set
(`auth/*` + tests, both pages + tests, `Layout`, `AppRoutes`, `App` + test,
`lib/api.ts`, `test/*`), plus greps for `console.log`/`any`/secrets.

## Verification method

Code, tests, specs, ADRs, and project references were verified by direct reading
with `file:line` citations; no code claim was inferred. Command evidence was
accepted from `docs/ai/003`, `docs/ai/004`, and the traceability matrix per the
orchestrator's efficiency instruction, and is marked as not independently re-run
in the review. Nothing found in the code contradicts the recorded command output.

## Findings count by severity

| Severity | Count | Headline |
| --- | --- | --- |
| Blocker | 0 | — |
| Major | 1 | Login timing side-channel: unknown email skips PBKDF2 (`AuthService.cs:46-52`); fix (~5 lines) or record as accepted risk |
| Minor | 4 | SPEC-001 §8 logging rule unimplemented; `PasswordHasherAdapter` `null!` (accepted-risk candidate); un-tested return-path guard; missing overlong-email register test |
| Nit | 3 | CORS method breadth vs ADR-0004; `UnitOfWork` unique-violation message future-proofing; client password regex drift |

## Adjudications (as requested)

- `PasswordHasherAdapter` `null!` → **Minor / accepted-risk candidate**: safe with the shipped default hasher, documented in code and in `docs/ai/003`; recommendation is to record it in ADR-0002 rather than invent a fake user.
- SPEC-001 §8 failed-login logging → **Minor**: either implement domain-only logging with a test, or amend the spec §8 with a change-log entry; today the spec and code disagree.
- T-003-11 → **Not a finding**: correctly `todo` in `specs/003-web-client/tasks.md:22`; AC-003-08 is not claimed yet. The seam (`handleUnauthorized`) is idempotent and tested at the context level; when wired, it needs the exactly-once/no-loop test at the `api.ts` boundary.

## Verdict

**Request changes** — no blockers; one major (timing side-channel) must be fixed
or explicitly accepted as a recorded risk; four minors; three nits. Full details:
`docs/reviews/review-auth-slice.md`.
