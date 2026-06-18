using AuditAI.Domain.Enums;

namespace AuditAI.Application.Controls.Contracts;

public sealed class UpdateControlRequest
{
    public Guid? DepartmentId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public ControlFrequency Frequency { get; init; }
}
