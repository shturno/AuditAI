using AuditAI.Domain.Enums;

namespace AuditAI.Application.ActionPlans.Contracts;

public sealed class ChangeActionPlanStatusRequest
{
    public ActionPlanStatus Status { get; init; }
}
