using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.AuditFindings.Interfaces;
using AuditAI.Application.AuditFindings.Mappers;
using AuditAI.Application.Common.Results;

namespace AuditAI.Application.AuditFindings.Services;

public sealed class GetAuditFindingByIdService
{
    private readonly IAuditFindingRepository _auditFindingRepository;

    public GetAuditFindingByIdService(IAuditFindingRepository auditFindingRepository)
    {
        _auditFindingRepository = auditFindingRepository;
    }

    public async Task<Result<AuditFindingResponse>> ExecuteAsync(Guid auditFindingId, CancellationToken cancellationToken = default)
    {
        var auditFinding = await _auditFindingRepository.GetByIdAsync(auditFindingId, cancellationToken);
        if (auditFinding is null)
        {
            return Result<AuditFindingResponse>.NotFound("Audit finding was not found.");
        }

        return Result<AuditFindingResponse>.Success(AuditFindingResponseMapper.ToResponse(auditFinding));
    }
}
