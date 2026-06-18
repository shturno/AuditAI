using AuditAI.Application.Controls.Contracts;
using FluentValidation;

namespace AuditAI.Application.Controls.Validators;

public sealed class ControlQueryParametersValidator : AbstractValidator<ControlQueryParameters>
{
    public const int MaxPageSize = 100;

    public ControlQueryParametersValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200);
    }
}
