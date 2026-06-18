using AuditAI.Domain.Enums;

namespace AuditAI.Application.Auth.Contracts;

public sealed record AuthenticatedUserResponse(
    Guid Id,
    Guid OrganizationId,
    Guid? DepartmentId,
    string FullName,
    string Email,
    UserRole Role);
