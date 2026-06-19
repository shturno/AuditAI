using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.AuditFindings.Interfaces;
using AuditAI.Application.AuditFindings.Mappers;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Results;

namespace AuditAI.Application.AuditFindings.Services;

public sealed class GetAuditFindingByIdService
{
    private readonly IAuditFindingRepository _auditFindingRepository;
    private readonly ICurrentUser _currentUser;

    public GetAuditFindingByIdService(
        IAuditFindingRepository auditFindingRepository,
        ICurrentUser currentUser)
    {
        _auditFindingRepository = auditFindingRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<AuditFindingResponse>> ExecuteAsync(Guid auditFindingId, CancellationToken cancellationToken = default)
    {
        if (!AuditFindingsCurrentUserContext.TryGetOrganization(_currentUser, out var organizationId))
        {
            return Result<AuditFindingResponse>.Unauthorized(AuditFindingsCurrentUserContext.UnauthorizedMessage);
        }

        var auditFinding = await _auditFindingRepository.GetByIdAsync(auditFindingId, organizationId, cancellationToken);
        if (auditFinding is null)
        {
            return Result<AuditFindingResponse>.NotFound("Audit finding was not found.");
        }

        return Result<AuditFindingResponse>.Success(AuditFindingResponseMapper.ToResponse(auditFinding));
    }
}
