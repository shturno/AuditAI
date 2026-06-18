namespace AuditAI.Application.Controls.Interfaces;

public interface IOrganizationLookup
{
    Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken);
}
