using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Authorization;
using AuditAI.Application.Evidence.Contracts;
using AuditAI.Application.Evidence.Interfaces;
using AuditAI.Application.Evidence.Mappers;

namespace AuditAI.Application.Evidence.Services;

public sealed class GetEvidenceByIdService
{
    private readonly IEvidenceRepository _evidenceRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IControlLookup _controlLookup;

    public GetEvidenceByIdService(
        IEvidenceRepository evidenceRepository,
        ICurrentUser currentUser,
        IControlLookup controlLookup)
    {
        _evidenceRepository = evidenceRepository;
        _currentUser = currentUser;
        _controlLookup = controlLookup;
    }

    public async Task<Result<EvidenceResponse>> ExecuteAsync(Guid evidenceId, CancellationToken cancellationToken = default)
    {
        if (!EvidenceCurrentUserContext.TryGetOrganization(_currentUser, out var organizationId))
        {
            return Result<EvidenceResponse>.Unauthorized(EvidenceCurrentUserContext.UnauthorizedMessage);
        }

        if (!RoleAuthorization.CanReadEvidence(_currentUser))
        {
            return Result<EvidenceResponse>.Forbidden(RoleAuthorization.EvidenceReadForbiddenMessage);
        }

        var evidence = await _evidenceRepository.GetByIdAsync(evidenceId, cancellationToken);
        if (evidence is null)
        {
            return Result<EvidenceResponse>.NotFound("Evidence was not found.");
        }

        var controlOrganizationId = await _controlLookup.GetControlOrganizationIdAsync(evidence.ControlId, cancellationToken);
        if (!controlOrganizationId.HasValue || controlOrganizationId.Value != organizationId)
        {
            return Result<EvidenceResponse>.NotFound("Evidence was not found.");
        }

        return Result<EvidenceResponse>.Success(EvidenceResponseMapper.ToResponse(evidence));
    }
}
