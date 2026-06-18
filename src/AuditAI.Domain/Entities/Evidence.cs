using AuditAI.Domain.Common;
using AuditAI.Domain.Enums;
using AuditAI.Domain.Exceptions;

namespace AuditAI.Domain.Entities;

public sealed class Evidence : Entity
{
    private Evidence(
        Guid id,
        Guid controlId,
        Guid submittedByUserId,
        string fileName,
        string storageReference,
        DateTimeOffset createdAt)
        : base(id)
    {
        ControlId = Guard.AgainstEmpty(controlId, nameof(controlId));
        SubmittedByUserId = Guard.AgainstEmpty(submittedByUserId, nameof(submittedByUserId));
        FileName = Guard.AgainstNullOrWhiteSpace(fileName, nameof(fileName));
        StorageReference = Guard.AgainstNullOrWhiteSpace(storageReference, nameof(storageReference));
        Status = EvidenceStatus.Pending;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid ControlId { get; }

    public Guid SubmittedByUserId { get; }

    public Guid? ReviewedByUserId { get; private set; }

    public string FileName { get; }

    public string StorageReference { get; }

    public EvidenceStatus Status { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    public static Evidence Create(
        Guid id,
        Guid controlId,
        Guid submittedByUserId,
        string fileName,
        string storageReference,
        DateTimeOffset createdAt)
    {
        return new Evidence(id, controlId, submittedByUserId, fileName, storageReference, createdAt);
    }

    public void Accept(Guid reviewerUserId, DateTimeOffset reviewedAt)
    {
        EnsurePendingForReview();

        ReviewedByUserId = Guard.AgainstEmpty(reviewerUserId, nameof(reviewerUserId));
        ReviewedAt = reviewedAt;
        RejectionReason = null;
        Status = EvidenceStatus.Accepted;
        UpdatedAt = reviewedAt;
    }

    public void Reject(Guid reviewerUserId, string rejectionReason, DateTimeOffset reviewedAt)
    {
        EnsurePendingForReview();

        ReviewedByUserId = Guard.AgainstEmpty(reviewerUserId, nameof(reviewerUserId));
        RejectionReason = Guard.AgainstNullOrWhiteSpace(rejectionReason, nameof(rejectionReason));
        ReviewedAt = reviewedAt;
        Status = EvidenceStatus.Rejected;
        UpdatedAt = reviewedAt;
    }

    private void EnsurePendingForReview()
    {
        if (Status != EvidenceStatus.Pending)
        {
            throw new DomainRuleViolationException("Only pending evidence can be reviewed.");
        }
    }
}
