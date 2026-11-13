namespace CrudWorks.Domain.Tasks;

/// <summary>
/// The three workflow states of a task. Named exactly as the API contract
/// (ADR-0004) serializes them and persisted as these strings.
/// </summary>
public enum TaskItemStatus
{
    Todo,
    InProgress,
    Done,
}
