using AuditAI.Application.AuditLogs.Contracts;

namespace AuditAI.Application.AuditLogs.Interfaces;

public interface IAuditLogWriter
{
    Task WriteAsync(AuditLogWriteEntry entry, CancellationToken cancellationToken);
}
