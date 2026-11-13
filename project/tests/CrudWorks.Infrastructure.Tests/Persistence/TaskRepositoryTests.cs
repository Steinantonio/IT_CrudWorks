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
    public async Task Migration_AppliesAddTasks()
    {
        await using var dbContext = CreateContext();

        var applied = await dbContext.Database.GetAppliedMigrationsAsync();
        var pending = await dbContext.Database.GetPendingMigrationsAsync();

        Assert.Contains(applied, migration => migration.EndsWith("_AddTasks", StringComparison.Ordinal));
        Assert.Empty(pending);
    }

    [Fact]
    public async Task TasksTable_HasTheUserIdIndex()
    {
        await using var dbContext = CreateContext();
        var connection = (SqliteConnection)dbContext.Database.GetDbConnection();

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'IX_Tasks_UserId'";
        var count = Convert.ToInt32(await command.ExecuteScalarAsync());

        Assert.Equal(1, count);
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
    public async Task ListByOwnerAsync_OrdersByCreatedAtUtcDescending()
    {
        // The factory stamps every task with UtcNow, so the seeder-style staggered
        // timestamps are written directly to make the ordering observable.
        var first = SeedTask("First task", minutesAgo: 30);
        var second = SeedTask("Second task", minutesAgo: 20);
        var third = SeedTask("Third task", minutesAgo: 10);

        await using var readContext = CreateContext();
        var tasks = await new TaskRepository(readContext)
            .ListByOwnerAsync(_ownerId, CancellationToken.None);

        Assert.Equal(["Third task", "Second task", "First task"], tasks.Select(task => task.Title));
        Assert.All(tasks, task => Assert.Equal(_ownerId, task.UserId));
    }

    [Fact]
    public async Task ListByOwnerAsync_ReturnsOnlyTheCallerTasks()
    {
        SeedTask("Owned task", minutesAgo: 5);

        var strangerId = await GetStrangerIdAsync();
        await using (var writeContext = CreateContext())
        {
            var strangerTask = TaskItem.Create(strangerId, "Stranger task", null, null, null);
            await new TaskRepository(writeContext).AddAsync(strangerTask, CancellationToken.None);
            await new UnitOfWork(writeContext).SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = CreateContext();
        var tasks = await new TaskRepository(readContext)
            .ListByOwnerAsync(_ownerId, CancellationToken.None);

        Assert.Equal(["Owned task"], tasks.Select(task => task.Title));
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

    private TaskItem SeedTask(string title, int minutesAgo)
    {
        var task = TaskItem.Create(_ownerId, title, null, TaskItemStatus.Todo, null);
        using var dbContext = CreateContext();
        dbContext.Tasks.Add(task);
        dbContext.Entry(task).Property(candidate => candidate.CreatedAtUtc).CurrentValue =
            DateTime.UtcNow.AddMinutes(-minutesAgo);
        dbContext.Entry(task).Property(candidate => candidate.UpdatedAtUtc).CurrentValue =
            DateTime.UtcNow.AddMinutes(-minutesAgo);
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
