using CrudWorks.Application.Common.Exceptions;
using CrudWorks.Application.Users;
using CrudWorks.Domain.Tasks;

namespace CrudWorks.Application.Tasks;

/// <summary>
/// Task use cases with ownership enforcement. Every lookup is scoped by the caller's
/// user id, so a task owned by someone else is reported exactly like a missing task
/// ("Task not found.", 404) and existence is never leaked.
/// </summary>
public sealed class TaskService(ITaskRepository taskRepository, IUnitOfWork unitOfWork) : ITaskService
{
    private const string NotFoundMessage = "Task not found.";

    public async Task<IReadOnlyList<TaskResponse>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tasks = await taskRepository.ListByOwnerAsync(userId, cancellationToken);
        return tasks.Select(task => task.ToTaskResponse()).ToList();
    }

    public async Task<TaskResponse> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var task = await RequireTaskAsync(id, userId, cancellationToken);
        return task.ToTaskResponse();
    }

    public async Task<TaskResponse> CreateAsync(Guid userId, CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        ThrowIfInvalid(TaskValidation.ValidateCreate(request));

        var task = TaskItem.Create(
            userId,
            request.Title!,
            request.Description,
            TaskValidation.TryParseStatus(request.Status),
            TaskValidation.ParseDueDate(request.DueDate));

        await taskRepository.AddAsync(task, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return task.ToTaskResponse();
    }

    public async Task<TaskResponse> UpdateAsync(Guid userId, Guid id, UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        ThrowIfInvalid(TaskValidation.ValidateUpdate(request));

        var task = await RequireTaskAsync(id, userId, cancellationToken);
        task.Update(
            request.Title!,
            request.Description,
            TaskValidation.TryParseStatus(request.Status)!.Value,
            TaskValidation.ParseDueDate(request.DueDate));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return task.ToTaskResponse();
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var task = await RequireTaskAsync(id, userId, cancellationToken);
        await taskRepository.DeleteAsync(task, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<TaskItem> RequireTaskAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        await taskRepository.FindAsync(id, userId, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);

    private static void ThrowIfInvalid(IReadOnlyDictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
