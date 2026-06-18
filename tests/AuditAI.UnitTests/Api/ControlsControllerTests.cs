using AuditAI.Api.Controllers;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Controls.Contracts;
using AuditAI.Application.Controls.Interfaces;
using AuditAI.Application.Controls.Services;
using AuditAI.Application.Controls.Validators;
using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AuditAI.UnitTests.Api;

public sealed class ControlsControllerTests
{
    [Fact]
    public async Task Should_ReturnBadRequest_When_CreateRequestIsInvalid()
    {
        var controller = CreateController();

        var result = await controller.Create(
            new CreateControlRequest
            {
                OrganizationId = Guid.Empty,
                Code = string.Empty,
                Category = string.Empty,
                Title = string.Empty,
                Description = string.Empty,
                Frequency = ControlFrequency.Monthly
            },
            CancellationToken.None);

        var badRequest = Assert.IsType<ActionResult<ControlResponse>>(result);
        var response = Assert.IsType<BadRequestObjectResult>(badRequest.Result);
        Assert.Equal(400, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_ControlDoesNotExist()
    {
        var controller = CreateController();

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        var notFound = Assert.IsType<ActionResult<ControlResponse>>(result);
        Assert.IsType<NotFoundObjectResult>(notFound.Result);
    }

    [Fact]
    public async Task Should_ReturnCreated_When_CreateRequestIsValid()
    {
        var controller = CreateController();

        var result = await controller.Create(
            new CreateControlRequest
            {
                OrganizationId = Guid.NewGuid(),
                DepartmentId = Guid.NewGuid(),
                Code = "CTRL-001",
                Category = "Access Management",
                Title = "Quarterly access review",
                Description = "Detailed quarterly access review.",
                Frequency = ControlFrequency.Quarterly
            },
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var payload = Assert.IsType<ControlResponse>(created.Value);

        Assert.Equal(nameof(ControlsController.GetById), created.ActionName);
        Assert.Equal("Access Management", payload.Category);
    }

    private static ControlsController CreateController()
    {
        var repository = new FakeControlRepository();
        var dateTimeProvider = new FakeDateTimeProvider();

        return new ControlsController(
            new CreateControlService(repository, dateTimeProvider, new CreateControlRequestValidator()),
            new GetControlByIdService(repository),
            new ListControlsService(repository, new ControlQueryParametersValidator()),
            new UpdateControlService(repository, dateTimeProvider, new UpdateControlRequestValidator()),
            new DeactivateControlService(repository, dateTimeProvider));
    }

    private sealed class FakeControlRepository : IControlRepository
    {
        private readonly List<Control> _controls = [];

        public Task AddAsync(Control control, CancellationToken cancellationToken)
        {
            _controls.Add(control);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsWithCodeAsync(
            Guid organizationId,
            string code,
            Guid? excludedControlId,
            CancellationToken cancellationToken)
        {
            var exists = _controls.Any(control =>
                control.OrganizationId == organizationId &&
                control.Code == code &&
                (!excludedControlId.HasValue || control.Id != excludedControlId.Value));

            return Task.FromResult(exists);
        }

        public Task<Control?> GetByIdAsync(Guid controlId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_controls.SingleOrDefault(control => control.Id == controlId));
        }

        public Task<Control?> GetByIdForUpdateAsync(Guid controlId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_controls.SingleOrDefault(control => control.Id == controlId));
        }

        public Task<PagedResult<Control>> ListAsync(ControlQueryParameters queryParameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<Control>(_controls, _controls.Count, 1, 20));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 06, 18, 15, 0, 0, TimeSpan.Zero);
    }
}
