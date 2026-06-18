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
        EnsureDueDateIsOnOrAfter(createdAt, dueDate);

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
        EnsureDueDateIsOnOrAfter(CreatedAt, dueDate);

        AssignedToUserId = Guard.AgainstEmpty(assignedToUserId, nameof(assignedToUserId));
        Title = Guard.AgainstNullOrWhiteSpace(title, nameof(title));
        Description = Guard.AgainstNullOrWhiteSpace(description, nameof(description));
        DueDate = dueDate;
        UpdatedAt = updatedAt;
    }

    public void MarkInProgress(DateTimeOffset updatedAt)
    {
        if (Status != ActionPlanStatus.Open)
        {
            throw new DomainRuleViolationException("Only open action plans can be moved to in progress.");
        }

        Status = ActionPlanStatus.InProgress;
        UpdatedAt = updatedAt;
    }

    public void Complete(DateTimeOffset updatedAt)
    {
        if (Status is ActionPlanStatus.Completed or ActionPlanStatus.Cancelled)
        {
            throw new DomainRuleViolationException("Completed or cancelled action plans cannot be completed again.");
        }

        Status = ActionPlanStatus.Completed;
        UpdatedAt = updatedAt;
    }

    public void MarkOverdue(DateTimeOffset updatedAt)
    {
        if (Status is ActionPlanStatus.Completed or ActionPlanStatus.Cancelled)
        {
            throw new DomainRuleViolationException("Completed or cancelled action plans cannot be marked as overdue.");
        }

        if (Status == ActionPlanStatus.Overdue)
        {
            throw new DomainRuleViolationException("Action plan is already overdue.");
        }

        Status = ActionPlanStatus.Overdue;
        UpdatedAt = updatedAt;
    }

    public void Cancel(DateTimeOffset updatedAt)
    {
        if (Status == ActionPlanStatus.Completed)
        {
            throw new DomainRuleViolationException("Completed action plans cannot be cancelled.");
        }

        if (Status == ActionPlanStatus.Cancelled)
        {
            throw new DomainRuleViolationException("Cancelled action plans cannot be cancelled again.");
        }

        Status = ActionPlanStatus.Cancelled;
        UpdatedAt = updatedAt;
    }

    public bool IsOpenForResolution()
    {
        return Status is ActionPlanStatus.Open or ActionPlanStatus.InProgress or ActionPlanStatus.Overdue;
    }

    private static void EnsureDueDateIsOnOrAfter(DateTimeOffset createdAt, DateTimeOffset dueDate)
    {
        if (dueDate < createdAt)
        {
            throw new DomainRuleViolationException("Action plan due date cannot be earlier than the creation date.");
        }
    }
}
