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

namespace AuditAI.UnitTests.Application.Controls;

public sealed class ControlServiceTests
{
    [Fact]
    public async Task Should_FailCreateControl_When_CurrentUserIsNotAuthenticated()
    {
        var repository = new FakeControlRepository();
        var lookup = new FakeControlReferenceLookup();
        var service = new CreateControlService(
            repository,
            FakeCurrentUser.Unauthenticated(),
            lookup,
            lookup,
            new FakeAuditLogWriter(),
            new FakeDateTimeProvider(),
            new CreateControlRequestValidator());

        var result = await service.ExecuteAsync(new CreateControlRequest
        {
            Code = "CTRL-001",
            Category = "Access Management",
            Title = "Quarterly access review",
            Description = "Detailed quarterly access review.",
            Frequency = ControlFrequency.Quarterly
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsUnauthorized);
    }

    [Fact]
    public async Task Should_RejectCreateControl_When_CurrentUserIsReviewer()
    {
        var repository = new FakeControlRepository();
        var organizationId = Guid.NewGuid();
        var lookup = new FakeControlReferenceLookup
        {
            ExistingOrganizationIds = { organizationId }
        };
        var service = new CreateControlService(
            repository,
            FakeCurrentUser.Authenticated(organizationId, UserRole.Reviewer),
            lookup,
            lookup,
            new FakeAuditLogWriter(),
            new FakeDateTimeProvider(),
            new CreateControlRequestValidator());

        var result = await service.ExecuteAsync(new CreateControlRequest
        {
            Code = "CTRL-001",
            Category = "Access Management",
            Title = "Quarterly access review",
            Description = "Detailed quarterly access review.",
            Frequency = ControlFrequency.Quarterly
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsForbidden);
    }

    [Fact]
    public async Task Should_CreateControl_Using_CurrentUserOrganization_InsteadOf_RequestOrganization()
    {
        var authenticatedOrganizationId = Guid.NewGuid();
        var requestOrganizationId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var repository = new FakeControlRepository();
        var lookup = new FakeControlReferenceLookup
        {
            ExistingOrganizationIds = { authenticatedOrganizationId },
            DepartmentsByOrganization = { [departmentId] = authenticatedOrganizationId }
        };
        var auditLogWriter = new FakeAuditLogWriter();
        var currentUser = FakeCurrentUser.Authenticated(authenticatedOrganizationId);
        var service = new CreateControlService(
            repository,
            currentUser,
            lookup,
            lookup,
            auditLogWriter,
            new FakeDateTimeProvider(),
            new CreateControlRequestValidator());

        var result = await service.ExecuteAsync(new CreateControlRequest
        {
            OrganizationId = requestOrganizationId,
            DepartmentId = departmentId,
            Code = "CTRL-001",
            Category = "Access Management",
            Title = "Quarterly access review",
            Description = "Detailed quarterly access review.",
            Frequency = ControlFrequency.Quarterly
        });

        Assert.True(result.IsSuccess);
        Assert.Single(repository.StoredControls);
        Assert.Equal(authenticatedOrganizationId, repository.StoredControls[0].OrganizationId);
        Assert.Single(auditLogWriter.Entries);
        Assert.Equal(currentUser.UserId, auditLogWriter.Entries[0].UserId);
        Assert.Equal(authenticatedOrganizationId, auditLogWriter.Entries[0].OrganizationId);
    }

    [Fact]
    public async Task Should_FailCreateControl_When_DepartmentDoesNotBelongToCurrentUserOrganization()
    {
        var authenticatedOrganizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var repository = new FakeControlRepository();
        var lookup = new FakeControlReferenceLookup
        {
            ExistingOrganizationIds = { authenticatedOrganizationId, otherOrganizationId },
            DepartmentsByOrganization = { [departmentId] = otherOrganizationId }
        };
        var service = new CreateControlService(
            repository,
            FakeCurrentUser.Authenticated(authenticatedOrganizationId),
            lookup,
            lookup,
            new FakeAuditLogWriter(),
            new FakeDateTimeProvider(),
            new CreateControlRequestValidator());

        var result = await service.ExecuteAsync(new CreateControlRequest
        {
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
    public async Task Should_ListControls_Using_CurrentUserOrganization()
    {
        var organizationId = Guid.NewGuid();
        var repository = new FakeControlRepository();
        repository.StoredControls.Add(Control.Create(
            Guid.NewGuid(),
            organizationId,
            null,
            "CTRL-001",
            "Access Management",
            "Quarterly access review",
            "Detailed quarterly access review.",
            ControlFrequency.Quarterly,
            new FakeDateTimeProvider().UtcNow));
        var service = new ListControlsService(
            repository,
            FakeCurrentUser.Authenticated(organizationId),
            new ControlQueryParametersValidator());

        var result = await service.ExecuteAsync(new ControlQueryParameters
        {
            OrganizationId = Guid.NewGuid(),
            PageNumber = 1,
            PageSize = 10
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.LastListQuery);
        Assert.Equal(organizationId, repository.LastListQuery!.OrganizationId);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_GettingControlFromAnotherOrganization()
    {
        var repository = new FakeControlRepository();
        var control = Control.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "CTRL-001",
            "Access Management",
            "Quarterly access review",
            "Detailed quarterly access review.",
            ControlFrequency.Quarterly,
            new FakeDateTimeProvider().UtcNow);
        repository.StoredControls.Add(control);
        var service = new GetControlByIdService(
            repository,
            FakeCurrentUser.Authenticated(Guid.NewGuid()));

        var result = await service.ExecuteAsync(control.Id);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task Should_UseAuthenticatedActor_When_UpdatingControl()
    {
        var organizationId = Guid.NewGuid();
        var existingDepartmentId = Guid.NewGuid();
        var newDepartmentId = Guid.NewGuid();
        var clock = new FakeDateTimeProvider();
        var repository = new FakeControlRepository();
        var auditLogWriter = new FakeAuditLogWriter();
        var currentUser = FakeCurrentUser.Authenticated(organizationId);
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
            currentUser,
            lookup,
            lookup,
            auditLogWriter,
            clock,
            new UpdateControlRequestValidator());

        var result = await service.ExecuteAsync(control.Id, new UpdateControlRequest
        {
            DepartmentId = newDepartmentId,
            Code = "CTRL-001",
            Category = "Access Management",
            Title = "Updated review",
            Description = "Updated quarterly access review.",
            Frequency = ControlFrequency.Quarterly
        });

        Assert.True(result.IsSuccess);
        Assert.Single(auditLogWriter.Entries);
        Assert.Equal(currentUser.UserId, auditLogWriter.Entries[0].UserId);
    }

    [Fact]
    public async Task Should_UseAuthenticatedActor_When_DeactivatingControl()
    {
        var organizationId = Guid.NewGuid();
        var clock = new FakeDateTimeProvider();
        var repository = new FakeControlRepository();
        var auditLogWriter = new FakeAuditLogWriter();
        var currentUser = FakeCurrentUser.Authenticated(organizationId);
        var control = Control.Create(
            Guid.NewGuid(),
            organizationId,
            null,
            "CTRL-001",
            "Access Management",
            "Quarterly access review",
            "Detailed quarterly access review.",
            ControlFrequency.Quarterly,
            clock.UtcNow);
        repository.StoredControls.Add(control);

        var service = new DeactivateControlService(
            repository,
            currentUser,
            auditLogWriter,
            clock);

        var result = await service.ExecuteAsync(control.Id);

        Assert.True(result.IsSuccess);
        Assert.Single(auditLogWriter.Entries);
        Assert.Equal(currentUser.UserId, auditLogWriter.Entries[0].UserId);
    }

    private sealed class FakeControlRepository : IControlRepository
    {
        public List<Control> StoredControls { get; } = [];

        public ControlQueryParameters? LastListQuery { get; private set; }

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
            LastListQuery = queryParameters;

            var items = StoredControls
                .Where(control => !queryParameters.OrganizationId.HasValue || control.OrganizationId == queryParameters.OrganizationId.Value)
                .ToArray();

            return Task.FromResult(new PagedResult<Control>(items, items.Length, queryParameters.PageNumber, queryParameters.PageSize));
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
        public List<AuditLogWriteEntry> Entries { get; } = [];

        public Task WriteAsync(AuditLogWriteEntry entry, CancellationToken cancellationToken)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
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

    private sealed class FakeCurrentUser : ICurrentUser
    {
        private FakeCurrentUser(
            bool isAuthenticated,
            Guid? userId,
            Guid? organizationId,
            UserRole? role)
        {
            IsAuthenticated = isAuthenticated;
            UserId = userId;
            OrganizationId = organizationId;
            Role = role;
        }

        public bool IsAuthenticated { get; }

        public Guid? UserId { get; }

        public string? Email => "user@auditai.test";

        public UserRole? Role { get; }

        public Guid? OrganizationId { get; }

        public Guid? DepartmentId => null;

        public static FakeCurrentUser Authenticated(Guid organizationId, UserRole role = UserRole.Auditor)
        {
            return new FakeCurrentUser(true, Guid.NewGuid(), organizationId, role);
        }

        public static FakeCurrentUser Unauthenticated()
        {
            return new FakeCurrentUser(false, null, null, null);
        }
    }
}
