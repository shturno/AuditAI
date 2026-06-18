using AuditAI.Domain.Enums;

namespace AuditAI.Application.Evidence.Contracts;

public sealed class EvidenceQueryParameters
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? ControlId { get; init; }

    public Guid? SubmittedByUserId { get; init; }

    public EvidenceStatus? Status { get; init; }
}
