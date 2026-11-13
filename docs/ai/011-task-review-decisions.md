# AI Evidence — 011: Task-slice review decisions

- **Date:** 2026-11-12
- **Agent:** orchestrator (adjudication of `docs/reviews/review-task-slice.md`)
- **Verdict received:** Request changes — 0 blockers, 1 major, 3 minors, 2 nits.

## Prompt summary

The orchestrator requested a severity-ordered review of the task slice (backend
SPEC-002 T-002-01…11 + frontend SPEC-003 T-003-07…11) against the specs, ADR-0004
(as clarified 2026-11-12), ADR-0005, and the DoD, with pre-verified command
evidence (no build/test re-runs). Explicit adjudications requested: the seeder
timestamp bypass, the required-status-on-update path, and an auth-review
regression check on `UnitOfWork`.

## Adjudications and resolutions

| Finding | Severity | Decision | Resolution |
| --- | --- | --- | --- |
| Numeric status strings (`"status": "1"`) accepted by `Enum.TryParse` → bypasses the validation matrix | Major | **Fix** | `TaskValidation.TryParseStatus` replaced with an explicit case-sensitive whitelist (`Todo\|InProgress\|Done` via `nameof`); pins added at Application and API layers |
| No end-to-end pin that a client-supplied `userId` is ignored | Minor | **Fix** | New API test posts a foreign `userId` and asserts the caller remains the owner |
| PUT `dueDate: null` clearing a set due date untested | Minor | **Fix** | Pins added; **exposed a real bug** (below) |
| Traceability rows R-15/R-16 stale | Minor | Orchestrator | Updated in `docs/04-requirement-traceability.md` |
| Stale "real-API run remains open" note in `specs/003-web-client/tasks.md` | Nit | Orchestrator | Reconciled with the recorded smoke evidence in `docs/ai/008` |
| Raw-JSON `"todo"` → 400 case missing at the API layer | Nit | **Fix** | `Create_WithLowercaseStatus_Returns400WithTheStatusKey` added |
| Seeder writes staggered timestamps via `Entry().Property` (bypasses domain mutation path) | Adjudicated | **Accepted risk** | Demo-only seeder; no domain invariant depends on timestamps; already documented in `docs/ai/007`. Keep the pattern out of production write paths |
| Status required on update | Adjudicated | **Confirmed working** | 400 path tested at both layers |

## Bug found by the review follow-up (not a listed finding)

The `dueDate: null` pin exposed that `TaskRepository.FindAsync` was
`AsNoTracking`, so every PUT mutated a **detached** entity and persisted
nothing — the PUT 200 response was in-memory only and the database was never
updated (a silent no-op on the update path). Fixed by removing `AsNoTracking`
from the write path; the read-only list keeps it. Regression pins at the
repository layer (`FindAsync_MutatedAndSaved_PersistsTheUpdateIncludingANullDueDate`,
real SQLite) and the API layer. **Recommendation** carried forward: revisit
tracked `GetAsync` if a dedicated tracked-read method is ever warranted.

## Verification

- `dotnet build CrudWorks.slnx -warnaserror` → 0 warnings / 0 errors (re-verified
  by the orchestrator).
- `dotnet test CrudWorks.slnx` → **143 passed, 0 failed** (37 Domain, 47
  Application, 25 Infrastructure, 34 Api; was 136, +7 cases).
- Red evidence captured before the fix: the numeric-status case failed with
  "No exception was thrown" (Application) and "Expected: BadRequest / Actual:
  Created" (API).
