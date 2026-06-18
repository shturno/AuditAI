using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;
using AuditAI.Domain.Exceptions;

namespace AuditAI.UnitTests.Domain;

public sealed class EvidenceTests
{
    [Fact]
    public void Should_RejectEvidence_When_RejectionReasonIsMissing()
    {
        var evidence = Evidence.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "report.pdf",
            "evidence/report.pdf",
            DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(() => evidence.Reject(Guid.NewGuid(), " ", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Should_AllowEvidenceApproval_When_StatusIsPending()
    {
        var reviewerId = Guid.NewGuid();
        var reviewedAt = DateTimeOffset.UtcNow;
        var evidence = Evidence.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "report.pdf",
            "evidence/report.pdf",
            reviewedAt.AddHours(-2));

        evidence.Accept(reviewerId, reviewedAt);

        Assert.Equal(EvidenceStatus.Accepted, evidence.Status);
        Assert.Equal(reviewerId, evidence.ReviewedByUserId);
        Assert.Equal(reviewedAt, evidence.ReviewedAt);
        Assert.Null(evidence.RejectionReason);
    }

    [Fact]
    public void Should_NotAllowEvidenceReview_When_StatusIsNotPending()
    {
        var evidence = Evidence.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "report.pdf",
            "evidence/report.pdf",
            DateTimeOffset.UtcNow);

        evidence.Accept(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Throws<DomainRuleViolationException>(() =>
            evidence.Reject(Guid.NewGuid(), "Missing data", DateTimeOffset.UtcNow));
    }
}
