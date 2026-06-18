using AuditAI.Domain.Common;
using AuditAI.Domain.Enums;

namespace AuditAI.Domain.Entities;

public sealed class User : Entity
{
    private User(
        Guid id,
        Guid organizationId,
        Guid? departmentId,
        string fullName,
        string email,
        string passwordHash,
        UserRole role,
        DateTimeOffset createdAt)
        : base(id)
    {
        OrganizationId = Guard.AgainstEmpty(organizationId, nameof(organizationId));
        DepartmentId = departmentId;
        FullName = Guard.AgainstNullOrWhiteSpace(fullName, nameof(fullName));
        Email = Guard.AgainstNullOrWhiteSpace(email, nameof(email)).ToLowerInvariant();
        PasswordHash = Guard.AgainstNullOrWhiteSpace(passwordHash, nameof(passwordHash));
        Role = role;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid OrganizationId { get; }

    public Guid? DepartmentId { get; private set; }

    public string FullName { get; private set; }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static User Create(
        Guid id,
        Guid organizationId,
        Guid? departmentId,
        string fullName,
        string email,
        string passwordHash,
        UserRole role,
        DateTimeOffset createdAt)
    {
        return new User(id, organizationId, departmentId, fullName, email, passwordHash, role, createdAt);
    }

    public void Rename(string fullName, DateTimeOffset updatedAt)
    {
        FullName = Guard.AgainstNullOrWhiteSpace(fullName, nameof(fullName));
        UpdatedAt = updatedAt;
    }

    public void ChangeEmail(string email, DateTimeOffset updatedAt)
    {
        Email = Guard.AgainstNullOrWhiteSpace(email, nameof(email)).ToLowerInvariant();
        UpdatedAt = updatedAt;
    }

    public void ChangePasswordHash(string passwordHash, DateTimeOffset updatedAt)
    {
        PasswordHash = Guard.AgainstNullOrWhiteSpace(passwordHash, nameof(passwordHash));
        UpdatedAt = updatedAt;
    }

    public void ChangeRole(UserRole role, DateTimeOffset updatedAt)
    {
        Role = role;
        UpdatedAt = updatedAt;
    }

    public void AssignDepartment(Guid departmentId, DateTimeOffset updatedAt)
    {
        DepartmentId = Guard.AgainstEmpty(departmentId, nameof(departmentId));
        UpdatedAt = updatedAt;
    }

    public void ClearDepartment(DateTimeOffset updatedAt)
    {
        DepartmentId = null;
        UpdatedAt = updatedAt;
    }
}
