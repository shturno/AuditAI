using AuditAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuditAI.Infrastructure.Persistence;

public sealed class AuditAIDbContext : DbContext
{
    public AuditAIDbContext(DbContextOptions<AuditAIDbContext> options)
        : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Control> Controls => Set<Control>();

    public DbSet<Evidence> Evidence => Set<Evidence>();

    public DbSet<AuditFinding> AuditFindings => Set<AuditFinding>();

    public DbSet<ActionPlan> ActionPlans => Set<ActionPlan>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditAIDbContext).Assembly);
    }
}
