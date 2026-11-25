using CrudWorks.Api.Pagination;
using CrudWorks.Application.Common;
using CrudWorks.Application.Common.Exceptions;
using CrudWorks.Application.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrudWorks.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TasksController(ITaskService taskService, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<TaskPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TaskPageResponse>> List(
        [FromQuery] string? cursor,
        CancellationToken cancellationToken,
        [FromQuery] int take = TaskService.DefaultPageSize)
    {
        var pageCursor = cursor is null ? null : TaskCursorCodec.Decode(cursor);
        var page = await taskService.ListAsync(RequireUserId(), pageCursor, take, cancellationToken);

        return Ok(new TaskPageResponse(
            page.Items,
            page.NextCursor is null ? null : TaskCursorCodec.Encode(page.NextCursor)));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TaskResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await taskService.GetAsync(RequireUserId(), id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TaskResponse>> Create(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await taskService.CreateAsync(RequireUserId(), request, cancellationToken);
        return Created(Url.Action(nameof(GetById), new { id = task.Id })!, task);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TaskResponse>> Update(
        Guid id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken) =>
        Ok(await taskService.UpdateAsync(RequireUserId(), id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await taskService.DeleteAsync(RequireUserId(), id, cancellationToken);
        return NoContent();
    }

    // The owner always comes from the token subject, never from the request body.
    private Guid RequireUserId() =>
        currentUser.UserId ?? throw new UnauthorizedException("Authentication required");
}
