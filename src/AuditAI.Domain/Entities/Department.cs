using AuditAI.Domain.Common;

namespace AuditAI.Domain.Entities;

public sealed class Department : Entity
{
    private Department(Guid id, Guid organizationId, string name, DateTimeOffset createdAt)
        : base(id)
    {
        OrganizationId = Guard.AgainstEmpty(organizationId, nameof(organizationId));
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid OrganizationId { get; }

    public string Name { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Department Create(Guid id, Guid organizationId, string name, DateTimeOffset createdAt)
    {
        return new Department(id, organizationId, name, createdAt);
    }

    public void Rename(string name, DateTimeOffset updatedAt)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        UpdatedAt = updatedAt;
    }
}
