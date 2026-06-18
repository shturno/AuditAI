using AuditAI.Application.Controls.Contracts;
using AuditAI.Application.Controls.Validators;
using AuditAI.Domain.Enums;

namespace AuditAI.UnitTests.Application.Controls;

public sealed class CreateControlRequestValidatorTests
{
    private readonly CreateControlRequestValidator _validator = new();

    [Fact]
    public async Task Should_RejectCreateControl_When_NameIsEmpty()
    {
        var request = new CreateControlRequest
        {
            OrganizationId = Guid.NewGuid(),
            Code = "CTRL-001",
            Category = "Access Management",
            Title = string.Empty,
            Description = "Detailed monthly access review.",
            Frequency = ControlFrequency.Monthly
        };

        var result = await _validator.ValidateAsync(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateControlRequest.Title));
    }

    [Fact]
    public async Task Should_AllowCreateControl_When_OrganizationIdIsEmpty()
    {
        var request = new CreateControlRequest
        {
            OrganizationId = Guid.Empty,
            Code = "CTRL-001",
            Category = "Access Management",
            Title = "User access review",
            Description = "Detailed monthly access review.",
            Frequency = ControlFrequency.Monthly
        };

        var result = await _validator.ValidateAsync(request);

        Assert.DoesNotContain(result.Errors, error => error.PropertyName == nameof(CreateControlRequest.OrganizationId));
    }

    [Fact]
    public async Task Should_RejectCreateControl_When_DescriptionExceedsLimit()
    {
        var request = new CreateControlRequest
        {
            OrganizationId = Guid.NewGuid(),
            Code = "CTRL-001",
            Category = "Access Management",
            Title = "User access review",
            Description = new string('a', 4001),
            Frequency = ControlFrequency.Monthly
        };

        var result = await _validator.ValidateAsync(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateControlRequest.Description));
    }
}
