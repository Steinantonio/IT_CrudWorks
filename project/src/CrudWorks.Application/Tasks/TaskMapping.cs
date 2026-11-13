using CrudWorks.Domain.Tasks;

namespace CrudWorks.Application.Tasks;

internal static class TaskMapping
{
    public static TaskResponse ToTaskResponse(this TaskItem task) =>
        new(task.Id, task.Title, task.Description, task.Status, task.DueDate,
            task.CreatedAtUtc, task.UpdatedAtUtc);
}
