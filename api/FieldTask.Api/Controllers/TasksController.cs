using FieldTask.Api.Domain.Enums;
using FieldTask.Api.Dtos;
using FieldTask.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace FieldTask.Api.Controllers;

[ApiController]
[Route("api/v1/tasks")]
[Authorize]
public class TasksController(ITaskService taskService) : ControllerBase
{
    // GET /api/v1/tasks?page=1&pageSize=10&status=Open&search=pump
    [HttpGet]
    public async Task<ActionResult<PagedResult<TaskListItemDto>>> GetTasks(
        int page = 1,
        int pageSize = 10,
        TaskStatusEnum? status = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var result = await taskService.GetTasksAsync(page, pageSize, status, search, ct);
        return Ok(result);
    }

    // GET /api/v1/tasks/5
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDetailDto>> GetTask(int id, CancellationToken ct)
    {
        var task = await taskService.GetTaskByIdAsync(id, ct);
        if (task is null)
            return TaskNotFound(id);

        return Ok(task);
    }

    // POST /api/v1/tasks
    [HttpPost]
    [ProducesResponseType<TaskDetailDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TaskDetailDto>> CreateTask(CreateTaskDto dto, CancellationToken ct)
    {
        var created = await taskService.CreateTaskAsync(dto, ct);
        return CreatedAtAction(nameof(GetTask), new { id = created.Id }, created);
    }

    // PUT /api/v1/tasks/5/assign
    [HttpPut("{id:int}/assign")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaskDetailDto>> AssignTask(int id, AssignTaskDto dto, CancellationToken ct)
    {
        var result = await taskService.AssignTaskAsync(id, dto, ct);

        switch (result.Outcome)
        {
            case AssignOutcome.Success:
                return Ok(result.Task);

            case AssignOutcome.TaskNotFound:
                return TaskNotFound(id);

            case AssignOutcome.TechnicianNotFound:
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Technician not found",
                    detail: $"No technician with id {dto.TechnicianId} exists.");

            case AssignOutcome.ConcurrencyConflict:
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Concurrency conflict",
                    detail: "The task was changed by someone else since you loaded it. Reload it and try again.");

            default:
                throw new InvalidOperationException($"Unhandled outcome: {result.Outcome}");
        }
    }

    // DELETE /api/v1/tasks/5
    [Authorize(Policy = "ManagerOnly")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(int id, CancellationToken ct)
    {
        var deleted = await taskService.DeleteTaskAsync(id, ct);
        if (!deleted)
            return TaskNotFound(id);

        return NoContent();
    }

    // One place to build the 404 body, so every "not found" looks the same.
    private ObjectResult TaskNotFound(int id) =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Task not found",
            detail: $"No task with id {id} exists.");
}
