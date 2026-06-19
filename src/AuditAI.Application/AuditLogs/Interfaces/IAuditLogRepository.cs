using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.Common.Pagination;
using AuditAuditLog = AuditAI.Domain.Entities.AuditLog;

namespace AuditAI.Application.AuditLogs.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditAuditLog auditLog, CancellationToken cancellationToken);

    Task<AuditAuditLog?> GetByIdAsync(Guid auditLogId, Guid organizationId, CancellationToken cancellationToken);

    Task<PagedResult<AuditAuditLog>> ListAsync(Guid organizationId, AuditLogQueryParameters queryParameters, CancellationToken cancellationToken);
}
