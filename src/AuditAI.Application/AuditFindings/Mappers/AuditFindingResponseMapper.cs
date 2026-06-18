using AuditAI.Application.AuditFindings.Contracts;
using AuditFindingEntity = AuditAI.Domain.Entities.AuditFinding;

namespace AuditAI.Application.AuditFindings.Mappers;

internal static class AuditFindingResponseMapper
{
    public static AuditFindingResponse ToResponse(AuditFindingEntity auditFinding)
    {
        return new AuditFindingResponse(
            auditFinding.Id,
            auditFinding.ControlId,
            auditFinding.CreatedByUserId,
            auditFinding.Title,
            auditFinding.Description,
            auditFinding.Severity,
            auditFinding.Status,
            auditFinding.CreatedAt,
            auditFinding.UpdatedAt,
            auditFinding.ResolvedAt);
    }

    public static AuditFindingListItemResponse ToListItem(AuditFindingEntity auditFinding)
    {
        return new AuditFindingListItemResponse(
            auditFinding.Id,
            auditFinding.ControlId,
            auditFinding.CreatedByUserId,
            auditFinding.Title,
            auditFinding.Severity,
            auditFinding.Status,
            auditFinding.CreatedAt,
            auditFinding.UpdatedAt);
    }
}
