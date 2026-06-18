using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Domain.Enums;
using FluentValidation;

namespace AuditAI.Application.AuditFindings.Validators;

public sealed class ChangeAuditFindingStatusRequestValidator : AbstractValidator<ChangeAuditFindingStatusRequest>
{
    public ChangeAuditFindingStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => status is AuditFindingStatus.InProgress or AuditFindingStatus.Resolved or AuditFindingStatus.Cancelled)
            .WithMessage("Status must be InProgress, Resolved, or Cancelled.");
    }
}
