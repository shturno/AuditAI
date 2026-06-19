using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.AuditFindings.Interfaces;
using AuditAI.Application.Common.Pagination;
using Microsoft.EntityFrameworkCore;
using AuditFindingEntity = AuditAI.Domain.Entities.AuditFinding;

namespace AuditAI.Infrastructure.Persistence.Repositories;

internal sealed class AuditFindingRepository : IAuditFindingRepository
{
    private readonly AuditAIDbContext _dbContext;

    public AuditFindingRepository(AuditAIDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AuditFindingEntity auditFinding, CancellationToken cancellationToken)
    {
        await _dbContext.AuditFindings.AddAsync(auditFinding, cancellationToken);
    }

    public async Task<AuditFindingEntity?> GetByIdAsync(
        Guid auditFindingId,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        return await (
            from auditFinding in _dbContext.AuditFindings.AsNoTracking()
            join control in _dbContext.Controls.AsNoTracking()
                on auditFinding.ControlId equals control.Id
            where auditFinding.Id == auditFindingId &&
                  control.OrganizationId == organizationId
            select auditFinding)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<AuditFindingEntity?> GetByIdForUpdateAsync(
        Guid auditFindingId,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        return await (
            from auditFinding in _dbContext.AuditFindings
            join control in _dbContext.Controls
                on auditFinding.ControlId equals control.Id
            where auditFinding.Id == auditFindingId &&
                  control.OrganizationId == organizationId
            select auditFinding)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditFindingEntity>> ListAsync(
        Guid organizationId,
        AuditFindingQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var query = from auditFinding in _dbContext.AuditFindings.AsNoTracking()
                    join control in _dbContext.Controls.AsNoTracking()
                        on auditFinding.ControlId equals control.Id
                    where control.OrganizationId == organizationId
                    select auditFinding;

        if (queryParameters.ControlId.HasValue)
        {
            query = query.Where(auditFinding => auditFinding.ControlId == queryParameters.ControlId.Value);
        }

        if (queryParameters.CreatedByUserId.HasValue)
        {
            query = query.Where(auditFinding => auditFinding.CreatedByUserId == queryParameters.CreatedByUserId.Value);
        }

        if (queryParameters.Severity.HasValue)
        {
            query = query.Where(auditFinding => auditFinding.Severity == queryParameters.Severity.Value);
        }

        if (queryParameters.Status.HasValue)
        {
            query = query.Where(auditFinding => auditFinding.Status == queryParameters.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(auditFinding => auditFinding.CreatedAt)
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<AuditFindingEntity>(items, totalCount, queryParameters.PageNumber, queryParameters.PageSize);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
