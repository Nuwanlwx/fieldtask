namespace FieldTask.Api.Domain.Entities;

public class TaskNote
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Key & Navigation property back to ServiceTask
    public int ServiceTaskId { get; set; }
    public ServiceTask ServiceTask { get; set; } = null!;
}                                                               