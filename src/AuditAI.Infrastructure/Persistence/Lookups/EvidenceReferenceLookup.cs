using AuditAI.Application.Evidence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuditAI.Infrastructure.Persistence.Lookups;

internal sealed class EvidenceReferenceLookup : IControlLookup, IUserLookup
{
    private readonly AuditAIDbContext _dbContext;

    public EvidenceReferenceLookup(AuditAIDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid?> GetControlOrganizationIdAsync(Guid controlId, CancellationToken cancellationToken)
    {
        return await _dbContext.Controls
            .AsNoTracking()
            .Where(control => control.Id == controlId)
            .Select(control => (Guid?)control.OrganizationId)
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
