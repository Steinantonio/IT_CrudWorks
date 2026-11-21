# PLAN-004 — Architecture boundary tests implementation plan

- **Spec:** `specs/004-architecture-tests/spec.md`
- **Status:** draft
- **Owner:** architect
- **Last updated:** 2026-11-18

## 1. Architecture impact

No runtime or production-layer behavior changes. Preserve the production reference graph; add one test-only project that inspects the four production assemblies.

| Layer / area | Change |
| --- | --- |
| Domain | No source or project-reference change. |
| Application | No source or project-reference change. |
| Infrastructure | No source or project-reference change. |
| Api | No source or project-reference change. |
| Tests / solution | Add `project/tests/CrudWorks.Architecture.Tests/` with its test project and architecture policy tests; register the project in `project/CrudWorks.slnx`. The test project references the production assemblies it inspects; these references do not alter the production graph. |
| Test documentation | Add a short README or source comment in the new test project explaining each rule and linking ADR-0001 / the constitution. |
| Frontend / CI | No change. CI/CD work is explicitly deferred by ADR-0007. |

**Evidence for current code facts:** direct references are visible in `project/src/CrudWorks.Domain/CrudWorks.Domain.csproj:1-9`, `project/src/CrudWorks.Application/CrudWorks.Application.csproj:3-5`, `project/src/CrudWorks.Infrastructure/CrudWorks.Infrastructure.csproj:3-5`, and `project/src/CrudWorks.Api/CrudWorks.Api.csproj:18-21`. Test conventions are visible in `project/tests/CrudWorks.Api.Tests/CrudWorks.Api.Tests.csproj:3-8,10-22`; solution membership is in `project/CrudWorks.slnx:1-14`.

## 2. Data model

None. This wave adds no tables, migrations, seed data, or persistence behavior.

## 3. Contract

No API routes, payloads, or status codes change. This is a local test-only contract for the architecture policy in `specs/004-architecture-tests/spec.md` §6 and ADR-0007 §"Wave 1 — Architecture tests (A1)".

## 4. Test strategy

| Layer | What is proven | Where |
| --- | --- | --- |
| Architecture policy | Named rules reject forbidden production project/type dependencies and Domain/Application framework namespace dependencies; Api is the composition root. | New `project/tests/CrudWorks.Architecture.Tests/` project |
| Sensitivity check | A temporary representative forbidden dependency makes its corresponding rule fail; the mutation is then reverted. | Local architecture-test run; retain real output in implementation evidence |
| Existing backend unit/integration/API | No behavior changes; existing tests remain regression coverage, not substitutes for the architecture assertions. | Existing `project/tests/CrudWorks.*.Tests/` projects |
| Frontend | No frontend behavior changes or tests required. | Not applicable |
| Local solution | New project is discovered in the full local test run; test execution itself uses no database or network service. | `project/CrudWorks.slnx` |

**Fact (provided):** The orchestrator confirmed that a temporary project using .NET SDK `10.0.404` restored `NetArchTest.Rules` `1.3.2` (NuGet reported `net10.0` compatibility), compiled, and executed an actual rule against `CrudWorks.Domain`; the expression checking for `Microsoft.EntityFrameworkCore` dependency printed `Domain EF Core dependency forbidden: True` and exited `0`. This clears the package smoke-test gate, but does not prove final-project integration or the complete boundary suite. Use version `1.3.2`; record new evidence if final-project integration exposes an issue. Do not add a custom architecture engine.

## 5. Task order

The executable task list is in `tasks.md`. The compatibility result has been provided and T-004-01 is complete; separate orchestrator approval of SPEC-004 remains a gate, not implementation authorization.

1. Orchestrator's temporary-project compatibility result is recorded; separately approve SPEC-004 before implementation.
2. After approval, add the test project using existing .NET target/xUnit conventions and register it in the solution.
3. Add the named project/type and framework boundary assertions for AC-004-01 and AC-004-02.
4. Demonstrate failure on one temporary forbidden dependency, revert it, and document rule purpose.
5. Run the architecture project tests and the full local solution tests; capture real evidence and confirm there is no mutation.

## 6. Risks and fallbacks

| Risk | Impact | Fallback |
| --- | --- | --- |
| The smoke test covered one rule in a temporary project, not final-project integration or all policies. | The new project or additional rules could expose an integration problem. | Use only the smoke-tested `NetArchTest.Rules` 1.3.2; capture actual project-test evidence, and stop for an orchestrator decision if integration fails. Do not build a custom engine. |
| Rules encode folders or class names instead of dependency policy. | Tests become brittle and permit/deny the wrong architecture. | Assert project/type and explicit framework namespace boundaries only; avoid arbitrary layout rules. |
| Tests duplicate compile-time project-reference enforcement or overclaim what they inspect. | Misleading coverage and maintenance cost. | Keep `.csproj` references as the direct graph enforcement; document the test coverage as type/namespace policy only. |
| .NET 10 SDK is unavailable. | The project cannot be built/tested at its required target. | Stop and escalate; do not downgrade the target framework. |
| There is no CI/CD guard. | Future local boundary regressions are not automatically checked in CI. | Keep the local solution test and document the known limitation; do not add CI work because the owner deferred it. |

## 7. Definition of done

- [x] SPEC-004 is separately approved and the ordered tasks are authorized before implementation starts.
- [x] The provided `NetArchTest.Rules` 1.3.2 compatibility result is retained as smoke-test evidence; final-project integration and the full policy suite have separate real test evidence, with no unverified version added.
- [x] Every AC in SPEC-004 has an automated test or named evidence, including the temporary mutation/revert check.
- [x] The existing production project-reference graph and dependency rule remain unchanged.
- [x] The architecture test project is included in the solution and runs locally without a database or network service.
- [x] Architecture-project and full-solution test output is captured; no command or result is claimed without output.
- [x] Rule rationale links to ADR-0001 / the constitution; no CI/CD work was added.
- [x] AI evidence entries are recorded and Luna's independent review found no blocking issue.
