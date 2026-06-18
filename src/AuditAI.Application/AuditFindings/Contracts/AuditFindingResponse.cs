using AuditAI.Domain.Enums;

namespace AuditAI.Application.AuditFindings.Contracts;

public sealed record AuditFindingResponse(
    Guid Id,
    Guid ControlId,
    Guid CreatedByUserId,
    string Title,
    string Description,
    AuditFindingSeverity Severity,
    AuditFindingStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ResolvedAt);
