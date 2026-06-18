using AuditAI.Domain.Enums;

namespace AuditAI.Application.AuditFindings.Contracts;

public sealed class UpdateAuditFindingRequest
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public AuditFindingSeverity Severity { get; init; }
}
