using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuditAI.Infrastructure.Persistence.DesignTime;

public sealed class AuditAIDbContextFactory : IDesignTimeDbContextFactory<AuditAIDbContext>
{
    public AuditAIDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuditAIDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=auditai_dev;Username=postgres;Password=changeme");

        return new AuditAIDbContext(optionsBuilder.Options);
    }
}
