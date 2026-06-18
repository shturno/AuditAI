using AuditAI.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace AuditAI.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult FromFailure(Result result)
    {
        if (result.IsValidationFailure)
        {
            var errors = result.ValidationErrors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).ToArray());

            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        if (result.IsNotFound)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Resource not found.",
                Detail = result.Error?.Message,
                Status = StatusCodes.Status404NotFound
            });
        }

        if (result.IsUnauthorized)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized.",
                Detail = result.Error?.Message,
                Status = StatusCodes.Status401Unauthorized
            });
        }

        return BadRequest(new ProblemDetails
        {
            Title = "Request could not be processed.",
            Detail = result.Error?.Message,
            Status = StatusCodes.Status400BadRequest
        });
    }
}
