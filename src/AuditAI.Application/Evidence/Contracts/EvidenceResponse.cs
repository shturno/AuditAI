using AuditAI.Domain.Enums;

namespace AuditAI.Application.Evidence.Contracts;

public sealed record EvidenceResponse(
    Guid Id,
    Guid ControlId,
    Guid SubmittedByUserId,
    Guid? ReviewedByUserId,
    string FileName,
    string StorageReference,
    EvidenceStatus Status,
    string? RejectionReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ReviewedAt);
