using CrudWorks.Application.Tasks;
using CrudWorks.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CrudWorks.Infrastructure.Persistence;

public sealed class TaskRepository(CrudWorksDbContext dbContext) : ITaskRepository
{
    public async Task<IReadOnlyList<TaskItem>> ListByOwnerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tasks = await dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.UserId == userId)
            .OrderByDescending(task => task.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return tasks;
    }

    /// <summary>Tracked on purpose: the same find feeds the update and delete use
    /// cases, whose mutations must be persisted by SaveChangesAsync. A no-tracking
    /// load would silently discard every update.</summary>
    public Task<TaskItem?> FindAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        dbContext.Tasks
            .SingleOrDefaultAsync(task => task.Id == id && task.UserId == userId, cancellationToken);

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        await dbContext.Tasks.AddAsync(task, cancellationToken);
    }

    public Task DeleteAsync(TaskItem task, CancellationToken cancellationToken)
    {
        dbContext.Tasks.Remove(task);
        return Task.CompletedTask;
    }
}
