# AI Evidence — 016: Full gate on one tree (release evidence)

- **Date:** 2026-11-12
- **Agent:** orchestrator, with the QA engineer (subagent session) reconciling
  the QA-owned release artifacts
- **Task:** Close the last evidence gap found by the final review — the backend
  gate and the frontend gate had been run in two different waves on two
  different trees, so the two sets of numbers had never been measured together.
- **Tool/model:** OpenCode session (MiniMax M3.1 Flash Free).

## Prompt summary

The final review (`docs/reviews/final-review.md`) closed with **0 blockers, 2
majors, 6 minors, 5 nits**. Both majors were documentation-truth defects, and
one of them had the same root cause as a risk the reviewer named separately:

> The backend gate was run in wave 2 and the frontend gate in wave 3, on two
> different trees. That split *is* the majors.

The orchestrator's action: run the **entire gate back to back on one tree**,
with the API live, and freeze the numbers. The QA engineer then reconciled the
QA-owned release artifacts against that run.

The two majors and the minors were also fixed in the same pass:

| Finding | Owner | Resolution |
| --- | --- | --- |
| Major — the test plan said the browser console "was never observed" and cited the wave-1 frontend count (51 / 9 files) while `docs/ai/015` and the DoD already carried 69 | QA | Reconciled against `docs/ai/015`; 43 stale statements corrected across the QA artifacts |
| Major — the traceability matrix still carried the *already-corrected* Infrastructure description ("migration `Up` methods, a `PasswordHasherAdapter` fallback, 3 seeder branches") | Orchestrator | Replaced with the accurate list: `UnitOfWork.cs:34-35`, the design-time factory body, the two migration **`Down`** rollback bodies, and 5 `DatabaseSeeder` branches |
| Minor — "nine likely questions" in the presentation; there are **eight** | Orchestrator | Corrected |
| Minor — "13 evidence entries"; there are **16** | Orchestrator | Corrected |
| Minor — `specs/README.md` still listed the three specs as "Approved" | Orchestrator | Corrected to "Implemented" |
| Minor — `docs/adr/0001` was still "proposed" while the other four ADRs were "accepted" | Orchestrator | Corrected to "accepted" |
| Minor — `project/README.md` said "C# 13" under SDK 10.0.404 with `LangVersion=latest` | Orchestrator | Corrected to "C# 14" |

## The single-tree gate (orchestrator, verbatim)

```text
=== BACKEND BUILD (from project/)
dotnet build CrudWorks.slnx -warnaserror
Compilação com êxito.
0 Aviso(s)
0 Erro(s)

=== BACKEND TEST (from project/)
dotnet test CrudWorks.slnx
Aprovado!  – Com falha: 0, Aprovado: 37, Ignorado: 0, Total: 37 - CrudWorks.Domain.Tests.dll (net10.0)
Aprovado!  – Com falha: 0, Aprovado: 57, Ignorado: 0, Total: 57 - CrudWorks.Application.Tests.dll (net10.0)
Aprovado!  – Com falha: 0, Aprovado: 25, Ignorado: 0, Total: 25 - CrudWorks.Infrastructure.Tests.dll (net10.0)
Aprovado!  – Com falha: 0, Aprovado: 46, Ignorado: 0, Total: 46 - CrudWorks.Api.Tests.dll (net10.0)
=> 165 passed, 0 failed, 0 skipped

=== FRONTEND LINT (from project/frontend/)
npm run lint
> oxlint
LINT_EXIT=0

=== FRONTEND TEST
npm run test
 Test Files  10 passed (10)
      Tests  69 passed (69)
   Duration  6.84s

=== FRONTEND BUILD
npm run build
✓ 183 modules transformed.
✓ built in 178ms

=== API HEALTH
GET http://localhost:5080/api/health -> 200

=== LIVE API E2E (from the repository root)
node docs/qa/e2e-api-check.js
SUMMARY 37/37 checks passed
E2E_EXIT=0

=== LEAK-DETECTOR SELF-TEST
node --test docs/qa/e2e-api-check.test.js
ℹ tests 6
ℹ pass 6
ℹ fail 0
```

The tree is pinned by a SHA-256 fingerprint over its 116 source files
(`39e5cf83cae7331b8a31e34e3cf756844f7f8f503a1b5e7a1d44ce17f0eb9af5`) — there
is no git repository, so no commit SHA exists to pin it with. The fingerprint
was identical before and after the QA reconciliation, proving that no source
file was touched by the documentation work.

## Frozen release numbers

| Gate | Result |
| --- | --- |
| Backend build (`-warnaserror`) | 0 warnings, 0 errors |
| Backend tests | **165** passed, 0 failed, 0 skipped (Domain 37, Application 57, Infrastructure 25, Api 46) |
| Frontend lint | exit 0, no findings |
| Frontend tests | **69** passed, 10 files |
| Frontend build | 183 modules, ~178 ms |
| Live API health | 200 |
| Live API end-to-end | **37/37**, exit 0 |
| Leak-detector self-test | **6/6** |
| Coverage (Domain + Application) | **348/348 = 100.00%** against the 80% DoD floor |
| **Total automated checks** | **277, all green** |

## A real bug in the QA script, found and fixed in this wave (QA-06)

The QA engineer had narrowed the idempotency claim: the script deleted the task
it created but left the scratch `Users` row behind, so the database was not
byte-identical after a run. There is no "delete user" endpoint, so the cleanup
had to go through the database directly.

**The first attempt at that cleanup was wrong, and the failure was exercised
for real.** The comparison of the scratch user's GUID was case-sensitive, while
EF stores `D10BDFF7-…` and the JSON payload carries `d10bdff7-…`. The delete
matched nothing, the script printed `CLEANUP FAILED … users=2` and **exited 3**
— the non-zero exit is the point: a green 37/37 can no longer hide a dirty
database. The comparison now uses `COLLATE NOCASE`.

After the fix, verified independently by the orchestrator:

```text
SUMMARY 37/37 checks passed
CLEANUP | scratch user qa-user-b+1794517132488@crudworks.local deleted; Users=1 Tasks=5 — the database is back at the seeded baseline
EXIT=0

db: project\src\CrudWorks.Api\crudworks.db
users: 1
  - demo@crudworks.local
tasks: 5
tasks owned by demo@crudworks.local: 5
BASELINE | seeded baseline reached (1 demo user, 5 tasks)
```

The idempotency claim is now **total**: the database returns to the seeded
baseline after every run, not merely the task set.

## Corrections

| What was asserted | What was wrong | What changed | Why |
| --- | --- | --- | --- |
| The backend and frontend gates were separate, on two trees | The two number sets were never measured together, so a change between the runs was covered by neither | One full gate, back to back, on one tree; the numbers frozen | The split was the root cause of both majors in the final review |
| The QA script left one scratch `Users` row per run | The idempotency claim had to be narrowed to "task set only" | The script deletes its own scratch user, by Id **and** Email, never touching the seeded rows, and **exits 3** if the cleanup fails | A release checklist must be repeatable, and a green result must not be able to hide a dirty database |
| The first cleanup implementation | Case-sensitive GUID comparison; EF stores upper-case, JSON carries lower-case | `COLLATE NOCASE`; the failure path was exercised for real (exit 3) before the fix | The failure was observed, not reasoned about |
| The traceability matrix carried the Infrastructure gap description that QA had already corrected | The correction reached `docs/ai/013` and `docs/qa/coverage-summary.md` but not the matrix that the DoD §11 names as the evidence gate | The matrix now states the accurate list | A correction that stops at the log is not a correction |
| "nine likely questions" / "13 evidence entries" | There are eight questions and sixteen entries | Corrected | The panel can verify a count in seconds |

## Findings from the final review, by severity

| Severity | Count | Disposition |
| --- | --- | --- |
| Blocker | 0 | — |
| Major | 2 | Both closed (this wave). Both were documentation-truth defects, not behaviour defects. |
| Minor | 6 | Closed: 4 corrected in this wave, 2 reclassified as known limitations with owners (no screen reader; only Chromium / no axe audit / hand-computed contrast). |
| Nit | 5 | 3 closed (the `inspect-coverage.js` header reworded by its owner; the two stale counts corrected). 2 remain as accepted nits: the native date widget's segment navigation, and the duplicated destination per task row. |

## The reviewer's judgement on the GenAI narrative

> The corrections are real, and this is the strongest part of the deliverable.
> … The candidate corrected *its own tooling* and said so … I found **no**
> recorded AI output left uncorrected. The weakness is not the process — it is
> **propagation**: two corrections reached the AI log and the QA artifact but
> not the traceability matrix and the test plan.

That weakness is what this wave closed: the propagation gap is exactly why the
final review was run against the documentation rather than only against the
code.

## Files affected

- `docs/ai/016-full-gate-one-tree.md` — this entry.
- `docs/qa/clean-run-report.md` — the wave-4 single-tree gate section; §0
  gate provenance; the findings table with a status column (QA-06 and QA-07
  closed).
- `docs/qa/test-plan.md` — the commands section, the frozen-numbers section,
  the risk row about the split, and the "one full gate on one tree" exit
  criterion.
- `docs/qa/coverage-summary.md` — regenerated against the wave-4 measurement.
- `docs/qa/e2e-api-check.js` — the scratch-user cleanup and the exit-3 guard.
- `docs/qa/tools/db-state.js` — **new**, prints the `Users` table and the
  baseline verdict.
- `docs/qa/logs/e2e-run-wave4-run1.log`, `e2e-run-wave4-run2.log` — **new**.
- `docs/04-requirement-traceability.md`, `docs/02-definition-of-done.md`,
  `docs/presentation.md`, `README.md`, `project/README.md`, `specs/README.md`,
  `docs/adr/0001-clean-architecture.md` — the documentation-truth fixes.

No file under `project/src/**` or `project/frontend/**` was modified. No
dependency was added. No commit was made.

## Still open

| Gap | Owner |
| --- | --- |
| No git repository, so the red→green history is recorded in `docs/ai/` instead of in commit history | Orchestrator (needs the owner's explicit approval) |
| No CI pipeline — the wave-4 gate is local evidence only (QA-05) | QA engineer |
| The browser console during the **rehearsal** | Orchestrator |
| The demo has not been rehearsed twice with timings | Orchestrator |
| The screen-reader half of AC-003-10; no axe/Lighthouse audit; Chromium only; contrast computed by hand; viewports emulated | Frontend implementer (documented limitation) |
| 21 uncovered Infrastructure lines | Backend implementer (outside the DoD floor) |
