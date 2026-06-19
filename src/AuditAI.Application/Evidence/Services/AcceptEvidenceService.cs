using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.AuditLogs.Services;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Authorization;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using AuditAI.Application.Evidence.Contracts;
using AuditAI.Application.Evidence.Interfaces;
using AuditAI.Application.Evidence.Mappers;
using AuditAI.Domain.Enums;
using FluentValidation;

namespace AuditAI.Application.Evidence.Services;

public sealed class AcceptEvidenceService
{
    private readonly IEvidenceRepository _evidenceRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IControlLookup _controlLookup;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<ReviewEvidenceRequest> _validator;

    public AcceptEvidenceService(
        IEvidenceRepository evidenceRepository,
        ICurrentUser currentUser,
        IControlLookup controlLookup,
        IAuditLogWriter auditLogWriter,
        IDateTimeProvider dateTimeProvider,
        IValidator<ReviewEvidenceRequest> validator)
    {
        _evidenceRepository = evidenceRepository;
        _currentUser = currentUser;
        _controlLookup = controlLookup;
        _auditLogWriter = auditLogWriter;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<Result<EvidenceResponse>> ExecuteAsync(
        Guid evidenceId,
        ReviewEvidenceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!EvidenceCurrentUserContext.TryGetActor(_currentUser, out var userId, out var organizationId))
        {
            return Result<EvidenceResponse>.Unauthorized(EvidenceCurrentUserContext.UnauthorizedMessage);
        }

        if (!RoleAuthorization.CanReviewEvidence(_currentUser))
        {
            return Result<EvidenceResponse>.Forbidden(RoleAuthorization.EvidenceReviewForbiddenMessage);
        }

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<EvidenceResponse>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var evidence = await _evidenceRepository.GetByIdForUpdateAsync(evidenceId, cancellationToken);
        if (evidence is null)
        {
            return Result<EvidenceResponse>.NotFound("Evidence was not found.");
        }

        var controlOrganizationId = await _controlLookup.GetControlOrganizationIdAsync(evidence.ControlId, cancellationToken);
        if (!controlOrganizationId.HasValue || controlOrganizationId.Value != organizationId)
        {
            return Result<EvidenceResponse>.NotFound("Evidence was not found.");
        }

        if (evidence.Status != EvidenceStatus.Pending)
        {
            return Result<EvidenceResponse>.ValidationFailure(
            [
                new ValidationError("Status", "Evidence can only be reviewed while pending.")
            ]);
        }

        evidence.Accept(userId, _dateTimeProvider.UtcNow);
        await _auditLogWriter.WriteAsync(
            new AuditLogWriteEntry(
                organizationId,
                userId,
                AuditAI.Domain.Enums.AuditLogAction.EvidenceAccepted,
                nameof(AuditAI.Domain.Entities.Evidence),
                evidence.Id,
                AuditLogMetadata.Build(
                    ("status", evidence.Status.ToString()))),
            cancellationToken);
        await _evidenceRepository.SaveChangesAsync(cancellationToken);

        return Result<EvidenceResponse>.Success(EvidenceResponseMapper.ToResponse(evidence));
    }
}
