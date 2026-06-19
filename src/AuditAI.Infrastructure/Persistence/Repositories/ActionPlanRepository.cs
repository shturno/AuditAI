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

    public async Task<AuditActionPlan?> GetByIdAsync(
        Guid actionPlanId,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        return await (
            from actionPlan in _dbContext.ActionPlans.AsNoTracking()
            join auditFinding in _dbContext.AuditFindings.AsNoTracking()
                on actionPlan.AuditFindingId equals auditFinding.Id
            join control in _dbContext.Controls.AsNoTracking()
                on auditFinding.ControlId equals control.Id
            where actionPlan.Id == actionPlanId &&
                  control.OrganizationId == organizationId
            select actionPlan)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<AuditActionPlan?> GetByIdForUpdateAsync(
        Guid actionPlanId,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        return await (
            from actionPlan in _dbContext.ActionPlans
            join auditFinding in _dbContext.AuditFindings
                on actionPlan.AuditFindingId equals auditFinding.Id
            join control in _dbContext.Controls
                on auditFinding.ControlId equals control.Id
            where actionPlan.Id == actionPlanId &&
                  control.OrganizationId == organizationId
            select actionPlan)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditActionPlan>> ListAsync(
        Guid organizationId,
        ActionPlanQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var query = from actionPlan in _dbContext.ActionPlans.AsNoTracking()
                    join auditFinding in _dbContext.AuditFindings.AsNoTracking()
                        on actionPlan.AuditFindingId equals auditFinding.Id
                    join control in _dbContext.Controls.AsNoTracking()
                        on auditFinding.ControlId equals control.Id
                    where control.OrganizationId == organizationId
                    select actionPlan;

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

    public async Task<bool> HasBlockingActionPlansForFindingAsync(
        Guid organizationId,
        Guid auditFindingId,
        CancellationToken cancellationToken)
    {
        return await (
            from actionPlan in _dbContext.ActionPlans.AsNoTracking()
            join auditFinding in _dbContext.AuditFindings.AsNoTracking()
                on actionPlan.AuditFindingId equals auditFinding.Id
            join control in _dbContext.Controls.AsNoTracking()
                on auditFinding.ControlId equals control.Id
            where actionPlan.AuditFindingId == auditFindingId &&
                  control.OrganizationId == organizationId &&
                  (actionPlan.Status == ActionPlanStatus.Open ||
                   actionPlan.Status == ActionPlanStatus.InProgress ||
                   actionPlan.Status == ActionPlanStatus.Overdue)
            select actionPlan)
            .AnyAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
