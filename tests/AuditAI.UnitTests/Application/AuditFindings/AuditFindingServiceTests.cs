using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.AuditFindings.Interfaces;
using AuditAI.Application.AuditFindings.Services;
using AuditAI.Application.AuditFindings.Validators;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Evidence.Interfaces;
using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;

namespace AuditAI.UnitTests.Application.AuditFindings;

public sealed class AuditFindingServiceTests
{
    [Fact]
    public async Task Should_FailCreateAuditFinding_When_ControlDoesNotExist()
    {
        var repository = new FakeAuditFindingRepository();
        var lookup = new FakeReferenceLookup();
        var service = new CreateAuditFindingService(repository, lookup, lookup, new FakeDateTimeProvider(), new CreateAuditFindingRequestValidator());

        var result = await service.ExecuteAsync(new CreateAuditFindingRequest
        {
            ControlId = Guid.NewGuid(),
            CreatedByUserId = Guid.NewGuid(),
            Title = "Missing approval",
            Description = "This control has no approval evidence.",
            Severity = AuditFindingSeverity.High
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "ControlId");
    }

    [Fact]
    public async Task Should_FailCreateAuditFinding_When_CreatorDoesNotExist()
    {
        var repository = new FakeAuditFindingRepository();
        var controlId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var lookup = new FakeReferenceLookup
        {
            ControlOrganizations = { [controlId] = organizationId }
        };
        var service = new CreateAuditFindingService(repository, lookup, lookup, new FakeDateTimeProvider(), new CreateAuditFindingRequestValidator());

        var result = await service.ExecuteAsync(new CreateAuditFindingRequest
        {
            ControlId = controlId,
            CreatedByUserId = Guid.NewGuid(),
            Title = "Missing approval",
            Description = "This control has no approval evidence.",
            Severity = AuditFindingSeverity.High
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "CreatedByUserId");
    }

    [Fact]
    public async Task Should_FailCreateAuditFinding_When_CreatorBelongsToAnotherOrganization()
    {
        var repository = new FakeAuditFindingRepository();
        var controlId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var lookup = new FakeReferenceLookup
        {
            ControlOrganizations = { [controlId] = Guid.NewGuid() },
            UserOrganizations = { [creatorId] = Guid.NewGuid() }
        };
        var service = new CreateAuditFindingService(repository, lookup, lookup, new FakeDateTimeProvider(), new CreateAuditFindingRequestValidator());

        var result = await service.ExecuteAsync(new CreateAuditFindingRequest
        {
            ControlId = controlId,
            CreatedByUserId = creatorId,
            Title = "Missing approval",
            Description = "This control has no approval evidence.",
            Severity = AuditFindingSeverity.High
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "CreatedByUserId");
    }

    [Fact]
    public async Task Should_CreateAuditFinding_When_ControlAndCreatorAreValid()
    {
        var repository = new FakeAuditFindingRepository();
        var controlId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var lookup = new FakeReferenceLookup
        {
            ControlOrganizations = { [controlId] = organizationId },
            UserOrganizations = { [creatorId] = organizationId }
        };
        var service = new CreateAuditFindingService(repository, lookup, lookup, new FakeDateTimeProvider(), new CreateAuditFindingRequestValidator());

        var result = await service.ExecuteAsync(new CreateAuditFindingRequest
        {
            ControlId = controlId,
            CreatedByUserId = creatorId,
            Title = "Missing approval",
            Description = "This control has no approval evidence.",
            Severity = AuditFindingSeverity.High
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(AuditFindingStatus.Open, result.Value.Status);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_FindingDoesNotExist()
    {
        var repository = new FakeAuditFindingRepository();
        var service = new GetAuditFindingByIdService(repository);

        var result = await service.ExecuteAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UpdatingMissingFinding()
    {
        var repository = new FakeAuditFindingRepository();
        var service = new UpdateAuditFindingService(repository, new FakeDateTimeProvider(), new UpdateAuditFindingRequestValidator());

        var result = await service.ExecuteAsync(Guid.NewGuid(), new UpdateAuditFindingRequest
        {
            Title = "Updated title",
            Description = "Updated description for the finding.",
            Severity = AuditFindingSeverity.Medium
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task Should_FailChangeStatus_When_TransitionIsInvalid()
    {
        var clock = new FakeDateTimeProvider();
        var repository = new FakeAuditFindingRepository();
        var finding = AuditFinding.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Missing approval",
            "This control has no approval evidence.",
            AuditFindingSeverity.High,
            clock.UtcNow);
        repository.StoredFindings.Add(finding);

        var service = new ChangeAuditFindingStatusService(repository, clock, new ChangeAuditFindingStatusRequestValidator());

        var result = await service.ExecuteAsync(finding.Id, new ChangeAuditFindingStatusRequest
        {
            Status = AuditFindingStatus.Resolved
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "Status");
    }

    [Fact]
    public async Task Should_ChangeStatus_When_TransitionIsValid()
    {
        var clock = new FakeDateTimeProvider();
        var repository = new FakeAuditFindingRepository();
        var finding = AuditFinding.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Missing approval",
            "This control has no approval evidence.",
            AuditFindingSeverity.High,
            clock.UtcNow);
        repository.StoredFindings.Add(finding);
        var service = new ChangeAuditFindingStatusService(repository, clock, new ChangeAuditFindingStatusRequestValidator());

        var inProgressResult = await service.ExecuteAsync(finding.Id, new ChangeAuditFindingStatusRequest
        {
            Status = AuditFindingStatus.InProgress
        });

        Assert.True(inProgressResult.IsSuccess);
        Assert.Equal(AuditFindingStatus.InProgress, inProgressResult.Value!.Status);
    }

    [Fact]
    public async Task Should_CancelFinding_When_CurrentStatusAllowsIt()
    {
        var clock = new FakeDateTimeProvider();
        var repository = new FakeAuditFindingRepository();
        var finding = AuditFinding.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Missing approval",
            "This control has no approval evidence.",
            AuditFindingSeverity.High,
            clock.UtcNow);
        repository.StoredFindings.Add(finding);
        var service = new ChangeAuditFindingStatusService(repository, clock, new ChangeAuditFindingStatusRequestValidator());

        var result = await service.ExecuteAsync(finding.Id, new ChangeAuditFindingStatusRequest
        {
            Status = AuditFindingStatus.Cancelled
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(AuditFindingStatus.Cancelled, result.Value!.Status);
    }

    private sealed class FakeAuditFindingRepository : IAuditFindingRepository
    {
        public List<AuditFinding> StoredFindings { get; } = [];

        public Task AddAsync(AuditFinding auditFinding, CancellationToken cancellationToken)
        {
            StoredFindings.Add(auditFinding);
            return Task.CompletedTask;
        }

        public Task<AuditFinding?> GetByIdAsync(Guid auditFindingId, CancellationToken cancellationToken)
        {
            return Task.FromResult(StoredFindings.SingleOrDefault(finding => finding.Id == auditFindingId));
        }

        public Task<AuditFinding?> GetByIdForUpdateAsync(Guid auditFindingId, CancellationToken cancellationToken)
        {
            return Task.FromResult(StoredFindings.SingleOrDefault(finding => finding.Id == auditFindingId));
        }

        public Task<PagedResult<AuditFinding>> ListAsync(AuditFindingQueryParameters queryParameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<AuditFinding>(StoredFindings, StoredFindings.Count, 1, 20));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeReferenceLookup : IControlLookup, IUserLookup
    {
        public Dictionary<Guid, Guid> ControlOrganizations { get; } = [];

        public Dictionary<Guid, Guid> UserOrganizations { get; } = [];

        public Task<Guid?> GetControlOrganizationIdAsync(Guid controlId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ControlOrganizations.TryGetValue(controlId, out var organizationId) ? (Guid?)organizationId : null);
        }

        public Task<Guid?> GetUserOrganizationIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(UserOrganizations.TryGetValue(userId, out var organizationId) ? (Guid?)organizationId : null);
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 06, 18, 15, 0, 0, TimeSpan.Zero);
    }
}
