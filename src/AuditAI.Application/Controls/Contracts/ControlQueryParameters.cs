using AuditAI.Domain.Enums;

namespace AuditAI.Application.Controls.Contracts;

public sealed class ControlQueryParameters
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? OrganizationId { get; init; }

    public Guid? DepartmentId { get; init; }

    public ControlStatus? Status { get; init; }

    public ControlFrequency? Frequency { get; init; }

    public string? SearchTerm { get; init; }
}
