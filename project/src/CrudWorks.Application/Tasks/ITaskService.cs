namespace CrudWorks.Application.Tasks;

/// <summary>
/// The task use cases. The caller's user id is always a parameter supplied by the
/// API host from the token subject — never from a request body.
/// </summary>
public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>> ListAsync(Guid userId, CancellationToken cancellationToken);

    Task<TaskResponse> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken);

    Task<TaskResponse> CreateAsync(Guid userId, CreateTaskRequest request, CancellationToken cancellationToken);

    Task<TaskResponse> UpdateAsync(Guid userId, Guid id, UpdateTaskRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}
