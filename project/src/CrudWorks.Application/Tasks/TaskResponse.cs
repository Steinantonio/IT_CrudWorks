using CrudWorks.Domain.Tasks;

namespace CrudWorks.Application.Tasks;

/// <summary>The task resource returned by the API. Mirrors ADR-0004 §4.</summary>
public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    DateOnly? DueDate,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
