using AuditAI.Application.ActionPlans.Contracts;
using AuditAI.Application.Common.Pagination;
using AuditActionPlan = AuditAI.Domain.Entities.ActionPlan;

namespace AuditAI.Application.ActionPlans.Interfaces;

public interface IActionPlanRepository
{
    Task AddAsync(AuditActionPlan actionPlan, CancellationToken cancellationToken);

    Task<AuditActionPlan?> GetByIdAsync(
        Guid actionPlanId,
        Guid organizationId,
        CancellationToken cancellationToken);

    Task<AuditActionPlan?> GetByIdForUpdateAsync(
        Guid actionPlanId,
        Guid organizationId,
        CancellationToken cancellationToken);

    Task<PagedResult<AuditActionPlan>> ListAsync(
        Guid organizationId,
        ActionPlanQueryParameters queryParameters,
        CancellationToken cancellationToken);

    Task<bool> HasBlockingActionPlansForFindingAsync(
        Guid organizationId,
        Guid auditFindingId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
