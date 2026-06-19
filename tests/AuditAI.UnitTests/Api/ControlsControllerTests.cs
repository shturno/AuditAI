using AuditAI.Api.Controllers;
using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
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
        var organizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var departmentId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var result = await controller.Create(
            new CreateControlRequest
            {
                OrganizationId = organizationId,
                DepartmentId = departmentId,
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

    [Fact]
    public async Task Should_ReturnForbidden_When_CurrentUserRoleCannotCreateControl()
    {
        var controller = CreateController(UserRole.Reviewer);

        var result = await controller.Create(
            new CreateControlRequest
            {
                OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Code = "CTRL-001",
                Category = "Access Management",
                Title = "Quarterly access review",
                Description = "Detailed quarterly access review.",
                Frequency = ControlFrequency.Quarterly
            },
            CancellationToken.None);

        var forbidden = Assert.IsType<ActionResult<ControlResponse>>(result);
        var response = Assert.IsType<ObjectResult>(forbidden.Result);
        Assert.Equal(403, response.StatusCode);
    }

    private static ControlsController CreateController(UserRole role = UserRole.Auditor)
    {
        var repository = new FakeControlRepository();
        var dateTimeProvider = new FakeDateTimeProvider();
        var lookup = new FakeControlReferenceLookup();
        var currentUser = FakeCurrentUser.Authenticated(role);

        return new ControlsController(
            new CreateControlService(repository, currentUser, lookup, lookup, new FakeAuditLogWriter(), dateTimeProvider, new CreateControlRequestValidator()),
            new GetControlByIdService(repository, currentUser),
            new ListControlsService(repository, currentUser, new ControlQueryParametersValidator()),
            new UpdateControlService(repository, currentUser, lookup, lookup, new FakeAuditLogWriter(), dateTimeProvider, new UpdateControlRequestValidator()),
            new DeactivateControlService(repository, currentUser, new FakeAuditLogWriter(), dateTimeProvider));
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

    private sealed class FakeAuditLogWriter : IAuditLogWriter
    {
        public Task WriteAsync(AuditLogWriteEntry entry, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeControlReferenceLookup : IOrganizationLookup, IDepartmentLookup
    {
        public FakeControlReferenceLookup()
        {
            var organizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var departmentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            ExistingOrganizationIds.Add(organizationId);
            DepartmentsByOrganization[departmentId] = organizationId;
        }

        public HashSet<Guid> ExistingOrganizationIds { get; } = [];

        public Dictionary<Guid, Guid> DepartmentsByOrganization { get; } = [];

        public Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistingOrganizationIds.Contains(organizationId));
        }

        public Task<bool> DepartmentExistsAsync(Guid departmentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(DepartmentsByOrganization.ContainsKey(departmentId));
        }

        public Task<bool> DepartmentBelongsToOrganizationAsync(
            Guid departmentId,
            Guid organizationId,
            CancellationToken cancellationToken)
        {
            var belongs = DepartmentsByOrganization.TryGetValue(departmentId, out var departmentOrganizationId) &&
                          departmentOrganizationId == organizationId;

            return Task.FromResult(belongs);
        }
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        private FakeCurrentUser(Guid userId, Guid organizationId, UserRole role)
        {
            UserId = userId;
            OrganizationId = organizationId;
            Role = role;
        }

        public bool IsAuthenticated => true;

        public Guid? UserId { get; }

        public string? Email => "user@auditai.test";

        public UserRole? Role { get; }

        public Guid? OrganizationId { get; }

        public Guid? DepartmentId => null;

        public static FakeCurrentUser Authenticated(UserRole role = UserRole.Auditor)
        {
            return new FakeCurrentUser(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                role);
        }
    }
}
