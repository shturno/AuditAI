using AuditAI.Domain.Common;
using AuditAI.Domain.Enums;
using AuditAI.Domain.Exceptions;

namespace AuditAI.Domain.Entities;

public sealed class Control : Entity
{
    private readonly List<Evidence> _evidenceItems = [];
    private readonly List<AuditFinding> _auditFindings = [];

    private Control(
        Guid id,
        Guid organizationId,
        Guid? departmentId,
        string code,
        string title,
        string? description,
        ControlStatus status,
        ControlFrequency frequency,
        DateTimeOffset createdAt)
        : base(id)
    {
        OrganizationId = Guard.AgainstEmpty(organizationId, nameof(organizationId));
        DepartmentId = departmentId;
        Code = Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Title = Guard.AgainstNullOrWhiteSpace(title, nameof(title));
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Status = status;
        Frequency = frequency;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid OrganizationId { get; }

    public Guid? DepartmentId { get; private set; }

    public string Code { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public ControlStatus Status { get; private set; }

    public ControlFrequency Frequency { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<Evidence> EvidenceItems => _evidenceItems.AsReadOnly();

    public IReadOnlyCollection<AuditFinding> AuditFindings => _auditFindings.AsReadOnly();

    public static Control Create(
        Guid id,
        Guid organizationId,
        Guid? departmentId,
        string code,
        string title,
        string? description,
        ControlFrequency frequency,
        DateTimeOffset createdAt)
    {
        return new Control(
            id,
            organizationId,
            departmentId,
            code,
            title,
            description,
            ControlStatus.Active,
            frequency,
            createdAt);
    }

    public void UpdateDetails(
        string code,
        string title,
        string? description,
        ControlFrequency frequency,
        DateTimeOffset updatedAt)
    {
        Code = Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Title = Guard.AgainstNullOrWhiteSpace(title, nameof(title));
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Frequency = frequency;
        UpdatedAt = updatedAt;
    }

    public void AssignDepartment(Guid? departmentId, DateTimeOffset updatedAt)
    {
        DepartmentId = departmentId;
        UpdatedAt = updatedAt;
    }

    public void Activate(DateTimeOffset updatedAt)
    {
        Status = ControlStatus.Active;
        UpdatedAt = updatedAt;
    }

    public void Deactivate(DateTimeOffset updatedAt)
    {
        Status = ControlStatus.Inactive;
        UpdatedAt = updatedAt;
    }

    public void AddEvidence(Evidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        if (evidence.ControlId != Id)
        {
            throw new DomainRuleViolationException("Evidence must belong to this control.");
        }

        _evidenceItems.Add(evidence);
        UpdatedAt = evidence.UpdatedAt;
    }

    public void AddFinding(AuditFinding auditFinding)
    {
        ArgumentNullException.ThrowIfNull(auditFinding);

        if (auditFinding.ControlId != Id)
        {
            throw new DomainRuleViolationException("Audit finding must belong to this control.");
        }

        _auditFindings.Add(auditFinding);
        UpdatedAt = auditFinding.UpdatedAt;
    }
}
