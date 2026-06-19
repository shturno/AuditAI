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
using FluentValidation;
using AuditEvidence = AuditAI.Domain.Entities.Evidence;

namespace AuditAI.Application.Evidence.Services;

public sealed class CreateEvidenceService
{
    private readonly IEvidenceRepository _evidenceRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IControlLookup _controlLookup;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<CreateEvidenceRequest> _validator;

    public CreateEvidenceService(
        IEvidenceRepository evidenceRepository,
        ICurrentUser currentUser,
        IControlLookup controlLookup,
        IAuditLogWriter auditLogWriter,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateEvidenceRequest> validator)
    {
        _evidenceRepository = evidenceRepository;
        _currentUser = currentUser;
        _controlLookup = controlLookup;
        _auditLogWriter = auditLogWriter;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<Result<EvidenceResponse>> ExecuteAsync(
        CreateEvidenceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!EvidenceCurrentUserContext.TryGetActor(_currentUser, out var userId, out var organizationId))
        {
            return Result<EvidenceResponse>.Unauthorized(EvidenceCurrentUserContext.UnauthorizedMessage);
        }

        if (!RoleAuthorization.CanSubmitEvidence(_currentUser))
        {
            return Result<EvidenceResponse>.Forbidden(RoleAuthorization.EvidenceSubmitForbiddenMessage);
        }

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<EvidenceResponse>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var controlOrganizationId = await _controlLookup.GetControlOrganizationIdAsync(request.ControlId, cancellationToken);
        if (!controlOrganizationId.HasValue || controlOrganizationId.Value != organizationId)
        {
            return Result<EvidenceResponse>.NotFound("Control was not found.");
        }

        var evidence = AuditEvidence.Create(
            Guid.NewGuid(),
            request.ControlId,
            userId,
            request.FileName,
            request.StorageReference,
            _dateTimeProvider.UtcNow);

        await _evidenceRepository.AddAsync(evidence, cancellationToken);
        await _auditLogWriter.WriteAsync(
            new AuditLogWriteEntry(
                organizationId,
                userId,
                AuditAI.Domain.Enums.AuditLogAction.EvidenceSubmitted,
                nameof(AuditAI.Domain.Entities.Evidence),
                evidence.Id,
                AuditLogMetadata.Build(
                    ("fileName", evidence.FileName),
                    ("status", evidence.Status.ToString()))),
            cancellationToken);
        await _evidenceRepository.SaveChangesAsync(cancellationToken);

        return Result<EvidenceResponse>.Success(EvidenceResponseMapper.ToResponse(evidence));
    }
}
