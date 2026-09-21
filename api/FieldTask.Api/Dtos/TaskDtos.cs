using FieldTask.Api.Domain.Enums;
using System.ComponentModel.DataAnnotations;


namespace FieldTask.Api.Dtos;

// Shape returned by the list endpoint — deliberately no Notes, keeps the payload light.
public record TaskListItemDto(
    int Id,
    string Title,
    TaskStatusEnum Status,
    int Priority,
    DateTime CreatedAt,
    int? TechnicianId,
    string? TechnicianName);

// Shape returned by the single-task endpoint — includes Notes and RowVersion.
public record TaskDetailDto(
    int Id,
    string Title,
    TaskStatusEnum Status,
    int Priority,
    DateTime CreatedAt,
    int? TechnicianId,
    string? TechnicianName,
    byte[] RowVersion,
    IReadOnlyList<TaskNoteDto> Notes);

public record TaskNoteDto(int Id, string Text, DateTime CreatedAt);

// Input for POST /tasks
public record CreateTaskDto(
    [Required, StringLength(200)] string Title,
    [Range(1, 5)] int Priority);

// Input for PUT /tasks/{id}/assign — RowVersion is what the client last saw,
// used to detect if someone else changed the task in the meantime.
public record AssignTaskDto(
    [Range(1, int.MaxValue)] int TechnicianId,
    [Required] byte[] RowVersion);
