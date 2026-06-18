using AuditAI.Application.Controls.Contracts;
using AuditAI.Application.Controls.Services;
using AuditAI.Application.Controls.Validators;
using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.AuditFindings.Services;
using AuditAI.Application.AuditFindings.Validators;
using AuditAI.Application.Evidence.Contracts;
using AuditAI.Application.Evidence.Services;
using AuditAI.Application.Evidence.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AuditAI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateControlService>();
        services.AddScoped<GetControlByIdService>();
        services.AddScoped<ListControlsService>();
        services.AddScoped<UpdateControlService>();
        services.AddScoped<DeactivateControlService>();
        services.AddScoped<CreateAuditFindingService>();
        services.AddScoped<GetAuditFindingByIdService>();
        services.AddScoped<ListAuditFindingsService>();
        services.AddScoped<UpdateAuditFindingService>();
        services.AddScoped<ChangeAuditFindingStatusService>();
        services.AddScoped<CreateEvidenceService>();
        services.AddScoped<GetEvidenceByIdService>();
        services.AddScoped<ListEvidenceService>();
        services.AddScoped<AcceptEvidenceService>();
        services.AddScoped<RejectEvidenceService>();

        services.AddScoped<IValidator<CreateControlRequest>, CreateControlRequestValidator>();
        services.AddScoped<IValidator<UpdateControlRequest>, UpdateControlRequestValidator>();
        services.AddScoped<IValidator<ControlQueryParameters>, ControlQueryParametersValidator>();
        services.AddScoped<IValidator<CreateAuditFindingRequest>, CreateAuditFindingRequestValidator>();
        services.AddScoped<IValidator<UpdateAuditFindingRequest>, UpdateAuditFindingRequestValidator>();
        services.AddScoped<IValidator<ChangeAuditFindingStatusRequest>, ChangeAuditFindingStatusRequestValidator>();
        services.AddScoped<IValidator<AuditFindingQueryParameters>, AuditFindingQueryParametersValidator>();
        services.AddScoped<IValidator<CreateEvidenceRequest>, CreateEvidenceRequestValidator>();
        services.AddScoped<IValidator<ReviewEvidenceRequest>, ReviewEvidenceRequestValidator>();
        services.AddScoped<IValidator<EvidenceQueryParameters>, EvidenceQueryParametersValidator>();

        return services;
    }
}
