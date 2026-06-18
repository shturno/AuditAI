using AuditAI.Domain.Enums;

namespace AuditAI.Application.ActionPlans.Contracts;

public sealed record ActionPlanResponse(
    Guid Id,
    Guid AuditFindingId,
    Guid AssignedToUserId,
    string Title,
    string Description,
    DateTimeOffset DueDate,
    ActionPlanStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
