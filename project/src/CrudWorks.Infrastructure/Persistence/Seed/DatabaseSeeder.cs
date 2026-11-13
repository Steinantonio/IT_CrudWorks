using CrudWorks.Application.Auth;
using CrudWorks.Domain.Tasks;
using CrudWorks.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace CrudWorks.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public const string DemoEmail = "demo@crudworks.local";
    public const string DemoDisplayName = "Demo User";

    // Demo credential documented in the README; not a secret and never reused outside Development.
    public const string DemoPassword = "Demo123!";

    /// <summary>
    /// Applies pending migrations and seeds the demo account when the database is
    /// empty. Called from the API host in Development only.
    /// </summary>
    public static async Task InitializeAsync(
        CrudWorksDbContext dbContext,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);
        await SeedDemoUserAsync(dbContext, passwordHasher, cancellationToken);
        await SeedDemoTasksAsync(dbContext, cancellationToken: cancellationToken);
    }

    public static async Task SeedDemoUserAsync(
        CrudWorksDbContext dbContext,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var user = User.Create(DemoEmail, DemoDisplayName, passwordHasher.Hash(DemoPassword));
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Seeds five demo tasks for the demo user, covering all three statuses, when the
    /// Tasks table has none. Idempotent: the count is checked before anything is added.
    /// </summary>
    public static async Task SeedDemoTasksAsync(
        CrudWorksDbContext dbContext,
        Guid? demoUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Tasks.AnyAsync(cancellationToken))
        {
            return;
        }

        Guid userId;
        if (demoUserId is { } explicitUserId)
        {
            userId = explicitUserId;
        }
        else
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(
                    candidate => candidate.Email == DemoEmail, cancellationToken)
                ?? throw new InvalidOperationException(
                    "The demo user must be seeded before the demo tasks.");
            userId = user.Id;
        }

        // Staggered timestamps make the newest-first list ordering visible in the demo.
        // The content is a story the demo user could actually own: a busy professional
        // working through personal and business errands, all with a description and a
        // due date so no card in the UI looks half-filled.
        (string Title, string Description, TaskItemStatus Status, DateOnly DueDate, int MinutesAgo)[] seed =
        [
            ("Prepare the quarterly report",
                "Pull the sales numbers, write the summary, and send the draft to finance.",
                TaskItemStatus.InProgress, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), 30),
            ("Review the vendor contract",
                "Compare the renewal terms with the budget and note the questions for legal.",
                TaskItemStatus.InProgress, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), 60),
            ("Book the dentist appointment",
                "Call the clinic and confirm the insurance coverage before the visit.",
                TaskItemStatus.Todo, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), 120),
            ("Renew the passport",
                "Gather the photo, proof of address, and the old passport for the appointment.",
                TaskItemStatus.Todo, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), 180),
            ("Send the tax documents to the accountant",
                "Upload the receipts and confirm the accountant received the files.",
                TaskItemStatus.Done, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), 240),
        ];

        foreach (var (title, description, status, dueDate, minutesAgo) in seed)
        {
            var task = TaskItem.Create(userId, title, description, status, dueDate);
            var createdAtUtc = DateTime.UtcNow.AddMinutes(-minutesAgo);
            // The domain factory stamps "now"; the seeder owns the demo timeline only.
            dbContext.Tasks.Add(task);
            dbContext.Entry(task).Property(candidate => candidate.CreatedAtUtc).CurrentValue = createdAtUtc;
            dbContext.Entry(task).Property(candidate => candidate.UpdatedAtUtc).CurrentValue = createdAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
