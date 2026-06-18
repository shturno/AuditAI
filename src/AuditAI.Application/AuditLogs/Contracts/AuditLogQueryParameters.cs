using AuditAI.Domain.Enums;

namespace AuditAI.Application.AuditLogs.Contracts;

public sealed class AuditLogQueryParameters
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? OrganizationId { get; init; }

    public Guid? UserId { get; init; }

    public string? EntityName { get; init; }

    public Guid? EntityId { get; init; }

    public AuditLogAction? Action { get; init; }

    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }
}
