using CrudWorks.Application.Tasks;

namespace CrudWorks.Api.Pagination;

/// <summary>The paginated task list returned by GET /api/tasks (ADR-0006).</summary>
public sealed record TaskPageResponse(IReadOnlyList<TaskResponse> Items, string? NextCursor);
