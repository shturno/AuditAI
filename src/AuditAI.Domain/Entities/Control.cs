using AuditAI.Domain.Common;
using AuditAI.Domain.Enums;

namespace AuditAI.Domain.Entities;

public sealed class Control : Entity
{
    private Control(
        Guid id,
        Guid organizationId,
        Guid? departmentId,
        string code,
        string category,
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
        Category = Guard.AgainstNullOrWhiteSpace(category, nameof(category));
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

    public string Category { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public ControlStatus Status { get; private set; }

    public ControlFrequency Frequency { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Control Create(
        Guid id,
        Guid organizationId,
        Guid? departmentId,
        string code,
        string category,
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
            category,
            title,
            description,
            ControlStatus.Active,
            frequency,
            createdAt);
    }

    public void UpdateDetails(
        string code,
        string category,
        string title,
        string? description,
        ControlFrequency frequency,
        DateTimeOffset updatedAt)
    {
        Code = Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Category = Guard.AgainstNullOrWhiteSpace(category, nameof(category));
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
}
