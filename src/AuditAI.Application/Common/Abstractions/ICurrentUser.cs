using AuditAI.Domain.Enums;

namespace AuditAI.Application.Common.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? Email { get; }

    UserRole? Role { get; }

    Guid? OrganizationId { get; }

    Guid? DepartmentId { get; }
}
