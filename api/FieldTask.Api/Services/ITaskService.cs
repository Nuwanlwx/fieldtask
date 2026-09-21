using FieldTask.Api.Domain.Enums;
using FieldTask.Api.Dtos;

namespace FieldTask.Api.Services;

public interface ITaskService
{
    Task<PagedResult<TaskListItemDto>> GetTasksAsync(
        int page, int pageSize, TaskStatusEnum? status, string? search, CancellationToken ct = default);

    Task<TaskDetailDto?> GetTaskByIdAsync(int id, CancellationToken ct = default);

    Task<TaskDetailDto> CreateTaskAsync(CreateTaskDto dto, CancellationToken ct = default);

    Task<AssignTaskResult> AssignTaskAsync(int id, AssignTaskDto dto, CancellationToken ct = default);

    Task<bool> DeleteTaskAsync(int id, CancellationToken ct = default);
}

public enum AssignOutcome { Success, TaskNotFound, TechnicianNotFound, ConcurrencyConflict }

public record AssignTaskResult(AssignOutcome Outcome, TaskDetailDto? Task);
