using System.ComponentModel.DataAnnotations;
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

    // Concurrency Token for optimistic locking
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}                                                                       