using CrudWorks.Domain.Common;

namespace CrudWorks.Domain.Tasks;

/// <summary>
/// A task owned by exactly one user. Ownership is part of the aggregate identity:
/// every state change goes through the factory or <see cref="Update"/>, which never
/// touches the owner, so the domain model cannot reassign a task.
/// </summary>
public sealed class TaskItem
{
    public const int TitleMinLength = 3;
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    private TaskItem()
    {
    }

    private TaskItem(
        Guid id,
        Guid userId,
        string title,
        string? description,
        TaskItemStatus status,
        DateOnly? dueDate,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        UserId = userId;
        Title = title;
        Description = description;
        Status = status;
        DueDate = dueDate;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid Id { get; private set; }

    /// <summary>The owner, assigned at creation and never changed afterwards.</summary>
    public Guid UserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public TaskItemStatus Status { get; private set; }

    public DateOnly? DueDate { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>Trims the title; an empty or whitespace-only description becomes null.</summary>
    public static TaskItem Create(
        Guid userId,
        string title,
        string? description,
        TaskItemStatus? status,
        DateOnly? dueDate)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        var now = DateTime.UtcNow;
        return new TaskItem(
            Guid.NewGuid(),
            userId,
            EnsureValidTitle(title),
            EnsureValidDescription(description),
            status ?? TaskItemStatus.Todo,
            dueDate,
            now,
            now);
    }

    /// <summary>
    /// Replaces the editable fields and refreshes <see cref="UpdatedAtUtc"/>. The id,
    /// owner, and creation timestamp are intentionally not parameters: an update can
    /// never reassign ownership or forge an earlier creation time.
    /// </summary>
    public void Update(string title, string? description, TaskItemStatus status, DateOnly? dueDate)
    {
        Title = EnsureValidTitle(title);
        Description = EnsureValidDescription(description);
        Status = status;
        DueDate = dueDate;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string EnsureValidTitle(string? title)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (trimmed.Length < TitleMinLength || trimmed.Length > TitleMaxLength)
        {
            throw new DomainException(
                $"Title must be between {TitleMinLength} and {TitleMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? EnsureValidDescription(string? description)
    {
        var trimmed = description?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (trimmed.Length > DescriptionMaxLength)
        {
            throw new DomainException(
                $"Description must be at most {DescriptionMaxLength} characters.");
        }

        return trimmed;
    }
}
