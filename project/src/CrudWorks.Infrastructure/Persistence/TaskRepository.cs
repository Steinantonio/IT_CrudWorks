using CrudWorks.Application.Tasks;
using CrudWorks.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CrudWorks.Infrastructure.Persistence;

public sealed class TaskRepository(CrudWorksDbContext dbContext) : ITaskRepository
{
    public async Task<IReadOnlyList<TaskItem>> ListPageAsync(
        Guid userId,
        TaskPageCursor? cursor,
        int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.UserId == userId);

        if (cursor is not null)
        {
            query = query.Where(task =>
                task.CreatedAtUtc < cursor.CreatedAtUtc
                || (task.CreatedAtUtc == cursor.CreatedAtUtc && task.Id.CompareTo(cursor.Id) < 0));
        }

        return await query
            .OrderByDescending(task => task.CreatedAtUtc)
            .ThenByDescending(task => task.Id)
            .Take(take + 1)
            .ToListAsync(cancellationToken);
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
