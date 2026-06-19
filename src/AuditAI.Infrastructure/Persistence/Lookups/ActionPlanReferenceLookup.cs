using AuditAI.Application.ActionPlans.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuditAI.Infrastructure.Persistence.Lookups;

internal sealed class ActionPlanReferenceLookup : IAuditFindingLookup, IUserLookup
{
    private readonly AuditAIDbContext _dbContext;

    public ActionPlanReferenceLookup(AuditAIDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid?> GetFindingOrganizationIdAsync(Guid auditFindingId, CancellationToken cancellationToken)
    {
        return await _dbContext.AuditFindings
            .AsNoTracking()
            .Where(auditFinding => auditFinding.Id == auditFindingId)
            .Join(
                _dbContext.Controls.AsNoTracking(),
                auditFinding => auditFinding.ControlId,
                control => control.Id,
                (_, control) => (Guid?)control.OrganizationId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> GetUserOrganizationIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => (Guid?)user.OrganizationId)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
