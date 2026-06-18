using AuditAI.Application.Common.Results;
using FluentValidation.Results;

namespace AuditAI.Application.Common.Validation;

internal static class ValidationExtensions
{
    public static IReadOnlyList<ValidationError> ToValidationErrors(this ValidationResult validationResult)
    {
        return validationResult.Errors
            .Select(error => new ValidationError(error.PropertyName, error.ErrorMessage))
            .ToArray();
    }
}
