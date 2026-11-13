# AI Evidence — 017: Final review findings closed

- **Date:** 2026-11-12
- **Agent:** orchestrator, with the architect (subagent session) correcting
  ADR-0001 and the frontend implementer (subagent session) closing the two
  frontend findings
- **Task:** Close the remaining findings from `docs/reviews/final-review.md`
  (0 blockers, 2 majors, 6 minors, 5 nits). The two majors were already closed
  in the previous wave; this wave closes the remaining minors and nits.
- **Tool/model:** OpenCode session (MiniMax M3.1 Flash Free).

## Prompt summary

The final review returned **Approve with comments** — 0 blockers, 2 majors, 6
minors, 5 nits. The two majors (both documentation-truth defects) were closed
in wave 4 by the single-tree gate and the documentation reconciliation.

The remaining findings were dispatched to their owners:

| Finding | Owner | Resolution |
| --- | --- | --- |
| Minor — the traceability matrix carried the already-corrected Infrastructure description | Orchestrator | Corrected in wave 4 |
| Minor — "nine likely questions"; there are **eight** | Orchestrator | Corrected |
| Minor — "13 evidence entries"; there were **16** (now 17) | Orchestrator | Corrected |
| Minor — `specs/README.md` listed the three specs as "Approved" | Orchestrator | Corrected to "Implemented" |
| Minor — `docs/adr/0001` was still "proposed" while the other four were "accepted" | Orchestrator | Corrected to "accepted" |
| Minor — `project/README.md` said "C# 13" under SDK 10.0.404 with `LangVersion=latest` | Orchestrator | Corrected to "C# 14" |
| Minor — `@vitest/coverage-v8` installed but unused | Frontend implementer | Disposition: the dependency is removed |
| Nit — `README.md` said "Two independent reviews"; there are three | Orchestrator | Corrected |
| Nit — `docs/adr/0001` folder map did not match the code (9 divergences) | Architect | Map rebuilt against the real tree |
| Nit — the client and the API disagreed on the password rule | Frontend implementer | The client now mirrors the API rule exactly |
| Nit — `docs/qa/e2e-api-check.js` left a scratch user row per run | QA engineer | Fixed in wave 4 (exit 3 on cleanup failure) |
| Nit — `docs/qa/tools/inspect-coverage.js` header called it "ad-hoc" | QA engineer | Reworded to "per-file coverage inspector" |

## The architect's work on ADR-0001

The review flagged the map's `HealthController.cs` and `Contracts/` entries.
The architect listed the real tree instead of taking the orchestrator's word
for it, and found **nine** divergences, not two:

- `CrudWorks.sln` → `CrudWorks.slnx`; the map omitted `project/frontend/`.
- `Application/Abstractions/Persistence|Auth|Security/...` → the ports are
  feature-grouped: `Application/{Auth,Common,Tasks,Users}/`.
- `Infrastructure/Persistence/Repositories/...` → the implementations sit
  directly in `Persistence/`.
- `Infrastructure/Auth/{...}` → `Infrastructure/Security/{...}`.
- `Api/Controllers/{...,HealthController}.cs` → there is no `HealthController`;
  the health endpoint is mapped in `Program.cs:103` via
  `app.MapHealthChecks("/api/health", ...)`.
- `Api/Contracts/...` → there is no `Contracts/` folder; the contracts live in
  `Application` as DTOs.

The architect also found and fixed a **contradicted claim outside the map**: a
trade-off bullet said "Mapping between API contracts and Application DTOs adds
code", which is false — there are no API-level contract types; the controllers
return Application DTOs directly and the manual mapping lives in `Application`
(`TaskMapping.cs:7`, `AuthMapping.cs:7`). The trade-off itself (manual mapping
is a cost) was preserved; only its description was corrected.

Every other ADR claim was checked against the code and found true, and ADR-0002
through ADR-0005 were read as well — no genuine contradiction, so nothing else
was written.

**The decision itself did not change.** The four-project structure, the
dependency rule, the ports in Application, the absence of a generic repository,
the thin controllers and the exclusions (MediatR/CQRS/AutoMapper/message
broker) are all implemented as decided. Only the description of the delivered
code was stale.

The architect also re-verified the dependency rule from the `.csproj` files
directly: `Domain` → none, `Application` → `Domain`, `Infrastructure` →
`Application`, `Api` → `Application` + `Infrastructure`.

## The frontend's two findings

**The unused dependency.** `@vitest/coverage-v8` was in `devDependencies` with
no `coverage` script, no vitest coverage configuration and no coverage run
anywhere in the repository. `AGENTS.md` §4 and the DoD §7 forbid unused
dependencies, and `docs/03-stack-baseline.md` §2 forbids adding them. The
disposition is to remove it: the DoD coverage floor is a **backend** floor
measured with coverlet, there is no frontend coverage floor, and no frontend
coverage report is committed. Removing it is the honest choice; wiring it up
would have created a new artifact and a new claim to defend.

**The password rule.** The client required `/[A-Za-z]/` and `/[0-9]/`; the API
required `password.Any(char.IsLetter)` and `password.Any(char.IsDigit)`.
`char.IsLetter` is true for Unicode letters (`é`, `ã`, `Ж`) while the regex is
not, so a password with a non-ASCII letter and a digit passed the API and was
rejected by the client. The client was stricter, which is the safe direction,
but the two were not the same rule.

The fix changed the **client** to mirror the API's semantics exactly, rather
than narrowing the API — narrowing the server's auth rule would be a
product-behaviour change to the auth contract, which is the architect's call,
not the frontend's.

## Corrections

| What was asserted | What was wrong | What changed | Why |
| --- | --- | --- | --- |
| ADR-0001's folder map described the delivered code | Nine divergences, including two files and a folder that do not exist | The map was rebuilt against the real tree, with notes explaining why `HealthController` and `Contracts/` are absent | An ADR is read as the description of what was built; a map that contradicts the tree is a defect in the ADR |
| "Mapping between API contracts and Application DTOs adds code" | There are no API-level contract types; the controllers return Application DTOs and the mapping lives in `Application` | Reworded to the actual mechanism | A trade-off that names a non-existent layer does not defend the decision |
| `@vitest/coverage-v8` was a dependency with nothing to show for it | No script, no configuration, no run | Removed, and the gate re-verified afterwards | An unused dependency is dead weight the DoD forbids |
| The client's password rule was ASCII-only while the API's was Unicode-aware | Two different rules for the same requirement | The client now mirrors the API exactly, with a test pinning the non-ASCII case | The client must not reject what the API would accept, and both must state one rule |

## Verification

The orchestrator re-verified the ADR map against the filesystem independently:
the `Application` folders are `Auth`, `Common`, `Tasks`, `Users`; the
`Infrastructure` folders are `Persistence`, `Security`; the `Api` project has
`Authentication`, `Controllers`, `ErrorHandling`, `Security`, `Program.cs` —
no `Contracts/`, no `HealthController`. The map matches.

The frontend gate (lint, test, build) and the backend test run for these two
fixes are recorded by the frontend implementer in its own evidence entry.

## Files affected

- `docs/adr/0001-clean-architecture.md` — the folder map rebuilt; the
  contradicted trade-off bullet reworded; notes on the health endpoint, the
  feature-grouped ports, and the absence of `Contracts/`; a change-log entry.
- `project/frontend/package.json` — `@vitest/coverage-v8` removed.
- `project/frontend/package-lock.json` — updated by `npm install`.
- `project/frontend/src/pages/RegisterPage.tsx` — the password rule now mirrors
  the API.
- `project/frontend/src/pages/RegisterPage.test.tsx` — a test pinning the
  non-ASCII case.
- `docs/04-requirement-traceability.md`, `docs/02-definition-of-done.md`,
  `README.md` — the documentation-truth fixes.
- `docs/ai/017-final-review-findings-closed.md` — this entry.

## Still open

| Gap | Owner |
| --- | --- |
| No git repository, so the red→green history is recorded in `docs/ai/` instead of in commit history | Orchestrator (needs the owner's explicit approval) |
| No CI pipeline — the wave-4 gate is local evidence only (QA-05) | QA engineer |
| The browser console during the **rehearsal**; the demo rehearsed twice with timings | Orchestrator |
| The screen-reader half of AC-003-10; no axe/Lighthouse audit; Chromium only; contrast computed by hand; viewports emulated | Frontend implementer (documented limitation) |
| 21 uncovered Infrastructure lines | Backend implementer (outside the DoD floor) |
