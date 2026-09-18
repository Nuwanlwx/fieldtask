using FieldTask.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FieldTask.Api.Data;

public class FieldTaskDbContext(DbContextOptions<FieldTaskDbContext> options) : DbContext(options)
{
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<ServiceTask> ServiceTasks => Set<ServiceTask>();
    public DbSet<TaskNote> TaskNotes => Set<TaskNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceTask>(entity =>
        {
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.TechnicianId);

            entity.HasOne(t => t.Technician)
                  .WithMany(tech => tech.Tasks)
                  .HasForeignKey(t => t.TechnicianId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(t => t.Notes)
                  .WithOne(n => n.ServiceTask)
                  .HasForeignKey(n => n.ServiceTaskId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}
