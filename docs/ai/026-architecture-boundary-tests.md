# AI Evidence Log

## Entry 026

- **Date:** 2026-11-18
- **Agent:** Backend implementer (subagent)
- **Task:** Implement Wave 1 architecture boundary tests for SPEC-004 (T-004-02, T-004-03, T-004-04)
- **Tool/model:** Direct implementation; no generative model used for code generation.

## Prompt

Not applicable. This change was implemented directly by the backend-implementer subagent from the approved SPEC-004, PLAN-004, and `specs/constitution.md`. The orchestrator had already confirmed `NetArchTest.Rules` 1.3.2 compatibility in a temporary smoke project, and the candidate package/version was mandated by the approved plan.

## Output summary

Created `project/tests/CrudWorks.Architecture.Tests/` as a local xUnit project targeting `net10.0` and referencing all four production assemblies. Added `NetArchTest.Rules` 1.3.2 with the exact version pinned. Implemented six named rules in `ArchitecturePolicyTests.cs` covering AC-004-01 and AC-004-02:

- Domain has no dependency on other `CrudWorks.*` projects.
- Domain has no dependency on the forbidden framework/database namespaces.
- Application depends only on Domain (no Infrastructure/Api).
- Application has no dependency on the forbidden framework/database namespaces.
- Infrastructure does not depend on Api.
- Api is the composition root depending on Application and Infrastructure.

Added a `README.md` linking the rules to ADR-0001, ADR-0007, the constitution, and SPEC-004. Registered the project in `project/CrudWorks.slnx`.

Demonstrated sensitivity by temporarily adding `CrudWorks.Domain.Common.MutationProbe` referencing `System.Net.Http`; the `Domain_Should_Not_Depend_On_Framework_Namespaces` test failed, identifying the offending type and namespace. The file was then removed and the test suite passed.

## Validation

- [x] Build: `dotnet build tests/CrudWorks.Architecture.Tests/CrudWorks.Architecture.Tests.csproj` → 0 warnings, 0 errors.
- [x] Unit tests: `dotnet test tests/CrudWorks.Architecture.Tests/CrudWorks.Architecture.Tests.csproj` → 6 passed.
- [x] Mutation sensitivity: temporary `MutationProbe.cs` caused `Domain_Should_Not_Depend_On_Framework_Namespaces` to fail; after revert, all 6 architecture tests passed.
- [x] Full solution tests: `dotnet test CrudWorks.slnx --verbosity minimal` → Domain 37, Application 57, Infrastructure 25, Architecture 6, Api 55 passed (180 total).
- [x] Architecture review: rules assert project/namespace policy only; no folder/class-layout enforcement added.

## Corrections

| What the AI suggested | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| N/A | N/A | N/A | No generative model was used for this change. |

One implementation correction made without AI assistance: the initial `Api_Is_Composition_Root` test used `Types.InAssembly(...).Should().HaveDependencyOn(...)`, which asserts that *all* selected types have the dependency. Because not every type in the Api assembly references Application/Infrastructure directly, the test failed even though the assembly genuinely depends on those layers. The rule was changed to assert that the negative rule (`ShouldNot().HaveDependencyOn(...)`) is *unsuccessful*, proving the dependency exists without requiring every type to reference it.

## Edge cases considered

- **Test-only references do not alter the production graph:** the architecture test project references all four production assemblies, but no production project references were changed.
- **Namespace matching:** `HaveDependencyOn("CrudWorks.Application")` matches sub-namespaces such as `CrudWorks.Application.Tasks`, so the rule covers any future Application sub-namespace without enumerating them.
- **Framework namespace coverage:** the forbidden namespace list (`Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore`, `System.Net.Http`, `System.Data`, `Microsoft.Data`) is asserted for both Domain and Application.
- **Mutation cleanup verified:** `git status` / diff confirmed `MutationProbe.cs` was removed before final test run.

## Auth and validation decisions

No auth or input-validation changes. The change is a local test-only capability.

## Files affected

- `project/tests/CrudWorks.Architecture.Tests/CrudWorks.Architecture.Tests.csproj`
- `project/tests/CrudWorks.Architecture.Tests/ArchitecturePolicyTests.cs`
- `project/tests/CrudWorks.Architecture.Tests/README.md`
- `project/CrudWorks.slnx`
- `specs/004-architecture-tests/tasks.md`

## Final decision

Accepted. The implementation satisfies T-004-02, T-004-03, and T-004-04 and leaves T-004-05 open for Luna's independent verification. No production source mutation remains.
