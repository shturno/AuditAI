using AuditAI.Domain.Enums;

namespace AuditAI.Application.AuditLogs.Contracts;

public sealed record AuditLogResponse(
    Guid Id,
    Guid OrganizationId,
    Guid? UserId,
    AuditLogAction Action,
    string EntityName,
    Guid EntityId,
    string? Metadata,
    DateTimeOffset Timestamp);
