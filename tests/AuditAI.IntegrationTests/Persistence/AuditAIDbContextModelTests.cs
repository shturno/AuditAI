using AuditAI.Domain.Entities;
using AuditAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuditAI.IntegrationTests.Persistence;

public sealed class AuditAIDbContextModelTests
{
    [Fact]
    public void Should_CreateDbContext_AndBuildModel()
    {
        var options = new DbContextOptionsBuilder<AuditAIDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=auditai_test;Username=postgres;Password=changeme")
            .Options;

        using var dbContext = new AuditAIDbContext(options);

        var model = dbContext.Model;

        Assert.NotNull(model.FindEntityType(typeof(Organization)));
        Assert.NotNull(model.FindEntityType(typeof(Department)));
        Assert.NotNull(model.FindEntityType(typeof(User)));
        Assert.NotNull(model.FindEntityType(typeof(Control)));
        Assert.NotNull(model.FindEntityType(typeof(Evidence)));
        Assert.NotNull(model.FindEntityType(typeof(AuditFinding)));
        Assert.NotNull(model.FindEntityType(typeof(ActionPlan)));
        Assert.NotNull(model.FindEntityType(typeof(AuditLog)));
    }
}
