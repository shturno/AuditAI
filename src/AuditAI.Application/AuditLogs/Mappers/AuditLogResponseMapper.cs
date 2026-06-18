using AuditAI.Application.AuditLogs.Contracts;
using AuditAuditLog = AuditAI.Domain.Entities.AuditLog;

namespace AuditAI.Application.AuditLogs.Mappers;

internal static class AuditLogResponseMapper
{
    public static AuditLogResponse ToResponse(AuditAuditLog auditLog)
    {
        return new AuditLogResponse(
            auditLog.Id,
            auditLog.OrganizationId,
            auditLog.UserId,
            auditLog.Action,
            auditLog.EntityName,
            auditLog.EntityId,
            auditLog.Metadata,
            auditLog.Timestamp);
    }

    public static AuditLogListItemResponse ToListItem(AuditAuditLog auditLog)
    {
        return new AuditLogListItemResponse(
            auditLog.Id,
            auditLog.OrganizationId,
            auditLog.UserId,
            auditLog.Action,
            auditLog.EntityName,
            auditLog.EntityId,
            auditLog.Timestamp);
    }
}
