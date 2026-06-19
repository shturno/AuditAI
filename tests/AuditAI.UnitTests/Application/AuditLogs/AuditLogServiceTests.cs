using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.AuditLogs.Services;
using AuditAI.Application.AuditLogs.Validators;
using AuditAI.Application.ActionPlans.Contracts;
using AuditAI.Application.ActionPlans.Services;
using AuditAI.Application.ActionPlans.Validators;
using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.AuditFindings.Services;
using AuditAI.Application.AuditFindings.Validators;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Controls.Contracts;
using AuditAI.Application.Controls.Interfaces;
using AuditAI.Application.Controls.Services;
using AuditAI.Application.Controls.Validators;
using AuditAI.Application.Evidence.Contracts;
using AuditAI.Application.Evidence.Interfaces;
using AuditAI.Application.Evidence.Services;
using AuditAI.Application.Evidence.Validators;
using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;
using AuditEvidence = AuditAI.Domain.Entities.Evidence;

namespace AuditAI.UnitTests.Application.AuditLogs;

public sealed class AuditLogServiceTests
{
    [Fact]
    public async Task Should_RecordControlCreated_When_CreateControlSucceeds()
    {
        var organizationId = Guid.NewGuid();
        var repository = new FakeControlRepository();
        var lookup = new FakeControlLookup { ExistingOrganizationIds = { organizationId } };
        var writer = new FakeAuditLogWriter();
        var currentUser = new FakeCurrentUser(Guid.NewGuid(), organizationId);
        var service = new CreateControlService(repository, currentUser, lookup, lookup, writer, new FakeDateTimeProvider(), new CreateControlRequestValidator());

        var result = await service.ExecuteAsync(new CreateControlRequest
        {
            OrganizationId = organizationId,
            Code = "CTRL-001",
            Category = "Access",
            Title = "Access review",
            Description = "Description",
            Frequency = ControlFrequency.Monthly
        });

        Assert.True(result.IsSuccess);
        Assert.Contains(writer.Entries, entry => entry.Action == AuditLogAction.ControlCreated);
    }

    [Fact]
    public async Task Should_NotRecordControlCreated_When_CreateControlValidationFails()
    {
        var repository = new FakeControlRepository();
        var lookup = new FakeControlLookup();
        var writer = new FakeAuditLogWriter();
        var service = new CreateControlService(repository, FakeCurrentUser.Unauthenticated(), lookup, lookup, writer, new FakeDateTimeProvider(), new CreateControlRequestValidator());

        var result = await service.ExecuteAsync(new CreateControlRequest
        {
            OrganizationId = Guid.Empty,
            Code = string.Empty,
            Category = string.Empty,
            Title = string.Empty,
            Description = string.Empty,
            Frequency = ControlFrequency.Monthly
        });

        Assert.False(result.IsSuccess);
        Assert.Empty(writer.Entries);
    }

    [Fact]
    public async Task Should_RecordEvidenceRejected_WithSafeMetadata()
    {
        var clock = new FakeDateTimeProvider();
        var controlId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var evidence = AuditEvidence.Create(Guid.NewGuid(), controlId, Guid.NewGuid(), "report.pdf", "evidence/report.pdf", clock.UtcNow);
        var repository = new FakeEvidenceRepository();
        repository.StoredEvidence.Add(evidence);
        var lookup = new FakeEvidenceLookup
        {
            ControlOrganizations = { [controlId] = organizationId }
        };
        var writer = new FakeAuditLogWriter();
        var service = new RejectEvidenceService(repository, new FakeCurrentUser(reviewerId, organizationId, UserRole.Reviewer), lookup, writer, clock, new ReviewEvidenceRequestValidator());

        var result = await service.ExecuteAsync(evidence.Id, new ReviewEvidenceRequest
        {
            RejectionReason = "Missing approval."
        });

        Assert.True(result.IsSuccess);
        var entry = Assert.Single(writer.Entries);
        Assert.Equal(AuditLogAction.EvidenceRejected, entry.Action);
        Assert.DoesNotContain("token", entry.Metadata ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Should_RecordAuditFindingStatusChanged_When_StatusChanges()
    {
        var clock = new FakeDateTimeProvider();
        var finding = AuditFinding.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Finding", "Description", AuditFindingSeverity.High, clock.UtcNow);
        var findingRepository = new FakeAuditFindingRepository();
        findingRepository.StoredFindings.Add(finding);
        var writer = new FakeAuditLogWriter();
        var findingLookup = new FakeFindingLookup { FindingOrganizations = { [finding.Id] = Guid.NewGuid() } };
        var service = new ChangeAuditFindingStatusService(
            findingRepository,
            new FakeActionPlanRepository(),
            writer,
            new FakeCurrentUser(Guid.NewGuid(), Guid.NewGuid()),
            clock,
            new ChangeAuditFindingStatusRequestValidator());

        var result = await service.ExecuteAsync(finding.Id, new ChangeAuditFindingStatusRequest
        {
            Status = AuditFindingStatus.InProgress
        });

        Assert.True(result.IsSuccess);
        Assert.Contains(writer.Entries, entry => entry.Action == AuditLogAction.AuditFindingStatusChanged);
    }

    [Fact]
    public async Task Should_RecordActionPlanStatusChanged_When_StatusChanges()
    {
        var clock = new FakeDateTimeProvider();
        var actionPlan = ActionPlan.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan", "Description", clock.UtcNow.AddDays(7), clock.UtcNow);
        var repository = new FakeActionPlanRepository();
        repository.StoredActionPlans.Add(actionPlan);
        var writer = new FakeAuditLogWriter();
        var findingLookup = new FakeFindingLookup { FindingOrganizations = { [actionPlan.AuditFindingId] = Guid.NewGuid() } };
        var service = new ChangeActionPlanStatusService(
            repository,
            writer,
            new FakeCurrentUser(Guid.NewGuid(), Guid.NewGuid()),
            clock,
            new ChangeActionPlanStatusRequestValidator());

        var result = await service.ExecuteAsync(actionPlan.Id, new ChangeActionPlanStatusRequest
        {
            Status = ActionPlanStatus.InProgress
        });

        Assert.True(result.IsSuccess);
        Assert.Contains(writer.Entries, entry => entry.Action == AuditLogAction.ActionPlanStatusChanged);
    }

    [Fact]
    public async Task Should_ValidateAuditLogListPaginationAndDateRange()
    {
        var repository = new FakeAuditLogRepository();
        var service = new ListAuditLogsService(repository, new FakeCurrentUser(Guid.NewGuid(), Guid.NewGuid(), UserRole.Admin), new AuditLogQueryParametersValidator());

        var result = await service.ExecuteAsync(new AuditLogQueryParameters
        {
            PageNumber = 0,
            From = new DateTimeOffset(2026, 06, 19, 0, 0, 0, TimeSpan.Zero),
            To = new DateTimeOffset(2026, 06, 18, 0, 0, 0, TimeSpan.Zero)
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsValidationFailure);
    }

    [Fact]
    public async Task Should_RejectAuditLogRead_When_CurrentUserIsReviewer()
    {
        var repository = new FakeAuditLogRepository();
        var service = new ListAuditLogsService(
            repository,
            new FakeCurrentUser(Guid.NewGuid(), Guid.NewGuid(), UserRole.Reviewer),
            new AuditLogQueryParametersValidator());

        var result = await service.ExecuteAsync(new AuditLogQueryParameters());

        Assert.False(result.IsSuccess);
        Assert.True(result.IsForbidden);
    }

    [Fact]
    public async Task Should_AllowAuditLogRead_When_CurrentUserIsAdmin()
    {
        var organizationId = Guid.NewGuid();
        var repository = new FakeAuditLogRepository();
        repository.StoredLogs.Add(AuditLog.Create(Guid.NewGuid(), organizationId, null, AuditLogAction.ControlCreated, "Control", Guid.NewGuid(), null, new FakeDateTimeProvider().UtcNow));
        var service = new ListAuditLogsService(
            repository,
            new FakeCurrentUser(Guid.NewGuid(), organizationId, UserRole.Admin),
            new AuditLogQueryParametersValidator());

        var result = await service.ExecuteAsync(new AuditLogQueryParameters());

        Assert.True(result.IsSuccess);
        Assert.Equal(organizationId, repository.LastOrganizationId);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_AuditLogDoesNotExist()
    {
        var repository = new FakeAuditLogRepository();
        var service = new GetAuditLogByIdService(repository, new FakeCurrentUser(Guid.NewGuid(), Guid.NewGuid(), UserRole.Admin));

        var result = await service.ExecuteAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
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

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public List<AuditLog> StoredLogs { get; } = [];
        public Guid? LastOrganizationId { get; private set; }

        public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
        {
            StoredLogs.Add(auditLog);
            return Task.CompletedTask;
        }

        public Task<AuditLog?> GetByIdAsync(Guid auditLogId, Guid organizationId, CancellationToken cancellationToken)
        {
            return Task.FromResult(StoredLogs.SingleOrDefault(log => log.Id == auditLogId && log.OrganizationId == organizationId));
        }

        public Task<PagedResult<AuditLog>> ListAsync(Guid organizationId, AuditLogQueryParameters queryParameters, CancellationToken cancellationToken)
        {
            LastOrganizationId = organizationId;
            var items = StoredLogs.Where(log => log.OrganizationId == organizationId).ToArray();
            return Task.FromResult(new PagedResult<AuditLog>(items, items.Length, 1, 20));
        }
    }

    private sealed class FakeControlRepository : IControlRepository
    {
        public List<Control> StoredControls { get; } = [];

        public Task AddAsync(Control control, CancellationToken cancellationToken)
        {
            StoredControls.Add(control);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsWithCodeAsync(Guid organizationId, string code, Guid? excludedControlId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<Control?> GetByIdAsync(Guid controlId, CancellationToken cancellationToken) => Task.FromResult<Control?>(null);

        public Task<Control?> GetByIdForUpdateAsync(Guid controlId, CancellationToken cancellationToken) => Task.FromResult<Control?>(null);

        public Task<PagedResult<Control>> ListAsync(ControlQueryParameters queryParameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<Control>(StoredControls, StoredControls.Count, 1, 20));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeControlLookup : IOrganizationLookup, IDepartmentLookup
    {
        public HashSet<Guid> ExistingOrganizationIds { get; } = [];

        public Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistingOrganizationIds.Contains(organizationId));
        }

        public Task<bool> DepartmentExistsAsync(Guid departmentId, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<bool> DepartmentBelongsToOrganizationAsync(Guid departmentId, Guid organizationId, CancellationToken cancellationToken) => Task.FromResult(false);
    }

    private sealed class FakeEvidenceRepository : IEvidenceRepository
    {
        public List<AuditEvidence> StoredEvidence { get; } = [];

        public Task AddAsync(AuditEvidence evidence, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<AuditEvidence?> GetByIdAsync(Guid evidenceId, CancellationToken cancellationToken) => Task.FromResult(StoredEvidence.SingleOrDefault(x => x.Id == evidenceId));

        public Task<AuditEvidence?> GetByIdForUpdateAsync(Guid evidenceId, CancellationToken cancellationToken) => Task.FromResult(StoredEvidence.SingleOrDefault(x => x.Id == evidenceId));

        public Task<PagedResult<AuditEvidence>> ListAsync(Guid organizationId, EvidenceQueryParameters queryParameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<AuditEvidence>(StoredEvidence, StoredEvidence.Count, 1, 20));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeEvidenceLookup : IControlLookup
    {
        public Dictionary<Guid, Guid> ControlOrganizations { get; } = [];

        public Task<Guid?> GetControlOrganizationIdAsync(Guid controlId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ControlOrganizations.TryGetValue(controlId, out var organizationId) ? (Guid?)organizationId : null);
        }
    }

    private sealed class FakeAuditFindingRepository : AuditAI.Application.AuditFindings.Interfaces.IAuditFindingRepository
    {
        public List<AuditFinding> StoredFindings { get; } = [];

        public Task AddAsync(AuditFinding auditFinding, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<AuditFinding?> GetByIdAsync(Guid auditFindingId, Guid organizationId, CancellationToken cancellationToken) => Task.FromResult(StoredFindings.SingleOrDefault(x => x.Id == auditFindingId));

        public Task<AuditFinding?> GetByIdForUpdateAsync(Guid auditFindingId, Guid organizationId, CancellationToken cancellationToken) => Task.FromResult(StoredFindings.SingleOrDefault(x => x.Id == auditFindingId));

        public Task<PagedResult<AuditFinding>> ListAsync(Guid organizationId, AuditAI.Application.AuditFindings.Contracts.AuditFindingQueryParameters queryParameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<AuditFinding>(StoredFindings, StoredFindings.Count, 1, 20));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeFindingLookup : AuditAI.Application.ActionPlans.Interfaces.IAuditFindingLookup
    {
        public Dictionary<Guid, Guid> FindingOrganizations { get; } = [];

        public Task<Guid?> GetFindingOrganizationIdAsync(Guid auditFindingId, CancellationToken cancellationToken)
        {
            return Task.FromResult(FindingOrganizations.TryGetValue(auditFindingId, out var organizationId) ? (Guid?)organizationId : null);
        }
    }

    private sealed class FakeActionPlanRepository : AuditAI.Application.ActionPlans.Interfaces.IActionPlanRepository
    {
        public List<ActionPlan> StoredActionPlans { get; } = [];

        public Task AddAsync(ActionPlan actionPlan, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<ActionPlan?> GetByIdAsync(Guid actionPlanId, Guid organizationId, CancellationToken cancellationToken) => Task.FromResult(StoredActionPlans.SingleOrDefault(x => x.Id == actionPlanId));

        public Task<ActionPlan?> GetByIdForUpdateAsync(Guid actionPlanId, Guid organizationId, CancellationToken cancellationToken) => Task.FromResult(StoredActionPlans.SingleOrDefault(x => x.Id == actionPlanId));

        public Task<PagedResult<ActionPlan>> ListAsync(Guid organizationId, AuditAI.Application.ActionPlans.Contracts.ActionPlanQueryParameters queryParameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<ActionPlan>(StoredActionPlans, StoredActionPlans.Count, 1, 20));
        }

        public Task<bool> HasBlockingActionPlansForFindingAsync(Guid organizationId, Guid auditFindingId, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeDateTimeProvider : AuditAI.Application.Common.Abstractions.IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 06, 18, 15, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public FakeCurrentUser(Guid userId, Guid organizationId, UserRole role = UserRole.Auditor)
        {
            UserId = userId;
            OrganizationId = organizationId;
            Role = role;
        }

        private FakeCurrentUser()
        {
        }

        public bool IsAuthenticated => UserId.HasValue && OrganizationId.HasValue;

        public Guid? UserId { get; }

        public string? Email => "user@auditai.test";

        public UserRole? Role { get; }

        public Guid? OrganizationId { get; }

        public Guid? DepartmentId => null;

        public static FakeCurrentUser Unauthenticated()
        {
            return new FakeCurrentUser();
        }
    }
}
