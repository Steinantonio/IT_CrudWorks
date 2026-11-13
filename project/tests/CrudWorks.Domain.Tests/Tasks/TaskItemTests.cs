using CrudWorks.Domain.Common;
using CrudWorks.Domain.Tasks;

namespace CrudWorks.Domain.Tests.Tasks;

public sealed class TaskItemTests
{
    [Fact]
    public void Create_WithValidInput_AssignsIdOwnerAndUtcTimestamps()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var ownerId = Guid.NewGuid();

        var task = TaskItem.Create(ownerId, "Write the proposal", "Draft and review.", null, null);

        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal(ownerId, task.UserId);
        Assert.Equal(DateTimeKind.Utc, task.CreatedAtUtc.Kind);
        Assert.InRange(task.CreatedAtUtc, before, DateTime.UtcNow.AddSeconds(1));
        Assert.Equal(task.CreatedAtUtc, task.UpdatedAtUtc);
    }

    [Fact]
    public void Create_TrimsTitleAndDescription()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "  Write the proposal  ", "  Draft and review.  ", null, null);

        Assert.Equal("Write the proposal", task.Title);
        Assert.Equal("Draft and review.", task.Description);
    }

    [Fact]
    public void Create_WithWhitespaceDescription_StoresNull()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Write the proposal", "   ", null, null);

        Assert.Null(task.Description);
    }

    [Fact]
    public void Create_WithNullDescription_StoresNull()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Write the proposal", null, null, null);

        Assert.Null(task.Description);
    }

    [Fact]
    public void Create_WithoutStatus_DefaultsToTodo()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Write the proposal", null, null, null);

        Assert.Equal(TaskItemStatus.Todo, task.Status);
    }

    [Fact]
    public void Create_WithExplicitStatus_UsesIt()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Write the proposal", null, TaskItemStatus.InProgress, null);

        Assert.Equal(TaskItemStatus.InProgress, task.Status);
    }

    [Fact]
    public void Create_WithDueDate_StoresIt()
    {
        var dueDate = new DateOnly(2026, 11, 14);

        var task = TaskItem.Create(Guid.NewGuid(), "Write the proposal", null, null, dueDate);

        Assert.Equal(dueDate, task.DueDate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData("  ab  ")]
    public void Create_WithTitleShorterThanThreeCharacters_ThrowsDomainException(string title)
    {
        Assert.Throws<DomainException>(() =>
            TaskItem.Create(Guid.NewGuid(), title, null, null, null));
    }

    [Fact]
    public void Create_WithTitleLongerThanTwoHundredCharacters_ThrowsDomainException()
    {
        var title = new string('a', 201);

        Assert.Throws<DomainException>(() =>
            TaskItem.Create(Guid.NewGuid(), title, null, null, null));
    }

    [Fact]
    public void Create_WithTitleOfExactlyTwoHundredCharacters_Succeeds()
    {
        var title = new string('a', 200);

        var task = TaskItem.Create(Guid.NewGuid(), title, null, null, null);

        Assert.Equal(title, task.Title);
    }

    [Fact]
    public void Create_WithDescriptionLongerThanTwoThousandCharacters_ThrowsDomainException()
    {
        var description = new string('a', 2001);

        Assert.Throws<DomainException>(() =>
            TaskItem.Create(Guid.NewGuid(), "Write the proposal", description, null, null));
    }

    [Fact]
    public void Create_WithDescriptionOfExactlyTwoThousandCharacters_Succeeds()
    {
        var description = new string('a', 2000);

        var task = TaskItem.Create(Guid.NewGuid(), "Write the proposal", description, null, null);

        Assert.Equal(description, task.Description);
    }

    [Fact]
    public void Update_ReplacesEditableFieldsAndRefreshesUpdatedAtUtc()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Write the proposal", "First draft.", TaskItemStatus.Todo, null);
        var originalUpdatedAtUtc = task.UpdatedAtUtc;

        // Simulate elapsed time so the refresh is observable.
        System.Threading.Thread.Sleep(5);
        task.Update("Review the proposal", null, TaskItemStatus.Done, new DateOnly(2026, 11, 20));

        Assert.Equal("Review the proposal", task.Title);
        Assert.Null(task.Description);
        Assert.Equal(TaskItemStatus.Done, task.Status);
        Assert.Equal(new DateOnly(2026, 11, 20), task.DueDate);
        Assert.True(task.UpdatedAtUtc >= originalUpdatedAtUtc);
    }

    [Fact]
    public void Update_NeverTouchesIdentityOwnershipOrCreatedAtUtc()
    {
        var ownerId = Guid.NewGuid();
        var task = TaskItem.Create(ownerId, "Write the proposal", null, TaskItemStatus.Todo, null);
        var originalId = task.Id;
        var originalCreatedAtUtc = task.CreatedAtUtc;

        task.Update("Review the proposal", "Second pass.", TaskItemStatus.Done, null);

        Assert.Equal(originalId, task.Id);
        Assert.Equal(ownerId, task.UserId);
        Assert.Equal(originalCreatedAtUtc, task.CreatedAtUtc);
    }

    [Fact]
    public void Update_TrimsTitleAndTurnsWhitespaceDescriptionIntoNull()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Write the proposal", "First draft.", TaskItemStatus.Todo, null);

        task.Update("  Review the proposal  ", "   ", TaskItemStatus.InProgress, null);

        Assert.Equal("Review the proposal", task.Title);
        Assert.Null(task.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithInvalidTitle_ThrowsDomainException(string title)
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Write the proposal", null, TaskItemStatus.Todo, null);

        Assert.Throws<DomainException>(() =>
            task.Update(title, null, TaskItemStatus.Todo, null));
    }

    [Fact]
    public void Update_WithDescriptionOverLimit_ThrowsDomainException()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Write the proposal", null, TaskItemStatus.Todo, null);

        Assert.Throws<DomainException>(() =>
            task.Update("Review the proposal", new string('a', 2001), TaskItemStatus.Todo, null));
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            TaskItem.Create(Guid.Empty, "Write the proposal", null, null, null));
    }
}
