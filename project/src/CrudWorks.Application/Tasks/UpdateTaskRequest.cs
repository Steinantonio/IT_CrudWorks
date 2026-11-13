namespace CrudWorks.Application.Tasks;

/// <summary>
/// Full-replacement update input. The status is typed as a string so a missing
/// field fails application validation ("required on update") instead of being
/// silently reset to a default by model binding.
/// </summary>
public sealed record UpdateTaskRequest(string? Title, string? Description, string? Status, string? DueDate);
