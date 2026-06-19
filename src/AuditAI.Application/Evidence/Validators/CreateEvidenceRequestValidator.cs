using AuditAI.Application.Evidence.Contracts;
using FluentValidation;

namespace AuditAI.Application.Evidence.Validators;

public sealed class CreateEvidenceRequestValidator : AbstractValidator<CreateEvidenceRequest>
{
    public CreateEvidenceRequestValidator()
    {
        RuleFor(x => x.ControlId)
            .NotEmpty();

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.StorageReference)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
