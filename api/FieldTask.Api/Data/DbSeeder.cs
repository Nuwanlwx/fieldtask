using FieldTask.Api.Domain.Entities;
using FieldTask.Api.Domain.Enums;

namespace FieldTask.Api.Data;

public static class DbSeeder
{
    public static void Seed(FieldTaskDbContext db)
    {
        // Each table is checked on its own, so a partly seeded database heals itself.
        if (!db.Technicians.Any())
        {
            db.Technicians.AddRange(
                new Technician { Name = "Alice Chen", Skill = "HVAC" },
                new Technician { Name = "Bruno Silva", Skill = "Electrical" },
                new Technician { Name = "Chidi Okafor", Skill = "Plumbing" });
            db.SaveChanges();
        }

        if (db.ServiceTasks.Any()) return; // already seeded, do nothing

        var technicians = db.Technicians.OrderBy(t => t.Id).ToList();

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
