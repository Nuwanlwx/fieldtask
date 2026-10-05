using FieldTask.Api.Domain.Entities;
using FieldTask.Api.Domain.Enums;
using FieldTask.Api.Dtos;
using FieldTask.Api.Services;
using FieldTask.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace FieldTask.Tests;

public class TaskServiceTests
{
    // NullLogger.Instance satisfies TaskService's ILogger<TaskService> dependency without
    // writing anywhere - the tests don't care what gets logged, only what TaskService returns.
    private static TaskService BuildService(FieldTask.Api.Data.FieldTaskDbContext db) =>
        new(db, new FakeClock(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)), NullLogger<TaskService>.Instance);

    [Fact]
    public async Task GetTasks_FiltersByStatus()
    {
        using var testDb = new SqlServerTestDatabase();
        using (var seedDb = testDb.CreateContext())
        {
            seedDb.ServiceTasks.AddRange(
                new ServiceTask { Title = "Open task", Status = TaskStatusEnum.Open, Priority = 1 },
                new ServiceTask { Title = "Assigned task", Status = TaskStatusEnum.Assigned, Priority = 1 },
                new ServiceTask { Title = "Done task", Status = TaskStatusEnum.Done, Priority = 1 });
            await seedDb.SaveChangesAsync();
        }

        using var db = testDb.CreateContext();
        var service = BuildService(db);

        var result = await service.GetTasksAsync(page: 1, pageSize: 10, status: TaskStatusEnum.Open, search: null);

        Assert.Equal(1, result.Total);
        Assert.Single(result.Items);
        Assert.Equal("Open task", result.Items[0].Title);
        Assert.Equal(TaskStatusEnum.Open, result.Items[0].Status);
    }

    [Fact]
    public async Task GetTaskById_UnknownId_ReturnsNull()
    {
        // The service returning null is what the controller (Step 1.5) turns into a 404
        // ProblemDetails response - that HTTP translation is already covered by the manual
        // Scalar/curl checks. This test covers the service-layer half of that behaviour.
        using var testDb = new SqlServerTestDatabase();
        using var db = testDb.CreateContext();
        var service = BuildService(db);

        var result = await service.GetTaskByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task AssignTask_SetsTechnicianAndStatus()
    {
        using var testDb = new SqlServerTestDatabase();
        int taskId, technicianId;
        using (var seedDb = testDb.CreateContext())
        {
            var technician = new Technician { Name = "Alice Chen", Skill = "HVAC" };
            var task = new ServiceTask { Title = "Fix leaking pump", Status = TaskStatusEnum.Open, Priority = 1 };
            seedDb.Technicians.Add(technician);
            seedDb.ServiceTasks.Add(task);
            await seedDb.SaveChangesAsync();
            technicianId = technician.Id;
            taskId = task.Id;
        }

        byte[] rowVersion;
        using (var readDb = testDb.CreateContext())
            rowVersion = (await readDb.ServiceTasks.FindAsync(taskId))!.RowVersion;

        using var db = testDb.CreateContext();
        var service = BuildService(db);

        var result = await service.AssignTaskAsync(taskId, new AssignTaskDto(technicianId, rowVersion));

        Assert.Equal(AssignOutcome.Success, result.Outcome);
        Assert.NotNull(result.Task);
        Assert.Equal(TaskStatusEnum.Assigned, result.Task!.Status);
        Assert.Equal(technicianId, result.Task.TechnicianId);
        Assert.Equal("Alice Chen", result.Task.TechnicianName);
    }

    [Fact]
    public async Task AssignTask_StaleRowVersion_ReturnsConcurrencyConflict()
    {
        // This is the project's main concurrency story: two "requests" load the same task,
        // one saves first, the second must be rejected instead of silently overwriting it.
        using var testDb = new SqlServerTestDatabase();
        int taskId, technicianId;
        using (var seedDb = testDb.CreateContext())
        {
            var technician = new Technician { Name = "Bruno Silva", Skill = "Electrical" };
            var task = new ServiceTask { Title = "Replace circuit breaker", Status = TaskStatusEnum.Open, Priority = 2 };
            seedDb.Technicians.Add(technician);
            seedDb.ServiceTasks.Add(task);
            await seedDb.SaveChangesAsync();
            technicianId = technician.Id;
            taskId = task.Id;
        }

        // The RowVersion both "clients" start out holding.
        byte[] staleRowVersion;
        using (var readDb = testDb.CreateContext())
            staleRowVersion = (await readDb.ServiceTasks.FindAsync(taskId))!.RowVersion;

        // "Client A" saves first and succeeds, which changes the row's RowVersion in the database.
        using (var firstDb = testDb.CreateContext())
        {
            var firstResult = await BuildService(firstDb)
                .AssignTaskAsync(taskId, new AssignTaskDto(technicianId, staleRowVersion));
            Assert.Equal(AssignOutcome.Success, firstResult.Outcome);
        }

        // "Client B" still has the OLD RowVersion and tries to save with it -> must be rejected.
        using var secondDb = testDb.CreateContext();
        var secondResult = await BuildService(secondDb)
            .AssignTaskAsync(taskId, new AssignTaskDto(technicianId, staleRowVersion));

        Assert.Equal(AssignOutcome.ConcurrencyConflict, secondResult.Outcome);
        Assert.Null(secondResult.Task);
    }
}
