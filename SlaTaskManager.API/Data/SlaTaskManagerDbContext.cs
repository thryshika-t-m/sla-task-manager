using Microsoft.EntityFrameworkCore;
using SlaTaskManager.API.Entities;

namespace SlaTaskManager.API.Data;

public class SlaTaskManagerDbContext : DbContext
{
    public SlaTaskManagerDbContext(DbContextOptions<SlaTaskManagerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Entities.Task> Tasks => Set<Entities.Task>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Entities.Task>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(e => e.AssignedTo)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Priority)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasMany(e => e.AuditLogs)
                .WithOne(e => e.Task)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Notes)
                .IsRequired()
                .HasMaxLength(2000);
        });
    }
}
