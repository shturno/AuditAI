using AuditAI.Domain.Enums;

namespace AuditAI.Application.ActionPlans.Contracts;

public sealed class ActionPlanQueryParameters
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? AuditFindingId { get; init; }

    public Guid? AssignedToUserId { get; init; }

    public ActionPlanStatus? Status { get; init; }

    public DateTimeOffset? DueBefore { get; init; }
}
