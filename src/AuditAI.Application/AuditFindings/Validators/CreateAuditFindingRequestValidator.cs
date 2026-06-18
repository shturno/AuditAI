using AuditAI.Application.AuditFindings.Contracts;
using FluentValidation;

namespace AuditAI.Application.AuditFindings.Validators;

public sealed class CreateAuditFindingRequestValidator : AbstractValidator<CreateAuditFindingRequest>
{
    public CreateAuditFindingRequestValidator()
    {
        RuleFor(x => x.ControlId).NotEmpty();
        RuleFor(x => x.CreatedByUserId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MinimumLength(10).MaximumLength(4000);
    }
}
