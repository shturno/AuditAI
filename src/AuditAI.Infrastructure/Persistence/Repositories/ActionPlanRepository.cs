using AuditAI.Application.ActionPlans.Contracts;
using AuditAI.Application.ActionPlans.Interfaces;
using AuditAI.Application.Common.Pagination;
using AuditAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using AuditActionPlan = AuditAI.Domain.Entities.ActionPlan;

namespace AuditAI.Infrastructure.Persistence.Repositories;

internal sealed class ActionPlanRepository : IActionPlanRepository
{
    private readonly AuditAIDbContext _dbContext;

    public ActionPlanRepository(AuditAIDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AuditActionPlan actionPlan, CancellationToken cancellationToken)
    {
        await _dbContext.ActionPlans.AddAsync(actionPlan, cancellationToken);
    }

    public async Task<AuditActionPlan?> GetByIdAsync(Guid actionPlanId, CancellationToken cancellationToken)
    {
        return await _dbContext.ActionPlans
            .AsNoTracking()
            .SingleOrDefaultAsync(actionPlan => actionPlan.Id == actionPlanId, cancellationToken);
    }

    public async Task<AuditActionPlan?> GetByIdForUpdateAsync(Guid actionPlanId, CancellationToken cancellationToken)
    {
        return await _dbContext.ActionPlans
            .SingleOrDefaultAsync(actionPlan => actionPlan.Id == actionPlanId, cancellationToken);
    }

    public async Task<PagedResult<AuditActionPlan>> ListAsync(
        ActionPlanQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ActionPlans
            .AsNoTracking()
            .AsQueryable();

        if (queryParameters.AuditFindingId.HasValue)
        {
            query = query.Where(actionPlan => actionPlan.AuditFindingId == queryParameters.AuditFindingId.Value);
        }

        if (queryParameters.AssignedToUserId.HasValue)
        {
            query = query.Where(actionPlan => actionPlan.AssignedToUserId == queryParameters.AssignedToUserId.Value);
        }

        if (queryParameters.Status.HasValue)
        {
            query = query.Where(actionPlan => actionPlan.Status == queryParameters.Status.Value);
        }

        if (queryParameters.DueBefore.HasValue)
        {
            query = query.Where(actionPlan => actionPlan.DueDate <= queryParameters.DueBefore.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(actionPlan => actionPlan.DueDate)
            .ThenByDescending(actionPlan => actionPlan.UpdatedAt)
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<AuditActionPlan>(items, totalCount, queryParameters.PageNumber, queryParameters.PageSize);
    }

    public async Task<bool> HasBlockingActionPlansForFindingAsync(Guid auditFindingId, CancellationToken cancellationToken)
    {
        return await _dbContext.ActionPlans
            .AsNoTracking()
            .AnyAsync(
                actionPlan => actionPlan.AuditFindingId == auditFindingId &&
                              (actionPlan.Status == ActionPlanStatus.Open ||
                               actionPlan.Status == ActionPlanStatus.InProgress ||
                               actionPlan.Status == ActionPlanStatus.Overdue),
                cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
