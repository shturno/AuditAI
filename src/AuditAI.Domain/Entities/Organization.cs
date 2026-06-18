using AuditAI.Domain.Exceptions;
using AuditAI.Domain.Common;

namespace AuditAI.Domain.Entities;

public sealed class Organization : Entity
{
    private readonly List<Department> _departments = [];
    private readonly List<User> _users = [];
    private readonly List<Control> _controls = [];
    private readonly List<AuditLog> _auditLogs = [];

    private Organization(Guid id, string name, DateTimeOffset createdAt)
        : base(id)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public string Name { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<Department> Departments => _departments.AsReadOnly();

    public IReadOnlyCollection<User> Users => _users.AsReadOnly();

    public IReadOnlyCollection<Control> Controls => _controls.AsReadOnly();

    public IReadOnlyCollection<AuditLog> AuditLogs => _auditLogs.AsReadOnly();

    public static Organization Create(Guid id, string name, DateTimeOffset createdAt)
    {
        return new Organization(id, name, createdAt);
    }

    public void Rename(string name, DateTimeOffset updatedAt)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        UpdatedAt = updatedAt;
    }

    public void RegisterDepartment(Department department)
    {
        ArgumentNullException.ThrowIfNull(department);

        if (department.OrganizationId != Id)
        {
            throw new DomainRuleViolationException("Department must belong to the same organization.");
        }

        _departments.Add(department);
        UpdatedAt = department.UpdatedAt;
    }

    public void RegisterUser(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.OrganizationId != Id)
        {
            throw new DomainRuleViolationException("User must belong to the same organization.");
        }

        _users.Add(user);
        UpdatedAt = user.UpdatedAt;
    }

    public void RegisterControl(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);

        if (control.OrganizationId != Id)
        {
            throw new DomainRuleViolationException("Control must belong to the same organization.");
        }

        _controls.Add(control);
        UpdatedAt = control.UpdatedAt;
    }

    public void RegisterAuditLog(AuditLog auditLog)
    {
        ArgumentNullException.ThrowIfNull(auditLog);

        if (auditLog.OrganizationId != Id)
        {
            throw new DomainRuleViolationException("Audit log must belong to the same organization.");
        }

        _auditLogs.Add(auditLog);
        UpdatedAt = auditLog.Timestamp;
    }
}
