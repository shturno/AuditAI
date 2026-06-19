using AuditAI.Application.ActionPlans.Contracts;
using AuditAI.Application.ActionPlans.Interfaces;
using AuditAI.Application.ActionPlans.Services;
using AuditAI.Application.ActionPlans.Validators;
using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Common.Results;
using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;

namespace AuditAI.UnitTests.Application.ActionPlans;

public sealed class ActionPlanServiceTests
{
    [Fact]
    public async Task Should_FailCreateActionPlan_When_CurrentUserIsMissing()
    {
        var service = new CreateActionPlanService(
            new FakeActionPlanRepository(),
            new FakeFindingLookup(),
            new FakeUserLookup(),
            new FakeAuditLogWriter(),
            new FakeCurrentUser(),
            new FakeDateTimeProvider(),
            new CreateActionPlanRequestValidator());

        var result = await service.ExecuteAsync(new CreateActionPlanRequest
        {
            AuditFindingId = Guid.NewGuid(),
            AssignedToUserId = Guid.NewGuid(),
            Title = "Remediate control",
            Description = "Implement the remediation plan.",
            DueDate = new FakeDateTimeProvider().UtcNow.AddDays(7)
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsUnauthorized);
    }

    [Fact]
    public async Task Should_FailCreateActionPlan_When_FindingBelongsToAnotherOrganization()
    {
        var organizationId = Guid.NewGuid();
        var currentUser = new FakeCurrentUser { IsAuthenticated = true, UserId = Guid.NewGuid(), OrganizationId = organizationId };
        var findingId = Guid.NewGuid();
        var service = new CreateActionPlanService(
            new FakeActionPlanRepository(),
            new FakeFindingLookup { FindingOrganizations = { [findingId] = Guid.NewGuid() } },
            new FakeUserLookup(),
            new FakeAuditLogWriter(),
            currentUser,
            new FakeDateTimeProvider(),
            new CreateActionPlanRequestValidator());

        var result = await service.ExecuteAsync(new CreateActionPlanRequest
        {
            AuditFindingId = findingId,
            AssignedToUserId = Guid.NewGuid(),
            Title = "Remediate control",
            Description = "Implement the remediation plan.",
            DueDate = new FakeDateTimeProvider().UtcNow.AddDays(7)
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task Should_FailCreateActionPlan_When_AssignedUserBelongsToAnotherOrganization()
    {
        var organizationId = Guid.NewGuid();
        var findingId = Guid.NewGuid();
        var assignedUserId = Guid.NewGuid();
        var service = new CreateActionPlanService(
            new FakeActionPlanRepository(),
            new FakeFindingLookup { FindingOrganizations = { [findingId] = organizationId } },
            new FakeUserLookup { UserOrganizations = { [assignedUserId] = Guid.NewGuid() } },
            new FakeAuditLogWriter(),
            new FakeCurrentUser { IsAuthenticated = true, UserId = Guid.NewGuid(), OrganizationId = organizationId },
            new FakeDateTimeProvider(),
            new CreateActionPlanRequestValidator());

        var result = await service.ExecuteAsync(new CreateActionPlanRequest
        {
            AuditFindingId = findingId,
            AssignedToUserId = assignedUserId,
            Title = "Remediate control",
            Description = "Implement the remediation plan.",
            DueDate = new FakeDateTimeProvider().UtcNow.AddDays(7)
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "AssignedToUserId");
    }

    [Fact]
    public async Task Should_CreateActionPlan_When_FindingAndAssignedUserAreValid()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var findingId = Guid.NewGuid();
        var assignedUserId = Guid.NewGuid();
        var auditLogWriter = new FakeAuditLogWriter();
        var service = new CreateActionPlanService(
            new FakeActionPlanRepository(),
            new FakeFindingLookup { FindingOrganizations = { [findingId] = organizationId } },
            new FakeUserLookup { UserOrganizations = { [assignedUserId] = organizationId } },
            auditLogWriter,
            new FakeCurrentUser { IsAuthenticated = true, UserId = userId, OrganizationId = organizationId },
            new FakeDateTimeProvider(),
            new CreateActionPlanRequestValidator());

        var result = await service.ExecuteAsync(new CreateActionPlanRequest
        {
            AuditFindingId = findingId,
            AssignedToUserId = assignedUserId,
            Title = "Remediate control",
            Description = "Implement the remediation plan.",
            DueDate = new FakeDateTimeProvider().UtcNow.AddDays(7)
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, auditLogWriter.LastEntry!.UserId);
        Assert.Equal(organizationId, auditLogWriter.LastEntry.OrganizationId);
    }

    [Fact]
    public async Task Should_AllowActionPlanList_When_CurrentUserIsAuthenticated()
    {
        var organizationId = Guid.NewGuid();
        var repository = new FakeActionPlanRepository();
        repository.StoredActionPlans.Add((
            ActionPlan.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan", "Description text", new FakeDateTimeProvider().UtcNow.AddDays(7), new FakeDateTimeProvider().UtcNow),
            organizationId));

        var service = new ListActionPlansService(
            repository,
            new FakeCurrentUser { IsAuthenticated = true, UserId = Guid.NewGuid(), OrganizationId = organizationId },
            new ActionPlanQueryParametersValidator());

        var result = await service.ExecuteAsync(new ActionPlanQueryParameters { PageNumber = 1, PageSize = 10 });

        Assert.True(result.IsSuccess);
        Assert.Equal(organizationId, repository.LastListOrganizationId);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_ActionPlanBelongsToAnotherOrganization()
    {
        var repository = new FakeActionPlanRepository();
        var actionPlan = ActionPlan.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan", "Description text", new FakeDateTimeProvider().UtcNow.AddDays(7), new FakeDateTimeProvider().UtcNow);
        repository.StoredActionPlans.Add((actionPlan, Guid.NewGuid()));

        var service = new GetActionPlanByIdService(
            repository,
            new FakeCurrentUser { IsAuthenticated = true, UserId = Guid.NewGuid(), OrganizationId = Guid.NewGuid() });

        var result = await service.ExecuteAsync(actionPlan.Id);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task Should_UseCurrentUserIdInAuditLog_When_UpdatingActionPlan()
    {
        var clock = new FakeDateTimeProvider();
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actionPlan = ActionPlan.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan", "Description text", clock.UtcNow.AddDays(7), clock.UtcNow);
        var repository = new FakeActionPlanRepository();
        repository.StoredActionPlans.Add((actionPlan, organizationId));
        var auditLogWriter = new FakeAuditLogWriter();

        var service = new UpdateActionPlanService(
            repository,
            new FakeUserLookup { UserOrganizations = { [actionPlan.AssignedToUserId] = organizationId } },
            auditLogWriter,
            new FakeCurrentUser { IsAuthenticated = true, UserId = userId, OrganizationId = organizationId },
            clock,
            new UpdateActionPlanRequestValidator());

        var result = await service.ExecuteAsync(actionPlan.Id, new UpdateActionPlanRequest
        {
            AssignedToUserId = actionPlan.AssignedToUserId,
            Title = "Updated plan",
            Description = "Updated description",
            DueDate = clock.UtcNow.AddDays(10)
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, auditLogWriter.LastEntry!.UserId);
    }

    [Fact]
    public async Task Should_UseCurrentUserIdInAuditLog_When_ChangingActionPlanStatus()
    {
        var clock = new FakeDateTimeProvider();
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actionPlan = ActionPlan.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan", "Description text", clock.UtcNow.AddDays(7), clock.UtcNow);
        var repository = new FakeActionPlanRepository();
        repository.StoredActionPlans.Add((actionPlan, organizationId));
        var auditLogWriter = new FakeAuditLogWriter();

        var service = new ChangeActionPlanStatusService(
            repository,
            auditLogWriter,
            new FakeCurrentUser { IsAuthenticated = true, UserId = userId, OrganizationId = organizationId },
            clock,
            new ChangeActionPlanStatusRequestValidator());

        var result = await service.ExecuteAsync(actionPlan.Id, new ChangeActionPlanStatusRequest
        {
            Status = ActionPlanStatus.InProgress
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, auditLogWriter.LastEntry!.UserId);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated { get; init; }

        public Guid? UserId { get; init; }

        public string? Email => null;

        public UserRole? Role => null;

        public Guid? OrganizationId { get; init; }

        public Guid? DepartmentId => null;
    }

    private sealed class FakeActionPlanRepository : IActionPlanRepository
    {
        public List<(ActionPlan ActionPlan, Guid OrganizationId)> StoredActionPlans { get; } = [];

        public Guid? LastListOrganizationId { get; private set; }

        public Task AddAsync(ActionPlan actionPlan, CancellationToken cancellationToken)
        {
            StoredActionPlans.Add((actionPlan, Guid.Empty));
            return Task.CompletedTask;
        }

        public Task<ActionPlan?> GetByIdAsync(Guid actionPlanId, Guid organizationId, CancellationToken cancellationToken)
        {
            return Task.FromResult<ActionPlan?>(StoredActionPlans.SingleOrDefault(item => item.ActionPlan.Id == actionPlanId && item.OrganizationId == organizationId).ActionPlan);
        }

        public Task<ActionPlan?> GetByIdForUpdateAsync(Guid actionPlanId, Guid organizationId, CancellationToken cancellationToken)
        {
            return Task.FromResult<ActionPlan?>(StoredActionPlans.SingleOrDefault(item => item.ActionPlan.Id == actionPlanId && item.OrganizationId == organizationId).ActionPlan);
        }

        public Task<PagedResult<ActionPlan>> ListAsync(Guid organizationId, ActionPlanQueryParameters queryParameters, CancellationToken cancellationToken)
        {
            LastListOrganizationId = organizationId;
            var items = StoredActionPlans
                .Where(item => item.OrganizationId == organizationId)
                .Select(item => item.ActionPlan)
                .ToArray();

            return Task.FromResult(new PagedResult<ActionPlan>(items, items.Length, queryParameters.PageNumber, queryParameters.PageSize));
        }

        public Task<bool> HasBlockingActionPlansForFindingAsync(Guid organizationId, Guid auditFindingId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeFindingLookup : IAuditFindingLookup
    {
        public Dictionary<Guid, Guid> FindingOrganizations { get; } = [];

        public Task<Guid?> GetFindingOrganizationIdAsync(Guid auditFindingId, CancellationToken cancellationToken)
            => Task.FromResult(FindingOrganizations.TryGetValue(auditFindingId, out var organizationId) ? (Guid?)organizationId : null);
    }

    private sealed class FakeUserLookup : AuditAI.Application.ActionPlans.Interfaces.IUserLookup
    {
        public Dictionary<Guid, Guid> UserOrganizations { get; } = [];

        public Task<Guid?> GetUserOrganizationIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(UserOrganizations.TryGetValue(userId, out var organizationId) ? (Guid?)organizationId : null);
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
