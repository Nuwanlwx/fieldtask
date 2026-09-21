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

            // Optimistic concurrency: EF adds "AND RowVersion = @original" to every UPDATE/DELETE.
            entity.Property(t => t.RowVersion).IsConcurrencyToken();

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

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampRowVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampRowVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // SQLite can't generate row versions like SQL Server's rowversion column does,
    // so give every inserted/updated ServiceTask a fresh random token ourselves.
    private void StampRowVersions()
    {
        foreach (var entry in ChangeTracker.Entries<ServiceTask>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
        }
    }
}
