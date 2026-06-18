using AuditAI.Application.Controls.Contracts;
using AuditAI.Application.Controls.Validators;

namespace AuditAI.UnitTests.Application.Controls;

public sealed class ControlQueryParametersValidatorTests
{
    private readonly ControlQueryParametersValidator _validator = new();

    [Fact]
    public async Task Should_RejectListControlsQuery_When_PaginationIsInvalid()
    {
        var query = new ControlQueryParameters
        {
            PageNumber = 0,
            PageSize = 101
        };

        var result = await _validator.ValidateAsync(query);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ControlQueryParameters.PageNumber));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ControlQueryParameters.PageSize));
    }
}
