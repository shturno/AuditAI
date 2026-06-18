using AuditAI.Domain.Enums;

namespace AuditAI.Application.ActionPlans.Contracts;

public sealed record ActionPlanListItemResponse(
    Guid Id,
    Guid AuditFindingId,
    Guid AssignedToUserId,
    string Title,
    DateTimeOffset DueDate,
    ActionPlanStatus Status,
    DateTimeOffset UpdatedAt);
