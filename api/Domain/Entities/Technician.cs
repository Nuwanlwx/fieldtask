namespace FieldTask.Api.Domain.Entities;

public class Technician
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Skill { get; set; } = string.Empty;

    // Navigation property: One Technician has many ServiceTasks
    public ICollection<ServiceTask> Tasks { get; set; } = new List<ServiceTask>();
}