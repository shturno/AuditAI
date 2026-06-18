using AuditAI.Application.AuditFindings.Contracts;
using FluentValidation;

namespace AuditAI.Application.AuditFindings.Validators;

public sealed class UpdateAuditFindingRequestValidator : AbstractValidator<UpdateAuditFindingRequest>
{
    public UpdateAuditFindingRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MinimumLength(10).MaximumLength(4000);
    }
}
