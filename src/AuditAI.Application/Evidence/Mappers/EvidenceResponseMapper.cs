using AuditAI.Application.Evidence.Contracts;
using AuditEvidence = AuditAI.Domain.Entities.Evidence;

namespace AuditAI.Application.Evidence.Mappers;

internal static class EvidenceResponseMapper
{
    public static EvidenceResponse ToResponse(AuditEvidence evidence)
    {
        return new EvidenceResponse(
            evidence.Id,
            evidence.ControlId,
            evidence.SubmittedByUserId,
            evidence.ReviewedByUserId,
            evidence.FileName,
            evidence.StorageReference,
            evidence.Status,
            evidence.RejectionReason,
            evidence.CreatedAt,
            evidence.UpdatedAt,
            evidence.ReviewedAt);
    }

    public static EvidenceListItemResponse ToListItem(AuditEvidence evidence)
    {
        return new EvidenceListItemResponse(
            evidence.Id,
            evidence.ControlId,
            evidence.SubmittedByUserId,
            evidence.FileName,
            evidence.Status,
            evidence.CreatedAt,
            evidence.UpdatedAt);
    }
}
