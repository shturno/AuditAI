namespace AuditAI.Application.Evidence.Interfaces;

public interface IControlLookup
{
    Task<Guid?> GetControlOrganizationIdAsync(Guid controlId, CancellationToken cancellationToken);
}
