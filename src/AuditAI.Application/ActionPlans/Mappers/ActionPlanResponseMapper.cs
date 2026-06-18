using AuditAI.Application.ActionPlans.Contracts;
using AuditActionPlan = AuditAI.Domain.Entities.ActionPlan;

namespace AuditAI.Application.ActionPlans.Mappers;

internal static class ActionPlanResponseMapper
{
    public static ActionPlanResponse ToResponse(AuditActionPlan actionPlan)
    {
        return new ActionPlanResponse(
            actionPlan.Id,
            actionPlan.AuditFindingId,
            actionPlan.AssignedToUserId,
            actionPlan.Title,
            actionPlan.Description,
            actionPlan.DueDate,
            actionPlan.Status,
            actionPlan.CreatedAt,
            actionPlan.UpdatedAt);
    }

    public static ActionPlanListItemResponse ToListItem(AuditActionPlan actionPlan)
    {
        return new ActionPlanListItemResponse(
            actionPlan.Id,
            actionPlan.AuditFindingId,
            actionPlan.AssignedToUserId,
            actionPlan.Title,
            actionPlan.DueDate,
            actionPlan.Status,
            actionPlan.UpdatedAt);
    }
}
