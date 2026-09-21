using FieldTask.Api.Data;
using FieldTask.Api.Domain.Entities;
using FieldTask.Api.Domain.Enums;
using FieldTask.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FieldTask.Api.Services;

public class TaskService(FieldTaskDbContext db, IClock clock) : ITaskService
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

        // Optimistic concurrency: tell EF Core "only apply this update if RowVersion in the
        // database still matches what the client last saw." If someone else changed the row
        // first, SaveChangesAsync throws DbUpdateConcurrencyException below.
        db.Entry(task).Property(t => t.RowVersion).OriginalValue = dto.RowVersion;

        task.TechnicianId = dto.TechnicianId;
        task.Status = TaskStatusEnum.Assigned;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
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
