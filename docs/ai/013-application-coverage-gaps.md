# AI Evidence — 013: Application coverage gaps (QA-03)

- **Date:** 2026-11-12
- **Agent:** backend implementer (subagent session)
- **Task:** Close QA-03 from the wave-1 QA clean run — the 12 uncovered lines in
  `CrudWorks.Application` (`AuthValidation.cs:47-49`, `TaskValidation.cs:52-54`,
  `:57-59`, `:68-70`) — with behaviour-level unit tests, without changing product
  behaviour.
- **Tool/model:** OpenCode session (MiniMax M3.1 Flash Free)
- **Related:** QA-04 is closed in `docs/ai/014-api-critical-paths.md`; this entry
  cross-references it and vice versa.

## Prompt summary

The orchestrator delegated QA-03 with these constraints:

- Read `AGENTS.md` (§4 non-negotiables, §11 file ownership), `specs/constitution.md`,
  `specs/002-task-crud/spec.md`, `docs/adr/0004-api-contract.md` and
  `docs/02-definition-of-done.md` first.
- The DoD §6 coverage floor was already met (Domain 100.00%, Application 94.92%);
  this is **not** a coverage crisis. Two named gaps only:
  - `project/src/CrudWorks.Application/Auth/AuthValidation.cs:47-49` — the
    invalid-email branch of `ValidateLogin` (the register path was covered).
  - `project/src/CrudWorks.Application/Tasks/TaskValidation.cs:52-54` (invalid title
    on update), `:57-59` (invalid description on update), `:68-70` (invalid due date
    on update).
- Read those two files and confirm exactly which branches those line numbers are
  before writing anything.
- Write **failing tests first** (TDD), asserting observable behaviour, matching the
  house style of `project/tests/CrudWorks.Application.Tests/`
  (`AuthServiceTests.cs`, `TaskServiceTests.cs`): naming, arrangement, assertion
  style and NSubstitute usage. Capture the RED output.
- If a test reveals the implementation is **wrong** (not merely untested), report
  it with `file:line` and fix it only inside the approved ADR-0004 / SPEC-002
  contract; if the code contradicts the spec, stop and report a conflict
  (AGENTS.md §3 and §13) instead of silently changing behaviour.
- Unit tests only. No restructuring to make coverage look better, no
  `ExcludeFromCodeCoverage`, no suppression attributes, no metric gaming. If a line
  cannot be covered without a scope-creep code change, report it instead.
- Verify with `dotnet build CrudWorks.slnx -warnaserror` and
  `dotnet test CrudWorks.slnx`, and record the per-project counts before and after.
- No git, no commits, no edits outside `project/src/**` backend, `project/tests/**`
  and this own `docs/ai/` entry.

## Output summary

### 1. The named lines are exactly the branches the QA report described

Confirmed by reading the two files before writing anything:

| File | Lines | Branch |
| --- | --- | --- |
| `project/src/CrudWorks.Application/Auth/AuthValidation.cs` | 47-49 | `if (!IsValidEmail(request.Email))` inside `ValidateLogin` — adds `errors["email"] = [EmailMessage]` |
| `project/src/CrudWorks.Application/Tasks/TaskValidation.cs` | 52-54 | `if (!IsValidTitle(request.Title))` inside `ValidateUpdate` — adds `errors["title"]` |
| `project/src/CrudWorks.Application/Tasks/TaskValidation.cs` | 57-59 | `if (!IsValidDescription(request.Description))` inside `ValidateUpdate` — adds `errors["description"]` |
| `project/src/CrudWorks.Application/Tasks/TaskValidation.cs` | 68-70 | `if (!IsValidOptionalDueDate(request.DueDate))` inside `ValidateUpdate` — adds `errors["dueDate"]` |

All four branches already existed and were already correct with respect to
ADR-0004 §5 and SPEC-002 §6. **No production code was changed** — the gap was
purely "untested", not "wrong". See "Corrections" for the RED evidence that proves
this from the other side.

### 2. New tests (10 cases)

`project/tests/CrudWorks.Application.Tests/Auth/AuthServiceTests.cs`

- `LoginAsync_WithAMalformedEmail_ThrowsValidationWithTheEmailKey` (theory,
  `""` and `"not-an-email"`): a malformed login body is a field-level validation
  failure; the request never reaches the repository, the password hasher or the
  token service.

`project/tests/CrudWorks.Application.Tests/Tasks/TaskServiceTests.cs`

- `UpdateAsync_WithAnInvalidTitle_ThrowsValidationWithTheTitleKey` (theory: `"ab"`,
  `""`, `"   "`).
- `UpdateAsync_WithAnOverlongDescription_ThrowsValidationWithTheDescriptionKey`.
- `UpdateAsync_WithAnInvalidDueDate_ThrowsValidationWithTheDueDateKey` (theory:
  `"not-a-date"`, `"2026-13-01"`, `"14/11/2026"`, `"2026-11-14T00:00:00Z"`).

Each asserts the exact error key set (`Assert.Equal(["title"], exception.Errors.Keys)`),
the exact ADR-0004 message, and that the request is rejected **before** the task is
loaded or written (`DidNotReceive().FindAsync(...)`,
`DidNotReceive().SaveChangesAsync(...)`).

### 3. RED — how it was obtained, honestly

The implementation already contained the behaviour, so the new tests turned green on
their first run. To still produce a **real** RED (a test that fails before the
behaviour exists), the four branches were **temporarily mutated** (the condition
inverted) in a scratch copy of the two files, the new tests were run against the
mutation, and the files were then restored **byte-identically** (SHA-256 verified
before and after).

This is a mutation-based RED, not a "write the test, watch it fail against missing
code" RED. Stating that plainly matters more than pretending otherwise.

```text
$ dotnet test tests\CrudWorks.Application.Tests\CrudWorks.Application.Tests.csproj \
    --filter "FullyQualifiedName~LoginAsync_WithAMalformedEmail|FullyQualifiedName~UpdateAsync_WithAnInvalidTitle|FullyQualifiedName~UpdateAsync_WithAnOverlongDescription|FullyQualifiedName~UpdateAsync_WithAnInvalidDueDate"

[xUnit.net 00:00:00.29]  CrudWorks.Application.Tests.Auth.AuthServiceTests.LoginAsync_WithAMalformedEmail_ThrowsValidationWithTheEmailKey(email: "") [FAIL]
[xUnit.net 00:00:00.30]  CrudWorks.Application.Tests.Auth.AuthServiceTests.LoginAsync_WithAMalformedEmail_ThrowsValidationWithTheEmailKey(email: "not-an-email") [FAIL]
[xUnit.net 00:00:00.30]  CrudWorks.Application.Tests.Tasks.TaskServiceTests.UpdateAsync_WithAnInvalidTitle_ThrowsValidationWithTheTitleKey(title: "   ") [FAIL]
[xUnit.net 00:00:00.30]  CrudWorks.Application.Tests.Tasks.TaskServiceTests.UpdateAsync_WithAnInvalidTitle_ThrowsValidationWithTheTitleKey(title: "ab") [FAIL]
[xUnit.net 00:00:00.31]  CrudWorks.Application.Tests.Tasks.TaskServiceTests.UpdateAsync_WithAnInvalidTitle_ThrowsValidationWithTheTitleKey(title: "") [FAIL]
[xUnit.net 00:00:00.31]  CrudWorks.Application.Tests.Tasks.TaskServiceTests.UpdateAsync_WithAnInvalidDueDate_ThrowsValidationWithTheDueDateKey(dueDate: "14/11/2026") [FAIL]
[xUnit.net 00:00:00.31]  CrudWorks.Application.Tests.Tasks.TaskServiceTests.UpdateAsync_WithAnInvalidDueDate_ThrowsValidationWithTheDueDateKey(dueDate: "2026-11-14T00:00:00Z") [FAIL]
[xUnit.net 00:00:00.31]  CrudWorks.Application.Tests.Tasks.TaskServiceTests.UpdateAsync_WithAnInvalidDueDate_ThrowsValidationWithTheDueDateKey(dueDate: "not-a-date") [FAIL]
[xUnit.net 00:00:00.31]  CrudWorks.Application.Tests.Tasks.TaskServiceTests.UpdateAsync_WithAnInvalidDueDate_ThrowsValidationWithTheDueDateKey(dueDate: "2026-13-01") [FAIL]
[xUnit.net 00:00:00.34]  CrudWorks.Application.Tests.Tasks.TaskServiceTests.UpdateAsync_WithAnOverlongDescription_ThrowsValidationWithTheDescriptionKey [FAIL]

  Com falha CrudWorks.Application.Tests.Auth.AuthServiceTests.LoginAsync_WithAMalformedEmail_ThrowsValidationWithTheEmailKey(email: "")
   Assert.Throws() Failure: Exception type was not an exact match
  Com falha CrudWorks.Application.Tests.Tasks.TaskServiceTests.UpdateAsync_WithAnInvalidDueDate_ThrowsValidationWithTheDueDateKey(dueDate: "not-a-date")
   Assert.Equal() Failure: Collections differ
                                           ↓ (pos 0)
Expected: <generated>                     ["dueDate"]
Actual:   KeyCollection<string, string[]> ["title", "description"]
                                           ↑ (pos 0)

Com falha! – Com falha:    10, Aprovado:     0, Ignorado:     0, Total:    10, Duração: 130 ms - CrudWorks.Application.Tests.dll (net10.0)
Exited with code 1
```

Full log: the run above was captured to a scratch file outside the repository.

### 4. GREEN — source restored byte-identically, tests green

SHA-256 of the two source files, **before the mutation and after the restore**:

```text
AuthValidation.cs   75BE2C6CD54BEBBD416BF8C9564387ADD68F362DD4108831AF49580E9FB7670A
TaskValidation.cs   B073560B2C94C4620911FA4D4327926B8CE5881AD439023331BF1D531C37188B
```

(Identical in both listings → the production code is exactly as it was before this
entry.)

```text
$ dotnet test tests\CrudWorks.Application.Tests\CrudWorks.Application.Tests.csproj
Aprovado!  – Com falha:     0, Aprovado:    57, Ignorado:     0, Total:    57, Duração: 150 ms - CrudWorks.Application.Tests.dll (net10.0)
```

### 5. Coverage after the change

`node docs/qa/tools/coverage-report.js --verify` (read-only; `docs/qa/**` was **not**
modified by me — the tracked `docs/qa/coverage-summary.md` belongs to QA, and QA has
since refreshed it for wave 2 on 2026-11-12 with figures that agree with the table
below):

| Scope | Before (wave 1) | After | Note |
| --- | ---: | ---: | --- |
| `CrudWorks.Domain` | 112/112 (100.00%) | 112/112 (100.00%) | unchanged |
| `CrudWorks.Application` | 224/236 (94.92%) | **236/236 (100.00%)** | QA-03 closed |
| **Domain + Application** | 336/348 (96.55%) | **348/348 (100.00%)** | DoD §6 floor (80%) comfortably met |

Per-file, the four gaps are closed:
`Auth/AuthValidation.cs 45/45`, `Tasks/TaskValidation.cs 61/61` — both 100.00%,
no uncovered line numbers left in `CrudWorks.Application`.

## Validation

- [x] Build: `dotnet build CrudWorks.slnx -warnaserror` → `Compilação com êxito. 0 Aviso(s) 0 Erro(s)`.
- [x] Unit tests: `dotnet test CrudWorks.slnx` → **165 passed, 0 failed, 0 skipped**
      (Domain 37, Application 57, Infrastructure 25, Api 46; was 143 / 47 Application / 34 Api).
- [x] Integration/API tests: unchanged in this entry — see `docs/ai/014-api-critical-paths.md`
      for the 12 added API cases (they are included in the 165 above).
- [x] Manual behavior: not applicable — no product behaviour changed, only unit
      tests were added.
- [x] Security review: the new login test pins that a malformed body never reaches
      the repository, the password hasher or the token service; the update tests pin
      that an invalid body never reads or writes a task. No secret is logged or
      asserted, and the generic `Invalid email or password` message on the
      credential-failure path is untouched.
- [x] Architecture review: the dependency rule is intact — only test files changed
      in `project/tests/**`; `project/src/**` is byte-identical to before.

### Commands and real output

| Command | Result |
| --- | --- |
| `dotnet build CrudWorks.slnx -warnaserror` (before) | `Compilação com êxito. 0 Aviso(s) 0 Erro(s)` |
| `dotnet test CrudWorks.slnx` (before) | 37 + 47 + 25 + 34 = **143 passed, 0 failed** |
| `dotnet test …Application.Tests… --filter <the 10 new cases>` (mutated source) | **10 failed, 0 passed** (RED) |
| `dotnet test …Application.Tests…` (restored source) | **57 passed, 0 failed** (GREEN) |
| `dotnet build CrudWorks.slnx -warnaserror` (after) | `Compilação com êxito. 0 Aviso(s) 0 Erro(s)` |
| `dotnet test CrudWorks.slnx` (after) | 37 + 57 + 25 + 46 = **165 passed, 0 failed, 0 skipped** |
| `dotnet test CrudWorks.slnx --collect:"XPlat Code Coverage"` | 4 × `coverage.cobertura.xml` produced |
| `node docs/qa/tools/coverage-report.js --verify` | 4/4 parser self-checks MATCH; Domain 100.00%, Application **100.00%**, Api 59.38% |
| `node docs/qa/tools/coverage-report.js --verify` (re-run 2026-11-12, read-only, no `--write`) | 4/4 parser self-checks MATCH; Api 364/613 (59.38%), Application 236/236 (100.00%), Domain 112/112 (100.00%), Infrastructure 432/453 (95.36%) |
| `node docs/qa/tools/inspect-coverage.js CrudWorks.Infrastructure` (2026-11-12, read-only) | `TOTAL 432/453 = 95.36%`; uncovered = design-time factory 13-16/18-19, `InitialCreate` 38-41, `AddTasks` 46-49, seeder 36-37/61-63, `UnitOfWork` 34-35; `PasswordHasherAdapter.cs` **18/18 = 100.00%** |
| `dotnet test CrudWorks.slnx` (re-run 2026-11-12) | 37 + 57 + 25 + 46 = **165 passed, 0 failed, 0 skipped** |

## Corrections

| What the AI (or the QA report) asserted | What was wrong / what changed | Why |
| --- | --- | --- |
| "Write failing tests first (TDD)" for 12 already-implemented lines | The behaviour already existed, so a "missing implementation" RED was impossible. The RED was obtained by a **temporary mutation** of the four branch conditions; the files were restored byte-identically (SHA-256 verified). | Reporting a fake RED would break AGENTS.md §7 ("never claim a test passed without the real output") and §4 ("the test fails before the implementation"). |
| The mutation could be left behind in the source | The first restore kept the original file timestamp, so MSBuild reused the stale mutated DLL and 20 tests failed. The files were re-stamped and rebuilt, and the SHA-256 of both files was compared with the pre-mutation listing. | A "restored" file that is not rebuilt proves nothing. |
| `UpdateAsync` reports the invalid field *and* does not touch the task | The service validates **before** `RequireTaskAsync` (`project/src/CrudWorks.Application/Tasks/TaskService.cs:53`), so an invalid body yields `400` even for a task the caller does not own. This is **not** a conflict with SPEC-002: AC-002-04 specifies a *valid* body, and no source file was changed. | Recorded so the next reviewer does not read it as an ownership leak. |
| A malformed login email could be a `401` | ADR-0004 §3 pins `400` for a malformed body and `401` for invalid credentials. The implementation returns `400` with the `email` field error — the contract-compliant behaviour, and it leaks no account existence (an unknown but well-formed email still returns the generic `401`). | No change needed; the new test pins the contract. |

### Corrections applied on 2026-11-12 (post-review, by the author of this entry)

The QA engineer re-measured the coverage with the aggregator and found two factual
errors in the Infrastructure gap list that this entry and
`docs/ai/014-api-critical-paths.md` published. **Both claims were checked against
the code and against the Cobertura files before any edit was made**, and both were
confirmed to be wrong; the entries are corrected in place, not silently rewritten.

| What this entry had asserted | What was wrong | Corrected statement | How it was settled |
| --- | --- | --- | --- |
| The uncovered migration lines are "the migration `Up` methods" | The `Up` methods are covered; the uncovered lines are the **`Down` (rollback) bodies** | `20261112021752_InitialCreate.cs:38-41` = the body of `Down` (`migrationBuilder.DropTable(name: "Users")`); `20261112184118_AddTasks.cs:46-49` = the body of `Down` (`migrationBuilder.DropTable(name: "Tasks")`) | Read the files: `Down` is declared at `20261112021752_InitialCreate.cs:37` and `20261112184118_AddTasks.cs:45`, and the uncovered ranges are exactly the lines that follow. Independently confirmed by `node docs/qa/tools/inspect-coverage.js CrudWorks.Infrastructure` → `UNCOVERED: 38, 39, 40, 41` and `UNCOVERED: 46, 47, 48, 49`, and by the `Up` methods being executed by `Migration_AppliesInitialCreate` (`project/tests/CrudWorks.Infrastructure.Tests/Persistence/UserRepositoryTests.cs:31`) and `Migration_AppliesAddTasks` (`project/tests/CrudWorks.Infrastructure.Tests/Persistence/TaskRepositoryTests.cs:41`), whose fixtures call `Database.MigrateAsync()` (`UserRepositoryTests.cs:22`, `TaskRepositoryTests.cs:26`). |
| "a `PasswordHasherAdapter` fallback" is an open gap | `Security/PasswordHasherAdapter.cs` is fully covered | 18/18 (100.00%) — the `catch (FormatException) { return false; }` fallback (`PasswordHasherAdapter.cs:40-44`) is executed by `Verify_WithMalformedHash_ReturnsFalse` (`project/tests/CrudWorks.Infrastructure.Tests/Security/PasswordHasherAdapterTests.cs:35`), and the null/empty-hash dummy-hash path (`PasswordHasherAdapter.cs:32`) by `Verify_WithNullHash_ReturnsFalse` / `Verify_WithEmptyHash_ReturnsFalse` (`PasswordHasherAdapterTests.cs:44` and `:50`) | `node docs/qa/tools/inspect-coverage.js CrudWorks.Infrastructure` → `18/18 100.00%  Security/PasswordHasherAdapter.cs` (no uncovered line listed). |
| "`docs/qa/coverage-summary.md` still shows the wave-1 numbers (Application 94.92%, Api 33.28%)" | Stale — QA refreshed it on 2026-11-12 (wave 2) | QA's artifact now carries Application 236/236 (100.00%), Api 364/613 (59.38%), Domain 112/112 (100.00%), Infrastructure 432/453 (95.36%) and the 165-test count; it agrees with the numbers in this entry | Read `docs/qa/coverage-summary.md:9-30` and re-ran `node docs/qa/tools/coverage-report.js --verify` (read-only, without `--write`) → identical figures, 4/4 parser self-checks MATCH. |

The QA-03 figures themselves (Application 236/236 = 100.00%, 165 tests passed,
Domain 37 / Application 57 / Infrastructure 25 / Api 46) were **not** affected by
the finding and were re-confirmed on 2026-11-12 by a fresh
`dotnet test CrudWorks.slnx` run:
`Aprovado! – Com falha: 0, Aprovado: 37 / 57 / 25 / 46` = **165 passed, 0 failed,
0 skipped**.

## Edge cases considered

- **Empty vs. malformed email on login.** Both are covered by the theory (`""` and
  `"not-an-email"`); `IsValidEmail` trims first, so `"   "` behaves like `""`.
- **Whitespace-only title on update.** `"   "` trims to length 0 → `title` error;
  pinned as a separate inline case.
- **Description over 2000 characters on update.** The description is optional, so
  only the length rule can fail; `null` stays valid (it clears the field).
- **Due date formats.** `"not-a-date"`, `"2026-13-01"`, `"14/11/2026"` and the
  ISO date-time `"2026-11-14T00:00:00Z"` are all rejected by
  `DateOnly.TryParseExact` with the `yyyy-MM-dd` format.
- **Status is not part of these tests.** The update path requires a status, so each
  new case passes a valid `"Todo"`; otherwise the error key set would contain
  `status` as well and the assertion would not isolate the branch under test.

## Auth and validation decisions

- No product behaviour was changed. The two `project/src/**` files are byte-identical
  to their state before this entry (SHA-256 above).
- The login validation test pins the *validation* failure (malformed body), not the
  credential failure: the generic `Invalid email or password` message and the
  constant-time hash verification on the unknown-email path
  (`project/src/CrudWorks.Application/Auth/AuthService.cs:52`) are unchanged and
  still covered by the existing `LoginAsync_*` tests.
- The update validation tests pin that no write occurs on an invalid body, which
  keeps SPEC-002 AC-002-09 ("no write occurs") true at the Application layer.

## Files affected

- `project/tests/CrudWorks.Application.Tests/Auth/AuthServiceTests.cs` (+1 theory, 2 cases)
- `project/tests/CrudWorks.Application.Tests/Tasks/TaskServiceTests.cs` (+3 tests, 8 cases)
- `docs/ai/013-application-coverage-gaps.md` (this entry)

**Post-review correction (2026-11-12):** only this entry and
`docs/ai/014-api-critical-paths.md` were edited. No production code and no test
was touched by the correction.

**No file under `project/src/**` was modified** — the two Application source files
were only temporarily mutated to obtain the RED and were then restored
byte-identically.

## Open questions

- `docs/qa/coverage-summary.md` has been refreshed by QA (wave 2, dated 2026-11-12)
  and now carries the current numbers: Application 236/236 (100.00%), Api
  364/613 (59.38%), Domain 112/112 (100.00%), Infrastructure 432/453 (95.36%),
  165 tests passed (Domain 37, Application 57, Infrastructure 25, Api 46). Those
  figures agree with this entry and with `docs/ai/014-api-critical-paths.md`; QA's
  artifact is the tracked one and I did not modify it.
- The Infrastructure gaps are still open. The re-measured list is **21 uncovered
  lines** (`node docs/qa/tools/inspect-coverage.js CrudWorks.Infrastructure`,
  read-only, 2026-11-12):

  | File (under `project/src/CrudWorks.Infrastructure/`) | Uncovered lines | What is not executed |
  | --- | --- | --- |
  | `Persistence/DesignTimeDbContextFactory.cs` | 13, 14, 15, 16, 18, 19 | the whole `CreateDbContext` body — a `dotnet ef` tooling factory that no runtime path calls |
  | `Persistence/Migrations/20261112021752_InitialCreate.cs` | 38, 39, 40, 41 | the body of the **`Down` (rollback)** method — `migrationBuilder.DropTable(name: "Users")` |
  | `Persistence/Migrations/20261112184118_AddTasks.cs` | 46, 47, 48, 49 | the body of the **`Down` (rollback)** method — `migrationBuilder.DropTable(name: "Tasks")` |
  | `Persistence/Seed/DatabaseSeeder.cs` | 36, 37 | the early `return` in `SeedDemoUserAsync` when the `Users` table already has rows |
  | `Persistence/Seed/DatabaseSeeder.cs` | 61, 62, 63 | the `demoUserId is { } explicitUserId` branch of `SeedDemoTasksAsync` |
  | `Persistence/UnitOfWork.cs` | 34, 35 | the `return false` guard in `IsUniqueConstraintViolation` when the inner exception is **not** a SQLite unique-constraint error |

  6 + 4 + 4 + 2 + 3 + 2 = **21**. They are outside QA-03/QA-04 and outside the DoD
  floor (Infrastructure is at 95.36%); the corrected list is also carried in
  `docs/ai/014-api-critical-paths.md`.

## Final decision

**Accepted.** QA-03 is closed: all 12 named lines are now covered by behaviour-level
unit tests, `CrudWorks.Application` reaches 100.00% line coverage, the whole
solution is green (165 passed, 0 failed), and no production code was changed
because none of it was wrong.
