using AuditAI.Application.Common.Results;
using AuditAI.Application.Evidence.Contracts;
using AuditAI.Application.Evidence.Interfaces;
using AuditAI.Application.Evidence.Mappers;

namespace AuditAI.Application.Evidence.Services;

public sealed class GetEvidenceByIdService
{
    private readonly IEvidenceRepository _evidenceRepository;

    public GetEvidenceByIdService(IEvidenceRepository evidenceRepository)
    {
        _evidenceRepository = evidenceRepository;
    }

    public async Task<Result<EvidenceResponse>> ExecuteAsync(Guid evidenceId, CancellationToken cancellationToken = default)
    {
        var evidence = await _evidenceRepository.GetByIdAsync(evidenceId, cancellationToken);
        if (evidence is null)
        {
            return Result<EvidenceResponse>.NotFound("Evidence was not found.");
        }

        return Result<EvidenceResponse>.Success(EvidenceResponseMapper.ToResponse(evidence));
    }
}
