namespace CrudWorks.Application.Tasks;

/// <summary>One page of task responses plus the position of the next page.</summary>
public sealed record TaskPage(IReadOnlyList<TaskResponse> Items, TaskPageCursor? NextCursor);
