using AuditAI.Application.Evidence.Contracts;
using FluentValidation;

namespace AuditAI.Application.Evidence.Validators;

public sealed class EvidenceQueryParametersValidator : AbstractValidator<EvidenceQueryParameters>
{
    public const int MaxPageSize = 100;

    public EvidenceQueryParametersValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);
    }
}
