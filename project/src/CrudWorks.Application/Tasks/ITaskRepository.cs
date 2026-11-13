using CrudWorks.Domain.Tasks;

namespace CrudWorks.Application.Tasks;

/// <summary>
/// Task persistence, owned by the Application layer. Every read is scoped by the
/// owning user id: the repository is the ownership boundary, so a foreign task is
/// indistinguishable from a missing one.
/// </summary>
public interface ITaskRepository
{
    /// <summary>All tasks owned by <paramref name="userId"/>, newest first.</summary>
    Task<IReadOnlyList<TaskItem>> ListByOwnerAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>The task only when it exists <em>and</em> is owned by <paramref name="userId"/>.</summary>
    Task<TaskItem?> FindAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    Task AddAsync(TaskItem task, CancellationToken cancellationToken);

    Task DeleteAsync(TaskItem task, CancellationToken cancellationToken);
}
