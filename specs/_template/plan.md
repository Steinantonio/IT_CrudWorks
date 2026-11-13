# PLAN-NNN — <Capability> implementation plan

- **Spec:** `specs/NNN-<name>/spec.md`
- **Status:** draft
- **Owner:** architect
- **Last updated:** YYYY-MM-DD

## 1. Architecture impact

Which layers change, and which must not. Name the files that will be created or
modified.

| Layer | Change |
| --- | --- |
| Domain | … |
| Application | … |
| Infrastructure | … |
| Api | … |
| Frontend | … |

## 2. Data model

Tables, columns, constraints, indexes, and migrations.

## 3. Contract

Routes, payloads, status codes, and the ADR that owns them. Do not duplicate the
whole contract; link it.

## 4. Test strategy

| Layer | What is proven | Where |
| --- | --- | --- |
| Domain unit | … | `tests/CrudWorks.Domain.Tests` |
| Application unit | … | `tests/CrudWorks.Application.Tests` |
| Infrastructure integration | … | `tests/CrudWorks.Infrastructure.Tests` |
| API integration | … | `tests/CrudWorks.Api.Tests` |
| Frontend component | … | `project/frontend/src/**/*.test.tsx` |

## 5. Task order

TDD order, smallest testable step first. The executable list lives in
`tasks.md`; this section explains the sequence and the dependencies.

1. …
2. …

## 6. Risks and fallbacks

| Risk | Impact | Fallback |
| --- | --- | --- |
| … | … | … |

## 7. Definition of done

- [ ] Every acceptance criterion in the spec has a test or a named manual check.
- [ ] The dependency rule holds.
- [ ] Build and tests pass with real output.
- [ ] Contract and README are current.
- [ ] AI evidence entry exists.
- [ ] Reviewer has no open blockers.
