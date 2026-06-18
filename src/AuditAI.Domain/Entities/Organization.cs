using AuditAI.Domain.Common;

namespace AuditAI.Domain.Entities;

public sealed class Organization : Entity
{
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

    public static Organization Create(Guid id, string name, DateTimeOffset createdAt)
    {
        return new Organization(id, name, createdAt);
    }

    public void Rename(string name, DateTimeOffset updatedAt)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        UpdatedAt = updatedAt;
    }
}
