using FieldTask.Api.Domain.Entities;
using FieldTask.Api.Domain.Enums;

namespace FieldTask.Api.Data;

public static class DbSeeder
{
    public static void Seed(FieldTaskDbContext db)
    {
        if (db.Technicians.Any()) return; // already seeded, do nothing

        var technicians = new List<Technician>
        {
            new() { Name = "Alice Chen", Skill = "HVAC" },
            new() { Name = "Bruno Silva", Skill = "Electrical" },
            new() { Name = "Chidi Okafor", Skill = "Plumbing" },
        };
        db.Technicians.AddRange(technicians);
        db.SaveChanges();

        var titles = new[]
        {
            "Fix leaking pump", "Replace circuit breaker", "Inspect boiler",
            "Repair AC unit", "Clear blocked drain", "Rewire panel",
            "Service generator", "Check water pressure", "Install thermostat",
            "Diagnose electrical fault"
        };

        var tasks = titles.Select((title, i) => new ServiceTask
        {
            Title = title,
            Priority = (i % 3) + 1,
            Status = i % 3 == 0 ? TaskStatusEnum.Open
                   : i % 3 == 1 ? TaskStatusEnum.Assigned
                   : TaskStatusEnum.Done,
            TechnicianId = i % 3 == 0 ? null : technicians[i % technicians.Count].Id,
            CreatedAt = DateTime.UtcNow.AddDays(-i)
        }).ToList();

        db.ServiceTasks.AddRange(tasks);
        db.SaveChanges();
    }
}
