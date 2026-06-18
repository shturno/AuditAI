namespace AuditAI.Application.ActionPlans.Interfaces;

public interface IAuditFindingLookup
{
    Task<Guid?> GetFindingOrganizationIdAsync(Guid auditFindingId, CancellationToken cancellationToken);
}
