namespace AuditAI.Application.Evidence.Contracts;

public sealed class ReviewEvidenceRequest
{
    public Guid ReviewerUserId { get; init; }

    public string? RejectionReason { get; init; }
}
