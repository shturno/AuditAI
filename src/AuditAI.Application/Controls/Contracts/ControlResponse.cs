using AuditAI.Domain.Enums;

namespace AuditAI.Application.Controls.Contracts;

public sealed record ControlResponse(
    Guid Id,
    Guid OrganizationId,
    Guid? DepartmentId,
    string Code,
    string Category,
    string Title,
    string? Description,
    ControlStatus Status,
    ControlFrequency Frequency,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
