using FieldTask.Api.Data;
using FieldTask.Api.Domain.Entities;
using FieldTask.Api.Domain.Enums;
using FieldTask.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;


namespace FieldTask.Api.Services;

public class TaskService(FieldTaskDbContext db, IClock clock, ILogger<TaskService> logger, IEmbeddingService embeddingService) : ITaskService

{
    public async Task<PagedResult<TaskListItemDto>> GetTasksAsync(
        int page, int pageSize, TaskStatusEnum? status, string? search, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        IQueryable<ServiceTask> query = db.ServiceTasks.AsNoTracking();

        if (status is not null)
            query = query.Where(t => t.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => EF.Functions.Like(t.Title, $"%{search}%"));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TaskListItemDto(
                t.Id, t.Title, t.Status, t.Priority, t.CreatedAt,
                t.TechnicianId, t.Technician == null ? null : t.Technician.Name))
            .ToListAsync(ct);

        return new PagedResult<TaskListItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<TaskDetailDto?> GetTaskByIdAsync(int id, CancellationToken ct = default)
    {
        var task = await db.ServiceTasks
            .AsNoTracking()
            .Include(t => t.Notes)
            .Include(t => t.Technician)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        return task is null ? null : ToDetailDto(task);
    }

    public async Task<TaskDetailDto> CreateTaskAsync(CreateTaskDto dto, CancellationToken ct = default)
    {
        var task = new ServiceTask
        {
            Title = dto.Title,
            Priority = dto.Priority,
            Status = TaskStatusEnum.Open,
            CreatedAt = clock.UtcNow
        };
        // Best-effort: task creation must succeed even if Gemini's embedding call is slow
        // or down. Without an embedding this task just won't surface in duplicate-search
        // results until a later backfill - it never blocks creating the task itself.
        try
        {
            var embedding = await embeddingService.GetEmbeddingAsync(dto.Title, ct);
            task.EmbeddingJson = JsonSerializer.Serialize(embedding);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not compute embedding for new task {Title} - continuing without it.", dto.Title);
        }

        db.ServiceTasks.Add(task);
        await db.SaveChangesAsync(ct);

        return ToDetailDto(task);
    }

    public async Task<AssignTaskResult> AssignTaskAsync(int id, AssignTaskDto dto, CancellationToken ct = default)
    {
        var task = await db.ServiceTasks
            .Include(t => t.Notes)
            .Include(t => t.Technician)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (task is null)
            return new AssignTaskResult(AssignOutcome.TaskNotFound, null);

        var technicianExists = await db.Technicians.AnyAsync(t => t.Id == dto.TechnicianId, ct);
        if (!technicianExists)
            return new AssignTaskResult(AssignOutcome.TechnicianNotFound, null);

        task.TechnicianId = dto.TechnicianId;
        task.Status = TaskStatusEnum.Assigned;

        // Force these two columns into the UPDATE even if their new values happen to match
        // what's already stored (e.g. two requests both assigning the same technician). EF only
        // issues an UPDATE - and only then checks RowVersion - for properties it considers
        // "modified"; if a reassignment is a no-op by value, EF would otherwise skip the UPDATE
        // entirely and the concurrency check below would never run, silently letting a stale
        // write through.
        db.Entry(task).Property(t => t.TechnicianId).IsModified = true;
        db.Entry(task).Property(t => t.Status).IsModified = true;

        // Optimistic concurrency: tell EF Core "only apply this update if RowVersion in the
        // database still matches what the client last saw." If someone else changed the row
        // first, SaveChangesAsync throws DbUpdateConcurrencyException below.
        db.Entry(task).Property(t => t.RowVersion).OriginalValue = dto.RowVersion;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict assigning task {TaskId} to technician {TechnicianId}", id, dto.TechnicianId);
            return new AssignTaskResult(AssignOutcome.ConcurrencyConflict, null);
        }

        

        await db.Entry(task).Reference(t => t.Technician).LoadAsync(ct);
        return new AssignTaskResult(AssignOutcome.Success, ToDetailDto(task));
    }

    public async Task<bool> DeleteTaskAsync(int id, CancellationToken ct = default)
    {
        var task = await db.ServiceTasks.FindAsync([id], ct);
        if (task is null) return false;

        db.ServiceTasks.Remove(task);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static TaskDetailDto ToDetailDto(ServiceTask task) => new(
        task.Id, task.Title, task.Status, task.Priority, task.CreatedAt,
        task.TechnicianId, task.Technician?.Name, task.RowVersion,
        task.Notes.Select(n => new TaskNoteDto(n.Id, n.Text, n.CreatedAt)).ToList());
}
