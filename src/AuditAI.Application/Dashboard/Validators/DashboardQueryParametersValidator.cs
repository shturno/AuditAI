using AuditAI.Application.Dashboard.Contracts;
using FluentValidation;

namespace AuditAI.Application.Dashboard.Validators;

public sealed class DashboardQueryParametersValidator : AbstractValidator<DashboardQueryParameters>
{
    public const int MaxRecentLimit = 20;

    public DashboardQueryParametersValidator()
    {
        RuleFor(x => x.RecentLimit)
            .InclusiveBetween(1, MaxRecentLimit);
    }
}
