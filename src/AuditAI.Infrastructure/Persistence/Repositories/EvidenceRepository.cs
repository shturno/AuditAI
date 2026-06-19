using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Evidence.Contracts;
using AuditAI.Application.Evidence.Interfaces;
using Microsoft.EntityFrameworkCore;
using AuditEvidence = AuditAI.Domain.Entities.Evidence;

namespace AuditAI.Infrastructure.Persistence.Repositories;

internal sealed class EvidenceRepository : IEvidenceRepository
{
    private readonly AuditAIDbContext _dbContext;

    public EvidenceRepository(AuditAIDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AuditEvidence evidence, CancellationToken cancellationToken)
    {
        await _dbContext.Evidence.AddAsync(evidence, cancellationToken);
    }

    public async Task<AuditEvidence?> GetByIdAsync(Guid evidenceId, CancellationToken cancellationToken)
    {
        return await _dbContext.Evidence
            .AsNoTracking()
            .SingleOrDefaultAsync(evidence => evidence.Id == evidenceId, cancellationToken);
    }

    public async Task<AuditEvidence?> GetByIdForUpdateAsync(Guid evidenceId, CancellationToken cancellationToken)
    {
        return await _dbContext.Evidence
            .SingleOrDefaultAsync(evidence => evidence.Id == evidenceId, cancellationToken);
    }

    public async Task<PagedResult<AuditEvidence>> ListAsync(
        Guid organizationId,
        EvidenceQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Evidence
            .AsNoTracking()
            .Where(evidence =>
                _dbContext.Controls.Any(control =>
                    control.Id == evidence.ControlId &&
                    control.OrganizationId == organizationId))
            .AsQueryable();

        if (queryParameters.ControlId.HasValue)
        {
            query = query.Where(evidence => evidence.ControlId == queryParameters.ControlId.Value);
        }

        if (queryParameters.SubmittedByUserId.HasValue)
        {
            query = query.Where(evidence => evidence.SubmittedByUserId == queryParameters.SubmittedByUserId.Value);
        }

        if (queryParameters.Status.HasValue)
        {
            query = query.Where(evidence => evidence.Status == queryParameters.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(evidence => evidence.CreatedAt)
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<AuditEvidence>(items, totalCount, queryParameters.PageNumber, queryParameters.PageSize);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
