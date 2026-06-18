using AuditAI.Application.Controls.Contracts;
using FluentValidation;

namespace AuditAI.Application.Controls.Validators;

public sealed class CreateControlRequestValidator : AbstractValidator<CreateControlRequest>
{
    public CreateControlRequestValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty();

        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Category)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(4000);
    }
}
