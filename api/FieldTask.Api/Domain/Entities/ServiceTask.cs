using FieldTask.Api.Domain.Enums;

namespace FieldTask.Api.Domain.Entities;

public class ServiceTask
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public TaskStatusEnum Status { get; set; } = TaskStatusEnum.Open;
    public int Priority { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Optional Foreign Key & Navigation property for Technician
    public int? TechnicianId { get; set; }
    public Technician? Technician { get; set; }

    // Navigation property: One ServiceTask has many TaskNotes
    public ICollection<TaskNote> Notes { get; set; } = new List<TaskNote>();

    // The task's embedding vector (from Gemini's embedding API), stored as JSON text
    // (e.g. "[0.012, -0.034, ...]"). Nullable - tasks created before this feature
    // existed won't have one until the backfill step.
    public string? EmbeddingJson { get; set; }

    // Concurrency token for optimistic locking. Mapped to SQL Server's native rowversion
    // column via IsRowVersion() in FieldTaskDbContext - the engine generates and changes this
    // value on every insert/update, so the app never writes to it directly.
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}                                                                       