using AuditAI.Application.ActionPlans.Contracts;
using AuditAI.Domain.Enums;
using FluentValidation;

namespace AuditAI.Application.ActionPlans.Validators;

public sealed class ChangeActionPlanStatusRequestValidator : AbstractValidator<ChangeActionPlanStatusRequest>
{
    public ChangeActionPlanStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => status is ActionPlanStatus.InProgress or ActionPlanStatus.Completed or ActionPlanStatus.Overdue or ActionPlanStatus.Cancelled)
            .WithMessage("Status must be InProgress, Completed, Overdue, or Cancelled.");
    }
}
