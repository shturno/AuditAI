using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.Common.Abstractions;
using AuditAuditLog = AuditAI.Domain.Entities.AuditLog;

namespace AuditAI.Application.AuditLogs.Services;

internal sealed class AuditLogWriter : IAuditLogWriter
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuditLogWriter(
        IAuditLogRepository auditLogRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _auditLogRepository = auditLogRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task WriteAsync(AuditLogWriteEntry entry, CancellationToken cancellationToken)
    {
        var auditLog = AuditAuditLog.Create(
            Guid.NewGuid(),
            entry.OrganizationId,
            entry.UserId,
            entry.Action,
            entry.EntityName,
            entry.EntityId,
            entry.Metadata,
            _dateTimeProvider.UtcNow);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
    }
}
