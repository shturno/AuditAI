using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.Common.Pagination;
using Microsoft.EntityFrameworkCore;
using AuditAuditLog = AuditAI.Domain.Entities.AuditLog;

namespace AuditAI.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly AuditAIDbContext _dbContext;

    public AuditLogRepository(AuditAIDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AuditAuditLog auditLog, CancellationToken cancellationToken)
    {
        await _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task<AuditAuditLog?> GetByIdAsync(Guid auditLogId, Guid organizationId, CancellationToken cancellationToken)
    {
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .SingleOrDefaultAsync(
                auditLog => auditLog.Id == auditLogId &&
                            auditLog.OrganizationId == organizationId,
                cancellationToken);
    }

    public async Task<PagedResult<AuditAuditLog>> ListAsync(
        Guid organizationId,
        AuditLogQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.AuditLogs
            .AsNoTracking()
            .Where(auditLog => auditLog.OrganizationId == organizationId)
            .AsQueryable();

        if (queryParameters.UserId.HasValue)
        {
            query = query.Where(auditLog => auditLog.UserId == queryParameters.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.EntityName))
        {
            query = query.Where(auditLog => auditLog.EntityName == queryParameters.EntityName.Trim());
        }

        if (queryParameters.EntityId.HasValue)
        {
            query = query.Where(auditLog => auditLog.EntityId == queryParameters.EntityId.Value);
        }

        if (queryParameters.Action.HasValue)
        {
            query = query.Where(auditLog => auditLog.Action == queryParameters.Action.Value);
        }

        if (queryParameters.From.HasValue)
        {
            query = query.Where(auditLog => auditLog.Timestamp >= queryParameters.From.Value);
        }

        if (queryParameters.To.HasValue)
        {
            query = query.Where(auditLog => auditLog.Timestamp <= queryParameters.To.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(auditLog => auditLog.Timestamp)
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<AuditAuditLog>(items, totalCount, queryParameters.PageNumber, queryParameters.PageSize);
    }
}
