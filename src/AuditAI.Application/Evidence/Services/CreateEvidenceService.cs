using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.AuditLogs.Services;
using AuditAI.Application.Common.Abstractions;
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
    private readonly IControlLookup _controlLookup;
    private readonly IUserLookup _userLookup;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<CreateEvidenceRequest> _validator;

    public CreateEvidenceService(
        IEvidenceRepository evidenceRepository,
        IControlLookup controlLookup,
        IUserLookup userLookup,
        IAuditLogWriter auditLogWriter,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateEvidenceRequest> validator)
    {
        _evidenceRepository = evidenceRepository;
        _controlLookup = controlLookup;
        _userLookup = userLookup;
        _auditLogWriter = auditLogWriter;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<Result<EvidenceResponse>> ExecuteAsync(
        CreateEvidenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<EvidenceResponse>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var referenceErrors = await EvidenceReferenceValidation.ValidateCreateAsync(
            request.ControlId,
            request.SubmittedByUserId,
            _controlLookup,
            _userLookup,
            cancellationToken);

        if (referenceErrors.Count > 0)
        {
            return Result<EvidenceResponse>.ValidationFailure(referenceErrors);
        }

        var evidence = AuditEvidence.Create(
            Guid.NewGuid(),
            request.ControlId,
            request.SubmittedByUserId,
            request.FileName,
            request.StorageReference,
            _dateTimeProvider.UtcNow);

        await _evidenceRepository.AddAsync(evidence, cancellationToken);
        var organizationId = await _controlLookup.GetControlOrganizationIdAsync(request.ControlId, cancellationToken);
        await _auditLogWriter.WriteAsync(
            new AuditLogWriteEntry(
                organizationId!.Value,
                request.SubmittedByUserId,
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
