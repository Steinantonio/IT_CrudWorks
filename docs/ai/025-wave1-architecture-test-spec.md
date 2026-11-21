# AI Evidence Log

## Entry 025

- **Date:** 2026-11-18
- **Agent:** orchestrator (with IT CrudWorks Architect)
- **Task:** Prepare and review SPEC-004 / PLAN-004 / TASKS-004 for Wave 1 and
  smoke-test `NetArchTest.Rules` compatibility as requested by the owner.
- **Tool/model:** GPT-6 Luna (OpenCode) + `it_crudworks/architect`; .NET SDK
  `10.0.404` for the temporary package smoke project.

## Prompt

Faithful summary: the owner approved ADR-0007 and asked to create the Wave 1
architecture-test documentation, verify `NetArchTest.Rules` quickly and update
the version if needed, then continue with the same SDD pattern. Architect was
asked to create only `specs/004-architecture-tests/{spec.md,plan.md,tasks.md}`,
keep the spec in draft, use the package result only after it was supplied, and
make no production code or CI changes. The orchestrator independently performed
the package smoke check and then reviewed/integrated the files and updated the
spec inventory.

## Output summary

- Created SPEC-004, PLAN-004, and ordered TASKS-004 for architecture boundary
  tests; SPEC-004 remains draft and all implementation tasks remain `todo`.
- Added SPEC-004 to the current-spec inventory in `specs/README.md`.
- Selected `NetArchTest.Rules` 1.3.2 after a successful temporary .NET 10 smoke
  test; updated ADR-0007 with this evidence.
- No production code, production test project, CI workflow, or package
  reference in the repository was changed.

## Validation

- [x] Temporary package restore: `dotnet add package NetArchTest.Rules` in
  `C:\Users\User\AppData\Local\Temp\opencode\NetArchTestSmoke` restored version
  `1.3.2`; NuGet reported compatibility with `net10.0`.
- [x] Temporary rule execution: `dotnet run` in that scratch project, using
  `CrudWorks.Domain` as a project reference, compiled and executed
  `Types.InAssembly(typeof(TaskItem).Assembly).ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore").GetResult().IsSuccessful`.
  Output: `Domain EF Core dependency forbidden: True`; exit code `0`.
- [ ] Final architecture-test project build/test: not run; the project does not
  exist yet and SPEC-004 remains draft.
- [ ] Full solution tests: not run.
- [ ] Manual behavior: not applicable; no runtime change.
- [x] Architecture review: rechecked the new spec/plan/tasks against their
  templates, ADR-0007, project references, solution inventory, and requirements.

## Corrections

| What the AI suggested | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| Describe the temporary smoke as a restore/build/test | The actual command was `dotnet run`; it compiled and ran a console rule, not an xUnit test | Corrected SPEC-004 wording to “restore/build/run”; explicitly state final test-project integration is still unverified | Keep the evidence claim exact |
| Treat the smoke as proof of the future architecture suite | It tested one rule against the Domain assembly only | Kept integration and full-rule-suite tasks as `todo` and retained separate acceptance criteria | A package smoke does not prove the final suite |

## Edge cases considered

- `NetArchTest.Rules` compatibility was verified against `net10.0` in a
  temporary console app using SDK `10.0.404`; a different implementation
  environment without .NET 10 remains a stop/escalate condition.
- Test-project references to production assemblies are not production
  dependency edges; the architecture policy must inspect only production
  assemblies.
- Framework namespace rules must not freeze folders or class names, and must
  complement—not claim to replace—compiler-enforced project references.
- The sensitivity mutation must be reverted; no mutation was performed as part
  of this documentation task.
- CI/CD remains explicitly deferred.

## Auth and validation decisions

- SPEC-004 is **draft**, not approved; no implementation is authorized yet.
- T-004-01 records the completed package evidence; T-004-02 through T-004-05
  remain `todo` behind separate spec approval.
- No production package version, source file, test project, or solution file was
  modified.

## Files affected

- `specs/004-architecture-tests/spec.md`
- `specs/004-architecture-tests/plan.md`
- `specs/004-architecture-tests/tasks.md`
- `specs/README.md`
- `docs/adr/0007-architecture-enhancements.md`
- `docs/improvement-plan.md`
- `docs/architecture-adr-plan.md`
- `docs/ai/025-wave1-architecture-test-spec.md`

## Final decision

**Accepted for documentation; implementation remains gated.** The package
smoke passed for `NetArchTest.Rules` 1.3.2 on `net10.0`. SPEC-004 requires owner
approval before any architecture-test code is written.
