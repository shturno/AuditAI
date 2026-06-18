using AuditAI.Application.Controls.Contracts;
using AuditAI.Application.Controls.Services;
using AuditAI.Application.Controls.Validators;
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

        services.AddScoped<IValidator<CreateControlRequest>, CreateControlRequestValidator>();
        services.AddScoped<IValidator<UpdateControlRequest>, UpdateControlRequestValidator>();
        services.AddScoped<IValidator<ControlQueryParameters>, ControlQueryParametersValidator>();

        return services;
    }
}
