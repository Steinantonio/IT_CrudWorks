# QA coverage summary

- **Date:** 2026-11-12
- **Wave:** wave 4 - final single-tree gate, re-measured by QA
- **Agent:** QA engineer
- **Environment:** Windows 11, .NET SDK 10.0.404, Node v26.13.1
- **Command:** `dotnet test CrudWorks.slnx --collect:"XPlat Code Coverage"` (from `project/`)
- **Aggregator:** `node docs/qa/tools/coverage-report.js --verify --write`
- **Test counts in the same run:** 165 passed, 0 failed, 0 skipped (Domain 37 / Application 57 / Infrastructure 25 / Api 46) - same run as the single-tree gate
- **Unit of measurement:** source line of a source file (de-duplicated per file, like
  Coverlet); a line counts as covered when any of the four test projects executed it.
- **Parser self-check:** `--verify` recomputes each Cobertura file's own header numbers
  (`lines-covered` / `lines-valid`) and compares them — 8/8 MATCH.

## Result by assembly (merged across the 4 test projects)

| Assembly | Lines covered | Lines valid | Line coverage |
| --- | ---: | ---: | ---: |
| CrudWorks.Api | 364 | 613 | 59.38% |
| CrudWorks.Application | 236 | 236 | 100.00% |
| CrudWorks.Domain | 112 | 112 | 100.00% |
| CrudWorks.Infrastructure | 432 | 453 | 95.36% |

## DoD floor (§6: at least 80% line coverage on Domain + Application)

| Scope | Lines covered | Lines valid | Line coverage | Floor | Verdict |
| --- | ---: | ---: | ---: | ---: | --- |
| CrudWorks.Domain | 112 | 112 | 100.00% | 80% | PASS |
| CrudWorks.Application | 236 | 236 | 100.00% | 80% | PASS |
| **Domain + Application combined** | 348 | 348 | 100.00% | 80% | PASS |

### Uncovered lines per file (Domain and Application)

#### CrudWorks.Domain

**No uncovered lines.** Every source line of CrudWorks.Domain is executed by at least
one test (112/112 = 100.00%). The
per-file breakdown below is kept as evidence; the "Uncovered line numbers" column is
empty for every file, not omitted.

| File | Lines covered / valid | Line coverage | Uncovered line numbers |
| --- | ---: | ---: | --- |
| Common/DomainException.cs | 1 / 1 | 100.00% | — |
| Tasks/TaskItem.cs | 71 / 71 | 100.00% | — |
| Users/User.cs | 40 / 40 | 100.00% | — |

#### CrudWorks.Application

**No uncovered lines.** Every source line of CrudWorks.Application is executed by at least
one test (236/236 = 100.00%). The
per-file breakdown below is kept as evidence; the "Uncovered line numbers" column is
empty for every file, not omitted.

| File | Lines covered / valid | Line coverage | Uncovered line numbers |
| --- | ---: | ---: | --- |
| Auth/AuthMapping.cs | 2 / 2 | 100.00% | — |
| Auth/AuthService.cs | 48 / 48 | 100.00% | — |
| Auth/AuthUserResponse.cs | 5 / 5 | 100.00% | — |
| Auth/AuthValidation.cs | 45 / 45 | 100.00% | — |
| Auth/JwtOptions.cs | 4 / 4 | 100.00% | — |
| Auth/LoginRequest.cs | 1 / 1 | 100.00% | — |
| Auth/LoginResponse.cs | 4 / 4 | 100.00% | — |
| Auth/RegisterRequest.cs | 1 / 1 | 100.00% | — |
| Auth/TokenResult.cs | 1 / 1 | 100.00% | — |
| Auth/UserSummary.cs | 1 / 1 | 100.00% | — |
| Common/Exceptions/ConflictException.cs | 1 / 1 | 100.00% | — |
| Common/Exceptions/NotFoundException.cs | 1 / 1 | 100.00% | — |
| Common/Exceptions/UnauthorizedException.cs | 1 / 1 | 100.00% | — |
| Common/Exceptions/ValidationException.cs | 2 / 2 | 100.00% | — |
| Tasks/CreateTaskRequest.cs | 1 / 1 | 100.00% | — |
| Tasks/TaskMapping.cs | 2 / 2 | 100.00% | — |
| Tasks/TaskResponse.cs | 8 / 8 | 100.00% | — |
| Tasks/TaskService.cs | 46 / 46 | 100.00% | — |
| Tasks/TaskValidation.cs | 61 / 61 | 100.00% | — |
| Tasks/UpdateTaskRequest.cs | 1 / 1 | 100.00% | — |

### Uncovered lines per class (Domain and Application)

Class-level numbers can exceed the file-level numbers above because a file may host
several classes (for example `TaskService` and its async state machine).

#### CrudWorks.Domain

| Class | File | Lines covered / valid | Line coverage | Uncovered line numbers |
| --- | --- | ---: | ---: | --- |
| CrudWorks.Domain.Common.DomainException | Common/DomainException.cs | 1 / 1 | 100.00% | — |
| CrudWorks.Domain.Tasks.TaskItem | Tasks/TaskItem.cs | 71 / 71 | 100.00% | — |
| CrudWorks.Domain.Users.User | Users/User.cs | 40 / 40 | 100.00% | — |

#### CrudWorks.Application

| Class | File | Lines covered / valid | Line coverage | Uncovered line numbers |
| --- | --- | ---: | ---: | --- |
| CrudWorks.Application.Auth.AuthMapping | Auth/AuthMapping.cs | 2 / 2 | 100.00% | — |
| CrudWorks.Application.Auth.AuthService | Auth/AuthService.cs | 15 / 15 | 100.00% | — |
| CrudWorks.Application.Auth.AuthService/&lt;GetCurrentUserAsync&gt;d__11 | Auth/AuthService.cs | 5 / 5 | 100.00% | — |
| CrudWorks.Application.Auth.AuthService/&lt;LoginAsync&gt;d__9 | Auth/AuthService.cs | 13 / 13 | 100.00% | — |
| CrudWorks.Application.Auth.AuthService/&lt;RegisterAsync&gt;d__8 | Auth/AuthService.cs | 15 / 15 | 100.00% | — |
| CrudWorks.Application.Auth.AuthUserResponse | Auth/AuthUserResponse.cs | 5 / 5 | 100.00% | — |
| CrudWorks.Application.Auth.AuthValidation | Auth/AuthValidation.cs | 45 / 45 | 100.00% | — |
| CrudWorks.Application.Auth.JwtOptions | Auth/JwtOptions.cs | 4 / 4 | 100.00% | — |
| CrudWorks.Application.Auth.LoginRequest | Auth/LoginRequest.cs | 1 / 1 | 100.00% | — |
| CrudWorks.Application.Auth.LoginResponse | Auth/LoginResponse.cs | 4 / 4 | 100.00% | — |
| CrudWorks.Application.Auth.RegisterRequest | Auth/RegisterRequest.cs | 1 / 1 | 100.00% | — |
| CrudWorks.Application.Auth.TokenResult | Auth/TokenResult.cs | 1 / 1 | 100.00% | — |
| CrudWorks.Application.Auth.UserSummary | Auth/UserSummary.cs | 1 / 1 | 100.00% | — |
| CrudWorks.Application.Common.Exceptions.ConflictException | Common/Exceptions/ConflictException.cs | 1 / 1 | 100.00% | — |
| CrudWorks.Application.Common.Exceptions.NotFoundException | Common/Exceptions/NotFoundException.cs | 1 / 1 | 100.00% | — |
| CrudWorks.Application.Common.Exceptions.UnauthorizedException | Common/Exceptions/UnauthorizedException.cs | 1 / 1 | 100.00% | — |
| CrudWorks.Application.Common.Exceptions.ValidationException | Common/Exceptions/ValidationException.cs | 2 / 2 | 100.00% | — |
| CrudWorks.Application.Tasks.CreateTaskRequest | Tasks/CreateTaskRequest.cs | 1 / 1 | 100.00% | — |
| CrudWorks.Application.Tasks.TaskMapping | Tasks/TaskMapping.cs | 2 / 2 | 100.00% | — |
| CrudWorks.Application.Tasks.TaskResponse | Tasks/TaskResponse.cs | 8 / 8 | 100.00% | — |
| CrudWorks.Application.Tasks.TaskService | Tasks/TaskService.cs | 6 / 6 | 100.00% | — |
| CrudWorks.Application.Tasks.TaskService/&lt;&gt;c | Tasks/TaskService.cs | 1 / 1 | 100.00% | — |
| CrudWorks.Application.Tasks.TaskService/&lt;CreateAsync&gt;d__6 | Tasks/TaskService.cs | 13 / 13 | 100.00% | — |
| CrudWorks.Application.Tasks.TaskService/&lt;DeleteAsync&gt;d__8 | Tasks/TaskService.cs | 5 / 5 | 100.00% | — |
| CrudWorks.Application.Tasks.TaskService/&lt;GetAsync&gt;d__5 | Tasks/TaskService.cs | 4 / 4 | 100.00% | — |
| CrudWorks.Application.Tasks.TaskService/&lt;ListAsync&gt;d__4 | Tasks/TaskService.cs | 3 / 3 | 100.00% | — |
| CrudWorks.Application.Tasks.TaskService/&lt;RequireTaskAsync&gt;d__9 | Tasks/TaskService.cs | 2 / 2 | 100.00% | — |
| CrudWorks.Application.Tasks.TaskService/&lt;UpdateAsync&gt;d__7 | Tasks/TaskService.cs | 12 / 12 | 100.00% | — |
| CrudWorks.Application.Tasks.TaskValidation | Tasks/TaskValidation.cs | 61 / 61 | 100.00% | — |
| CrudWorks.Application.Tasks.UpdateTaskRequest | Tasks/UpdateTaskRequest.cs | 1 / 1 | 100.00% | — |

**Verdict:** PASS — the 80% line-coverage floor of DoD §6 is met for
Domain and Application.

## CrudWorks.Infrastructure — line coverage

| Scope | Lines covered | Lines valid | Line coverage |
| --- | ---: | ---: | ---: |
| CrudWorks.Infrastructure (whole assembly) | 432 | 453 | 95.36% |
| hand-written sources | 432 | 453 | 95.36% |
| compiler-generated sources | 0 | 0 | n/a |

**21 uncovered lines in total** — every one of them is named below.

| File | Lines covered / valid | Line coverage | Uncovered line numbers |
| --- | ---: | ---: | --- |
| Persistence/Configurations/TaskConfiguration.cs | 31 / 31 | 100.00% | — |
| Persistence/Configurations/UserConfiguration.cs | 19 / 19 | 100.00% | — |
| Persistence/CrudWorksDbContext.cs | 6 / 6 | 100.00% | — |
| Persistence/DesignTimeDbContextFactory.cs | 0 / 6 | 0.00% | 13, 14, 15, 16, 18, 19 |
| Persistence/Migrations/20261112021752_InitialCreate.cs | 21 / 25 | 84.00% | 38, 39, 40, 41 |
| Persistence/Migrations/20261112021752_InitialCreate.Designer.cs | 33 / 33 | 100.00% | — |
| Persistence/Migrations/20261112184118_AddTasks.cs | 29 / 33 | 87.88% | 46, 47, 48, 49 |
| Persistence/Migrations/20261112184118_AddTasks.Designer.cs | 79 / 79 | 100.00% | — |
| Persistence/Migrations/CrudWorksDbContextModelSnapshot.cs | 79 / 79 | 100.00% | — |
| Persistence/Seed/DatabaseSeeder.cs | 47 / 52 | 90.38% | 36, 37, 61, 62, 63 |
| Persistence/TaskRepository.cs | 18 / 18 | 100.00% | — |
| Persistence/UnitOfWork.cs | 17 / 19 | 89.47% | 34, 35 |
| Persistence/UserRepository.cs | 10 / 10 | 100.00% | — |
| Security/JwtTokenService.cs | 25 / 25 | 100.00% | — |
| Security/PasswordHasherAdapter.cs | 18 / 18 | 100.00% | — |

The per-file line numbers are `file:line` inside
`project/src/CrudWorks.Infrastructure/`.

## CrudWorks.Api — line coverage

| Scope | Lines covered | Lines valid | Line coverage |
| --- | ---: | ---: | ---: |
| CrudWorks.Api (whole assembly) | 364 | 613 | 59.38% |
| hand-written sources | 229 | 229 | 100.00% |
| compiler-generated sources | 135 | 384 | 35.16% |

### Hand-written vs compiler-generated split

The assembly percentage is dominated by the OpenAPI XML-comment source generator,
which emits code under `obj/Debug/` that the team neither writes nor can meaningfully
test. The split below is measured from the Cobertura file, not assumed.

| Origin | Files | Lines covered | Lines valid | Line coverage |
| --- | ---: | ---: | ---: | ---: |
| hand-written | 9 | 229 | 229 | 100.00% |
| compiler-generated | 1 | 135 | 384 | 35.16% |

### Uncovered lines in the hand-written API sources

**None.** Every hand-written line of `CrudWorks.Api` is executed by at least one test
(229/229).

### Uncovered lines in the compiler-generated API sources

`obj/Debug/net10.0/Microsoft.AspNetCore.OpenApi.SourceGenerators/Microsoft.AspNetCore.OpenApi.SourceGenerators.XmlCommentGenerator/OpenApiXmlCommentSupport.generated.cs` — 135/384 (35.16%), uncovered lines:

20, 21, 22, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 58, 61, 88, 89, 102, 103, 120, 121, 122, 123, 124, 125, 126, 127, 129, 130, 131, 132, 142, 143, 144, 145, 147, 148, 149, 152, 153, 154, 155, 156, 158, 159, 170, 171, 187, 188, 189, 194, 195, 196, 197, 240, 242, 243, 244, 246, 247, 248, 251, 252, 268, 269, 270, 296, 297, 298, 332, 333, 351, 352, 355, 356, 357, 358, 359, 360, 361, 362, 363, 364, 365, 366, 367, 368, 369, 370, 371, 372, 373, 374, 375, 376, 377, 378, 379, 380, 381, 382, 383, 385, 386, 387, 388, 389, 390, 391, 392, 393, 394, 395, 397, 398, 399, 400, 401, 402, 403, 404, 405, 407, 408, 409, 410, 411, 413, 414, 415, 416, 417, 418, 419, 420, 421, 422, 423, 424, 432, 433, 434, 435, 436, 437, 438, 439, 440, 441, 442, 443, 444, 445, 446, 447, 448, 449, 450, 451, 452, 453, 454, 455, 456, 458, 459, 460, 461, 462, 463, 464, 465, 467, 468, 469, 470, 471, 472, 473, 474, 475, 476, 477, 484, 485, 486, 487, 488, 489, 492, 493, 496, 497, 498, 501, 502, 504, 514, 515, 516, 517, 518, 519, 520, 526, 527, 528, 529, 530, 531, 532, 533, 534, 535, 536, 537, 538, 539, 541, 542, 543, 544, 545, 546, 548, 550, 551, 552, 553, 554, 555, 556, 557, 558, 559, 569, 570, 571, 572, 576, 577, 579, 580, 582, 584, 586, 587, 588, 591

## Raw coverage files

Current generation (the newest coverage run per test project — this is the
measurement the tables above describe):

- `project\tests\CrudWorks.Domain.Tests\TestResults\766e5398-d6df-42ce-8a57-24f70edd676b\coverage.cobertura.xml`
- `project\tests\CrudWorks.Application.Tests\TestResults\43acf42e-8bcd-49d0-99c0-7a350d1c6ab0\coverage.cobertura.xml`
- `project\tests\CrudWorks.Infrastructure.Tests\TestResults\0e4adfa0-354b-4068-9605-3eb20514e079\coverage.cobertura.xml`
- `project\tests\CrudWorks.Api.Tests\TestResults\ba356267-e417-4593-966e-c0c5e35dc672\coverage.cobertura.xml`
- `project\tests\CrudWorks.Domain.Tests\TestResults\6b0a46fd-15be-476a-b582-dfbbbaf7fc18\coverage.cobertura.xml`
- `project\tests\CrudWorks.Application.Tests\TestResults\f61d8ffe-00e0-4dde-9cd3-ef8324f754d9\coverage.cobertura.xml`
- `project\tests\CrudWorks.Infrastructure.Tests\TestResults\91254f3a-2b46-4237-8a3f-818c558b537d\coverage.cobertura.xml`
- `project\tests\CrudWorks.Api.Tests\TestResults\4f730a85-4cf3-4623-9891-80d2a61dbb94\coverage.cobertura.xml`

Superseded (older runs of the same test projects — identical measurement, not merged):

- `project\tests\CrudWorks.Domain.Tests\TestResults\766e5398-d6df-42ce-8a57-24f70edd676b\coverage.cobertura.xml` (stale)
- `project\tests\CrudWorks.Application.Tests\TestResults\43acf42e-8bcd-49d0-99c0-7a350d1c6ab0\coverage.cobertura.xml` (stale)
- `project\tests\CrudWorks.Infrastructure.Tests\TestResults\0e4adfa0-354b-4068-9605-3eb20514e079\coverage.cobertura.xml` (stale)
- `project\tests\CrudWorks.Api.Tests\TestResults\ba356267-e417-4593-966e-c0c5e35dc672\coverage.cobertura.xml` (stale)

The Cobertura XML files live under `project/tests/*/TestResults/` and are gitignored
(`project/.gitignore`: `TestResults/`, `coverage/`). This markdown summary is the tracked artifact.

## Reading of the gaps (QA)

<!-- QA:NOTES:START -->
This block is written by the QA engineer. Everything outside the markers is
regenerated by `docs/qa/tools/coverage-report.js`; the tool preserves this block
verbatim across re-runs.

### Scope and currency of this measurement

This document covers the **backend** coverage only. It was **re-measured by QA
in wave 4** (2026-11-12) on the **final tree** — the same tree the full
single-tree gate ran on (`docs/qa/clean-run-report.md` §"Full gate on one
tree"):

```
dotnet test CrudWorks.slnx --collect:"XPlat Code Coverage"   (from project/)
node docs/qa/tools/coverage-report.js --verify --write
```

The percentages below are that wave-4 measurement. They are **identical** to the
wave-2 numbers (the wave-3 and wave-4 changes touched no backend source), which
is exactly what the wave-4 re-run was meant to prove rather than assume.

**The backend and the frontend gates are no longer two separate runs.** The
wave-4 run executed the whole gate back to back on one tree — backend build
with warnings as errors, backend tests, coverage collector, `npm run lint`,
`npm run test`, `npm run build`, the live API checks (37/37) and the
leak-detector self-test (6/6). The frontend suite (10 files / 69 cases, lint
exit 0, 183 modules; **wave 5 re-measured it at 11 files / 80 cases and 185
modules — see `docs/ai/020`**) was measured **on that same tree**; the earlier
split — backend in wave 2, frontend in wave 3 — is kept in
`docs/qa/clean-run-report.md` §"Gate provenance" as history and is now closed
(**QA-07**).

### What changed since the wave-1 summary

| Scope | Wave 1 (wave-1 clean run) | Wave 2 (backend re-measurement) | Wave 4 (final tree, this document) |
| --- | --- | --- | --- |
| Backend test cases | 143 (37 / 47 / 25 / 34) | **165 (37 / 57 / 25 / 46)** | **165 (37 / 57 / 25 / 46)** |
| `CrudWorks.Domain` | 112/112 (100.00%) | 112/112 (100.00%) | 112/112 (100.00%) |
| `CrudWorks.Application` | 224/236 (94.92%) | **236/236 (100.00%)** | **236/236 (100.00%)** |
| Domain + Application combined | 336/348 (96.55%) | **348/348 (100.00%)** | **348/348 (100.00%)** |
| `CrudWorks.Infrastructure` | 432/453 (95.36%) | 432/453 (95.36%) | 432/453 (95.36%) |
| `CrudWorks.Api` | 204/613 (33.28%) | 364/613 (59.38%) | 364/613 (59.38%) |

QA-03 (the 12 uncovered `Application` lines) and QA-04 (the API critical paths)
were closed by the backend implementer in `docs/ai/013-application-coverage-gaps.md`
and `docs/ai/014-api-critical-paths.md`. QA re-ran the suites and the coverage
collector in wave 2 **and again in wave 4** (2026-11-12) and reproduced those
numbers **from the Cobertura files themselves**; the tables in this document are
QA's own measurement, not a copy of the implementer's report.

### `CrudWorks.Infrastructure` — the 21 uncovered lines, named

| File (under `project/src/CrudWorks.Infrastructure/`) | Uncovered lines | What is not executed | Value | Owner |
| --- | --- | --- | --- | --- |
| `Persistence/DesignTimeDbContextFactory.cs` | 13, 14, 15, 16, 18, 19 | the whole `CreateDbContext` body — a `dotnet ef` tooling factory that no runtime path calls | Low — only the EF CLI uses it | Backend implementer (optional) |
| `Persistence/Migrations/20261112021752_InitialCreate.cs` | 38, 39, 40, 41 | the body of the `Down` (rollback) method — `migrationBuilder.DropTable(name: "Users")` | Low — no test ever rolls a schema back | Backend implementer (optional) |
| `Persistence/Migrations/20261112184118_AddTasks.cs` | 46, 47, 48, 49 | the body of the `Down` (rollback) method — `migrationBuilder.DropTable(name: "Tasks")` | Low — same as above | Backend implementer (optional) |
| `Persistence/Seed/DatabaseSeeder.cs` | 36, 37 | the early `return` in `SeedDemoUserAsync` when the `Users` table already has rows | Medium — the "do not re-seed the demo user" path is not proven | Backend implementer |
| `Persistence/Seed/DatabaseSeeder.cs` | 61, 62, 63 | the `demoUserId is { } explicitUserId` branch of `SeedDemoTasksAsync` | Low — only the explicit-id overload reaches it | Backend implementer (optional) |
| `Persistence/UnitOfWork.cs` | 34, 35 | the `return false` guard in `IsUniqueConstraintViolation` when the inner exception is **not** a SQLite unique-constraint error | Medium — the non-SQLite path of the conflict mapping is not proven | Backend implementer |

6 + 4 + 4 + 2 + 3 + 2 = **21 uncovered lines**, all of them named above. No line
is omitted from this table, and no other Infrastructure file has an uncovered
line.

**Correction to `docs/ai/014`.** That entry states that the uncovered migration
lines are "the `Up` methods of the two migrations". They are not. The `Up` methods
are executed by `Migration_AppliesInitialCreate`
(`project/tests/CrudWorks.Infrastructure.Tests/Persistence/UserRepositoryTests.cs:31`)
and `Migration_AppliesAddTasks`
(`project/tests/CrudWorks.Infrastructure.Tests/Persistence/TaskRepositoryTests.cs:41`),
and the coverage agrees: the uncovered lines are `38-41` and `46-49`, which are
the bodies of the **`Down` (rollback)** methods — read directly from
`20261112021752_InitialCreate.cs:37-41` and `20261112184118_AddTasks.cs:45-49`.
The distinction matters: rolling a schema back is a different behaviour from
applying it, and only the rollback is unproven.

**Correction to the QA-03 / QA-04 gap lists.** Both list "a `PasswordHasherAdapter`
fallback branch" among the open gaps. In this measurement
`Security/PasswordHasherAdapter.cs` is **18/18 (100.00%)** — the
`catch (FormatException) { return false; }` fallback at
`PasswordHasherAdapter.cs:40-44` is executed by `Verify_WithMalformedHash_ReturnsFalse`
(`project/tests/CrudWorks.Infrastructure.Tests/Security/PasswordHasherAdapterTests.cs:35`).
It is therefore **not** among the 21. The 21 are exactly the six groups listed
above.

### `CrudWorks.Api` — the hand-written vs compiler-generated split, verified by QA

QA verified the claim made in `docs/ai/014` directly from the Cobertura file
instead of repeating it:

- The `CrudWorks.Api` package in the merged coverage contains **10 files**: 9
  hand-written files under `project/src/CrudWorks.Api/` and 1 compiler-generated
  file under `obj/Debug/net10.0/Microsoft.AspNetCore.OpenApi.SourceGenerators/`.
- **Hand-written: 229 covered / 229 valid = 100.00%** — every hand-written API
  line is executed by at least one test.
- **Compiler-generated** (`OpenApiXmlCommentSupport.generated.cs`): 135 / 384 =
  35.16%.

The 249 uncovered lines of the assembly (613 − 364) are **all** inside that one
generated file; the hand-written sources have **no** uncovered line. The
whole-assembly figure of 59.38% is therefore a denominator artefact of the OpenAPI
XML-comment source generator, not "41% of the API is untested". The split is
recomputed from the Cobertura file on every run of
`docs/qa/tools/coverage-report.js`, so it cannot silently drift.

**Caveat, stated so the number is not misread.** The generated file is part of the
compiled assembly, and the DoD floor (`docs/02-definition-of-done.md` §6) covers
Domain + Application only. If the panel asks for the whole-assembly figure, the
number to quote is **59.38%**; the hand-written/generated split above is the
honest reading of it.

### Infrastructure recommendation

*Reproduction.* The per-file tables above can be re-derived from the same
Cobertura files with `node docs/qa/tools/inspect-coverage.js <package>` — for
example `CrudWorks.Infrastructure` or `CrudWorks.Api` — which prints the
covered/valid count and the uncovered line numbers per file.

**Recommendation.** The Infrastructure gaps are outside the DoD floor and
`CrudWorks.Infrastructure` already sits at 95.36%. The three lowest-value items
are the design-time factory and the two migration rollback bodies — 14 of the 21
lines. The two worth a follow-up if the timebox allows are
`DatabaseSeeder.cs:36-37` (the "the database already has a user" path) and
`UnitOfWork.cs:34-35` (the non-SQLite inner-exception path of the conflict
mapping).

**Open question** for the orchestrator: is a schema-rollback test worth a wave?
QA's recommendation is **no** — it is a tooling path, not a user-visible
behaviour, and the DoD does not require it.
<!-- QA:NOTES:END -->
