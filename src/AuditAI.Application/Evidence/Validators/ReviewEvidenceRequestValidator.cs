using AuditAI.Application.Evidence.Contracts;
using FluentValidation;

namespace AuditAI.Application.Evidence.Validators;

public sealed class ReviewEvidenceRequestValidator : AbstractValidator<ReviewEvidenceRequest>
{
    public ReviewEvidenceRequestValidator()
    {
        RuleFor(x => x.RejectionReason)
            .MaximumLength(2000);
    }
}
