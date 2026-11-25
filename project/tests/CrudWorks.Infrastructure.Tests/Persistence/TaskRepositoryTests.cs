using CrudWorks.Application.Tasks;
using CrudWorks.Domain.Tasks;
using CrudWorks.Domain.Users;
using CrudWorks.Infrastructure.Persistence;
using CrudWorks.Infrastructure.Persistence.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CrudWorks.Infrastructure.Tests.Persistence;

public sealed class TaskRepositoryTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<CrudWorksDbContext> _options = null!;
    private Guid _ownerId;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<CrudWorksDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using var dbContext = CreateContext();
        await dbContext.Database.MigrateAsync();

        var owner = User.Create("owner@crudworks.local", "Owner", "hash");
        var stranger = User.Create("stranger@crudworks.local", "Stranger", "hash");
        dbContext.Users.AddRange(owner, stranger);
        await dbContext.SaveChangesAsync();
        _ownerId = owner.Id;
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Migration_AppliesTaskMigrations()
    {
        await using var dbContext = CreateContext();

        var applied = await dbContext.Database.GetAppliedMigrationsAsync();
        var pending = await dbContext.Database.GetPendingMigrationsAsync();

        Assert.Contains(applied, migration => migration.EndsWith("_AddTasks", StringComparison.Ordinal));
        Assert.Contains(applied, migration =>
            migration.EndsWith("_AddTaskPaginationIndex", StringComparison.Ordinal));
        Assert.Empty(pending);
    }

    [Fact]
    public async Task TasksTable_HasTheCompositePaginationIndex()
    {
        await using var dbContext = CreateContext();
        var connection = (SqliteConnection)dbContext.Database.GetDbConnection();

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'IX_Tasks_UserId_CreatedAtUtc_Id'";
        var compositeCount = Convert.ToInt32(await command.ExecuteScalarAsync());

        command.CommandText =
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'IX_Tasks_UserId'";
        var oldCount = Convert.ToInt32(await command.ExecuteScalarAsync());

        command.CommandText = "PRAGMA index_info('IX_Tasks_UserId_CreatedAtUtc_Id')";
        var columns = new List<string>();
        using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(2));
            }
        }

        Assert.Equal(1, compositeCount);
        Assert.Equal(0, oldCount);
        Assert.Equal(["UserId", "CreatedAtUtc", "Id"], columns);
    }

    [Fact]
    public async Task TasksTable_EnforcesTheForeignKeyToUsers()
    {
        await using var dbContext = CreateContext();
        var task = TaskItem.Create(Guid.NewGuid(), "Orphan task", null, null, null);

        dbContext.Tasks.Add(task);

        // Microsoft.Data.Sqlite enforces foreign keys by default; a task whose owner
        // does not exist must not persist.
        await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task AddAsync_ThenFindAsync_RoundTripsAllFieldsIncludingNullables()
    {
        var dueDate = new DateOnly(2026, 11, 14);
        var task = TaskItem.Create(_ownerId, "Write the proposal", "Draft and review.",
            TaskItemStatus.InProgress, dueDate);

        await using (var writeContext = CreateContext())
        {
            var repository = new TaskRepository(writeContext);
            await repository.AddAsync(task, CancellationToken.None);
            await new UnitOfWork(writeContext).SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = CreateContext();
        var persisted = await new TaskRepository(readContext)
            .FindAsync(task.Id, _ownerId, CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal(task.Id, persisted.Id);
        Assert.Equal(_ownerId, persisted.UserId);
        Assert.Equal("Write the proposal", persisted.Title);
        Assert.Equal("Draft and review.", persisted.Description);
        Assert.Equal(TaskItemStatus.InProgress, persisted.Status);
        Assert.Equal(dueDate, persisted.DueDate);
        Assert.Equal(task.CreatedAtUtc, persisted.CreatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, persisted.CreatedAtUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, persisted.UpdatedAtUtc.Kind);
    }

    [Fact]
    public async Task AddAsync_WithNullDescriptionAndDueDate_RoundTripsAsNulls()
    {
        var task = TaskItem.Create(_ownerId, "Write the proposal", null, TaskItemStatus.Todo, null);

        await using (var writeContext = CreateContext())
        {
            var repository = new TaskRepository(writeContext);
            await repository.AddAsync(task, CancellationToken.None);
            await new UnitOfWork(writeContext).SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = CreateContext();
        var persisted = await new TaskRepository(readContext)
            .FindAsync(task.Id, _ownerId, CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Null(persisted.Description);
        Assert.Null(persisted.DueDate);
    }

    [Fact]
    public async Task FindAsync_MutatedAndSaved_PersistsTheUpdateIncludingANullDueDate()
    {
        var task = TaskItem.Create(_ownerId, "Write the proposal", "Draft and review.",
            TaskItemStatus.Todo, new DateOnly(2026, 11, 14));
        await using (var writeContext = CreateContext())
        {
            var repository = new TaskRepository(writeContext);
            await repository.AddAsync(task, CancellationToken.None);
            await new UnitOfWork(writeContext).SaveChangesAsync(CancellationToken.None);
        }

        // The find behind the update path must return a tracked entity: a
        // no-tracking load would let SaveChangesAsync silently discard the mutation.
        await using (var updateContext = CreateContext())
        {
            var loaded = await new TaskRepository(updateContext)
                .FindAsync(task.Id, _ownerId, CancellationToken.None);
            Assert.NotNull(loaded);
            loaded.Update("Review the proposal", null, TaskItemStatus.Done, null);
            await new UnitOfWork(updateContext).SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = CreateContext();
        var persisted = await new TaskRepository(readContext)
            .FindAsync(task.Id, _ownerId, CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal("Review the proposal", persisted.Title);
        Assert.Null(persisted.Description);
        Assert.Equal(TaskItemStatus.Done, persisted.Status);
        Assert.Null(persisted.DueDate);
    }

    [Fact]
    public async Task FindAsync_WithAnotherUsersId_ReturnsNull()
    {
        var task = TaskItem.Create(_ownerId, "Write the proposal", null, null, null);
        await using (var writeContext = CreateContext())
        {
            await new TaskRepository(writeContext).AddAsync(task, CancellationToken.None);
            await new UnitOfWork(writeContext).SaveChangesAsync(CancellationToken.None);
        }

        var strangerId = await GetStrangerIdAsync();
        await using var readContext = CreateContext();
        var persisted = await new TaskRepository(readContext)
            .FindAsync(task.Id, strangerId, CancellationToken.None);

        Assert.Null(persisted);
    }

    [Fact]
    public async Task FindAsync_WithMissingTask_ReturnsNull()
    {
        await using var dbContext = CreateContext();

        var persisted = await new TaskRepository(dbContext)
            .FindAsync(Guid.NewGuid(), _ownerId, CancellationToken.None);

        Assert.Null(persisted);
    }

    [Fact]
    public async Task ListPageAsync_OrdersByCreatedAtUtcDescending()
    {
        // The factory stamps every task with UtcNow, so the seeder-style staggered
        // timestamps are written directly to make the ordering observable.
        SeedTask("First task", minutesAgo: 30);
        SeedTask("Second task", minutesAgo: 20);
        SeedTask("Third task", minutesAgo: 10);

        await using var readContext = CreateContext();
        var tasks = await new TaskRepository(readContext)
            .ListPageAsync(_ownerId, null, 10, CancellationToken.None);

        Assert.Equal(["Third task", "Second task", "First task"], tasks.Select(task => task.Title));
        Assert.All(tasks, task => Assert.Equal(_ownerId, task.UserId));
    }

    [Fact]
    public async Task ListPageAsync_ReturnsOnlyTheCallerTasks()
    {
        SeedTask("Owned task", minutesAgo: 5);

        var strangerId = await GetStrangerIdAsync();
        SeedTaskFor(strangerId, "Stranger task", minutesAgo: 1);

        await using var readContext = CreateContext();
        var tasks = await new TaskRepository(readContext)
            .ListPageAsync(_ownerId, null, 10, CancellationToken.None);

        Assert.Equal(["Owned task"], tasks.Select(task => task.Title));
    }

    [Fact]
    public async Task ListPageAsync_WithACursor_ReturnsTheNextPageWithoutDuplicates()
    {
        for (var index = 1; index <= 12; index++)
        {
            SeedTask($"Task {index}", minutesAgo: index);
        }

        await using var firstContext = CreateContext();
        var firstPage = await new TaskRepository(firstContext)
            .ListPageAsync(_ownerId, null, 5, CancellationToken.None);

        Assert.Equal(6, firstPage.Count);
        Assert.Equal(
            ["Task 1", "Task 2", "Task 3", "Task 4", "Task 5", "Task 6"],
            firstPage.Select(task => task.Title));

        var cursor = new TaskPageCursor(firstPage[4].CreatedAtUtc, firstPage[4].Id);

        await using var secondContext = CreateContext();
        var secondPage = await new TaskRepository(secondContext)
            .ListPageAsync(_ownerId, cursor, 5, CancellationToken.None);

        Assert.Equal(
            ["Task 6", "Task 7", "Task 8", "Task 9", "Task 10", "Task 11"],
            secondPage.Select(task => task.Title));
        Assert.DoesNotContain(secondPage, task => firstPage.Take(5).Any(first => first.Id == task.Id));
    }

    [Fact]
    public async Task ListPageAsync_WithACursorFromAnotherUser_KeepsTheOwnerFilter()
    {
        SeedTask("Owner newer", minutesAgo: 1);
        SeedTask("Owner older", minutesAgo: 10);
        var strangerId = await GetStrangerIdAsync();
        var strangerTask = SeedTaskFor(strangerId, "Stranger task", minutesAgo: 5);
        // Older than the cursor: without the UserId filter this row would be returned.
        SeedTaskFor(strangerId, "Stranger older", minutesAgo: 20);
        var cursor = new TaskPageCursor(strangerTask.CreatedAtUtc, strangerTask.Id);

        await using var readContext = CreateContext();
        var page = await new TaskRepository(readContext)
            .ListPageAsync(_ownerId, cursor, 10, CancellationToken.None);

        Assert.Equal(["Owner older"], page.Select(task => task.Title));
    }

    [Fact]
    public async Task ListPageAsync_WithTheSameTimestamp_UsesTheIdTieBreaker()
    {
        var timestamp = DateTime.UtcNow.AddMinutes(-5);
        SeedTaskAt("Same time A", timestamp);
        SeedTaskAt("Same time B", timestamp);

        await using var firstContext = CreateContext();
        var firstPage = await new TaskRepository(firstContext)
            .ListPageAsync(_ownerId, null, 1, CancellationToken.None);

        Assert.Equal(2, firstPage.Count);
        var cursor = new TaskPageCursor(firstPage[0].CreatedAtUtc, firstPage[0].Id);

        await using var secondContext = CreateContext();
        var secondPage = await new TaskRepository(secondContext)
            .ListPageAsync(_ownerId, cursor, 1, CancellationToken.None);

        Assert.Single(secondPage);
        Assert.NotEqual(firstPage[0].Id, secondPage[0].Id);
        Assert.Equal(timestamp, firstPage[0].CreatedAtUtc);
        Assert.Equal(timestamp, secondPage[0].CreatedAtUtc);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheTaskAcrossContexts()
    {
        var task = SeedTask("Doomed task", minutesAgo: 1);

        await using (var writeContext = CreateContext())
        {
            var tracked = await new TaskRepository(writeContext)
                .FindAsync(task.Id, _ownerId, CancellationToken.None);
            Assert.NotNull(tracked);
            await new TaskRepository(writeContext).DeleteAsync(tracked, CancellationToken.None);
            await new UnitOfWork(writeContext).SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = CreateContext();
        var deleted = await new TaskRepository(readContext)
            .FindAsync(task.Id, _ownerId, CancellationToken.None);

        Assert.Null(deleted);
    }

    [Fact]
    public async Task Seeder_WithEmptyTasks_SeedsFiveTasksAcrossAllStatusesIdempotently()
    {
        // The demo user comes from the TASKS-001 seeder; tasks reuse it. The fixture
        // context already has other users, so the demo user is added directly here.
        await using (var dbContext = CreateContext())
        {
            dbContext.Users.Add(User.Create(
                DatabaseSeeder.DemoEmail, DatabaseSeeder.DemoDisplayName, "hash"));
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = CreateContext())
        {
            await DatabaseSeeder.SeedDemoTasksAsync(dbContext);
        }

        // Running the seeder again must not duplicate the seed.
        await using (var secondContext = CreateContext())
        {
            var user = await secondContext.Users.SingleAsync(candidate =>
                candidate.Email == DatabaseSeeder.DemoEmail);
            await DatabaseSeeder.SeedDemoTasksAsync(secondContext, user.Id);
        }

        await using var readContext = CreateContext();
        var user2 = await readContext.Users.SingleAsync(candidate =>
            candidate.Email == DatabaseSeeder.DemoEmail);
        var seeded = await readContext.Tasks
            .Where(task => task.UserId == user2.Id)
            .OrderByDescending(task => task.CreatedAtUtc)
            .ToListAsync();

        Assert.Equal(5, seeded.Count);
        Assert.Contains(seeded, task => task.Status == TaskItemStatus.Todo);
        Assert.Contains(seeded, task => task.Status == TaskItemStatus.InProgress);
        Assert.Contains(seeded, task => task.Status == TaskItemStatus.Done);
        Assert.Equal(2, seeded.Count(task => task.Status == TaskItemStatus.Todo));
        Assert.Equal(2, seeded.Count(task => task.Status == TaskItemStatus.InProgress));
        Assert.Equal(1, seeded.Count(task => task.Status == TaskItemStatus.Done));
        // Descending order must be visible in the demo data.
        Assert.True(seeded[0].CreatedAtUtc >= seeded[^1].CreatedAtUtc);
        Assert.Equal(["Prepare the quarterly report", "Review the vendor contract",
            "Book the dentist appointment", "Renew the passport",
            "Send the tax documents to the accountant"], seeded.Select(task => task.Title));
        // Every seeded task is a complete example: the demo must never show an empty
        // description or a card without a due date.
        Assert.All(seeded, task => Assert.False(string.IsNullOrWhiteSpace(task.Description)));
        Assert.All(seeded, task => Assert.NotNull(task.DueDate));
    }

    private TaskItem SeedTask(string title, int minutesAgo) =>
        SeedTaskFor(_ownerId, title, minutesAgo);

    private TaskItem SeedTaskFor(Guid userId, string title, int minutesAgo) =>
        SeedTaskAt(userId, title, DateTime.UtcNow.AddMinutes(-minutesAgo));

    private TaskItem SeedTaskAt(string title, DateTime createdAtUtc) =>
        SeedTaskAt(_ownerId, title, createdAtUtc);

    private TaskItem SeedTaskAt(Guid userId, string title, DateTime createdAtUtc)
    {
        var task = TaskItem.Create(userId, title, null, TaskItemStatus.Todo, null);
        using var dbContext = CreateContext();
        dbContext.Tasks.Add(task);
        dbContext.Entry(task).Property(candidate => candidate.CreatedAtUtc).CurrentValue = createdAtUtc;
        dbContext.Entry(task).Property(candidate => candidate.UpdatedAtUtc).CurrentValue = createdAtUtc;
        dbContext.SaveChanges();
        return task;
    }

    private async Task<Guid> GetStrangerIdAsync()
    {
        await using var dbContext = CreateContext();
        var stranger = await dbContext.Users.SingleAsync(candidate =>
            candidate.Email == "stranger@crudworks.local");
        return stranger.Id;
    }

    private CrudWorksDbContext CreateContext() => new(_options);
}
