using AuditAI.Domain.Enums;

namespace AuditAI.Application.Evidence.Contracts;

public sealed record EvidenceListItemResponse(
    Guid Id,
    Guid ControlId,
    Guid SubmittedByUserId,
    string FileName,
    EvidenceStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
