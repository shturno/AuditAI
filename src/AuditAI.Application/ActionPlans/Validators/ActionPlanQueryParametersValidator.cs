using AuditAI.Application.ActionPlans.Contracts;
using FluentValidation;

namespace AuditAI.Application.ActionPlans.Validators;

public sealed class ActionPlanQueryParametersValidator : AbstractValidator<ActionPlanQueryParameters>
{
    public const int MaxPageSize = 100;

    public ActionPlanQueryParametersValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
    }
}
