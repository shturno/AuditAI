using AuditAI.Domain.Enums;

namespace AuditAI.Application.AuditFindings.Contracts;

public sealed class ChangeAuditFindingStatusRequest
{
    public AuditFindingStatus Status { get; init; }
}
