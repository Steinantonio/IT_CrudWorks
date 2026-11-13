using CrudWorks.Domain.Users;
using CrudWorks.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CrudWorks.Infrastructure.Tests.Persistence;

public sealed class UserRepositoryTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<CrudWorksDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<CrudWorksDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using var dbContext = CreateContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Migration_AppliesInitialCreate()
    {
        await using var dbContext = CreateContext();

        var applied = await dbContext.Database.GetAppliedMigrationsAsync();
        var pending = await dbContext.Database.GetPendingMigrationsAsync();

        Assert.Contains(applied, migration => migration.EndsWith("_InitialCreate", StringComparison.Ordinal));
        Assert.Empty(pending);
    }

    [Fact]
    public async Task AddAsync_ThenFindByEmail_ReturnsPersistedUser()
    {
        var user = User.Create("demo@crudworks.local", "Demo User", "hash");

        await using (var writeContext = CreateContext())
        {
            var repository = new UserRepository(writeContext);
            await repository.AddAsync(user, CancellationToken.None);
            await new UnitOfWork(writeContext).SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = CreateContext();
        var persisted = await new UserRepository(readContext)
            .FindByEmailAsync("demo@crudworks.local", CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal(user.Id, persisted.Id);
        Assert.Equal("Demo User", persisted.DisplayName);
        Assert.Equal("hash", persisted.PasswordHash);
        Assert.Equal(DateTimeKind.Utc, persisted.CreatedAtUtc.Kind);
    }

    [Fact]
    public async Task FindByEmailAsync_WithUnknownEmail_ReturnsNull()
    {
        await using var dbContext = CreateContext();

        var persisted = await new UserRepository(dbContext)
            .FindByEmailAsync("missing@crudworks.local", CancellationToken.None);

        Assert.Null(persisted);
    }

    [Fact]
    public async Task FindByIdAsync_ReturnsPersistedUser()
    {
        var user = User.Create("demo@crudworks.local", "Demo User", "hash");
        await using (var writeContext = CreateContext())
        {
            var repository = new UserRepository(writeContext);
            await repository.AddAsync(user, CancellationToken.None);
            await new UnitOfWork(writeContext).SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = CreateContext();
        var persisted = await new UserRepository(readContext)
            .FindByIdAsync(user.Id, CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal("demo@crudworks.local", persisted.Email);
    }

    [Fact]
    public async Task SaveChangesAsync_WithDuplicateEmail_ThrowsConflictAndKeepsOneUser()
    {
        await using var dbContext = CreateContext();
        var repository = new UserRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);

        await repository.AddAsync(
            User.Create("demo@crudworks.local", "First User", "hash"), CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        await repository.AddAsync(
            User.Create("demo@crudworks.local", "Second User", "hash"), CancellationToken.None);

        await Assert.ThrowsAsync<CrudWorks.Application.Common.Exceptions.ConflictException>(
            () => unitOfWork.SaveChangesAsync(CancellationToken.None));

        await using var readContext = CreateContext();
        Assert.Equal(1, await readContext.Users.CountAsync());
    }

    private CrudWorksDbContext CreateContext() => new(_options);
}
