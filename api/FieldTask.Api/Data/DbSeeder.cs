using FieldTask.Api.Domain.Entities;
using FieldTask.Api.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace FieldTask.Api.Data;

public static class DbSeeder
{
    public static void Seed(FieldTaskDbContext db)
    {
        if (!db.Users.Any())
        {
            var hasher = new PasswordHasher<User>();

            var users = new[]
            {
                new User { Username = "alice", Role = "Manager" },
                new User { Username = "bruno", Role = "Technician" },
                new User { Username = "chidi", Role = "Technician" }
            };

            // HashPassword needs a user instance (some hashing schemes mix in user data), but
            // doesn't read anything else off it here - the instance is just a parameter shape.
            users[0].PasswordHash = hasher.HashPassword(users[0], "manager123");
            users[1].PasswordHash = hasher.HashPassword(users[1], "tech123");
            users[2].PasswordHash = hasher.HashPassword(users[2], "tech123");

            db.Users.AddRange(users);
            db.SaveChanges();
        }

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
