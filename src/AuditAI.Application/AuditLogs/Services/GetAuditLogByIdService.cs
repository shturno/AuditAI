using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.AuditLogs.Mappers;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Authorization;
using AuditAI.Application.Common.Results;

namespace AuditAI.Application.AuditLogs.Services;

public sealed class GetAuditLogByIdService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentUser _currentUser;

    public GetAuditLogByIdService(
        IAuditLogRepository auditLogRepository,
        ICurrentUser currentUser)
    {
        _auditLogRepository = auditLogRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<AuditLogResponse>> ExecuteAsync(
        Guid auditLogId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.OrganizationId.HasValue)
        {
            return Result<AuditLogResponse>.Unauthorized("An authenticated user context is required.");
        }

        if (!RoleAuthorization.CanReadAuditLogs(_currentUser))
        {
            return Result<AuditLogResponse>.Forbidden(RoleAuthorization.AuditLogsReadForbiddenMessage);
        }

        var auditLog = await _auditLogRepository.GetByIdAsync(
            auditLogId,
            _currentUser.OrganizationId.Value,
            cancellationToken);
        if (auditLog is null)
        {
            return Result<AuditLogResponse>.NotFound("Audit log was not found.");
        }

        return Result<AuditLogResponse>.Success(AuditLogResponseMapper.ToResponse(auditLog));
    }
}
