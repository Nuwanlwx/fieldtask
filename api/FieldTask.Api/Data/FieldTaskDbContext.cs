using FieldTask.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FieldTask.Api.Data;

public class FieldTaskDbContext(DbContextOptions<FieldTaskDbContext> options) : DbContext(options)
{
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<ServiceTask> ServiceTasks => Set<ServiceTask>();
    public DbSet<TaskNote> TaskNotes => Set<TaskNote>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<ServiceTask>(entity =>
        {
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.TechnicianId);

            // Optimistic concurrency: IsRowVersion() both maps this to SQL Server's native
            // rowversion column type (auto-generated and changed by the engine on every write)
            // and marks it as a concurrency token, so EF adds "AND RowVersion = @original" to
            // every UPDATE/DELETE. IsConcurrencyToken() alone would only do the latter half.
            entity.Property(t => t.RowVersion).IsRowVersion();

            entity.HasOne(t => t.Technician)
                  .WithMany(tech => tech.Tasks)
                  .HasForeignKey(t => t.TechnicianId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(t => t.Notes)
                  .WithOne(n => n.ServiceTask)
                  .HasForeignKey(n => n.ServiceTaskId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
        });


        base.OnModelCreating(modelBuilder);
    }
}
