# AI Evidence Log

## Entry 027

- **Date:** 2026-11-18
- **Agent:** orchestrator / verifier
- **Task:** Independently verify the implementation of SPEC-004 / Wave 1 and
  complete T-004-05 evidence.
- **Tool/model:** GPT-6 Luna (OpenCode); .NET SDK 10.0.404.

## Prompt

Faithful summary: the owner assigned implementation to `@codificador` and
verification/orchestration to Luna. The coder delivered T-004-02 through
T-004-04. Luna was asked to review the policy rules, confirm the temporary
mutation was removed, run the architecture project and full solution build/tests,
and record evidence before closing T-004-05. No CI/CD or commit.

## Output summary

- Independently reviewed the six architecture-policy tests, project references,
  test project registration, and README rationale.
- Reproduced the temporary forbidden dependency, captured its expected test
  failure, removed it, and reran the clean full solution test suite.
- Verified the mutation probe is absent and no production source files are
  modified.
- Independently built the solution and ran the architecture project and full
  solution tests; all passed. QA mapped all five acceptance criteria with no
  remaining gap.
- Recorded T-004-05 evidence, updated requirement traceability/presentation, and
  moved SPEC-004 to `implemented`.

## Validation

- [x] Build: `dotnet build CrudWorks.slnx --verbosity minimal` →
  **Build succeeded; 0 warnings, 0 errors**.
- [x] Architecture tests:
  `dotnet test tests/CrudWorks.Architecture.Tests/CrudWorks.Architecture.Tests.csproj --no-build --verbosity minimal`
  → **6 passed, 0 failed, 0 skipped**.
- [x] Full backend solution tests:
  Initial independent run:
  `dotnet test CrudWorks.slnx --no-build --verbosity minimal` →
  Domain 37, Application 57, Infrastructure 25, Architecture 6, Api 55;
  **180 passed, 0 failed, 0 skipped**.
- [x] Mutation sensitivity: with temporary `CrudWorks.Domain.Common.MutationProbe`
  referencing `System.Net.Http`,
  `dotnet test tests/CrudWorks.Architecture.Tests/CrudWorks.Architecture.Tests.csproj --filter "FullyQualifiedName~Domain_Should_Not_Depend_On_Framework_Namespaces" --verbosity normal`
  failed as expected (exit 1): `Dependency: System.Net.Http` and
  `Failing types: CrudWorks.Domain.Common.MutationProbe`.
- [x] Mutation cleanup and final rerun: removed `MutationProbe.cs`;
  `dotnet test CrudWorks.slnx --verbosity minimal` → Domain 37, Application 57,
  Infrastructure 25, Architecture 6, Api 55; **180 passed, 0 failed,
  0 skipped**. `functions.grep` for `MutationProbe` under `project/` returned
  no matches; `git diff --name-only -- project/src` returned no output.
- [x] Whitespace check: `git diff --check` emitted no whitespace errors; only
  existing line-ending normalization warnings were reported.
- [x] QA mapping: read-only review mapped AC-004-01 through AC-004-05 to tests
  and evidence; all passed. The missing raw mutation command/output identified
  by QA was captured by Luna before closure.
- [ ] Frontend/E2E/leak-detector suites: not rerun; no frontend/runtime code was
  changed by Wave 1. Their earlier full-stack baseline remains historical, not
  part of this verification.
- [x] Security/behavior: reviewed the diff; no production behavior or
  authentication/authorization code changed.
- [x] Architecture review: six named rules align with AC-004-01/02 and the
  accepted dependency direction; no folder/class-layout rules or CI work.

## Corrections

| What the AI suggested | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| Initial shell command used `rg` to search for the temporary mutation | `rg` is not installed in the PowerShell environment; the command exited 1 after the other checks | Re-ran the search with `functions.grep`, then performed the authorized mutation/revert test and captured the failing output | Do not infer absence from a failed command; retain reproducible evidence |

No code corrections were needed during Luna's review.

## Edge cases considered

- Api composition-root tests must establish an assembly dependency exists without
  asserting that every type in Api depends on both lower layers.
- `NetArchTest.Rules` inspecting test assemblies must not treat test-only
  references as production dependency edges.
- Mutation sensitivity was performed by the implementer; Luna separately
  confirmed no mutation remains and the architecture suite passes after revert.
- No database/network service is required by the architecture rules.
- The full solution test command covers backend projects only; frontend and live
  E2E were deliberately not represented as rerun evidence.

## Auth and validation decisions

- No production code, API contract, data model, or CI/CD workflow changed.
- `NetArchTest.Rules` remains pinned to the smoke-tested 1.3.2.
- T-004-05 is complete; QA mapped all ACs; SPEC-004 is `implemented`.
- No commit was made. Frontend/E2E/leak-detector checks remain the earlier
  baseline and were not rerun for this backend-only wave.

## Files affected

- `specs/004-architecture-tests/spec.md`
- `specs/004-architecture-tests/tasks.md`
- `specs/004-architecture-tests/plan.md`
- `specs/README.md`
- `docs/04-requirement-traceability.md`
- `docs/presentation.md`
- `docs/improvement-plan.md`
- `docs/architecture-adr-plan.md`
- `docs/ai/027-architecture-wave1-verification.md`

## Final decision

**Verified and closed; no blocking findings.** T-004-05 has real build/test
output and mutation failure/revert evidence; QA confirms all acceptance criteria
are met. SPEC-004 is `implemented`. No commit or CI work.
