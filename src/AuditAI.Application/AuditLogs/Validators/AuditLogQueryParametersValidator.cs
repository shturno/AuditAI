using AuditAI.Application.AuditLogs.Contracts;
using FluentValidation;

namespace AuditAI.Application.AuditLogs.Validators;

public sealed class AuditLogQueryParametersValidator : AbstractValidator<AuditLogQueryParameters>
{
    public const int MaxPageSize = 100;

    public AuditLogQueryParametersValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(x => x)
            .Must(query => !query.From.HasValue || !query.To.HasValue || query.From.Value <= query.To.Value)
            .WithMessage("From must be earlier than or equal to To.");
    }
}
