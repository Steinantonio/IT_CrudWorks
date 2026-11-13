namespace CrudWorks.Application.Tasks;

/// <summary>
/// Create input. Fields are nullable because a JSON body may omit them; the
/// application layer is the authority that turns that into field-level errors.
/// There is deliberately no user id: the owner comes from the access token.
/// </summary>
public sealed record CreateTaskRequest(string? Title, string? Description, string? Status, string? DueDate);
