using AuditAI.Domain.Enums;

namespace AuditAI.Application.AuditFindings.Contracts;

public sealed record AuditFindingListItemResponse(
    Guid Id,
    Guid ControlId,
    Guid CreatedByUserId,
    string Title,
    AuditFindingSeverity Severity,
    AuditFindingStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
