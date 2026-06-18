using AuditAI.Application.ActionPlans.Contracts;
using AuditAI.Application.Common.Pagination;
using AuditActionPlan = AuditAI.Domain.Entities.ActionPlan;

namespace AuditAI.Application.ActionPlans.Interfaces;

public interface IActionPlanRepository
{
    Task AddAsync(AuditActionPlan actionPlan, CancellationToken cancellationToken);

    Task<AuditActionPlan?> GetByIdAsync(Guid actionPlanId, CancellationToken cancellationToken);

    Task<AuditActionPlan?> GetByIdForUpdateAsync(Guid actionPlanId, CancellationToken cancellationToken);

    Task<PagedResult<AuditActionPlan>> ListAsync(ActionPlanQueryParameters queryParameters, CancellationToken cancellationToken);

    Task<bool> HasBlockingActionPlansForFindingAsync(Guid auditFindingId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
