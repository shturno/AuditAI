using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.AuditFindings.Interfaces;
using AuditAI.Application.AuditFindings.Services;
using AuditAI.Application.AuditFindings.Validators;
using AuditAI.Application.ActionPlans.Interfaces;
using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Evidence.Interfaces;
using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;

namespace AuditAI.UnitTests.Application.AuditFindings;

public sealed class AuditFindingServiceTests
{
    [Fact]
    public async Task Should_FailCreateAuditFinding_When_CurrentUserIsMissing()
    {
        var repository = new FakeAuditFindingRepository();
        var service = new CreateAuditFindingService(
            repository,
            new FakeControlLookup(),
            new FakeAuditLogWriter(),
            new FakeCurrentUser(),
            new FakeDateTimeProvider(),
            new CreateAuditFindingRequestValidator());

        var result = await service.ExecuteAsync(new CreateAuditFindingRequest
        {
            ControlId = Guid.NewGuid(),
            Title = "Missing approval",
            Description = "This control has no approval evidence.",
            Severity = AuditFindingSeverity.High
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsUnauthorized);
    }

    [Fact]
    public async Task Should_CreateAuditFinding_When_CurrentUserAndControlAreValid()
    {
        var repository = new FakeAuditFindingRepository();
        var controlId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var currentUser = new FakeCurrentUser
        {
            IsAuthenticated = true,
            UserId = userId,
            OrganizationId = organizationId,
            Role = UserRole.Auditor
        };
        var controlLookup = new FakeControlLookup { ControlOrganizations = { [controlId] = organizationId } };
        var auditLogWriter = new FakeAuditLogWriter();
        var service = new CreateAuditFindingService(
            repository,
            controlLookup,
            auditLogWriter,
            currentUser,
            new FakeDateTimeProvider(),
            new CreateAuditFindingRequestValidator());

        var result = await service.ExecuteAsync(new CreateAuditFindingRequest
        {
            ControlId = controlId,
            Title = "Missing approval",
            Description = "This control has no approval evidence.",
            Severity = AuditFindingSeverity.High
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value!.CreatedByUserId);
        Assert.Equal(userId, auditLogWriter.LastEntry!.UserId);
        Assert.Equal(organizationId, auditLogWriter.LastEntry.OrganizationId);
    }

    [Fact]
    public async Task Should_RejectCreateAuditFinding_When_CurrentUserIsReviewer()
    {
        var repository = new FakeAuditFindingRepository();
        var controlId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var service = new CreateAuditFindingService(
            repository,
            new FakeControlLookup { ControlOrganizations = { [controlId] = organizationId } },
            new FakeAuditLogWriter(),
            new FakeCurrentUser
            {
                IsAuthenticated = true,
                UserId = Guid.NewGuid(),
                OrganizationId = organizationId,
                Role = UserRole.Reviewer
            },
            new FakeDateTimeProvider(),
            new CreateAuditFindingRequestValidator());

        var result = await service.ExecuteAsync(new CreateAuditFindingRequest
        {
            ControlId = controlId,
            Title = "Missing approval",
            Description = "This control has no approval evidence.",
            Severity = AuditFindingSeverity.High
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsForbidden);
    }

    [Fact]
    public async Task Should_FailCreateAuditFinding_When_ControlBelongsToAnotherOrganization()
    {
        var repository = new FakeAuditFindingRepository();
        var currentUser = new FakeCurrentUser
        {
            IsAuthenticated = true,
            UserId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Role = UserRole.Auditor
        };
        var controlLookup = new FakeControlLookup
        {
            ControlOrganizations =
            {
                [Guid.NewGuid()] = Guid.NewGuid()
            }
        };
        var service = new CreateAuditFindingService(
            repository,
            controlLookup,
            new FakeAuditLogWriter(),
            currentUser,
            new FakeDateTimeProvider(),
            new CreateAuditFindingRequestValidator());

        var controlId = controlLookup.ControlOrganizations.Keys.First();
        var result = await service.ExecuteAsync(new CreateAuditFindingRequest
        {
            ControlId = controlId,
            Title = "Missing approval",
            Description = "This control has no approval evidence.",
            Severity = AuditFindingSeverity.High
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task Should_AllowAuditFindingList_When_CurrentUserIsAuthenticated()
    {
        var repository = new FakeAuditFindingRepository();
        var organizationId = Guid.NewGuid();
        repository.StoredFindings.Add((
            AuditFinding.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Finding", "Description text", AuditFindingSeverity.High, new FakeDateTimeProvider().UtcNow),
            organizationId));

        var service = new ListAuditFindingsService(
            repository,
            new FakeCurrentUser
            {
                IsAuthenticated = true,
                UserId = Guid.NewGuid(),
                OrganizationId = organizationId,
                Role = UserRole.Auditor
            },
            new AuditFindingQueryParametersValidator());

        var result = await service.ExecuteAsync(new AuditFindingQueryParameters { PageNumber = 1, PageSize = 10 });

        Assert.True(result.IsSuccess);
        Assert.Equal(organizationId, repository.LastListOrganizationId);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_FindingBelongsToAnotherOrganization()
    {
        var repository = new FakeAuditFindingRepository();
        var organizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();
        var finding = AuditFinding.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Finding",
            "Description text",
            AuditFindingSeverity.High,
            new FakeDateTimeProvider().UtcNow);
        repository.StoredFindings.Add((finding, otherOrganizationId));

        var service = new GetAuditFindingByIdService(
            repository,
            new FakeCurrentUser
            {
                IsAuthenticated = true,
                UserId = Guid.NewGuid(),
                OrganizationId = organizationId,
                Role = UserRole.Auditor
            });

        var result = await service.ExecuteAsync(finding.Id);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task Should_UseCurrentUserIdInAuditLog_When_UpdatingFinding()
    {
        var clock = new FakeDateTimeProvider();
        var repository = new FakeAuditFindingRepository();
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var finding = AuditFinding.Create(Guid.NewGuid(), Guid.NewGuid(), userId, "Finding", "Description text", AuditFindingSeverity.High, clock.UtcNow);
        repository.StoredFindings.Add((finding, organizationId));

        var auditLogWriter = new FakeAuditLogWriter();
        var service = new UpdateAuditFindingService(
            repository,
            auditLogWriter,
            new FakeCurrentUser { IsAuthenticated = true, UserId = userId, OrganizationId = organizationId, Role = UserRole.Auditor },
            clock,
            new UpdateAuditFindingRequestValidator());

        var result = await service.ExecuteAsync(finding.Id, new UpdateAuditFindingRequest
        {
            Title = "Updated title",
            Description = "Updated description",
            Severity = AuditFindingSeverity.Medium
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, auditLogWriter.LastEntry!.UserId);
    }

    [Fact]
    public async Task Should_UseCurrentUserIdInAuditLog_When_ChangingFindingStatus()
    {
        var clock = new FakeDateTimeProvider();
        var repository = new FakeAuditFindingRepository();
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var finding = AuditFinding.Create(Guid.NewGuid(), Guid.NewGuid(), userId, "Finding", "Description text", AuditFindingSeverity.High, clock.UtcNow);
        finding.MarkInProgress(clock.UtcNow.AddMinutes(1));
        repository.StoredFindings.Add((finding, organizationId));

        var auditLogWriter = new FakeAuditLogWriter();
        var service = new ChangeAuditFindingStatusService(
            repository,
            new FakeActionPlanRepository(),
            auditLogWriter,
            new FakeCurrentUser { IsAuthenticated = true, UserId = userId, OrganizationId = organizationId, Role = UserRole.Auditor },
            clock,
            new ChangeAuditFindingStatusRequestValidator());

        var result = await service.ExecuteAsync(finding.Id, new ChangeAuditFindingStatusRequest
        {
            Status = AuditFindingStatus.Cancelled
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, auditLogWriter.LastEntry!.UserId);
    }

    [Fact]
    public async Task Should_NotResolveCriticalFinding_When_BlockingActionPlansExist()
    {
        var clock = new FakeDateTimeProvider();
        var repository = new FakeAuditFindingRepository();
        var organizationId = Guid.NewGuid();
        var finding = AuditFinding.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Critical", "Description text", AuditFindingSeverity.Critical, clock.UtcNow);
        finding.MarkInProgress(clock.UtcNow.AddMinutes(1));
        repository.StoredFindings.Add((finding, organizationId));

        var service = new ChangeAuditFindingStatusService(
            repository,
            new FakeActionPlanRepository { HasBlockingActionPlans = true },
            new FakeAuditLogWriter(),
            new FakeCurrentUser { IsAuthenticated = true, UserId = Guid.NewGuid(), OrganizationId = organizationId, Role = UserRole.Auditor },
            clock,
            new ChangeAuditFindingStatusRequestValidator());

        var result = await service.ExecuteAsync(finding.Id, new ChangeAuditFindingStatusRequest
        {
            Status = AuditFindingStatus.Resolved
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "Status");
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated { get; init; }

        public Guid? UserId { get; init; }

        public string? Email => null;

        public UserRole? Role { get; init; }

        public Guid? OrganizationId { get; init; }

        public Guid? DepartmentId => null;
    }

    private sealed class FakeAuditFindingRepository : IAuditFindingRepository
    {
        public List<(AuditFinding Finding, Guid OrganizationId)> StoredFindings { get; } = [];

        public Guid? LastListOrganizationId { get; private set; }

        public Task AddAsync(AuditFinding auditFinding, CancellationToken cancellationToken)
        {
            StoredFindings.Add((auditFinding, Guid.Empty));
            return Task.CompletedTask;
        }

        public Task<AuditFinding?> GetByIdAsync(Guid auditFindingId, Guid organizationId, CancellationToken cancellationToken)
        {
            return Task.FromResult<AuditFinding?>(StoredFindings.SingleOrDefault(item => item.Finding.Id == auditFindingId && item.OrganizationId == organizationId).Finding);
        }

        public Task<AuditFinding?> GetByIdForUpdateAsync(Guid auditFindingId, Guid organizationId, CancellationToken cancellationToken)
        {
            return Task.FromResult<AuditFinding?>(StoredFindings.SingleOrDefault(item => item.Finding.Id == auditFindingId && item.OrganizationId == organizationId).Finding);
        }

        public Task<PagedResult<AuditFinding>> ListAsync(Guid organizationId, AuditFindingQueryParameters queryParameters, CancellationToken cancellationToken)
        {
            LastListOrganizationId = organizationId;
            var items = StoredFindings
                .Where(item => item.OrganizationId == organizationId)
                .Select(item => item.Finding)
                .ToArray();

            return Task.FromResult(new PagedResult<AuditFinding>(items, items.Length, queryParameters.PageNumber, queryParameters.PageSize));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeActionPlanRepository : IActionPlanRepository
    {
        public bool HasBlockingActionPlans { get; init; }

        public Task AddAsync(ActionPlan actionPlan, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<ActionPlan?> GetByIdAsync(Guid actionPlanId, Guid organizationId, CancellationToken cancellationToken) => Task.FromResult<ActionPlan?>(null);

        public Task<ActionPlan?> GetByIdForUpdateAsync(Guid actionPlanId, Guid organizationId, CancellationToken cancellationToken) => Task.FromResult<ActionPlan?>(null);

        public Task<PagedResult<ActionPlan>> ListAsync(Guid organizationId, AuditAI.Application.ActionPlans.Contracts.ActionPlanQueryParameters queryParameters, CancellationToken cancellationToken)
            => Task.FromResult(new PagedResult<ActionPlan>(Array.Empty<ActionPlan>(), 0, queryParameters.PageNumber, queryParameters.PageSize));

        public Task<bool> HasBlockingActionPlansForFindingAsync(Guid organizationId, Guid auditFindingId, CancellationToken cancellationToken)
            => Task.FromResult(HasBlockingActionPlans);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeControlLookup : IControlLookup
    {
        public Dictionary<Guid, Guid> ControlOrganizations { get; } = [];

        public Task<Guid?> GetControlOrganizationIdAsync(Guid controlId, CancellationToken cancellationToken)
            => Task.FromResult(ControlOrganizations.TryGetValue(controlId, out var organizationId) ? (Guid?)organizationId : null);
    }

    private sealed class FakeAuditLogWriter : IAuditLogWriter
    {
        public AuditLogWriteEntry? LastEntry { get; private set; }

        public Task WriteAsync(AuditLogWriteEntry entry, CancellationToken cancellationToken)
        {
            LastEntry = entry;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 06, 18, 15, 0, 0, TimeSpan.Zero);
    }
}
