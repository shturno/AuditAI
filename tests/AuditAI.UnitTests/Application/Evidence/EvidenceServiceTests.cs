using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Evidence.Contracts;
using AuditAI.Application.Evidence.Interfaces;
using AuditAI.Application.Evidence.Services;
using AuditAI.Application.Evidence.Validators;
using AuditAI.Domain.Enums;
using AuditEvidence = AuditAI.Domain.Entities.Evidence;

namespace AuditAI.UnitTests.Application.Evidence;

public sealed class EvidenceServiceTests
{
    [Fact]
    public async Task Should_FailCreateEvidence_When_ControlDoesNotExist()
    {
        var repository = new FakeEvidenceRepository();
        var lookup = new FakeEvidenceReferenceLookup();
        var service = new CreateEvidenceService(repository, lookup, lookup, new FakeAuditLogWriter(), new FakeDateTimeProvider(), new CreateEvidenceRequestValidator());

        var result = await service.ExecuteAsync(new CreateEvidenceRequest
        {
            ControlId = Guid.NewGuid(),
            SubmittedByUserId = Guid.NewGuid(),
            FileName = "report.pdf",
            StorageReference = "evidence/report.pdf"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "ControlId");
    }

    [Fact]
    public async Task Should_FailCreateEvidence_When_SubmitterDoesNotExist()
    {
        var repository = new FakeEvidenceRepository();
        var controlId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var lookup = new FakeEvidenceReferenceLookup
        {
            ControlOrganizations = { [controlId] = organizationId }
        };
        var service = new CreateEvidenceService(repository, lookup, lookup, new FakeAuditLogWriter(), new FakeDateTimeProvider(), new CreateEvidenceRequestValidator());

        var result = await service.ExecuteAsync(new CreateEvidenceRequest
        {
            ControlId = controlId,
            SubmittedByUserId = Guid.NewGuid(),
            FileName = "report.pdf",
            StorageReference = "evidence/report.pdf"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "SubmittedByUserId");
    }

    [Fact]
    public async Task Should_FailCreateEvidence_When_SubmitterBelongsToAnotherOrganization()
    {
        var repository = new FakeEvidenceRepository();
        var controlId = Guid.NewGuid();
        var submitterId = Guid.NewGuid();
        var lookup = new FakeEvidenceReferenceLookup
        {
            ControlOrganizations = { [controlId] = Guid.NewGuid() },
            UserOrganizations = { [submitterId] = Guid.NewGuid() }
        };
        var service = new CreateEvidenceService(repository, lookup, lookup, new FakeAuditLogWriter(), new FakeDateTimeProvider(), new CreateEvidenceRequestValidator());

        var result = await service.ExecuteAsync(new CreateEvidenceRequest
        {
            ControlId = controlId,
            SubmittedByUserId = submitterId,
            FileName = "report.pdf",
            StorageReference = "evidence/report.pdf"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "SubmittedByUserId");
    }

    [Fact]
    public async Task Should_CreateEvidence_When_ControlAndSubmitterAreValid()
    {
        var repository = new FakeEvidenceRepository();
        var controlId = Guid.NewGuid();
        var submitterId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var lookup = new FakeEvidenceReferenceLookup
        {
            ControlOrganizations = { [controlId] = organizationId },
            UserOrganizations = { [submitterId] = organizationId }
        };
        var service = new CreateEvidenceService(repository, lookup, lookup, new FakeAuditLogWriter(), new FakeDateTimeProvider(), new CreateEvidenceRequestValidator());

        var result = await service.ExecuteAsync(new CreateEvidenceRequest
        {
            ControlId = controlId,
            SubmittedByUserId = submitterId,
            FileName = "report.pdf",
            StorageReference = "evidence/report.pdf"
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(EvidenceStatus.Pending, result.Value.Status);
    }

    [Fact]
    public async Task Should_FailReviewEvidence_When_EvidenceDoesNotExist()
    {
        var repository = new FakeEvidenceRepository();
        var lookup = new FakeEvidenceReferenceLookup();
        var service = new AcceptEvidenceService(repository, lookup, lookup, new FakeAuditLogWriter(), new FakeDateTimeProvider(), new ReviewEvidenceRequestValidator());

        var result = await service.ExecuteAsync(Guid.NewGuid(), new ReviewEvidenceRequest
        {
            ReviewerUserId = Guid.NewGuid()
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task Should_FailAcceptEvidence_When_ReviewerDoesNotExist()
    {
        var controlId = Guid.NewGuid();
        var evidence = AuditEvidence.Create(Guid.NewGuid(), controlId, Guid.NewGuid(), "report.pdf", "evidence/report.pdf", new FakeDateTimeProvider().UtcNow);
        var repository = new FakeEvidenceRepository();
        repository.StoredEvidence.Add(evidence);

        var lookup = new FakeEvidenceReferenceLookup
        {
            ControlOrganizations = { [controlId] = Guid.NewGuid() }
        };
        var service = new AcceptEvidenceService(repository, lookup, lookup, new FakeAuditLogWriter(), new FakeDateTimeProvider(), new ReviewEvidenceRequestValidator());

        var result = await service.ExecuteAsync(evidence.Id, new ReviewEvidenceRequest
        {
            ReviewerUserId = Guid.NewGuid()
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "ReviewerUserId");
    }

    [Fact]
    public async Task Should_FailRejectEvidence_When_RejectionReasonIsEmpty()
    {
        var service = new RejectEvidenceService(
            new FakeEvidenceRepository(),
            new FakeEvidenceReferenceLookup(),
            new FakeEvidenceReferenceLookup(),
            new FakeAuditLogWriter(),
            new FakeDateTimeProvider(),
            new ReviewEvidenceRequestValidator());

        var result = await service.ExecuteAsync(Guid.NewGuid(), new ReviewEvidenceRequest
        {
            ReviewerUserId = Guid.NewGuid(),
            RejectionReason = string.Empty
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "RejectionReason");
    }

    [Fact]
    public async Task Should_FailReviewEvidence_When_EvidenceIsAlreadyAcceptedOrRejected()
    {
        var clock = new FakeDateTimeProvider();
        var controlId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var evidence = AuditEvidence.Create(Guid.NewGuid(), controlId, Guid.NewGuid(), "report.pdf", "evidence/report.pdf", clock.UtcNow);
        evidence.Accept(reviewerId, clock.UtcNow);

        var repository = new FakeEvidenceRepository();
        repository.StoredEvidence.Add(evidence);
        var lookup = new FakeEvidenceReferenceLookup
        {
            ControlOrganizations = { [controlId] = Guid.NewGuid() },
            UserOrganizations = { [reviewerId] = Guid.NewGuid() }
        };
        var service = new RejectEvidenceService(repository, lookup, lookup, new FakeAuditLogWriter(), clock, new ReviewEvidenceRequestValidator());

        var result = await service.ExecuteAsync(evidence.Id, new ReviewEvidenceRequest
        {
            ReviewerUserId = reviewerId,
            RejectionReason = "Missing approval."
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "Status");
    }

    [Fact]
    public async Task Should_RejectEvidence_When_EvidenceIsPendingAndReasonIsValid()
    {
        var clock = new FakeDateTimeProvider();
        var controlId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var evidence = AuditEvidence.Create(Guid.NewGuid(), controlId, Guid.NewGuid(), "report.pdf", "evidence/report.pdf", clock.UtcNow);

        var repository = new FakeEvidenceRepository();
        repository.StoredEvidence.Add(evidence);
        var lookup = new FakeEvidenceReferenceLookup
        {
            ControlOrganizations = { [controlId] = organizationId },
            UserOrganizations = { [reviewerId] = organizationId }
        };
        var service = new RejectEvidenceService(repository, lookup, lookup, new FakeAuditLogWriter(), clock, new ReviewEvidenceRequestValidator());

        var result = await service.ExecuteAsync(evidence.Id, new ReviewEvidenceRequest
        {
            ReviewerUserId = reviewerId,
            RejectionReason = "Missing approval."
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(EvidenceStatus.Rejected, result.Value.Status);
        Assert.Equal("Missing approval.", result.Value.RejectionReason);
    }

    private sealed class FakeEvidenceRepository : IEvidenceRepository
    {
        public List<AuditEvidence> StoredEvidence { get; } = [];

        public Task AddAsync(AuditEvidence evidence, CancellationToken cancellationToken)
        {
            StoredEvidence.Add(evidence);
            return Task.CompletedTask;
        }

        public Task<AuditEvidence?> GetByIdAsync(Guid evidenceId, CancellationToken cancellationToken)
        {
            return Task.FromResult(StoredEvidence.SingleOrDefault(evidence => evidence.Id == evidenceId));
        }

        public Task<AuditEvidence?> GetByIdForUpdateAsync(Guid evidenceId, CancellationToken cancellationToken)
        {
            return Task.FromResult(StoredEvidence.SingleOrDefault(evidence => evidence.Id == evidenceId));
        }

        public Task<PagedResult<AuditEvidence>> ListAsync(EvidenceQueryParameters queryParameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<AuditEvidence>(StoredEvidence, StoredEvidence.Count, 1, 20));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEvidenceReferenceLookup : IControlLookup, IUserLookup
    {
        public Dictionary<Guid, Guid> ControlOrganizations { get; } = [];

        public Dictionary<Guid, Guid> UserOrganizations { get; } = [];

        public Task<Guid?> GetControlOrganizationIdAsync(Guid controlId, CancellationToken cancellationToken)
        {
            return Task.FromResult(
                ControlOrganizations.TryGetValue(controlId, out var organizationId)
                    ? (Guid?)organizationId
                    : null);
        }

        public Task<Guid?> GetUserOrganizationIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(
                UserOrganizations.TryGetValue(userId, out var organizationId)
                    ? (Guid?)organizationId
                    : null);
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
}
