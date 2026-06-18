using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.AuditLogs.Services;
using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.AuditFindings.Interfaces;
using AuditAI.Application.AuditFindings.Mappers;
using AuditAI.Application.ActionPlans.Interfaces;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using FluentValidation;

namespace AuditAI.Application.AuditFindings.Services;

public sealed class UpdateAuditFindingService
{
    private readonly IAuditFindingRepository _auditFindingRepository;
    private readonly IAuditFindingLookup _auditFindingLookup;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<UpdateAuditFindingRequest> _validator;

    public UpdateAuditFindingService(
        IAuditFindingRepository auditFindingRepository,
        IAuditFindingLookup auditFindingLookup,
        IAuditLogWriter auditLogWriter,
        IDateTimeProvider dateTimeProvider,
        IValidator<UpdateAuditFindingRequest> validator)
    {
        _auditFindingRepository = auditFindingRepository;
        _auditFindingLookup = auditFindingLookup;
        _auditLogWriter = auditLogWriter;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<Result<AuditFindingResponse>> ExecuteAsync(
        Guid auditFindingId,
        UpdateAuditFindingRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<AuditFindingResponse>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var auditFinding = await _auditFindingRepository.GetByIdForUpdateAsync(auditFindingId, cancellationToken);
        if (auditFinding is null)
        {
            return Result<AuditFindingResponse>.NotFound("Audit finding was not found.");
        }

        auditFinding.UpdateDetails(
            request.Title,
            request.Description,
            request.Severity,
            _dateTimeProvider.UtcNow);

        var organizationId = await _auditFindingLookup.GetFindingOrganizationIdAsync(auditFinding.Id, cancellationToken);
        await _auditLogWriter.WriteAsync(
            new AuditLogWriteEntry(
                organizationId!.Value,
                null,
                AuditAI.Domain.Enums.AuditLogAction.AuditFindingUpdated,
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
