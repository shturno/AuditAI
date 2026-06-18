using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;
using AuditAI.Infrastructure.Auth.PasswordHashing;
using AuditAI.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace AuditAI.IntegrationTests.Infrastructure;

public sealed class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("auditai_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithCleanUp(true)
        .Build();

    private HttpClient? _client;

    internal CustomWebApplicationFactory Factory { get; private set; } = null!;

    public HttpClient Client => _client ??= Factory.CreateClient();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Factory = new CustomWebApplicationFactory(_container.GetConnectionString());
        await ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await Factory.DisposeAsync();
        await _container.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditAIDbContext>();
        var passwordHasher = new AspNetPasswordHasher();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var organization = Organization.Create(TestData.OrganizationId, "AuditAI Org", TestData.SeedTimestamp);
        var otherOrganization = Organization.Create(TestData.OtherOrganizationId, "Other Org", TestData.SeedTimestamp);
        var department = Department.Create(TestData.DepartmentId, TestData.OrganizationId, "Finance", TestData.SeedTimestamp);
        var otherDepartment = Department.Create(TestData.OtherDepartmentId, TestData.OtherOrganizationId, "HR", TestData.SeedTimestamp);
        var user = User.Create(
            TestData.UserId,
            TestData.OrganizationId,
            TestData.DepartmentId,
            "Evidence Submitter",
            TestData.UserEmail,
            passwordHasher.HashPassword(TestData.UserPassword),
            UserRole.Auditor,
            TestData.SeedTimestamp);
        var otherUser = User.Create(
            TestData.OtherUserId,
            TestData.OtherOrganizationId,
            TestData.OtherDepartmentId,
            "Other User",
            "other@auditai.test",
            passwordHasher.HashPassword("OtherPassword123!"),
            UserRole.Auditor,
            TestData.SeedTimestamp);
        var control = Control.Create(
            TestData.ControlId,
            TestData.OrganizationId,
            TestData.DepartmentId,
            "CTRL-A",
            "Access Management",
            "Quarterly access review",
            "Seeded control for evidence tests.",
            ControlFrequency.Quarterly,
            TestData.SeedTimestamp);
        var otherControl = Control.Create(
            TestData.OtherControlId,
            TestData.OtherOrganizationId,
            TestData.OtherDepartmentId,
            "CTRL-B",
            "Vendor Management",
            "Vendor control",
            "Seeded control for mismatch tests.",
            ControlFrequency.Yearly,
            TestData.SeedTimestamp);

        dbContext.Organizations.AddRange(organization, otherOrganization);
        dbContext.Departments.AddRange(department, otherDepartment);
        dbContext.Users.AddRange(user, otherUser);
        dbContext.Controls.AddRange(control, otherControl);

        await dbContext.SaveChangesAsync();
    }
}
