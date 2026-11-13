# AI Evidence — 014: API critical-path coverage (QA-04)

- **Date:** 2026-11-12
- **Agent:** backend implementer (subagent session)
- **Task:** Close QA-04 from the wave-1 QA clean run — assess whether the *critical
  API paths* are covered (`CrudWorks.Api` was at 33.28% line coverage) and add
  API-level integration tests for the critical paths that were not.
- **Tool/model:** OpenCode session (MiniMax M3.1 Flash Free)
- **Related:** QA-03 is closed in `docs/ai/013-application-coverage-gaps.md`; this
  entry cross-references it and vice versa.

## Prompt summary

The orchestrator delegated QA-04 with these constraints:

- The DoD §6 floor covers **Domain + Application only**; `CrudWorks.Api` at 33.28%
  is outside it. The DoD still says "critical API paths are covered".
- **Assess** whether the critical API paths are in fact covered. Where a critical
  path has no coverage, add API-level integration tests in
  `project/tests/CrudWorks.Api.Tests/` using the existing `CrudWorksApiFactory`
  fixture.
- Priority order given by the orchestrator:
  1. the error-handling path (`ProblemDetailsExceptionHandler` / `ApiProblemDetails`),
  2. the CORS setup,
  3. the health check,
  4. the `JwtSigningKey` resolution behaviour in Development vs non-Development.
- **Do NOT chase the last few percent for their own sake** — cover what matters and
  report the rest as a known gap. State clearly in the report which API paths are
  considered critical and whether they are covered.
- No metric gaming: no restructuring, no `ExcludeFromCodeCoverage`, no suppression.
- Own `project/src/**` backend and `project/tests/**`; test files are mine.
  Edit the two Application source files only for a genuine bug inside the approved
  spec/ADR contract.
- Do not touch `project/frontend/**`, `docs/04-requirement-traceability.md`,
  `docs/02-definition-of-done.md`, `specs/**/tasks.md`, `docs/presentation.md`,
  `docs/qa/**`, or another agent's `docs/ai/` entry.
- No git, no commits. Verify with `dotnet build CrudWorks.slnx -warnaserror` and
  `dotnet test CrudWorks.slnx` and record the per-project counts before and after.

## Output summary

### 1. Assessment — what I consider a *critical* API path, and its state before

| # | Critical API path | Why critical | Covered before? |
| --- | --- | --- | --- |
| C1 | RFC 7807 error contract: status, title, `type`, `traceId`, field-level `errors` | Every error the client can see goes through it; the contract is ADR-0004 §1 | **Partially** — status codes, titles and `application/problem+json` were asserted; the `type` URLs, the `traceId` and the `500` branch were **not** |
| C2 | The unexpected-failure branch of the handler | It decides whether an internal exception leaks to the client | **No** |
| C3 | CORS: only the configured frontend origin may read a response | AGENTS.md §4 / constitution Article 5 forbid a wildcard origin | **No** |
| C4 | Health check: anonymous, `200`, ADR-0004 §2 body shape | Liveness probe and DoD §2 checkbox | **Partially** — only the status code |
| C5 | `JwtSigningKey` resolution: configured key wins, Development falls back to an ephemeral key, every other environment fails fast | A missing secret must not silently produce unsigned or predictable tokens | **No** |
| C6 | A valid token whose `sub` is not a GUID | The owner is always resolved from `sub`; an unresolvable subject must not authenticate | **No** |
| C7 | OpenAPI document served in Development | DoD §2 checkbox | **No** |

Already covered and **not** re-tested (verified against the existing suite, no
duplication): every endpoint's status code, `401` on all five anonymous task routes,
IDOR `404` on read/update/delete, `409` on duplicate email, `400` field errors on
create and on the missing-status update, `204` delete, and the `Location` header.

### 2. New tests (12 cases)

New file `project/tests/CrudWorks.Api.Tests/Pipeline/ApiPipelineTests.cs`:

- `ProblemDetails_ForAValidationFailure_CarriesTheContractTypeStatusAndTraceId` (C1)
- `ProblemDetails_ForAnUnexpectedFailure_Returns500WithAGenericTitleAndNoInternalDetail` (C2)
- `Cors_PreflightFromTheConfiguredOrigin_ReturnsTheOriginMethodsAndHeaders` (C3)
- `Cors_PreflightFromAnUnknownOrigin_ReturnsNoAllowOriginHeader` (C3)
- `Cors_ActualRequestFromTheConfiguredOrigin_ReturnsTheAllowOriginHeader` (C3)
- `OpenApiDocument_InDevelopment_IsServed` (C7)

New file `project/tests/CrudWorks.Api.Tests/Pipeline/JwtSigningKeyTests.cs`:

- `Development_WithoutAConfiguredSigningKey_StartsAndSignsWithAnEphemeralKey` (C5)
- `NonDevelopment_WithoutAConfiguredSigningKey_FailsTheStartup` (C5)
- `AConfiguredSigningKeyThatIsInvalid_FailsTheStartup` (theory, 2 cases: not
  base64 / decodes to fewer than 32 bytes) (C5)

Extended `project/tests/CrudWorks.Api.Tests/HealthTests.cs`:

- `GetHealth_ReturnsTheContractShape` (C4)

Extended `project/tests/CrudWorks.Api.Tests/Auth/AuthApiTests.cs`:

- `Me_WithAValidTokenWhoseSubjectIsNotAGuid_Returns401` (C6)

Three new test hosts were needed, all built on the existing `CrudWorksApiFactory`
pattern (own temp SQLite file, `Pooling=False`, file deleted on dispose):
`FailingAuthServiceApiFactory` (an `IAuthService` stub that always throws, to reach
the `500` branch), `DevelopmentWithoutSigningKeyApiFactory` (Development, no
`Jwt:SigningKey`) and `ProductionWithoutSigningKeyApiFactory` /
`InvalidSigningKeyApiFactory` (no key / a broken key).

### 3. RED — how it was obtained

The API behaviour already existed, so the new tests turned green on their first
run. As in `docs/ai/013-application-coverage-gaps.md`, the RED was obtained by a
**temporary mutation** of two production lines, run, and then reverted
byte-identically (SHA-256 verified before and after):

- `project/src/CrudWorks.Api/Program.cs` — the CORS policy changed from
  `policy.WithOrigins(allowedOrigin)` to `policy.AllowAnyOrigin()`.
- `project/src/CrudWorks.Api/ErrorHandling/ProblemDetailsExceptionHandler.cs` — the
  `default:` branch changed from `title = "An unexpected error occurred."` to
  `title = exception.Message` (the leak the test must prevent).

```text
$ dotnet test tests\CrudWorks.Api.Tests\CrudWorks.Api.Tests.csproj --filter "FullyQualifiedName~ApiPipelineTests"

[xUnit.net 00:00:01.70]  CrudWorks.Api.Tests.Pipeline.ApiPipelineTests.Cors_PreflightFromAnUnknownOrigin_ReturnsNoAllowOriginHeader [FAIL]
[xUnit.net 00:00:01.82]  CrudWorks.Api.Tests.Pipeline.ApiPipelineTests.Cors_PreflightFromTheConfiguredOrigin_ReturnsTheOriginMethodsAndHeaders [FAIL]
[xUnit.net 00:00:01.84]  CrudWorks.Api.Tests.Pipeline.ApiPipelineTests.Cors_ActualRequestFromTheConfiguredOrigin_ReturnsTheAllowOriginHeader [FAIL]
[xUnit.net 00:00:02.20]  CrudWorks.Api.Tests.Pipeline.ApiPipelineTests.ProblemDetails_ForAnUnexpectedFailure_Returns500WithAGenericTitleAndNoInternalDetail [FAIL]

  Com falha CrudWorks.Api.Tests.Pipeline.ApiPipelineTests.Cors_PreflightFromAnUnknownOrigin_ReturnsNoAllowOriginHeader [1 s]
   Assert.False() Failure
  Com falha CrudWorks.Api.Tests.Pipeline.ApiPipelineTests.Cors_PreflightFromTheConfiguredOrigin_ReturnsTheOriginMethodsAndHeaders [2 ms]
   Assert.Equal() Failure: Strings differ
  Com falha CrudWorks.Api.Tests.Pipeline.ApiPipelineTests.Cors_ActualRequestFromTheConfiguredOrigin_ReturnsTheAllowOriginHeader [18 ms]
   Assert.Equal() Failure: Strings differ
  Com falha CrudWorks.Api.Tests.Pipeline.ApiPipelineTests.ProblemDetails_ForAnUnexpectedFailure_Returns500WithAGenericTitleAndNoInternalDetail [218 ms]
   Assert.Equal() Failure: Strings differ
     at CrudWorks.Api.Tests.Pipeline.ApiPipelineTests.ProblemDetails_ForAnUnexpectedFailure_Returns500WithAGenericTitleAndNoInternalDetail() in C:\Trabalho\IT_CrudWorks\project\tests\CrudWorks.Api.Tests\Pipeline\ApiPipelineTests.cs:line 76

Com falha! – Com falha:     4, Aprovado:     2, Ignorado:     0, Total:     6, Duração: 1 s - CrudWorks.Api.Tests.dll (net10.0)
Exited with code 1
```

The four failures are exactly the four tests that must fail when the CORS policy is
widened and when the `500` title leaks the exception message.

SHA-256 before the mutation and after the restore:

```text
Program.cs                        F5106AF2A5824E35949BD7F89C09F131C4238D9B5FC4615F93F8B663A1A6C0F6
ProblemDetailsExceptionHandler.cs E6A3888277F2E58D71EEB0F60FAA15912232A64BFA9F09CEACF8F37F3B097532
```

(Identical in both listings → the production code is exactly as it was before this
entry.)

### 4. GREEN

```text
$ dotnet test tests\CrudWorks.Api.Tests\CrudWorks.Api.Tests.csproj
Aprovado!  – Com falha:     0, Aprovado:    46, Ignorado:     0, Total:    46, Duração: 4 s - CrudWorks.Api.Tests.dll (net10.0)
```

### 5. Coverage after the change — and what the number actually means

`node docs/qa/tools/coverage-report.js --verify` (read-only):

| Assembly | Before (wave 1) | After |
| --- | ---: | ---: |
| `CrudWorks.Api` | 204/613 (33.28%) | **364/613 (59.38%)** |
| `CrudWorks.Application` | 224/236 (94.92%) | 236/236 (100.00%) — see `docs/ai/013` |
| `CrudWorks.Domain` | 112/112 (100.00%) | 112/112 (100.00%) |
| `CrudWorks.Infrastructure` | 432/453 (95.36%) | 432/453 (95.36%) — unchanged |

**Fact — the denominator explains the number.** Splitting the `CrudWorks.Api`
assembly of the API test run into hand-written code and compiler-generated code:

```text
CrudWorks.Api hand-written: 229/229 = 100.00%
CrudWorks.Api generated code: 135/384 = 35.16%   (Microsoft.AspNetCore.OpenApi.SourceGenerators...generated.cs)
```

Every hand-written line of the API project is now covered. The remaining 384 lines of
the denominator are the **OpenAPI XML-comment source generator** output under
`obj/Debug/`, which is not our code and is not meaningfully testable. So "Api at
59.38%" is a tooling artefact of what the OpenAPI generator emits, not 41% of the
API being untested.

**Re-measured independently (2026-11-12).** QA recomputed this split from the
Cobertura file itself and reproduced it; I then reproduced it a second time with
`node docs/qa/tools/inspect-coverage.js CrudWorks.Api` (read-only): the 9
hand-written files total **229/229** and the single generated file totals
**135/384**. The split agrees with QA's `docs/qa/coverage-summary.md`.

## Validation

- [x] Build: `dotnet build CrudWorks.slnx -warnaserror` → `Compilação com êxito. 0 Aviso(s) 0 Erro(s)`.
- [x] Unit tests: `dotnet test CrudWorks.slnx` → **165 passed, 0 failed, 0 skipped**
      (Domain 37, Application 57, Infrastructure 25, Api 46; was 143 / 34 Api).
- [x] Integration/API tests: the 46 API tests (34 before) run against the real
      pipeline through `WebApplicationFactory`, each with its own SQLite file.
- [x] Manual behavior: not applicable — no product behaviour changed. The live
      HTTP behaviour is exercised by the API tests themselves.
- [x] Security review: the new tests prove that (a) an unexpected failure answers
      `500` with a generic title and **no** exception message or type, (b) only
      `http://localhost:5173` receives `Access-Control-Allow-Origin`, (c) a valid
      token whose `sub` is not a GUID answers `401` instead of authenticating, and
      (d) a missing or malformed signing key stops the host outside Development.
- [x] Architecture review: unchanged. The new test hosts only replace an
      *Application-layer* interface (`IAuthService`) through `ConfigureTestServices`;
      no `DbContext` is mocked, no repository is faked in the API tests, and the
      dependency rule is untouched.

### Commands and real output

| Command | Result |
| --- | --- |
| `dotnet build CrudWorks.slnx -warnaserror` (before) | `Compilação com êxito. 0 Aviso(s) 0 Erro(s)` |
| `dotnet test CrudWorks.slnx` (before) | 37 + 47 + 25 + 34 = **143 passed, 0 failed** |
| `dotnet test …Api.Tests… --filter FullyQualifiedName~ApiPipelineTests` (mutated source) | **4 failed, 2 passed** (RED) |
| `dotnet test …Api.Tests…` (restored source) | **46 passed, 0 failed** (GREEN) |
| `dotnet build CrudWorks.slnx -warnaserror` (after) | `Compilação com êxito. 0 Aviso(s) 0 Erro(s)` |
| `dotnet test CrudWorks.slnx` (after) | 37 + 57 + 25 + 46 = **165 passed, 0 failed, 0 skipped** |
| `dotnet test CrudWorks.slnx --collect:"XPlat Code Coverage"` | 4 × `coverage.cobertura.xml` produced |
| `node docs/qa/tools/coverage-report.js --verify` | 4/4 parser self-checks MATCH; Api 59.38% (100% of hand-written code) |
| `node docs/qa/tools/coverage-report.js --verify` (re-run 2026-11-12, read-only, no `--write`) | 4/4 parser self-checks MATCH; Api 364/613 (59.38%), Application 236/236 (100.00%), Domain 112/112 (100.00%), Infrastructure 432/453 (95.36%) |
| `node docs/qa/tools/inspect-coverage.js CrudWorks.Api` (2026-11-12, read-only) | hand-written 9 files = 38+12+13+22+33+80+5+24+2 = **229/229 (100.00%)**; generated `OpenApiXmlCommentSupport.generated.cs` = 135/384 (35.16%); TOTAL 364/613 = 59.38% |
| `node docs/qa/tools/inspect-coverage.js CrudWorks.Infrastructure` (2026-11-12, read-only) | `TOTAL 432/453 = 95.36%`; uncovered = design-time factory 13-16/18-19, `InitialCreate` 38-41, `AddTasks` 46-49, seeder 36-37/61-63, `UnitOfWork` 34-35; `PasswordHasherAdapter.cs` **18/18 = 100.00%** |
| `dotnet test CrudWorks.slnx` (re-run 2026-11-12) | 37 + 57 + 25 + 46 = **165 passed, 0 failed, 0 skipped** |

## Corrections

| What the AI (or the wave-1 report) asserted | What was wrong / what changed | Why |
| --- | --- | --- |
| "`CrudWorks.Api` line coverage is 33.28% — outside the DoD floor" | The number is real, but the reading "the API is untested" is not: a large part of the denominator is compiler-generated OpenAPI code. After the change, 100% of the hand-written API code is covered. | A coverage percentage without the denominator breakdown invites a wrong conclusion in the presentation. |
| A CORS test could assert the `Access-Control-Allow-Origin` header value | With `AllowAnyOrigin()` the header is `*`, so the "allowed origin" assertion also fails under the mutation. The tests therefore assert the **exact origin string**, not just the header's presence — except for the negative case, which asserts the header is **absent** for an unknown origin. | A presence-only assertion would not catch a wildcard origin. |
| The `500` branch could be reached by sending a malformed body | It cannot: a malformed body is rejected by the MVC input formatter (`400` through the framework's own `ValidationProblemDetails`), never by the exception handler. The branch is reached by replacing the Application service with a throwing stub through `ConfigureTestServices` — a test double at the **Application** boundary, not a mocked `DbContext`. | Keeps the test at the right level (AGENTS.md §4: assert behaviour, not implementation). |
| The `JwtSigningKey` type is `internal`, so it cannot be unit tested | True — and no `InternalsVisibleTo` was added. The behaviour is exercised through the real host: the missing-key and broken-key cases are proven by the **startup failure**, and the Development fallback by a token the API issued being accepted while a token signed with the configured test key is rejected. | No production/assembly-visibility change for a test; the observable behaviour is what matters. |
| A leftover `CrudWorks.Api` process (PID 10512) from the wave-1 QA run held `bin\Debug\net10.0\CrudWorks.Application.dll` open, and `dotnet build` failed with `MSB3026` | The stray QA API instance was stopped (`Stop-Process -Id 10512`) and the build was re-run. It was a local dev instance of the QA clean run, not repository state. | The build could not proceed otherwise; the QA tooling can restart it with `node docs/qa/tools/api-ctl.js start`. |
| The first restore of the mutated files left the original timestamps, so MSBuild reused the stale mutated DLL and 20 tests failed | The files were re-stamped and rebuilt, and the SHA-256 of both was compared with the pre-mutation listing. | A restored file that is not rebuilt proves nothing. |

### Corrections applied on 2026-11-12 (post-review, by the author of this entry)

The QA engineer re-measured the coverage with the aggregator and found two factual
errors in the gap list published by this entry. **Both claims were checked against
the code and against the Cobertura files before any edit was made**, and both were
confirmed to be wrong; the entries are corrected in place, not silently rewritten.

| What this entry had asserted | What was wrong | Corrected statement | How it was settled |
| --- | --- | --- | --- |
| The uncovered migration lines are "the `Up` methods of the two migrations" | The `Up` methods are covered; the uncovered lines are the **`Down` (rollback) bodies** | `20261112021752_InitialCreate.cs:38-41` = the body of `Down` (`migrationBuilder.DropTable(name: "Users")`); `20261112184118_AddTasks.cs:46-49` = the body of `Down` (`migrationBuilder.DropTable(name: "Tasks")`) | Read the files: `Down` is declared at `20261112021752_InitialCreate.cs:37` and `20261112184118_AddTasks.cs:45`, and the uncovered ranges are exactly the lines that follow. Independently confirmed by `node docs/qa/tools/inspect-coverage.js CrudWorks.Infrastructure` → `UNCOVERED: 38, 39, 40, 41` and `UNCOVERED: 46, 47, 48, 49`, and by the `Up` methods being executed by `Migration_AppliesInitialCreate` (`UserRepositoryTests.cs:31`) and `Migration_AppliesAddTasks` (`TaskRepositoryTests.cs:41`), both of which run `Database.MigrateAsync()` in their fixture (`UserRepositoryTests.cs:22`, `TaskRepositoryTests.cs:26`). |
| "the `PasswordHasherAdapter` fallback branch" is an open gap | `Security/PasswordHasherAdapter.cs` is fully covered | 18/18 (100.00%) — the `catch (FormatException) { return false; }` fallback (`PasswordHasherAdapter.cs:40-44`) is executed by `Verify_WithMalformedHash_ReturnsFalse` (`project/tests/CrudWorks.Infrastructure.Tests/Security/PasswordHasherAdapterTests.cs:35`), and the null/empty-hash dummy-hash path (`PasswordHasherAdapter.cs:32`) by `Verify_WithNullHash_ReturnsFalse` / `Verify_WithEmptyHash_ReturnsFalse` (`PasswordHasherAdapterTests.cs:44` and `:50`) | `node docs/qa/tools/inspect-coverage.js CrudWorks.Infrastructure` → `18/18 100.00%  Security/PasswordHasherAdapter.cs` (no uncovered line listed). |
| "`UnitOfWork` (the two `SaveChangesAsync` branches)" and "three seeder branches" | Both descriptions were imprecise | `UnitOfWork.cs:34-35` is the `return false` guard inside `IsUniqueConstraintViolation` (the non-SQLite inner-exception path), not a `SaveChangesAsync` branch; the seeder contributes **5** lines (`DatabaseSeeder.cs:36-37` and `:61-63`), not three | Read `project/src/CrudWorks.Infrastructure/Persistence/UnitOfWork.cs:28-43` and `.../Seed/DatabaseSeeder.cs:30-71`. |
| "`docs/qa/coverage-summary.md` still carries the wave-1 numbers (Api 33.28%, Application 94.92%)" | Stale — QA refreshed it on 2026-11-12 (wave 2) | QA's artifact now carries Api 364/613 (59.38%), Application 236/236 (100.00%), Domain 112/112 (100.00%), Infrastructure 432/453 (95.36%) and the 165-test count; it agrees with the numbers in this entry | Read `docs/qa/coverage-summary.md:9-30` and re-ran `node docs/qa/tools/coverage-report.js --verify` (read-only, without `--write`) → identical figures. |

The hand-written/generated split claimed in §5 of this entry (**229/229 = 100.00%**
hand-written, **135/384 = 35.16%** compiler-generated) was **not** part of the
finding and was re-verified independently: `node docs/qa/tools/inspect-coverage.js
CrudWorks.Api` lists the 9 hand-written files at 38+12+13+22+33+80+5+24+2 =
**229/229** and the single generated file `OpenApiXmlCommentSupport.generated.cs`
at 135/384, total 364/613 = 59.38%. It agrees with QA's
`docs/qa/coverage-summary.md:161-181`.

## Edge cases considered

- **Preflight from an unknown origin.** The request is answered, but **without** an
  `Access-Control-Allow-Origin` header — the browser, not the server, is what
  enforces the read. The test asserts the header is absent.
- **Actual (non-preflight) request from the allowed origin.** Asserted separately:
  the header must be present on a normal `GET` too, not only on a preflight.
- **Development fallback key.** The ephemeral key is a process-wide random value
  (`project/src/CrudWorks.Api/Security/JwtSigningKey.cs:29`), so a token signed
  with the *configured* test key must be rejected. That asymmetry is what proves
  the fallback is really in use, instead of a test that would also pass if the
  configured key had silently been used.
- **A signing key that is valid base64 but too short** (16 bytes) is rejected with
  its own message, separately from "not base64". Both are startup failures in
  *every* environment, including Development.
- **A token with a non-GUID `sub`.** The signature, issuer, audience and lifetime
  are all valid; only the subject cannot be parsed. The answer must be `401`
  `Authentication required`, not a `500` and not an authenticated call.
- **The `500` body.** Asserted to contain neither the internal exception message
  nor the exception type name.

## Auth and validation decisions

- No product behaviour was changed; the two temporarily mutated production files
  were restored byte-identically (SHA-256 above).
- The CORS policy stays restricted to `Cors:AllowedOrigin`
  (`http://localhost:5173` by default, `project/src/CrudWorks.Api/Program.cs:71-79`).
  Credentials are still not allowed; the token travels in the `Authorization`
  header.
- The `500` answer stays generic (`An unexpected error occurred.`), and the
  exception is still logged server-side with the method and path
  (`project/src/CrudWorks.Api/ErrorHandling/ProblemDetailsExceptionHandler.cs:44-48`).
- The signing-key rule stays: configuration wins; Development falls back to an
  ephemeral key with a warning; any other environment fails fast
  (`project/src/CrudWorks.Api/Security/JwtSigningKey.cs:16-31`).

## Files affected

- `project/tests/CrudWorks.Api.Tests/Pipeline/ApiPipelineTests.cs` (new)
- `project/tests/CrudWorks.Api.Tests/Pipeline/JwtSigningKeyTests.cs` (new)
- `project/tests/CrudWorks.Api.Tests/HealthTests.cs` (+1 test)
- `project/tests/CrudWorks.Api.Tests/Auth/AuthApiTests.cs` (+1 test)
- `docs/ai/014-api-critical-paths.md` (this entry)

**Post-review correction (2026-11-12):** only this entry and
`docs/ai/013-application-coverage-gaps.md` were edited. No production code and no
test was touched by the correction.

**No file under `project/src/**` was modified** — `Program.cs` and
`ProblemDetailsExceptionHandler.cs` were only temporarily mutated to obtain the RED
and were then restored byte-identically.

## Open questions / known gaps

- **Infrastructure gaps (not QA-04, reported for completeness).** The re-measured
  Infrastructure coverage is **432/453 (95.36%)**, i.e. exactly **21 uncovered
  lines**, all of them named here. Re-verified by me on 2026-11-12 with
  `node docs/qa/tools/inspect-coverage.js CrudWorks.Infrastructure`
  (read-only, no write to `docs/qa/**`):

  | File (under `project/src/CrudWorks.Infrastructure/`) | Uncovered lines | What is not executed |
  | --- | --- | --- |
  | `Persistence/DesignTimeDbContextFactory.cs` | 13, 14, 15, 16, 18, 19 | the whole `CreateDbContext` body — a `dotnet ef` tooling factory that no runtime path calls |
  | `Persistence/Migrations/20261112021752_InitialCreate.cs` | 38, 39, 40, 41 | the body of the **`Down` (rollback)** method — `migrationBuilder.DropTable(name: "Users")` |
  | `Persistence/Migrations/20261112184118_AddTasks.cs` | 46, 47, 48, 49 | the body of the **`Down` (rollback)** method — `migrationBuilder.DropTable(name: "Tasks")` |
  | `Persistence/Seed/DatabaseSeeder.cs` | 36, 37 | the early `return` in `SeedDemoUserAsync` when the `Users` table already has rows |
  | `Persistence/Seed/DatabaseSeeder.cs` | 61, 62, 63 | the `demoUserId is { } explicitUserId` branch of `SeedDemoTasksAsync` |
  | `Persistence/UnitOfWork.cs` | 34, 35 | the `return false` guard in `IsUniqueConstraintViolation` when the inner exception is **not** a SQLite unique-constraint error |

  6 + 4 + 4 + 2 + 3 + 2 = **21**. The `Up` (apply) methods of both migrations
  **are** covered — by `Migration_AppliesInitialCreate`
  (`project/tests/CrudWorks.Infrastructure.Tests/Persistence/UserRepositoryTests.cs:31`)
  and `Migration_AppliesAddTasks`
  (`project/tests/CrudWorks.Infrastructure.Tests/Persistence/TaskRepositoryTests.cs:41`);
  only the rollback path is unproven. `Security/PasswordHasherAdapter.cs` is
  **18/18 (100.00%)** and is **not** a gap. All of these are outside the DoD
  floor (Infrastructure is at 95.36%) and outside QA-04. **Recommendation:** let
  the orchestrator decide whether they are worth a follow-up; the design-time
  factory and the two migration rollback bodies are the least valuable.
- **`docs/qa/coverage-summary.md` has been refreshed by QA** (wave 2, dated
  2026-11-12): Api 364/613 (59.38%), Application 236/236 (100.00%), Domain
  112/112 (100.00%), Infrastructure 432/453 (95.36%), 165 tests passed. Those
  numbers agree with this entry; QA's artifact is the tracked one and I did not
  modify it.
- **The API percentage will stay below 80%** as long as the OpenAPI
  source-generator output is counted in the denominator. If the panel reads the
  number, the hand-written/generated split above is the honest way to present it.

## Final decision

**Accepted.** QA-04 is closed for every path I classified as critical: the RFC 7807
contract (including the `type` URLs, the `traceId` and the `500` branch), the CORS
restriction, the health-check contract shape, the `JwtSigningKey` resolution in
Development and outside it, the non-GUID subject, and the OpenAPI document. All
hand-written API code is now covered (229/229); the remaining denominator is
compiler-generated code. The Infrastructure gaps are reported as a known gap rather
than chased — the list is now the corrected one (21 lines: the design-time factory,
the two migration **`Down`** bodies, five seeder lines and the two
`UnitOfWork` guard lines; `PasswordHasherAdapter` is at 100%).
