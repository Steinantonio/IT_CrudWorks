using CrudWorks.Application.Common.Exceptions;
using CrudWorks.Application.Tasks;
using CrudWorks.Application.Users;
using CrudWorks.Domain.Tasks;
using NSubstitute;

namespace CrudWorks.Application.Tests.Tasks;

public sealed class TaskServiceTests
{
    private const string NotFoundMessage = "Task not found.";

    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TaskService _taskService;

    public TaskServiceTests()
    {
        _taskService = new TaskService(_taskRepository, _unitOfWork);
    }

    [Fact]
    public async Task ListAsync_DelegatesToTheRepositoryScopedByTheCaller()
    {
        var ownerId = Guid.NewGuid();
        var ownerTasks = new List<TaskItem>
        {
            TaskItem.Create(ownerId, "Second task", null, TaskItemStatus.Todo, null),
            TaskItem.Create(ownerId, "First task", null, TaskItemStatus.Done, null),
        };
        _taskRepository.ListByOwnerAsync(ownerId, Arg.Any<CancellationToken>())
            .Returns(ownerTasks);

        var responses = await _taskService.ListAsync(ownerId, CancellationToken.None);

        Assert.Equal(2, responses.Count);
        Assert.Equal(["Second task", "First task"], responses.Select(task => task.Title));
        // The service never queries without an owner id.
        await _taskRepository.Received(1).ListByOwnerAsync(ownerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_AssignsTheCallerUserIdAndServerTimestamps()
    {
        var ownerId = Guid.NewGuid();

        var response = await _taskService.CreateAsync(
            ownerId,
            new CreateTaskRequest("  Write the proposal  ", "  Draft and review.  ", "InProgress", "2026-11-14"),
            CancellationToken.None);

        Assert.Equal("Write the proposal", response.Title);
        Assert.Equal("Draft and review.", response.Description);
        Assert.Equal(TaskItemStatus.InProgress, response.Status);
        Assert.Equal(new DateOnly(2026, 11, 14), response.DueDate);

        await _taskRepository.Received(1).AddAsync(
            Arg.Is<TaskItem>(task =>
                task.UserId == ownerId
                && task.Title == "Write the proposal"
                && task.Status == TaskItemStatus.InProgress),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithoutStatus_DefaultsToTodo()
    {
        var response = await _taskService.CreateAsync(
            Guid.NewGuid(),
            new CreateTaskRequest("Write the proposal", null, null, null),
            CancellationToken.None);

        Assert.Equal(TaskItemStatus.Todo, response.Status);
    }

    [Fact]
    public async Task CreateAsync_WithWhitespaceDescription_StoresNull()
    {
        var response = await _taskService.CreateAsync(
            Guid.NewGuid(),
            new CreateTaskRequest("Write the proposal", "   ", null, null),
            CancellationToken.None);

        Assert.Null(response.Description);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithInvalidTitle_ThrowsValidationWithTheTitleKey(string title)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _taskService.CreateAsync(
            Guid.NewGuid(),
            new CreateTaskRequest(title, null, null, null),
            CancellationToken.None));

        Assert.Equal(["title"], exception.Errors.Keys);
        Assert.Equal(["Title must be between 3 and 200 characters."], exception.Errors["title"]);
        await _taskRepository.DidNotReceive().AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithOverlongDescription_ThrowsValidationWithTheDescriptionKey()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _taskService.CreateAsync(
            Guid.NewGuid(),
            new CreateTaskRequest("Write the proposal", new string('a', 2001), null, null),
            CancellationToken.None));

        Assert.Equal(["description"], exception.Errors.Keys);
        Assert.Equal(["Description must be at most 2000 characters."], exception.Errors["description"]);
    }

    [Theory]
    [InlineData("todo")]
    [InlineData("inprogress")]
    [InlineData("Unknown")]
    // "1" would map to InProgress under Enum.TryParse's integer-name handling;
    // only the exact names are accepted (ADR-0004 §4).
    [InlineData("1")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithUnknownStatus_ThrowsValidationWithTheStatusKey(string status)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _taskService.CreateAsync(
            Guid.NewGuid(),
            new CreateTaskRequest("Write the proposal", null, status, null),
            CancellationToken.None));

        Assert.Equal(["status"], exception.Errors.Keys);
        Assert.Equal(["Status must be Todo, InProgress, or Done."], exception.Errors["status"]);
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2026-13-01")]
    [InlineData("14/11/2026")]
    [InlineData("2026-11-14T00:00:00Z")]
    [InlineData("")]
    public async Task CreateAsync_WithInvalidDueDate_ThrowsValidationWithTheDueDateKey(string dueDate)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _taskService.CreateAsync(
            Guid.NewGuid(),
            new CreateTaskRequest("Write the proposal", null, null, dueDate),
            CancellationToken.None));

        Assert.Equal(["dueDate"], exception.Errors.Keys);
        Assert.Equal(["Due date must be a valid date."], exception.Errors["dueDate"]);
    }

    [Fact]
    public async Task GetAsync_WithAnOwnedTask_ReturnsTheMappedResponse()
    {
        var ownerId = Guid.NewGuid();
        var task = TaskItem.Create(ownerId, "Write the proposal", "Draft and review.", TaskItemStatus.Todo, null);
        _taskRepository.FindAsync(task.Id, ownerId, Arg.Any<CancellationToken>()).Returns(task);

        var response = await _taskService.GetAsync(ownerId, task.Id, CancellationToken.None);

        Assert.Equal(task.Id, response.Id);
        Assert.Equal("Write the proposal", response.Title);
        Assert.Equal("Draft and review.", response.Description);
        Assert.Equal(TaskItemStatus.Todo, response.Status);
    }

    [Fact]
    public async Task GetAsync_WithAnotherUsersTask_ThrowsNotFoundWithoutLeakingExistence()
    {
        _taskRepository.FindAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TaskItem?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _taskService.GetAsync(
            Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(NotFoundMessage, exception.Message);
    }

    [Fact]
    public async Task GetAsync_WithMissingTask_ThrowsNotFound()
    {
        _taskRepository.FindAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TaskItem?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _taskService.GetAsync(
            Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(NotFoundMessage, exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesEditableFieldsAndKeepsOwnerAndCreatedAtUtc()
    {
        var ownerId = Guid.NewGuid();
        var task = TaskItem.Create(ownerId, "Write the proposal", "First draft.", TaskItemStatus.Todo, null);
        var originalCreatedAtUtc = task.CreatedAtUtc;
        _taskRepository.FindAsync(task.Id, ownerId, Arg.Any<CancellationToken>()).Returns(task);

        var response = await _taskService.UpdateAsync(
            ownerId,
            task.Id,
            new UpdateTaskRequest("Review the proposal", null, "Done", "2026-11-20"),
            CancellationToken.None);

        Assert.Equal("Review the proposal", response.Title);
        Assert.Null(response.Description);
        Assert.Equal(TaskItemStatus.Done, response.Status);
        Assert.Equal(new DateOnly(2026, 11, 20), response.DueDate);
        Assert.Equal(originalCreatedAtUtc, task.CreatedAtUtc);
        Assert.Equal(ownerId, task.UserId);
        Assert.True(task.UpdatedAtUtc >= originalCreatedAtUtc);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithANullDueDate_ClearsAPreviouslySetDueDate()
    {
        var ownerId = Guid.NewGuid();
        var task = TaskItem.Create(
            ownerId, "Write the proposal", "Draft and review.", TaskItemStatus.Todo, new DateOnly(2026, 11, 14));
        _taskRepository.FindAsync(task.Id, ownerId, Arg.Any<CancellationToken>()).Returns(task);

        var response = await _taskService.UpdateAsync(
            ownerId,
            task.Id,
            new UpdateTaskRequest("Write the proposal", "Draft and review.", "Todo", null),
            CancellationToken.None);

        Assert.Null(response.DueDate);
        // The other editable fields are replaced, not lost, by the null due date.
        Assert.Equal("Write the proposal", response.Title);
        Assert.Equal("Draft and review.", response.Description);
        Assert.Equal(TaskItemStatus.Todo, response.Status);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithAnotherUsersTask_ThrowsNotFoundAndDoesNotSave()
    {
        _taskRepository.FindAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TaskItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _taskService.UpdateAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new UpdateTaskRequest("Review the proposal", null, "Done", null),
            CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithMissingStatus_ThrowsValidationWithTheStatusKey()
    {
        var ownerId = Guid.NewGuid();
        var task = TaskItem.Create(ownerId, "Write the proposal", null, TaskItemStatus.Todo, null);
        _taskRepository.FindAsync(task.Id, ownerId, Arg.Any<CancellationToken>()).Returns(task);

        // The status is required on update: a missing field must fail validation
        // instead of silently resetting the task state.
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _taskService.UpdateAsync(
            ownerId,
            task.Id,
            new UpdateTaskRequest("Review the proposal", null, null, null),
            CancellationToken.None));

        Assert.Equal(["status"], exception.Errors.Keys);
        Assert.Equal(["Status must be Todo, InProgress, or Done."], exception.Errors["status"]);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateAsync_WithAnInvalidTitle_ThrowsValidationWithTheTitleKey(string title)
    {
        var ownerId = Guid.NewGuid();
        var task = TaskItem.Create(ownerId, "Write the proposal", null, TaskItemStatus.Todo, null);
        _taskRepository.FindAsync(task.Id, ownerId, Arg.Any<CancellationToken>()).Returns(task);

        // The status is valid, so the title is the only reported field.
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _taskService.UpdateAsync(
            ownerId,
            task.Id,
            new UpdateTaskRequest(title, null, "Todo", null),
            CancellationToken.None));

        Assert.Equal(["title"], exception.Errors.Keys);
        Assert.Equal(["Title must be between 3 and 200 characters."], exception.Errors["title"]);
        // The request is rejected before the task is loaded or written.
        await _taskRepository.DidNotReceive().FindAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithAnOverlongDescription_ThrowsValidationWithTheDescriptionKey()
    {
        var ownerId = Guid.NewGuid();
        var task = TaskItem.Create(ownerId, "Write the proposal", null, TaskItemStatus.Todo, null);
        _taskRepository.FindAsync(task.Id, ownerId, Arg.Any<CancellationToken>()).Returns(task);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _taskService.UpdateAsync(
            ownerId,
            task.Id,
            new UpdateTaskRequest("Write the proposal", new string('a', 2001), "Todo", null),
            CancellationToken.None));

        Assert.Equal(["description"], exception.Errors.Keys);
        Assert.Equal(["Description must be at most 2000 characters."], exception.Errors["description"]);
        await _taskRepository.DidNotReceive().FindAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2026-13-01")]
    [InlineData("14/11/2026")]
    [InlineData("2026-11-14T00:00:00Z")]
    public async Task UpdateAsync_WithAnInvalidDueDate_ThrowsValidationWithTheDueDateKey(string dueDate)
    {
        var ownerId = Guid.NewGuid();
        var task = TaskItem.Create(ownerId, "Write the proposal", null, TaskItemStatus.Todo, null);
        _taskRepository.FindAsync(task.Id, ownerId, Arg.Any<CancellationToken>()).Returns(task);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _taskService.UpdateAsync(
            ownerId,
            task.Id,
            new UpdateTaskRequest("Write the proposal", null, "Todo", dueDate),
            CancellationToken.None));

        Assert.Equal(["dueDate"], exception.Errors.Keys);
        Assert.Equal(["Due date must be a valid date."], exception.Errors["dueDate"]);
        await _taskRepository.DidNotReceive().FindAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WithAnOwnedTask_DeletesAndSaves()
    {
        var ownerId = Guid.NewGuid();
        var task = TaskItem.Create(ownerId, "Write the proposal", null, TaskItemStatus.Todo, null);
        _taskRepository.FindAsync(task.Id, ownerId, Arg.Any<CancellationToken>()).Returns(task);

        await _taskService.DeleteAsync(ownerId, task.Id, CancellationToken.None);

        await _taskRepository.Received(1).DeleteAsync(task, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WithAnotherUsersTask_ThrowsNotFoundAndDoesNotDelete()
    {
        _taskRepository.FindAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TaskItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _taskService.DeleteAsync(
            Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));

        await _taskRepository.DidNotReceive().DeleteAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
