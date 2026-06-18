using AuditAI.Domain.Common;
using AuditAI.Domain.Enums;
using AuditAI.Domain.Exceptions;

namespace AuditAI.Domain.Entities;

public sealed class ActionPlan : Entity
{
    private ActionPlan(
        Guid id,
        Guid auditFindingId,
        Guid assignedToUserId,
        string title,
        string description,
        DateTimeOffset dueDate,
        DateTimeOffset createdAt)
        : base(id)
    {
        if (dueDate < createdAt)
        {
            throw new DomainRuleViolationException("Action plan due date cannot be earlier than the creation date.");
        }

        AuditFindingId = Guard.AgainstEmpty(auditFindingId, nameof(auditFindingId));
        AssignedToUserId = Guard.AgainstEmpty(assignedToUserId, nameof(assignedToUserId));
        Title = Guard.AgainstNullOrWhiteSpace(title, nameof(title));
        Description = Guard.AgainstNullOrWhiteSpace(description, nameof(description));
        DueDate = dueDate;
        Status = ActionPlanStatus.Open;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid AuditFindingId { get; }

    public Guid AssignedToUserId { get; private set; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public DateTimeOffset DueDate { get; private set; }

    public ActionPlanStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ActionPlan Create(
        Guid id,
        Guid auditFindingId,
        Guid assignedToUserId,
        string title,
        string description,
        DateTimeOffset dueDate,
        DateTimeOffset createdAt)
    {
        return new ActionPlan(id, auditFindingId, assignedToUserId, title, description, dueDate, createdAt);
    }

    public void UpdateDetails(
        Guid assignedToUserId,
        string title,
        string description,
        DateTimeOffset dueDate,
        DateTimeOffset updatedAt)
    {
        if (dueDate < CreatedAt)
        {
            throw new DomainRuleViolationException("Action plan due date cannot be earlier than the creation date.");
        }

        AssignedToUserId = Guard.AgainstEmpty(assignedToUserId, nameof(assignedToUserId));
        Title = Guard.AgainstNullOrWhiteSpace(title, nameof(title));
        Description = Guard.AgainstNullOrWhiteSpace(description, nameof(description));
        DueDate = dueDate;
        UpdatedAt = updatedAt;
    }

    public void MarkInProgress(DateTimeOffset updatedAt)
    {
        Status = ActionPlanStatus.InProgress;
        UpdatedAt = updatedAt;
    }

    public void Complete(DateTimeOffset updatedAt)
    {
        Status = ActionPlanStatus.Completed;
        UpdatedAt = updatedAt;
    }

    public void MarkOverdue(DateTimeOffset updatedAt)
    {
        Status = ActionPlanStatus.Overdue;
        UpdatedAt = updatedAt;
    }

    public void Cancel(DateTimeOffset updatedAt)
    {
        Status = ActionPlanStatus.Cancelled;
        UpdatedAt = updatedAt;
    }

    public bool IsOpenForResolution()
    {
        return Status is ActionPlanStatus.Open or ActionPlanStatus.InProgress or ActionPlanStatus.Overdue;
    }
}
