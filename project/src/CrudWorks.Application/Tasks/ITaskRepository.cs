using CrudWorks.Domain.Tasks;

namespace CrudWorks.Application.Tasks;

/// <summary>
/// Task persistence, owned by the Application layer. Every read is scoped by the
/// owning user id: the repository is the ownership boundary, so a foreign task is
/// indistinguishable from a missing one.
/// </summary>
public interface ITaskRepository
{
    /// <summary>
    /// At most <paramref name="take"/> + 1 tasks owned by <paramref name="userId"/>,
    /// ordered by <c>CreatedAtUtc DESC, Id DESC</c> and strictly after
    /// <paramref name="cursor"/> when one is supplied. The extra row lets the
    /// service decide whether a next page exists without a count query.
    /// </summary>
    Task<IReadOnlyList<TaskItem>> ListPageAsync(
        Guid userId,
        TaskPageCursor? cursor,
        int take,
        CancellationToken cancellationToken);

    /// <summary>The task only when it exists <em>and</em> is owned by <paramref name="userId"/>.</summary>
    Task<TaskItem?> FindAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    Task AddAsync(TaskItem task, CancellationToken cancellationToken);

    Task DeleteAsync(TaskItem task, CancellationToken cancellationToken);
}
