namespace AuditAI.Application.ActionPlans.Interfaces;

public interface IUserLookup
{
    Task<Guid?> GetUserOrganizationIdAsync(Guid userId, CancellationToken cancellationToken);
}
