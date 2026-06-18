using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Controls.Contracts;
using AuditAI.Application.Controls.Interfaces;
using AuditAI.Application.Controls.Services;
using AuditAI.Application.Controls.Validators;
using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;

namespace AuditAI.UnitTests.Application.Controls;

public sealed class ControlServiceTests
{
    [Fact]
    public async Task Should_ReturnNotFound_When_ControlDoesNotExist()
    {
        var repository = new FakeControlRepository();
        var lookup = new FakeControlReferenceLookup();
        var service = new UpdateControlService(
            repository,
            lookup,
            lookup,
            new FakeDateTimeProvider(),
            new UpdateControlRequestValidator());

        var result = await service.ExecuteAsync(
            Guid.NewGuid(),
            new UpdateControlRequest
            {
                Code = "CTRL-001",
                Category = "Access Management",
                Title = "Quarterly access review",
                Description = "Detailed quarterly access review.",
                Frequency = ControlFrequency.Quarterly
            });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task Should_CreateControl_When_RequestIsValid()
    {
        var organizationId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var repository = new FakeControlRepository();
        var lookup = new FakeControlReferenceLookup
        {
            ExistingOrganizationIds = { organizationId },
            DepartmentsByOrganization = { [departmentId] = organizationId }
        };
        var service = new CreateControlService(
            repository,
            lookup,
            lookup,
            new FakeDateTimeProvider(),
            new CreateControlRequestValidator());

        var result = await service.ExecuteAsync(
            new CreateControlRequest
            {
                OrganizationId = organizationId,
                DepartmentId = departmentId,
                Code = "CTRL-001",
                Category = "Access Management",
                Title = "Quarterly access review",
                Description = "Detailed quarterly access review.",
                Frequency = ControlFrequency.Quarterly
            });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(repository.StoredControls);
        Assert.Equal("Access Management", repository.StoredControls[0].Category);
    }

    [Fact]
    public async Task Should_NotBypassDomainInvariants_When_RequestIsInvalid()
    {
        var organizationId = Guid.NewGuid();
        var repository = new FakeControlRepository();
        var lookup = new FakeControlReferenceLookup
        {
            ExistingOrganizationIds = { organizationId }
        };
        var service = new CreateControlService(
            repository,
            lookup,
            lookup,
            new FakeDateTimeProvider(),
            new CreateControlRequestValidator());

        var result = await service.ExecuteAsync(
            new CreateControlRequest
            {
                OrganizationId = organizationId,
                Code = "CTRL-001",
                Category = "Access Management",
                Title = "Valid title",
                Description = string.Empty,
                Frequency = ControlFrequency.Monthly
            });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsValidationFailure);
        Assert.Empty(repository.StoredControls);
    }

    [Fact]
    public async Task Should_FailCreateControl_When_OrganizationDoesNotExist()
    {
        var repository = new FakeControlRepository();
        var lookup = new FakeControlReferenceLookup();
        var service = new CreateControlService(
            repository,
            lookup,
            lookup,
            new FakeDateTimeProvider(),
            new CreateControlRequestValidator());

        var result = await service.ExecuteAsync(
            new CreateControlRequest
            {
                OrganizationId = Guid.NewGuid(),
                Code = "CTRL-001",
                Category = "Access Management",
                Title = "Quarterly access review",
                Description = "Detailed quarterly access review.",
                Frequency = ControlFrequency.Quarterly
            });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsValidationFailure);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "OrganizationId");
    }

    [Fact]
    public async Task Should_FailCreateControl_When_DepartmentDoesNotExist()
    {
        var organizationId = Guid.NewGuid();
        var repository = new FakeControlRepository();
        var lookup = new FakeControlReferenceLookup
        {
            ExistingOrganizationIds = { organizationId }
        };
        var service = new CreateControlService(
            repository,
            lookup,
            lookup,
            new FakeDateTimeProvider(),
            new CreateControlRequestValidator());

        var result = await service.ExecuteAsync(
            new CreateControlRequest
            {
                OrganizationId = organizationId,
                DepartmentId = Guid.NewGuid(),
                Code = "CTRL-001",
                Category = "Access Management",
                Title = "Quarterly access review",
                Description = "Detailed quarterly access review.",
                Frequency = ControlFrequency.Quarterly
            });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsValidationFailure);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "DepartmentId");
    }

    [Fact]
    public async Task Should_FailCreateControl_When_DepartmentDoesNotBelongToOrganization()
    {
        var organizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var repository = new FakeControlRepository();
        var lookup = new FakeControlReferenceLookup
        {
            ExistingOrganizationIds = { organizationId, otherOrganizationId },
            DepartmentsByOrganization = { [departmentId] = otherOrganizationId }
        };
        var service = new CreateControlService(
            repository,
            lookup,
            lookup,
            new FakeDateTimeProvider(),
            new CreateControlRequestValidator());

        var result = await service.ExecuteAsync(
            new CreateControlRequest
            {
                OrganizationId = organizationId,
                DepartmentId = departmentId,
                Code = "CTRL-001",
                Category = "Access Management",
                Title = "Quarterly access review",
                Description = "Detailed quarterly access review.",
                Frequency = ControlFrequency.Quarterly
            });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsValidationFailure);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "DepartmentId");
    }

    [Fact]
    public async Task Should_FailUpdateControl_When_DepartmentDoesNotBelongToOrganization()
    {
        var organizationId = Guid.NewGuid();
        var existingDepartmentId = Guid.NewGuid();
        var invalidDepartmentId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();
        var clock = new FakeDateTimeProvider();
        var repository = new FakeControlRepository();
        var control = Control.Create(
            Guid.NewGuid(),
            organizationId,
            existingDepartmentId,
            "CTRL-001",
            "Access Management",
            "Quarterly access review",
            "Detailed quarterly access review.",
            ControlFrequency.Quarterly,
            clock.UtcNow);
        repository.StoredControls.Add(control);

        var lookup = new FakeControlReferenceLookup
        {
            ExistingOrganizationIds = { organizationId, otherOrganizationId },
            DepartmentsByOrganization =
            {
                [existingDepartmentId] = organizationId,
                [invalidDepartmentId] = otherOrganizationId
            }
        };
        var service = new UpdateControlService(
            repository,
            lookup,
            lookup,
            clock,
            new UpdateControlRequestValidator());

        var result = await service.ExecuteAsync(
            control.Id,
            new UpdateControlRequest
            {
                DepartmentId = invalidDepartmentId,
                Code = "CTRL-001",
                Category = "Access Management",
                Title = "Updated review",
                Description = "Updated quarterly access review.",
                Frequency = ControlFrequency.Quarterly
            });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsValidationFailure);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "DepartmentId");
    }

    [Fact]
    public async Task Should_UpdateControl_When_OrganizationAndDepartmentAreValid()
    {
        var organizationId = Guid.NewGuid();
        var existingDepartmentId = Guid.NewGuid();
        var newDepartmentId = Guid.NewGuid();
        var clock = new FakeDateTimeProvider();
        var repository = new FakeControlRepository();
        var control = Control.Create(
            Guid.NewGuid(),
            organizationId,
            existingDepartmentId,
            "CTRL-001",
            "Access Management",
            "Quarterly access review",
            "Detailed quarterly access review.",
            ControlFrequency.Quarterly,
            clock.UtcNow);
        repository.StoredControls.Add(control);

        var lookup = new FakeControlReferenceLookup
        {
            ExistingOrganizationIds = { organizationId },
            DepartmentsByOrganization =
            {
                [existingDepartmentId] = organizationId,
                [newDepartmentId] = organizationId
            }
        };
        var service = new UpdateControlService(
            repository,
            lookup,
            lookup,
            clock,
            new UpdateControlRequestValidator());

        var result = await service.ExecuteAsync(
            control.Id,
            new UpdateControlRequest
            {
                DepartmentId = newDepartmentId,
                Code = "CTRL-001",
                Category = "Access Management",
                Title = "Updated review",
                Description = "Updated quarterly access review.",
                Frequency = ControlFrequency.Quarterly
            });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(newDepartmentId, result.Value.DepartmentId);
        Assert.Equal("Updated review", result.Value.Title);
    }

    private sealed class FakeControlRepository : IControlRepository
    {
        public List<Control> StoredControls { get; } = [];

        public Task AddAsync(Control control, CancellationToken cancellationToken)
        {
            StoredControls.Add(control);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsWithCodeAsync(
            Guid organizationId,
            string code,
            Guid? excludedControlId,
            CancellationToken cancellationToken)
        {
            var exists = StoredControls.Any(control =>
                control.OrganizationId == organizationId &&
                control.Code == code &&
                (!excludedControlId.HasValue || control.Id != excludedControlId.Value));

            return Task.FromResult(exists);
        }

        public Task<Control?> GetByIdAsync(Guid controlId, CancellationToken cancellationToken)
        {
            return Task.FromResult(StoredControls.SingleOrDefault(control => control.Id == controlId));
        }

        public Task<Control?> GetByIdForUpdateAsync(Guid controlId, CancellationToken cancellationToken)
        {
            return Task.FromResult(StoredControls.SingleOrDefault(control => control.Id == controlId));
        }

        public Task<PagedResult<Control>> ListAsync(ControlQueryParameters queryParameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<Control>(StoredControls, StoredControls.Count, 1, 20));
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

    private sealed class FakeControlReferenceLookup : IOrganizationLookup, IDepartmentLookup
    {
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
}
