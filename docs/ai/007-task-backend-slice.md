# AI Evidence Log

## Entry 007

- **Date:** 2026-11-12
- **Agent:** backend
- **Task:** SPEC-002 backend slice — tasks T-002-01 through T-002-11
- **Tool/model:** OpenCode subagent (GLM-5.3-Flash Free)

## Prompt

The orchestrator asked for the task CRUD slice on top of the hardened auth slice:
`TaskItem`/`TaskItemStatus` with a static factory and `Update` (invariants in the
domain, not setters); `ITaskRepository`/`ITaskService` with ownership-scoped queries
(foreign id → `NotFoundException`, message "Task not found."), DTOs without any
`userId`, and `TaskValidation` mirroring ADR-0004 §5 messages exactly; EF
`TaskConfiguration` (table `Tasks`, FK Restrict, `IX_Tasks_UserId`, string status,
nullable `DateOnly`), `TaskRepository` with `AsNoTracking` and descending
`CreatedAtUtc` ordering, the `AddTasks` migration; `TasksController` with 201+Location,
204, and the review-nit fix keying `UnitOfWork` unique-violation mapping on the
violated columns; seeder extension with five idempotent demo tasks across all three
statuses; tests at every layer including the three IDOR cases; real build/test output;
no commits; statuses updated in `tasks.md`.

## Output summary

Added 22 domain tests, 26 application tests, 11 infrastructure tests, and 18 API
tests (136 total, up from 58). The ownership rule lives in
`TaskRepository.FindAsync(id, userId)` and `TaskService.RequireTaskAsync`, so a task
owned by someone else is reported exactly like a missing one. Status validation
parses the string at the application boundary (`TaskValidation.TryParseStatus`) so an
unknown value returns the contract message with the `status` key instead of a JSON
binding error; `dueDate` is likewise accepted as a string and parsed with
`DateOnly.TryParseExact("yyyy-MM-dd")`. Response serialization uses
`JsonStringEnumConverter` with default naming so `status` serializes as
`Todo`/`InProgress`/`Done` and `dueDate` as `yyyy-MM-dd` (pinned by raw-JSON tests).

## Representative output (delivered code, after corrections)

This is a representative sample of the AI-assisted output **as delivered**, after
the review corrections below. It is not a raw transcript; the raw suggestions and
the reasons they changed are in the correction table.

`project/src/CrudWorks.Application/Tasks/TaskService.cs:28-46` — the use case owns
validation, status/date parsing, the owner id, and the transaction boundary:

```csharp
public async Task<TaskResponse> CreateAsync(Guid userId, CreateTaskRequest request,
    CancellationToken cancellationToken)
{
    ArgumentNullException.ThrowIfNull(request);

    ThrowIfInvalid(TaskValidation.ValidateCreate(request));

    var task = TaskItem.Create(
        userId,
        request.Title!,
        request.Description,
        TaskValidation.TryParseStatus(request.Status),
        TaskValidation.ParseDueDate(request.DueDate));

    await taskRepository.AddAsync(task, cancellationToken);
    await unitOfWork.SaveChangesAsync(cancellationToken);

    return task.ToTaskResponse();
}
```

`project/src/CrudWorks.Api/Controllers/TasksController.cs:9-37` — the controller is
thin, authorized at the class level, and never accepts an owner id from the body:

```csharp
[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TasksController(ITaskService taskService, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TaskResponse>> Create(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await taskService.CreateAsync(RequireUserId(), request, cancellationToken);
        return Created(Url.Action(nameof(GetById), new { id = task.Id })!, task);
    }

    // The owner always comes from the token subject, never from the request body.
    private Guid RequireUserId() =>
        currentUser.UserId ?? throw new UnauthorizedException("Authentication required");
}
```

The full endpoint set, DTOs, validators, EF configuration, migration, and the 136
tests of this slice are in the files listed under "Files affected".

## Validation

- [x] Build: `dotnet build CrudWorks.slnx -warnaserror` → `0 Aviso(s) 0 Erro(s)`
- [x] Tests: `dotnet test CrudWorks.slnx --no-build` → Domain 37, Application 45,
      Infrastructure 24, Api 30; `Com falha: 0` (136 total, was 58)
- [x] Migration: `dotnet ef migrations add AddTasks -p src/CrudWorks.Infrastructure -s src/CrudWorks.Api -o Persistence/Migrations` → `Done`; generated file matches
      PLAN-002 §2 (FK Restrict, `IX_Tasks_UserId`, TEXT status/dueDate)
- [x] Smoke run against a temp SQLite (`CrudWorks.Api.exe` on port 5299): demo login
      200; `GET /api/tasks` → 5 seeded tasks ordered newest first with statuses
      `Todo,InProgress,Todo,Done,InProgress`; POST → 201 (status `InProgress`,
      `dueDate: 2026-11-20`); PUT → `Done` with refreshed `updatedAtUtc`; DELETE →
      204 empty body; IDOR GET/PUT/DELETE by a second user → 404 while the owner's
      list still had 5 tasks; short title → 400 with
      `errors.title: ["Title must be between 3 and 200 characters."]`; anonymous
      GET → 401
- [x] Security review: ownership scoping in the repository, never in the controller;
      no `userId` in any request DTO; no password/token/authorization header logged;
      seeded demo credential unchanged and Development-only

## Corrections

| What the AI suggested | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| `SaveChangesAsync` inside the repository | ADR-0001: repositories never save implicitly; `IUnitOfWork` owns the transaction boundary | `TaskService` calls `unitOfWork.SaveChangesAsync` explicitly | Keep the transaction boundary in the use case |
| Enum-typed request DTO for `status` | An unknown string would fail model binding before the application validation and lose the ADR-0004 §5 message and `status` key | Request DTOs carry `string?`; `TaskValidation` parses at the boundary | Contract requires `400` + `errors.status` for unknown status values |
| `DateTime?` for `dueDate` in the response | A timestamp would leak into the JSON | `DateOnly?`; System.Text.Json serializes `yyyy-MM-dd` | ADR-0004 §1 pins the date format |
| Seeder writing staggered timestamps through domain setters | The domain has no setters (by design) and the factory stamps `UtcNow` | Seeder uses `dbContext.Entry(task).Property(...).CurrentValue` | Keeps the demo timeline without opening a mutation path in the domain |
| Seeder test reusing `SeedDemoUserAsync` | That method only seeds an *empty* Users table; the fixture already had users | Test adds the demo user directly, then calls `SeedDemoTasksAsync` | One seeder, one demo user; idempotency still asserted twice |
| First `CreateTaskAsync` helper call in the update test | Passed a description in the `status` parameter position → 400 | Named argument `description:` | Test bug, not product bug |

## Edge cases considered

- Foreign task id is indistinguishable from a missing one (404, "Task not found."),
  proven for read, update, and delete, with the row verified intact for the owner.
- Missing `status` on update → 400 with `errors.status` (never silently resets);
  missing on create → defaults to `Todo`.
- Whitespace-only description → `null` in storage and in JSON.
- Title exactly 3/200 and description exactly 2000 accepted; off-by-one rejected.
- Unknown fields (a client-sent `userId`) are ignored by the JSON binder per
  ADR-0004 §1; the server always derives the owner from the token subject.
- Seeder re-run adds nothing (count asserted twice in the test).
- `updatedAtUtc >= createdAtUtc` pinned by both an application and an API test.

## Files affected

- `project/src/CrudWorks.Domain/Tasks/TaskItem.cs`, `TaskItemStatus.cs`
- `project/src/CrudWorks.Application/Tasks/*` (ITaskRepository, ITaskService, DTOs,
  TaskValidation, TaskService, TaskMapping)
- `project/src/CrudWorks.Infrastructure/Persistence/Configurations/TaskConfiguration.cs`,
  `TaskRepository.cs`, `CrudWorksDbContext.cs`, `UnitOfWork.cs` (review-nit fix),
  `Seed/DatabaseSeeder.cs`, `Migrations/20261112184118_AddTasks.*` + snapshot
- `project/src/CrudWorks.Api/Controllers/TasksController.cs`, `Program.cs`
  (enum converter, DI registrations)
- `project/tests/CrudWorks.Domain.Tests/Tasks/TaskItemTests.cs`,
  `CrudWorks.Application.Tests/Tasks/TaskServiceTests.cs`,
  `CrudWorks.Infrastructure.Tests/Persistence/TaskRepositoryTests.cs`,
  `CrudWorks.Api.Tests/Tasks/TasksApiTests.cs`
- `specs/002-task-crud/tasks.md` (status column, T-002-01..11 → done)

## Final decision

Accepted. All 11 backend tasks are implemented and verified with real build, test,
migration, and smoke-run output. Frontend tasks T-002-12..14 remain with the
frontend owner.

## Review follow-up (task slice)

Implemented the findings from `docs/reviews/review-task-slice.md` (backend items
only; the traceability/note items belong to other owners).

### 1. MAJOR — numeric status strings accepted ("1" → InProgress)

- **What:** `TaskValidation.TryParseStatus` used `Enum.TryParse`, whose
  integer-name handling mapped `"1"`/`"0"`/`"2"` onto `InProgress`/`Todo`/`Done`,
  returning 201/200 where ADR-0004 §4 (case-sensitive names only) requires 400.
- **Why the fix:** the contract pins the three exact names; numeric strings were
  invented acceptance behavior. ~3-line guard, no contract change.
- **Fix:** explicit switch over `nameof(TaskItemStatus.{Todo,InProgress,Done})`
  with a `_ => null` fallback in
  `project/src/CrudWorks.Application/Tasks/TaskValidation.cs` (same trim
  behavior, same message "Status must be Todo, InProgress, or Done.", same
  `status` key).
- **New tests:**
  - `TaskServiceTests.CreateAsync_WithUnknownStatus_ThrowsValidationWithTheStatusKey`
    gained the `[InlineData("1")]` case (failed first: no exception thrown).
  - `TasksApiTests.Create_WithNumericStatus_Returns400WithTheStatusKey`
    (failed first: raw `{"status":"1"}` returned 201 Created).

### 2. MINOR — client-supplied `userId` is ignored (pin)

- `TasksApiTests.Create_WithAUserIdInTheBody_IsIgnoredAndTheCallerRemainsTheOwner`:
  POST with `userId = <another user's id>` (that user resolved via
  `GET /api/auth/me`); asserts 201, the task is in the caller's list, and the
  other user's list does not contain it. Passed without code changes — pin.

### 3. MINOR — PUT `dueDate: null` clears (pin that found a real bug)

- **New tests:**
  - `TaskServiceTests.UpdateAsync_WithANullDueDate_ClearsAPreviouslySetDueDate`
    (Application layer, fake repository) — passed immediately.
  - `TasksApiTests.Update_WithANullDueDate_ClearsAPreviouslySetDueDate` —
    **failed**: the PUT response showed `dueDate: null` but a re-GET still
    returned 2026-11-14.
- **Root cause found by this pin:** `TaskRepository.FindAsync` loaded entities
  with `AsNoTracking()`, so `TaskService.UpdateAsync` mutated a detached entity
  and `SaveChangesAsync` persisted nothing — **every** PUT was a silent no-op in
  the database, masked by tests that never re-GET after update. `AsNoTracking`
  removed from `FindAsync` (the write path); `ListByOwnerAsync` (read-only)
  keeps it. No interface, contract, or migration change.
- **Regression pin added:**
  `TaskRepositoryTests.FindAsync_MutatedAndSaved_PersistsTheUpdateIncludingANullDueDate`
  (real SQLite, find → mutate → save → re-find).

### 4. NIT — lowercase status at the API boundary (pin)

- `TasksApiTests.Create_WithLowercaseStatus_Returns400WithTheStatusKey`
  (raw `"status": "todo"` → 400 with `errors.status`). Passed without code
  changes; the Application-layer case already existed.

### Commands and results

- `dotnet build CrudWorks.slnx -warnaserror` → `0 Aviso(s) / 0 Erro(s)`
  (Compilação com êxito). One retry was needed: a leftover `CrudWorks.Api`
  process (PID 13832) held the output DLL (MSB3026); killed, rebuilt clean.
- `dotnet test CrudWorks.slnx --no-build` → all pass:
  Domain.Tests 37, Application.Tests 47, Infrastructure.Tests 25, Api.Tests 34
  = **143 total, 0 failed, 0 skipped** (was 136; +7 test cases).

### Notes

- No changes to controllers, migration, seed, or the API contract; validation
  messages unchanged. The `AsNoTracking` removal is an Infrastructure-only
  persistence fix, verified by new pins at the repository and API layers.
- The update-path no-op was discovered by the review-requested test, not by the
  reviewer — the reviewer had classified the dueDate pin as a minor test gap.
