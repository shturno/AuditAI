using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.AuditLogs.Mappers;
using AuditAI.Application.Common.Results;

namespace AuditAI.Application.AuditLogs.Services;

public sealed class GetAuditLogByIdService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public GetAuditLogByIdService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result<AuditLogResponse>> ExecuteAsync(
        Guid auditLogId,
        CancellationToken cancellationToken = default)
    {
        var auditLog = await _auditLogRepository.GetByIdAsync(auditLogId, cancellationToken);
        if (auditLog is null)
        {
            return Result<AuditLogResponse>.NotFound("Audit log was not found.");
        }

        return Result<AuditLogResponse>.Success(AuditLogResponseMapper.ToResponse(auditLog));
    }
}
