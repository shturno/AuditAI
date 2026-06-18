using AuditAI.Domain.Common;
using AuditAI.Domain.Enums;
using AuditAI.Domain.Exceptions;

namespace AuditAI.Domain.Entities;

public sealed class AuditFinding : Entity
{
    private readonly List<ActionPlan> _actionPlans = [];

    private AuditFinding(
        Guid id,
        Guid controlId,
        Guid createdByUserId,
        string title,
        string description,
        AuditFindingSeverity severity,
        DateTimeOffset createdAt)
        : base(id)
    {
        ControlId = Guard.AgainstEmpty(controlId, nameof(controlId));
        CreatedByUserId = Guard.AgainstEmpty(createdByUserId, nameof(createdByUserId));
        Title = Guard.AgainstNullOrWhiteSpace(title, nameof(title));
        Description = Guard.AgainstNullOrWhiteSpace(description, nameof(description));
        Severity = severity;
        Status = AuditFindingStatus.Open;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid ControlId { get; }

    public Guid CreatedByUserId { get; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public AuditFindingSeverity Severity { get; private set; }

    public AuditFindingStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public IReadOnlyCollection<ActionPlan> ActionPlans => _actionPlans.AsReadOnly();

    public static AuditFinding Create(
        Guid id,
        Guid controlId,
        Guid createdByUserId,
        string title,
        string description,
        AuditFindingSeverity severity,
        DateTimeOffset createdAt)
    {
        return new AuditFinding(id, controlId, createdByUserId, title, description, severity, createdAt);
    }

    public void UpdateDetails(
        string title,
        string description,
        AuditFindingSeverity severity,
        DateTimeOffset updatedAt)
    {
        Title = Guard.AgainstNullOrWhiteSpace(title, nameof(title));
        Description = Guard.AgainstNullOrWhiteSpace(description, nameof(description));
        Severity = severity;
        UpdatedAt = updatedAt;
    }

    public void MarkInProgress(DateTimeOffset updatedAt)
    {
        Status = AuditFindingStatus.InProgress;
        UpdatedAt = updatedAt;
    }

    public void Resolve(DateTimeOffset resolvedAt)
    {
        if (Severity == AuditFindingSeverity.Critical &&
            _actionPlans.Any(static plan => plan.IsOpenForResolution()))
        {
            throw new DomainRuleViolationException(
                "A critical finding cannot be resolved while it has open action plans.");
        }

        Status = AuditFindingStatus.Resolved;
        ResolvedAt = resolvedAt;
        UpdatedAt = resolvedAt;
    }

    public void Cancel(DateTimeOffset updatedAt)
    {
        Status = AuditFindingStatus.Cancelled;
        UpdatedAt = updatedAt;
    }

    public void AddActionPlan(ActionPlan actionPlan)
    {
        ArgumentNullException.ThrowIfNull(actionPlan);

        if (actionPlan.AuditFindingId != Id)
        {
            throw new DomainRuleViolationException("Action plan must belong to this audit finding.");
        }

        _actionPlans.Add(actionPlan);
        UpdatedAt = actionPlan.UpdatedAt;
    }
}
