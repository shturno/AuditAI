namespace AuditAI.Application.ActionPlans.Contracts;

public sealed class CreateActionPlanRequest
{
    public Guid AuditFindingId { get; init; }

    public Guid AssignedToUserId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public DateTimeOffset DueDate { get; init; }
}
