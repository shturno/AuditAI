using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Evidence.Contracts;
using AuditEvidence = AuditAI.Domain.Entities.Evidence;

namespace AuditAI.Application.Evidence.Interfaces;

public interface IEvidenceRepository
{
    Task AddAsync(AuditEvidence evidence, CancellationToken cancellationToken);

    Task<AuditEvidence?> GetByIdAsync(Guid evidenceId, CancellationToken cancellationToken);

    Task<AuditEvidence?> GetByIdForUpdateAsync(Guid evidenceId, CancellationToken cancellationToken);

    Task<PagedResult<AuditEvidence>> ListAsync(
        Guid organizationId,
        EvidenceQueryParameters queryParameters,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
