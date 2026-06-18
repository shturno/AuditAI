namespace AuditAI.Application.Evidence.Interfaces;

public interface IUserLookup
{
    Task<Guid?> GetUserOrganizationIdAsync(Guid userId, CancellationToken cancellationToken);
}
