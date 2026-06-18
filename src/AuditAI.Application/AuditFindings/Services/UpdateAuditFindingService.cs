using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.AuditFindings.Interfaces;
using AuditAI.Application.AuditFindings.Mappers;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using FluentValidation;

namespace AuditAI.Application.AuditFindings.Services;

public sealed class UpdateAuditFindingService
{
    private readonly IAuditFindingRepository _auditFindingRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<UpdateAuditFindingRequest> _validator;

    public UpdateAuditFindingService(
        IAuditFindingRepository auditFindingRepository,
        IDateTimeProvider dateTimeProvider,
        IValidator<UpdateAuditFindingRequest> validator)
    {
        _auditFindingRepository = auditFindingRepository;
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

        await _auditFindingRepository.SaveChangesAsync(cancellationToken);

        return Result<AuditFindingResponse>.Success(AuditFindingResponseMapper.ToResponse(auditFinding));
    }
}
