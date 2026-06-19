using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.ActionPlans.Interfaces;
using AuditAI.Application.AuditFindings.Interfaces;
using AuditAI.Application.Auth.Interfaces;
using AuditAI.Application.Controls.Interfaces;
using AuditAI.Application.Evidence.Interfaces;
using AuditAI.Application.Dashboard.Interfaces;
using AuditAI.Infrastructure.Auth.Jwt;
using AuditAI.Infrastructure.Auth.PasswordHashing;
using AuditAI.Infrastructure.Persistence.Lookups;
using AuditAI.Infrastructure.Persistence;
using AuditAI.Infrastructure.Persistence.Repositories;
using AuditAI.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuditAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Secret),
                "JWT secret is required.")
            .ValidateOnStart();

        services.AddDbContext<AuditAIDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserAuthRepository, UserAuthRepository>();
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IControlRepository, ControlRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IActionPlanRepository, ActionPlanRepository>();
        services.AddScoped<IAuditFindingRepository, AuditFindingRepository>();
        services.AddScoped<IAuditFindingLookup, ActionPlanReferenceLookup>();
        services.AddScoped<AuditAI.Application.ActionPlans.Interfaces.IUserLookup, ActionPlanReferenceLookup>();
        services.AddScoped<IOrganizationLookup, ControlReferenceLookup>();
        services.AddScoped<IDepartmentLookup, ControlReferenceLookup>();
        services.AddScoped<IEvidenceRepository, EvidenceRepository>();
        services.AddScoped<IDashboardSummaryRepository, DashboardSummaryRepository>();
        services.AddScoped<IControlLookup, EvidenceReferenceLookup>();
        services.AddScoped<AuditAI.Application.Evidence.Interfaces.IUserLookup, EvidenceReferenceLookup>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
