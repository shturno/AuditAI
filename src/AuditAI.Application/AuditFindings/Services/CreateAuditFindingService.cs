using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.AuditLogs.Services;
using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.AuditFindings.Interfaces;
using AuditAI.Application.AuditFindings.Mappers;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using AuditAI.Application.Evidence.Interfaces;
using FluentValidation;
using AuditFindingEntity = AuditAI.Domain.Entities.AuditFinding;

namespace AuditAI.Application.AuditFindings.Services;

public sealed class CreateAuditFindingService
{
    private readonly IAuditFindingRepository _auditFindingRepository;
    private readonly IControlLookup _controlLookup;
    private readonly IUserLookup _userLookup;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<CreateAuditFindingRequest> _validator;

    public CreateAuditFindingService(
        IAuditFindingRepository auditFindingRepository,
        IControlLookup controlLookup,
        IUserLookup userLookup,
        IAuditLogWriter auditLogWriter,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateAuditFindingRequest> validator)
    {
        _auditFindingRepository = auditFindingRepository;
        _controlLookup = controlLookup;
        _userLookup = userLookup;
        _auditLogWriter = auditLogWriter;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<Result<AuditFindingResponse>> ExecuteAsync(
        CreateAuditFindingRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<AuditFindingResponse>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var referenceErrors = await AuditFindingReferenceValidation.ValidateCreateAsync(
            request.ControlId,
            request.CreatedByUserId,
            _controlLookup,
            _userLookup,
            cancellationToken);

        if (referenceErrors.Count > 0)
        {
            return Result<AuditFindingResponse>.ValidationFailure(referenceErrors);
        }

        var auditFinding = AuditFindingEntity.Create(
            Guid.NewGuid(),
            request.ControlId,
            request.CreatedByUserId,
            request.Title,
            request.Description,
            request.Severity,
            _dateTimeProvider.UtcNow);

        await _auditFindingRepository.AddAsync(auditFinding, cancellationToken);
        var organizationId = await _controlLookup.GetControlOrganizationIdAsync(request.ControlId, cancellationToken);
        await _auditLogWriter.WriteAsync(
            new AuditLogWriteEntry(
                organizationId!.Value,
                request.CreatedByUserId,
                AuditAI.Domain.Enums.AuditLogAction.AuditFindingCreated,
                nameof(AuditAI.Domain.Entities.AuditFinding),
                auditFinding.Id,
                AuditLogMetadata.Build(
                    ("severity", auditFinding.Severity.ToString()),
                    ("status", auditFinding.Status.ToString()))),
            cancellationToken);
        await _auditFindingRepository.SaveChangesAsync(cancellationToken);

        return Result<AuditFindingResponse>.Success(AuditFindingResponseMapper.ToResponse(auditFinding));
    }
}
