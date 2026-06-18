using AuditAI.Application.ActionPlans.Contracts;
using FluentValidation;

namespace AuditAI.Application.ActionPlans.Validators;

public sealed class UpdateActionPlanRequestValidator : AbstractValidator<UpdateActionPlanRequest>
{
    public UpdateActionPlanRequestValidator()
    {
        RuleFor(x => x.AssignedToUserId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.DueDate).NotEqual(default(DateTimeOffset));
    }
}
