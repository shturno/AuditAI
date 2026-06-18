using AuditAI.Domain.Enums;

namespace AuditAI.Application.Controls.Contracts;

public sealed record ControlListItemResponse(
    Guid Id,
    Guid OrganizationId,
    Guid? DepartmentId,
    string Code,
    string Category,
    string Title,
    ControlStatus Status,
    ControlFrequency Frequency,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
