using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.Common.Pagination;
using AuditFindingEntity = AuditAI.Domain.Entities.AuditFinding;

namespace AuditAI.Application.AuditFindings.Interfaces;

public interface IAuditFindingRepository
{
    Task AddAsync(AuditFindingEntity auditFinding, CancellationToken cancellationToken);

    Task<AuditFindingEntity?> GetByIdAsync(
        Guid auditFindingId,
        Guid organizationId,
        CancellationToken cancellationToken);

    Task<AuditFindingEntity?> GetByIdForUpdateAsync(
        Guid auditFindingId,
        Guid organizationId,
        CancellationToken cancellationToken);

    Task<PagedResult<AuditFindingEntity>> ListAsync(
        Guid organizationId,
        AuditFindingQueryParameters queryParameters,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
