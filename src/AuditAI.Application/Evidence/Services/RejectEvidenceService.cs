using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.AuditLogs.Services;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using AuditAI.Application.Evidence.Contracts;
using AuditAI.Application.Evidence.Interfaces;
using AuditAI.Application.Evidence.Mappers;
using AuditAI.Domain.Enums;
using FluentValidation;

namespace AuditAI.Application.Evidence.Services;

public sealed class RejectEvidenceService
{
    private readonly IEvidenceRepository _evidenceRepository;
    private readonly IControlLookup _controlLookup;
    private readonly IUserLookup _userLookup;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<ReviewEvidenceRequest> _validator;

    public RejectEvidenceService(
        IEvidenceRepository evidenceRepository,
        IControlLookup controlLookup,
        IUserLookup userLookup,
        IAuditLogWriter auditLogWriter,
        IDateTimeProvider dateTimeProvider,
        IValidator<ReviewEvidenceRequest> validator)
    {
        _evidenceRepository = evidenceRepository;
        _controlLookup = controlLookup;
        _userLookup = userLookup;
        _auditLogWriter = auditLogWriter;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<Result<EvidenceResponse>> ExecuteAsync(
        Guid evidenceId,
        ReviewEvidenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<EvidenceResponse>.ValidationFailure(validationResult.ToValidationErrors());
        }

        if (string.IsNullOrWhiteSpace(request.RejectionReason))
        {
            return Result<EvidenceResponse>.ValidationFailure(
            [
                new ValidationError("RejectionReason", "Rejection reason is required when rejecting evidence.")
            ]);
        }

        var evidence = await _evidenceRepository.GetByIdForUpdateAsync(evidenceId, cancellationToken);
        if (evidence is null)
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

        var referenceErrors = await EvidenceReferenceValidation.ValidateReviewerAsync(
            evidence.ControlId,
            request.ReviewerUserId,
            _controlLookup,
            _userLookup,
            cancellationToken);

        if (referenceErrors.Count > 0)
        {
            return Result<EvidenceResponse>.ValidationFailure(referenceErrors);
        }

        evidence.Reject(request.ReviewerUserId, request.RejectionReason, _dateTimeProvider.UtcNow);
        var organizationId = await _controlLookup.GetControlOrganizationIdAsync(evidence.ControlId, cancellationToken);
        await _auditLogWriter.WriteAsync(
            new AuditLogWriteEntry(
                organizationId!.Value,
                request.ReviewerUserId,
                AuditAI.Domain.Enums.AuditLogAction.EvidenceRejected,
                nameof(AuditAI.Domain.Entities.Evidence),
                evidence.Id,
                AuditLogMetadata.Build(
                    ("status", evidence.Status.ToString()),
                    ("rejectionReason", request.RejectionReason))),
            cancellationToken);
        await _evidenceRepository.SaveChangesAsync(cancellationToken);

        return Result<EvidenceResponse>.Success(EvidenceResponseMapper.ToResponse(evidence));
    }
}
