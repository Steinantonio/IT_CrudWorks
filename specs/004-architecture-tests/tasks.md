# TASKS-004 — Architecture boundary tests

- **Spec:** `specs/004-architecture-tests/spec.md`
- **Plan:** `specs/004-architecture-tests/plan.md`
- **Status:** implementing — approved by project owner
- **Last updated:** 2026-11-18

## Status legend

`todo` · `red` (failing test written) · `green` (test passes) · `done` (refactored,
verified, documented) · `blocked`

## Tasks

| ID | Task | Owner | Depends on | Test / evidence | Status |
| --- | --- | --- | --- | --- | --- |
| T-004-01 | Record the orchestrator's temporary-project `NetArchTest.Rules` compatibility result: .NET SDK `10.0.404`, package `1.3.2`, restore/build/actual Domain EF Core rule execution on `net10.0`; note that this does not validate final-project integration or the full rule suite. | Orchestrator | — | Provided output: `Domain EF Core dependency forbidden: True`; exit `0`; smoke evidence recorded in SPEC-004 and PLAN-004 | done |
| T-004-02 | After SPEC-004 is separately approved, add `CrudWorks.Architecture.Tests` using the existing test target/xUnit conventions, reference the production assemblies under test, and register the project in `project/CrudWorks.slnx`. Use the smoke-tested `NetArchTest.Rules` version `1.3.2`. | Backend implementer | T-004-01; SPEC-004 approval | Project restore/build output and solution membership; final-project integration evidence | done |
| T-004-03 | Write the named project/type and framework boundary rules for Domain, Application, Infrastructure, and Api, matching AC-004-01 and AC-004-02. Keep the rules independent of arbitrary folders/class names and do not replace compiler project-reference enforcement. | Backend implementer | T-004-02 | Architecture test names/results map to AC-004-01 and AC-004-02 | done |
| T-004-04 | Prove rule sensitivity with one temporary forbidden dependency, capture the corresponding failing test, revert the mutation, and add a short README or source comment linking each policy to ADR-0001 / the constitution. | Backend implementer | T-004-03 | Failing mutation output, passing result after revert, clean diff, and rule rationale | done |
| T-004-05 | Run the architecture project tests and full local solution tests; confirm no database/network service is required and no temporary mutation remains. Record actual command output and request independent review. Do not add CI/CD. | Backend implementer; reviewer verifies | T-004-04 | Luna verification: solution build 0 warnings/errors; architecture 6/6; full solution 180/180; no mutation remains; no blocking review findings | done |

## Notes

- T-004-01 is complete based on the orchestrator-provided smoke result. SPEC-004 and this ordered task list are approved; implementation is authorized under the listed scope.
- CI/CD is out of scope per the owner-approved ADR-0007. All verification in this wave is local.
- The initial Architect documentation handoff did not run build/tests; later
  implementation and Luna verification are recorded in AI entries 026 and 027.
- Luna independently ran the solution build, architecture project tests, and full solution tests; exact commands and output are recorded in `docs/ai/027-architecture-wave1-verification.md`.
- `MutationProbe` search returned no matches under `project/`; no production source path appears in the diff.
- Update task status only with the evidence required by that task.
