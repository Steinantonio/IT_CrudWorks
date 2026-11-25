using System.Diagnostics;
using CrudWorks.Application.Tasks;
using CrudWorks.Domain.Tasks;
using CrudWorks.Domain.Users;
using CrudWorks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

// Reproducible 10,000-row smoke for ADR-0006. It creates a temporary SQLite
// database, applies the real migrations, seeds one user with 10,000 tasks, then
// measures the first and cursor pages through the production repository.
//
// Usage (from the repository root):
//   dotnet run --project project/tools/pagination-smoke

var databasePath = Path.Combine(
    Path.GetTempPath(),
    $"crudworks-pagination-smoke-{Guid.NewGuid():N}.db");

var options = new DbContextOptionsBuilder<CrudWorksDbContext>()
    .UseSqlite($"Data Source={databasePath};Pooling=False")
    .Options;

Guid userId;

try
{
await using (var setup = new CrudWorksDbContext(options))
{
    await setup.Database.MigrateAsync();

    var user = User.Create("smoke@crudworks.local", "Smoke User", "hash");
    setup.Users.Add(user);
    await setup.SaveChangesAsync();
    userId = user.Id;

    const int total = 10_000;
    const int batchSize = 1_000;
    var baseTime = DateTime.UtcNow;

    for (var batchStart = 0; batchStart < total; batchStart += batchSize)
    {
        var tasks = new List<TaskItem>();
        for (var index = batchStart; index < batchStart + batchSize; index++)
        {
            tasks.Add(TaskItem.Create(userId, $"Task {index}", null, TaskItemStatus.Todo, null));
        }

        setup.Tasks.AddRange(tasks);

        for (var offset = 0; offset < tasks.Count; offset++)
        {
            var timestamp = baseTime.AddSeconds(-(batchStart + offset));
            setup.Entry(tasks[offset]).Property(task => task.CreatedAtUtc).CurrentValue = timestamp;
            setup.Entry(tasks[offset]).Property(task => task.UpdatedAtUtc).CurrentValue = timestamp;
        }

        await setup.SaveChangesAsync();
        setup.ChangeTracker.Clear();
    }
}

await using (var read = new CrudWorksDbContext(options))
{
    var repository = new TaskRepository(read);
    var firstPage = await repository.ListPageAsync(userId, null, 10, CancellationToken.None);

    if (firstPage.Count != 11)
    {
        throw new InvalidOperationException($"Expected 11 rows for take=10, got {firstPage.Count}.");
    }

    var cursor = new TaskPageCursor(firstPage[9].CreatedAtUtc, firstPage[9].Id);
    var firstTimes = new List<double>();
    var cursorTimes = new List<double>();

    for (var run = 0; run < 30; run++)
    {
        var stopwatch = Stopwatch.StartNew();
        await repository.ListPageAsync(userId, null, 10, CancellationToken.None);
        stopwatch.Stop();
        firstTimes.Add(stopwatch.Elapsed.TotalMilliseconds);

        stopwatch = Stopwatch.StartNew();
        await repository.ListPageAsync(userId, cursor, 10, CancellationToken.None);
        stopwatch.Stop();
        cursorTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
    }

    Console.WriteLine(
        $"rows=10000 first_p95_ms={P95(firstTimes):F2} cursor_p95_ms={P95(cursorTimes):F2}");

    var connection = read.Database.GetDbConnection();
    await connection.OpenAsync();
    using var command = connection.CreateCommand();
    command.CommandText = """
        EXPLAIN QUERY PLAN
        SELECT * FROM Tasks
        WHERE UserId = @userId
          AND (CreatedAtUtc < @c OR (CreatedAtUtc = @c AND Id < @i))
        ORDER BY CreatedAtUtc DESC, Id DESC
        LIMIT 11
        """;

    var userIdParameter = command.CreateParameter();
    userIdParameter.ParameterName = "@userId";
    userIdParameter.Value = userId.ToString();
    command.Parameters.Add(userIdParameter);

    var createdParameter = command.CreateParameter();
    createdParameter.ParameterName = "@c";
    createdParameter.Value = cursor.CreatedAtUtc;
    command.Parameters.Add(createdParameter);

    var idParameter = command.CreateParameter();
    idParameter.ParameterName = "@i";
    idParameter.Value = cursor.Id.ToString();
    command.Parameters.Add(idParameter);

    using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"plan={reader.GetString(3)}");
    }
}

}
finally
{
    if (File.Exists(databasePath))
    {
        File.Delete(databasePath);
    }
}

static double P95(List<double> values)
{
    var sorted = values.OrderBy(value => value).ToArray();
    var index = Math.Min(
        Math.Max((int)Math.Ceiling(0.95 * sorted.Length) - 1, 0),
        sorted.Length - 1);
    return sorted[index];
}
