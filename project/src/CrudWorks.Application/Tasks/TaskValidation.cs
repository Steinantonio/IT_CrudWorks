using System.Globalization;
using CrudWorks.Domain.Tasks;

namespace CrudWorks.Application.Tasks;

/// <summary>
/// Business validation for the task use cases. Messages match the validation matrix
/// in ADR-0004 §5 so the client can display them directly; keys are the camelCase
/// JSON field names.
/// </summary>
internal static class TaskValidation
{
    public const string TitleMessage = "Title must be between 3 and 200 characters.";
    public const string DescriptionMessage = "Description must be at most 2000 characters.";
    public const string StatusMessage = "Status must be Todo, InProgress, or Done.";
    public const string DueDateMessage = "Due date must be a valid date.";

    private const string DueDateFormat = "yyyy-MM-dd";

    public static Dictionary<string, string[]> ValidateCreate(CreateTaskRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (!IsValidTitle(request.Title))
        {
            errors["title"] = [TitleMessage];
        }

        if (!IsValidDescription(request.Description))
        {
            errors["description"] = [DescriptionMessage];
        }

        if (!IsValidOptionalStatus(request.Status))
        {
            errors["status"] = [StatusMessage];
        }

        if (!IsValidOptionalDueDate(request.DueDate))
        {
            errors["dueDate"] = [DueDateMessage];
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateUpdate(UpdateTaskRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (!IsValidTitle(request.Title))
        {
            errors["title"] = [TitleMessage];
        }

        if (!IsValidDescription(request.Description))
        {
            errors["description"] = [DescriptionMessage];
        }

        // Required on update: a missing field must not silently reset the status.
        if (TryParseStatus(request.Status) is null)
        {
            errors["status"] = [StatusMessage];
        }

        if (!IsValidOptionalDueDate(request.DueDate))
        {
            errors["dueDate"] = [DueDateMessage];
        }

        return errors;
    }

    /// <summary>The status string is parsed here, at the application boundary; the
    /// closed enum keeps the domain free of string parsing. Only the exact names
    /// match (ADR-0004 §4): numeric strings ("1") and other casings are rejected.</summary>
    public static TaskItemStatus? TryParseStatus(string? status) => status?.Trim() switch
    {
        nameof(TaskItemStatus.Todo) => TaskItemStatus.Todo,
        nameof(TaskItemStatus.InProgress) => TaskItemStatus.InProgress,
        nameof(TaskItemStatus.Done) => TaskItemStatus.Done,
        _ => null,
    };

    public static DateOnly? ParseDueDate(string? dueDate) =>
        DateOnly.TryParseExact(dueDate, DueDateFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var parsed)
            ? parsed
            : null;

    private static bool IsValidTitle(string? title)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        return trimmed.Length >= TaskItem.TitleMinLength && trimmed.Length <= TaskItem.TitleMaxLength;
    }

    private static bool IsValidDescription(string? description)
    {
        var trimmed = description?.Trim();
        return trimmed is null || trimmed.Length <= TaskItem.DescriptionMaxLength;
    }

    private static bool IsValidOptionalStatus(string? status) =>
        status is null || TryParseStatus(status) is not null;

    private static bool IsValidOptionalDueDate(string? dueDate) =>
        dueDate is null || ParseDueDate(dueDate) is not null;
}
