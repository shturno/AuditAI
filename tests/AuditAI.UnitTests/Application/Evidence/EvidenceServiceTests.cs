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
    public async Task Should_FailCreateEvidence_When_CurrentUserIsNotAuthenticated()
    {
        var service = new CreateEvidenceService(
            new FakeEvidenceRepository(),
            FakeCurrentUser.Unauthenticated(),
            new FakeControlLookup(),
            new FakeAuditLogWriter(),
            new FakeDateTimeProvider(),
            new CreateEvidenceRequestValidator());

        var result = await service.ExecuteAsync(new CreateEvidenceRequest
        {
            ControlId = Guid.NewGuid(),
            FileName = "report.pdf",
            StorageReference = "evidence/report.pdf"
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsUnauthorized);
    }

    [Fact]
    public async Task Should_CreateEvidence_Using_CurrentUserAsSubmitter()
    {
        var controlId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var repository = new FakeEvidenceRepository();
        var writer = new FakeAuditLogWriter();
        var service = new CreateEvidenceService(
            repository,
            new FakeCurrentUser(userId, organizationId),
            new FakeControlLookup { ControlOrganizations = { [controlId] = organizationId } },
            writer,
            new FakeDateTimeProvider(),
            new CreateEvidenceRequestValidator());

        var result = await service.ExecuteAsync(new CreateEvidenceRequest
        {
            ControlId = controlId,
            FileName = "report.pdf",
            StorageReference = "evidence/report.pdf"
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(userId, result.Value.SubmittedByUserId);
        var logEntry = Assert.Single(writer.Entries);
        Assert.Equal(userId, logEntry.UserId);
        Assert.Equal(organizationId, logEntry.OrganizationId);
    }

    [Fact]
    public async Task Should_FailCreateEvidence_When_ControlBelongsToAnotherOrganization()
    {
        var service = new CreateEvidenceService(
            new FakeEvidenceRepository(),
            new FakeCurrentUser(Guid.NewGuid(), Guid.NewGuid()),
            new FakeControlLookup { ControlOrganizations = { [Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")] = Guid.NewGuid() } },
            new FakeAuditLogWriter(),
            new FakeDateTimeProvider(),
            new CreateEvidenceRequestValidator());

        var result = await service.ExecuteAsync(new CreateEvidenceRequest
        {
            ControlId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            FileName = "report.pdf",
            StorageReference = "evidence/report.pdf"
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task Should_ListEvidence_Using_CurrentUserOrganization()
    {
        var organizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();
        var controlId = Guid.NewGuid();
        var otherControlId = Guid.NewGuid();
        var repository = new FakeEvidenceRepository();
        repository.OrganizationByControlId[controlId] = organizationId;
        repository.OrganizationByControlId[otherControlId] = otherOrganizationId;
        repository.StoredEvidence.Add(AuditEvidence.Create(Guid.NewGuid(), controlId, Guid.NewGuid(), "visible.pdf", "evidence/visible.pdf", new FakeDateTimeProvider().UtcNow));
        repository.StoredEvidence.Add(AuditEvidence.Create(Guid.NewGuid(), otherControlId, Guid.NewGuid(), "hidden.pdf", "evidence/hidden.pdf", new FakeDateTimeProvider().UtcNow));

        var result = await new ListEvidenceService(
            repository,
            new FakeCurrentUser(Guid.NewGuid(), organizationId),
            new EvidenceQueryParametersValidator())
            .ExecuteAsync(new EvidenceQueryParameters());

        Assert.True(result.IsSuccess);
        var page = Assert.IsType<PagedResult<EvidenceListItemResponse>>(result.Value);
        Assert.Single(page.Items);
        Assert.Equal("visible.pdf", page.Items[0].FileName);
        Assert.Equal(organizationId, repository.LastOrganizationId);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_GettingEvidenceFromAnotherOrganization()
    {
        var controlId = Guid.NewGuid();
        var evidence = AuditEvidence.Create(Guid.NewGuid(), controlId, Guid.NewGuid(), "report.pdf", "evidence/report.pdf", new FakeDateTimeProvider().UtcNow);
        var repository = new FakeEvidenceRepository();
        repository.StoredEvidence.Add(evidence);
        var lookup = new FakeControlLookup { ControlOrganizations = { [controlId] = Guid.NewGuid() } };

        var result = await new GetEvidenceByIdService(
            repository,
            new FakeCurrentUser(Guid.NewGuid(), Guid.NewGuid()),
            lookup)
            .ExecuteAsync(evidence.Id);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task Should_AcceptEvidence_Using_CurrentUserAsReviewer()
    {
        var clock = new FakeDateTimeProvider();
        var controlId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var evidence = AuditEvidence.Create(Guid.NewGuid(), controlId, Guid.NewGuid(), "report.pdf", "evidence/report.pdf", clock.UtcNow);
        var repository = new FakeEvidenceRepository();
        repository.StoredEvidence.Add(evidence);
        var writer = new FakeAuditLogWriter();

        var result = await new AcceptEvidenceService(
            repository,
            new FakeCurrentUser(userId, organizationId),
            new FakeControlLookup { ControlOrganizations = { [controlId] = organizationId } },
            writer,
            clock,
            new ReviewEvidenceRequestValidator())
            .ExecuteAsync(evidence.Id, new ReviewEvidenceRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(EvidenceStatus.Accepted, result.Value!.Status);
        Assert.Equal(userId, result.Value.ReviewedByUserId);
        Assert.Equal(userId, Assert.Single(writer.Entries).UserId);
    }

    [Fact]
    public async Task Should_RejectEvidence_Using_CurrentUserAsReviewer()
    {
        var clock = new FakeDateTimeProvider();
        var controlId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var evidence = AuditEvidence.Create(Guid.NewGuid(), controlId, Guid.NewGuid(), "report.pdf", "evidence/report.pdf", clock.UtcNow);
        var repository = new FakeEvidenceRepository();
        repository.StoredEvidence.Add(evidence);
        var writer = new FakeAuditLogWriter();

        var result = await new RejectEvidenceService(
            repository,
            new FakeCurrentUser(userId, organizationId),
            new FakeControlLookup { ControlOrganizations = { [controlId] = organizationId } },
            writer,
            clock,
            new ReviewEvidenceRequestValidator())
            .ExecuteAsync(evidence.Id, new ReviewEvidenceRequest
            {
                RejectionReason = "Missing approval."
            });

        Assert.True(result.IsSuccess);
        Assert.Equal(EvidenceStatus.Rejected, result.Value!.Status);
        Assert.Equal(userId, result.Value.ReviewedByUserId);
        Assert.Equal(userId, Assert.Single(writer.Entries).UserId);
    }

    [Fact]
    public async Task Should_FailRejectEvidence_When_RejectionReasonIsEmpty()
    {
        var result = await new RejectEvidenceService(
            new FakeEvidenceRepository(),
            new FakeCurrentUser(Guid.NewGuid(), Guid.NewGuid()),
            new FakeControlLookup(),
            new FakeAuditLogWriter(),
            new FakeDateTimeProvider(),
            new ReviewEvidenceRequestValidator())
            .ExecuteAsync(Guid.NewGuid(), new ReviewEvidenceRequest
            {
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
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var evidence = AuditEvidence.Create(Guid.NewGuid(), controlId, Guid.NewGuid(), "report.pdf", "evidence/report.pdf", clock.UtcNow);
        evidence.Accept(userId, clock.UtcNow);

        var repository = new FakeEvidenceRepository();
        repository.StoredEvidence.Add(evidence);

        var result = await new RejectEvidenceService(
            repository,
            new FakeCurrentUser(userId, organizationId),
            new FakeControlLookup { ControlOrganizations = { [controlId] = organizationId } },
            new FakeAuditLogWriter(),
            clock,
            new ReviewEvidenceRequestValidator())
            .ExecuteAsync(evidence.Id, new ReviewEvidenceRequest
            {
                RejectionReason = "Missing approval."
            });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "Status");
    }

    private sealed class FakeEvidenceRepository : IEvidenceRepository
    {
        public List<AuditEvidence> StoredEvidence { get; } = [];
        public Dictionary<Guid, Guid> OrganizationByControlId { get; } = [];
        public Guid? LastOrganizationId { get; private set; }

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

        public Task<PagedResult<AuditEvidence>> ListAsync(Guid organizationId, EvidenceQueryParameters queryParameters, CancellationToken cancellationToken)
        {
            LastOrganizationId = organizationId;

            var items = StoredEvidence
                .Where(evidence => OrganizationByControlId.TryGetValue(evidence.ControlId, out var orgId) && orgId == organizationId)
                .ToArray();

            return Task.FromResult(new PagedResult<AuditEvidence>(items, items.Length, 1, 20));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeControlLookup : IControlLookup
    {
        public Dictionary<Guid, Guid> ControlOrganizations { get; } = [];

        public Task<Guid?> GetControlOrganizationIdAsync(Guid controlId, CancellationToken cancellationToken)
        {
            return Task.FromResult(
                ControlOrganizations.TryGetValue(controlId, out var organizationId)
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

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public FakeCurrentUser(Guid userId, Guid organizationId)
        {
            IsAuthenticated = true;
            UserId = userId;
            OrganizationId = organizationId;
        }

        public bool IsAuthenticated { get; }

        public Guid? UserId { get; }

        public string? Email => null;

        public UserRole? Role => UserRole.Auditor;

        public Guid? OrganizationId { get; }

        public Guid? DepartmentId => null;

        public static FakeCurrentUser Unauthenticated() => new();

        private FakeCurrentUser()
        {
            IsAuthenticated = false;
        }
    }
}
