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
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    private const string NotFoundMessage = "Task not found.";

    public async Task<TaskPage> ListAsync(
        Guid userId,
        TaskPageCursor? cursor,
        int take,
        CancellationToken cancellationToken)
    {
        var pageSize = ValidatePageSize(take);
        var tasks = await taskRepository.ListPageAsync(userId, cursor, pageSize, cancellationToken);
        var hasMore = tasks.Count > pageSize;
        var items = hasMore ? tasks.Take(pageSize).ToList() : tasks;
        var nextCursor = hasMore
            ? new TaskPageCursor(items[^1].CreatedAtUtc, items[^1].Id)
            : null;

        return new TaskPage(items.Select(task => task.ToTaskResponse()).ToList(), nextCursor);
    }

    private static int ValidatePageSize(int take)
    {
        if (take is < 1 or > MaxPageSize)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["take"] = [$"Page size must be between 1 and {MaxPageSize}."],
            });
        }

        return take;
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
