using AuditAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AuditAI.IntegrationTests.Infrastructure;

internal sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public CustomWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", _connectionString),
                new KeyValuePair<string, string?>("Jwt:Issuer", "AuditAI.Tests"),
                new KeyValuePair<string, string?>("Jwt:Audience", "AuditAI.Tests.Users"),
                new KeyValuePair<string, string?>("Jwt:Secret", "integration-tests-secret-key-with-32chars"),
                new KeyValuePair<string, string?>("Jwt:ExpirationMinutes", "60")
            ]);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AuditAIDbContext>>();

            services.AddDbContext<AuditAIDbContext>(options =>
                options.UseNpgsql(_connectionString));
        });
    }
}
