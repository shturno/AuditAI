using AuditAI.Domain.Entities;
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

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var organization = Organization.Create(TestData.OrganizationId, "AuditAI Org", TestData.SeedTimestamp);
        var otherOrganization = Organization.Create(TestData.OtherOrganizationId, "Other Org", TestData.SeedTimestamp);
        var department = Department.Create(TestData.DepartmentId, TestData.OrganizationId, "Finance", TestData.SeedTimestamp);
        var otherDepartment = Department.Create(TestData.OtherDepartmentId, TestData.OtherOrganizationId, "HR", TestData.SeedTimestamp);

        dbContext.Organizations.AddRange(organization, otherOrganization);
        dbContext.Departments.AddRange(department, otherDepartment);

        await dbContext.SaveChangesAsync();
    }
}
