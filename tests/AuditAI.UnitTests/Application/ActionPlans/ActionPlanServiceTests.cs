using AuditAI.Application.ActionPlans.Contracts;
using AuditAI.Application.ActionPlans.Interfaces;
using AuditAI.Application.ActionPlans.Services;
using AuditAI.Application.ActionPlans.Validators;
using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Evidence.Interfaces;
using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;

namespace AuditAI.UnitTests.Application.ActionPlans;

public sealed class ActionPlanServiceTests
{
    [Fact]
    public async Task Should_FailCreateActionPlan_When_FindingDoesNotExist()
    {
        var repository = new FakeActionPlanRepository();
        var findingLookup = new FakeFindingLookup();
        var userLookup = new FakeUserLookup();
        var service = new CreateActionPlanService(repository, findingLookup, userLookup, new FakeAuditLogWriter(), new FakeDateTimeProvider(), new CreateActionPlanRequestValidator());

        var result = await service.ExecuteAsync(new CreateActionPlanRequest
        {
            AuditFindingId = Guid.NewGuid(),
            AssignedToUserId = Guid.NewGuid(),
            Title = "Remediate control",
            Description = "Implement the remediation plan.",
            DueDate = new FakeDateTimeProvider().UtcNow.AddDays(7)
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "AuditFindingId");
    }

    [Fact]
    public async Task Should_FailCreateActionPlan_When_AssignedUserDoesNotExist()
    {
        var repository = new FakeActionPlanRepository();
        var findingId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var findingLookup = new FakeFindingLookup
        {
            FindingOrganizations = { [findingId] = organizationId }
        };
        var userLookup = new FakeUserLookup();
        var service = new CreateActionPlanService(repository, findingLookup, userLookup, new FakeAuditLogWriter(), new FakeDateTimeProvider(), new CreateActionPlanRequestValidator());

        var result = await service.ExecuteAsync(new CreateActionPlanRequest
        {
            AuditFindingId = findingId,
            AssignedToUserId = Guid.NewGuid(),
            Title = "Remediate control",
            Description = "Implement the remediation plan.",
            DueDate = new FakeDateTimeProvider().UtcNow.AddDays(7)
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "AssignedToUserId");
    }

    [Fact]
    public async Task Should_FailCreateActionPlan_When_AssignedUserBelongsToAnotherOrganization()
    {
        var repository = new FakeActionPlanRepository();
        var findingId = Guid.NewGuid();
        var assignedUserId = Guid.NewGuid();
        var findingLookup = new FakeFindingLookup
        {
            FindingOrganizations = { [findingId] = Guid.NewGuid() }
        };
        var userLookup = new FakeUserLookup
        {
            UserOrganizations = { [assignedUserId] = Guid.NewGuid() }
        };
        var service = new CreateActionPlanService(repository, findingLookup, userLookup, new FakeAuditLogWriter(), new FakeDateTimeProvider(), new CreateActionPlanRequestValidator());

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
        var repository = new FakeActionPlanRepository();
        var findingId = Guid.NewGuid();
        var assignedUserId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var findingLookup = new FakeFindingLookup
        {
            FindingOrganizations = { [findingId] = organizationId }
        };
        var userLookup = new FakeUserLookup
        {
            UserOrganizations = { [assignedUserId] = organizationId }
        };
        var service = new CreateActionPlanService(repository, findingLookup, userLookup, new FakeAuditLogWriter(), new FakeDateTimeProvider(), new CreateActionPlanRequestValidator());

        var result = await service.ExecuteAsync(new CreateActionPlanRequest
        {
            AuditFindingId = findingId,
            AssignedToUserId = assignedUserId,
            Title = "Remediate control",
            Description = "Implement the remediation plan.",
            DueDate = new FakeDateTimeProvider().UtcNow.AddDays(7)
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(ActionPlanStatus.Open, result.Value.Status);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_ActionPlanDoesNotExist()
    {
        var repository = new FakeActionPlanRepository();
        var service = new GetActionPlanByIdService(repository);

        var result = await service.ExecuteAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UpdatingMissingActionPlan()
    {
        var repository = new FakeActionPlanRepository();
        var service = new UpdateActionPlanService(repository, new FakeFindingLookup(), new FakeUserLookup(), new FakeAuditLogWriter(), new FakeDateTimeProvider(), new UpdateActionPlanRequestValidator());

        var result = await service.ExecuteAsync(Guid.NewGuid(), new UpdateActionPlanRequest
        {
            AssignedToUserId = Guid.NewGuid(),
            Title = "Updated title",
            Description = "Updated description",
            DueDate = new FakeDateTimeProvider().UtcNow.AddDays(10)
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task Should_FailChangeStatus_When_TransitionIsInvalid()
    {
        var clock = new FakeDateTimeProvider();
        var repository = new FakeActionPlanRepository();
        var actionPlan = ActionPlan.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Remediate control",
            "Implement the remediation plan.",
            clock.UtcNow.AddDays(7),
            clock.UtcNow);
        actionPlan.Complete(clock.UtcNow.AddDays(1));
        repository.StoredActionPlans.Add(actionPlan);
        var findingLookup = new FakeFindingLookup { FindingOrganizations = { [actionPlan.AuditFindingId] = Guid.NewGuid() } };
        var service = new ChangeActionPlanStatusService(repository, findingLookup, new FakeAuditLogWriter(), clock, new ChangeActionPlanStatusRequestValidator());

        var result = await service.ExecuteAsync(actionPlan.Id, new ChangeActionPlanStatusRequest
        {
            Status = ActionPlanStatus.InProgress
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "Status");
    }

    [Fact]
    public async Task Should_ChangeStatus_When_TransitionIsValid()
    {
        var clock = new FakeDateTimeProvider();
        var repository = new FakeActionPlanRepository();
        var actionPlan = ActionPlan.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Remediate control",
            "Implement the remediation plan.",
            clock.UtcNow.AddDays(7),
            clock.UtcNow);
        repository.StoredActionPlans.Add(actionPlan);
        var findingLookup = new FakeFindingLookup { FindingOrganizations = { [actionPlan.AuditFindingId] = Guid.NewGuid() } };
        var service = new ChangeActionPlanStatusService(repository, findingLookup, new FakeAuditLogWriter(), clock, new ChangeActionPlanStatusRequestValidator());

        var result = await service.ExecuteAsync(actionPlan.Id, new ChangeActionPlanStatusRequest
        {
            Status = ActionPlanStatus.InProgress
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(ActionPlanStatus.InProgress, result.Value!.Status);
    }

    [Fact]
    public async Task Should_CompleteActionPlan_When_CurrentStatusAllowsIt()
    {
        var clock = new FakeDateTimeProvider();
        var repository = new FakeActionPlanRepository();
        var actionPlan = ActionPlan.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Remediate control",
            "Implement the remediation plan.",
            clock.UtcNow.AddDays(7),
            clock.UtcNow);
        repository.StoredActionPlans.Add(actionPlan);
        var findingLookup = new FakeFindingLookup { FindingOrganizations = { [actionPlan.AuditFindingId] = Guid.NewGuid() } };
        var service = new ChangeActionPlanStatusService(repository, findingLookup, new FakeAuditLogWriter(), clock, new ChangeActionPlanStatusRequestValidator());

        var result = await service.ExecuteAsync(actionPlan.Id, new ChangeActionPlanStatusRequest
        {
            Status = ActionPlanStatus.Completed
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(ActionPlanStatus.Completed, result.Value!.Status);
    }

    [Fact]
    public async Task Should_CancelActionPlan_When_CurrentStatusAllowsIt()
    {
        var clock = new FakeDateTimeProvider();
        var repository = new FakeActionPlanRepository();
        var actionPlan = ActionPlan.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Remediate control",
            "Implement the remediation plan.",
            clock.UtcNow.AddDays(7),
            clock.UtcNow);
        repository.StoredActionPlans.Add(actionPlan);
        var findingLookup = new FakeFindingLookup { FindingOrganizations = { [actionPlan.AuditFindingId] = Guid.NewGuid() } };
        var service = new ChangeActionPlanStatusService(repository, findingLookup, new FakeAuditLogWriter(), clock, new ChangeActionPlanStatusRequestValidator());

        var result = await service.ExecuteAsync(actionPlan.Id, new ChangeActionPlanStatusRequest
        {
            Status = ActionPlanStatus.Cancelled
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(ActionPlanStatus.Cancelled, result.Value!.Status);
    }

    private sealed class FakeActionPlanRepository : IActionPlanRepository
    {
        public List<ActionPlan> StoredActionPlans { get; } = [];

        public Task AddAsync(ActionPlan actionPlan, CancellationToken cancellationToken)
        {
            StoredActionPlans.Add(actionPlan);
            return Task.CompletedTask;
        }

        public Task<ActionPlan?> GetByIdAsync(Guid actionPlanId, CancellationToken cancellationToken)
        {
            return Task.FromResult(StoredActionPlans.SingleOrDefault(actionPlan => actionPlan.Id == actionPlanId));
        }

        public Task<ActionPlan?> GetByIdForUpdateAsync(Guid actionPlanId, CancellationToken cancellationToken)
        {
            return Task.FromResult(StoredActionPlans.SingleOrDefault(actionPlan => actionPlan.Id == actionPlanId));
        }

        public Task<PagedResult<ActionPlan>> ListAsync(ActionPlanQueryParameters queryParameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<ActionPlan>(StoredActionPlans, StoredActionPlans.Count, 1, 20));
        }

        public Task<bool> HasBlockingActionPlansForFindingAsync(Guid auditFindingId, CancellationToken cancellationToken)
        {
            return Task.FromResult(StoredActionPlans.Any(actionPlan => actionPlan.AuditFindingId == auditFindingId && actionPlan.IsOpenForResolution()));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFindingLookup : IAuditFindingLookup
    {
        public Dictionary<Guid, Guid> FindingOrganizations { get; } = [];

        public Task<Guid?> GetFindingOrganizationIdAsync(Guid auditFindingId, CancellationToken cancellationToken)
        {
            return Task.FromResult(FindingOrganizations.TryGetValue(auditFindingId, out var organizationId) ? (Guid?)organizationId : null);
        }
    }

    private sealed class FakeUserLookup : IUserLookup
    {
        public Dictionary<Guid, Guid> UserOrganizations { get; } = [];

        public Task<Guid?> GetUserOrganizationIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(UserOrganizations.TryGetValue(userId, out var organizationId) ? (Guid?)organizationId : null);
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
}
