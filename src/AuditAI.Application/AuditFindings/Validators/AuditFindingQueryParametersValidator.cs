using AuditAI.Application.AuditFindings.Contracts;
using FluentValidation;

namespace AuditAI.Application.AuditFindings.Validators;

public sealed class AuditFindingQueryParametersValidator : AbstractValidator<AuditFindingQueryParameters>
{
    public const int MaxPageSize = 100;

    public AuditFindingQueryParametersValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
    }
}
