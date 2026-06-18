using AuditAI.Domain.Enums;

namespace AuditAI.Application.AuditFindings.Contracts;

public sealed class AuditFindingQueryParameters
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? ControlId { get; init; }

    public Guid? CreatedByUserId { get; init; }

    public AuditFindingSeverity? Severity { get; init; }

    public AuditFindingStatus? Status { get; init; }
}
