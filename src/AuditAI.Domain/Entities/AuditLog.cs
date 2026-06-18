using AuditAI.Domain.Common;
using AuditAI.Domain.Enums;

namespace AuditAI.Domain.Entities;

public sealed class AuditLog : Entity
{
    private AuditLog(
        Guid id,
        Guid organizationId,
        Guid? userId,
        AuditLogAction action,
        string entityName,
        Guid entityId,
        string? metadata,
        DateTimeOffset timestamp)
        : base(id)
    {
        OrganizationId = Guard.AgainstEmpty(organizationId, nameof(organizationId));
        UserId = userId;
        Action = action;
        EntityName = Guard.AgainstNullOrWhiteSpace(entityName, nameof(entityName));
        EntityId = Guard.AgainstEmpty(entityId, nameof(entityId));
        Metadata = string.IsNullOrWhiteSpace(metadata) ? null : metadata.Trim();
        Timestamp = timestamp;
    }

    public Guid OrganizationId { get; }

    public Guid? UserId { get; }

    public AuditLogAction Action { get; }

    public string EntityName { get; }

    public Guid EntityId { get; }

    public string? Metadata { get; }

    public DateTimeOffset Timestamp { get; }

    public static AuditLog Create(
        Guid id,
        Guid organizationId,
        Guid? userId,
        AuditLogAction action,
        string entityName,
        Guid entityId,
        string? metadata,
        DateTimeOffset timestamp)
    {
        return new AuditLog(id, organizationId, userId, action, entityName, entityId, metadata, timestamp);
    }
}
