# SPEC-004 — Architecture boundary tests

- **Status:** implemented
- **Owner:** architect
- **Approved by:** project owner
- **Requirements:** R-02, R-25, R-26
- **ADRs:** 0001, 0007
- **Last updated:** 2026-11-18

## 1. Problem

The four-project dependency rule is already represented by the production project references, but it has no dedicated executable architecture-policy test. This wave adds a local test project that makes forbidden project/type and framework dependencies visible without changing application behavior or freezing folder layout.

## 2. Scope

**In scope**

- Add `CrudWorks.Architecture.Tests` under `project/tests/` and register it in `project/CrudWorks.slnx`.
- Assert the Wave 1/A1 dependency boundaries and forbidden framework namespaces defined below.
- Use `NetArchTest.Rules` version `1.3.2`, whose compatibility smoke check was confirmed by the orchestrator. This verifies the candidate package in a temporary project, not its integration in the final architecture test project.
- Document the reason for each rule and link ADR-0001 and the constitution in a short test-project README or source comment.
- Demonstrate that at least one representative forbidden dependency makes its corresponding test fail, then remove the temporary mutation.

**Out of scope**

- Changing production project references, application code, or existing business/API tests.
- CI/CD, GitHub Actions, deployment, or workflow changes; CI/CD is explicitly deferred by ADR-0007.
- A custom architecture-test engine, folder/class naming rules, database/network tests, or a broad test-framework/package upgrade.
- Claiming the architecture tests replace compiler enforcement from the existing project references.

## 3. Actors and glossary

| Term | Meaning |
| --- | --- |
| Production project | One of `CrudWorks.Domain`, `CrudWorks.Application`, `CrudWorks.Infrastructure`, or `CrudWorks.Api`. |
| Boundary rule | A testable policy about allowed project/type dependencies or forbidden framework namespaces. |
| Composition root | `CrudWorks.Api`, which may compose Application and Infrastructure. |
| Forbidden dependency | A project/type or namespace dependency disallowed by the table in §6. |

## 4. User stories

| ID | Story |
| --- | --- |
| US-004-01 | As a maintainer, I want local architecture tests for the layer boundaries so that a forbidden dependency is caught during review. |
| US-004-02 | As a reviewer, I want each rule linked to the accepted architecture decision and constitution so that its purpose is clear. |

## 5. Acceptance criteria

### AC-004-01 — Project boundary policy is explicit and executable

- **Given** the four production assemblies and the architecture test project
- **When** the architecture policy tests run
- **Then** named tests state and enforce these boundaries: Domain has no dependency on other `CrudWorks.*` projects; Application may depend on Domain but not Infrastructure or Api; Infrastructure must not depend on Api; Api is the composition root and may depend on Application and Infrastructure.
- **And** rules inspect project/type dependency policy, not arbitrary folders or class names; the architecture test project's own references to the production assemblies are not treated as production dependencies.

### AC-004-02 — Domain and Application stay framework-independent

- **Given** the Domain and Application production assemblies
- **When** the architecture tests inspect their type dependencies
- **Then** Domain and Application have no dependencies on `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore`, `System.Net.Http`, `System.Data`, or `Microsoft.Data` namespaces; the relevant rule identifies the offending dependency on failure.

### AC-004-03 — A forbidden dependency is detected

- **Given** the boundary tests pass on the unmodified production projects
- **When** an implementer temporarily introduces one representative forbidden project or framework type dependency and runs its corresponding architecture test
- **Then** that test fails; after the temporary mutation is reverted, the test passes and no mutation remains in the delivered tree.

### AC-004-04 — Architecture tests are local and self-contained

- **Given** the architecture test project is registered in the solution and its packages are restored
- **When** its tests execute without a database or network service
- **Then** they pass using assembly/type inspection only, and the project is included in the ordinary local solution test run.

### AC-004-05 — The policy is reviewable and uses the verified candidate

- **Given** the test project and its rule documentation
- **When** a reviewer inspects the rules
- **Then** each rule links to ADR-0001 or the constitution, and the project uses only the `NetArchTest.Rules` version confirmed by the orchestrator's temporary-project restore/build/run smoke check.
- **And** the temporary-project result is not represented as evidence that the final architecture-test project or its full rule suite has been integrated or run.

## 6. Rules and validation

| Boundary / rule | Allowed policy | Behavior on violation |
| --- | --- | --- |
| Domain | No dependency on another `CrudWorks.*` project; no forbidden framework/database namespace from AC-004-02. | The named Domain rule fails and identifies the dependency. |
| Application | Domain is the only permitted other `CrudWorks` project; no dependency on Infrastructure or Api; no forbidden namespace from AC-004-02. | The named Application rule fails and identifies the dependency. |
| Infrastructure | Application is its direct project-reference boundary (Domain may be transitively available); it must not depend on Api. | The named Infrastructure rule fails for an Api dependency. |
| Api | Composition root; may depend on Application and Infrastructure. | The named Api rule documents/enforces the allowed composition boundary; no lower-layer rule may permit a dependency on Api. |
| Framework boundary | Domain/Application may not depend on the namespace prefixes listed in AC-004-02. | Corresponding framework-boundary test fails. |

The existing `.csproj` references remain the primary direct project-reference enforcement. The architecture tests protect the stated type/namespace policy; they must not claim to parse every project-file reference or duplicate compiler checks.

## 7. API contract

Not applicable: this is a local test/documentation capability and introduces no runtime route or API behavior. ADR-0004 remains unchanged.

## 8. Non-functional requirements

- **Performance:** Local assembly/type inspection only; no database setup or external service.
- **Security:** No runtime authentication, authorization, secrets, or data handling changes.
- **Accessibility:** Not applicable; no user interface changes.
- **Observability:** Test failures identify the violated rule/dependency; no runtime logging changes.
- **Maintainability:** Rules describe architectural boundaries and namespaces, not implementation folder/class layout.

## 9. Dependencies and assumptions

- **Fact:** The current direct project references match the accepted four-project direction: Domain has no `ProjectReference` (`project/src/CrudWorks.Domain/CrudWorks.Domain.csproj:1-9`); Application references Domain (`project/src/CrudWorks.Application/CrudWorks.Application.csproj:3-5`); Infrastructure references Application (`project/src/CrudWorks.Infrastructure/CrudWorks.Infrastructure.csproj:3-5`); Api references Application and Infrastructure (`project/src/CrudWorks.Api/CrudWorks.Api.csproj:18-21`).
- **Fact:** The shared build properties target `net10.0`, enable nullable and implicit usings, and treat warnings as errors (`project/Directory.Build.props:1-9`). The existing API test project targets `net10.0` and uses xUnit conventions (`project/tests/CrudWorks.Api.Tests/CrudWorks.Api.Tests.csproj:3-8,10-22`).
- **Fact:** The current solution lists the four production projects and four existing test projects (`project/CrudWorks.slnx:1-14`).
- **Fact (provided):** The orchestrator's temporary project using .NET SDK `10.0.404` restored `NetArchTest.Rules` `1.3.2` (NuGet reported compatibility with `net10.0`), compiled, and executed an actual rule against `CrudWorks.Domain`: `Types.InAssembly(typeof(TaskItem).Assembly).ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore").GetResult().IsSuccessful` printed `Domain EF Core dependency forbidden: True` and exited `0`. This confirms package compatibility for the smoke case, not final-project integration or the complete policy suite.
- **Decision:** Use the smoke-tested `NetArchTest.Rules` `1.3.2` for this wave. If final-project integration reveals a compatibility problem, stop and return to the orchestrator; do not silently change libraries or build a custom engine.
- **Fact (provided):** .NET SDK `10.0.404` was available in the orchestrator's smoke-test environment. If the implementation environment differs and lacks .NET 10, stop and escalate rather than retargeting.
- The test project will reference the production assemblies it inspects; those test-only references are outside the production dependency graph.

## 10. Open questions

| Question | Owner | Deadline | Resolution |
| --- | --- | --- | --- |
| Does the implementation environment differ from the smoke-test environment and lack .NET 10? | Orchestrator | Before implementation, if environments differ | Resolved: this workspace reports SDK `10.0.404` (same environment used for the smoke test); stop/escalate only if a later implementation environment differs and lacks .NET 10. |

## 11. Change log

| Date | Change | Reason | Approved by |
| --- | --- | --- | --- |
| 2026-11-18 | Initial draft for Wave 1/A1 only; added testable boundary, isolation, mutation-sensitivity, and local-execution criteria. | Prepare the approved ADR-0007 wave for separate spec review; package compatibility evidence had not yet been provided at drafting time. | — |
| 2026-11-18 | Recorded the orchestrator-provided `NetArchTest.Rules` 1.3.2 / .NET SDK 10.0.404 temporary-project smoke result and closed the package compatibility question. | The candidate restored, compiled, and ran one actual Domain EF Core rule against `net10.0`; final-project integration remains unverified. | Orchestrator (evidence only; not spec approval) |
| 2026-11-18 | Approved SPEC-004 and authorized Wave 1 implementation. | The project owner approved this spec and ordered task list; implementation follows the existing TDD and evidence workflow. | Project owner |
| 2026-11-18 | Implemented and verified Wave 1; all five acceptance criteria have evidence. | Six architecture tests pass; the temporary `System.Net.Http` mutation was detected and reverted; the clean full solution run passed 180/180; QA mapped AC-004-01 through AC-004-05 with no remaining gap. | QA evidence + orchestrator |
