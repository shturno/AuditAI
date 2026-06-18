using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.AuditFindings.Interfaces;
using AuditAI.Application.AuditFindings.Mappers;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using AuditAI.Domain.Enums;
using AuditAI.Domain.Exceptions;
using FluentValidation;

namespace AuditAI.Application.AuditFindings.Services;

public sealed class ChangeAuditFindingStatusService
{
    private readonly IAuditFindingRepository _auditFindingRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<ChangeAuditFindingStatusRequest> _validator;

    public ChangeAuditFindingStatusService(
        IAuditFindingRepository auditFindingRepository,
        IDateTimeProvider dateTimeProvider,
        IValidator<ChangeAuditFindingStatusRequest> validator)
    {
        _auditFindingRepository = auditFindingRepository;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<Result<AuditFindingResponse>> ExecuteAsync(
        Guid auditFindingId,
        ChangeAuditFindingStatusRequest request,
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

        try
        {
            switch (request.Status)
            {
                case AuditFindingStatus.InProgress:
                    auditFinding.MarkInProgress(_dateTimeProvider.UtcNow);
                    break;
                case AuditFindingStatus.Resolved:
                    auditFinding.Resolve(_dateTimeProvider.UtcNow);
                    break;
                case AuditFindingStatus.Cancelled:
                    auditFinding.Cancel(_dateTimeProvider.UtcNow);
                    break;
                default:
                    return Result<AuditFindingResponse>.ValidationFailure(
                    [
                        new ValidationError("Status", "Unsupported status transition.")
                    ]);
            }
        }
        catch (DomainRuleViolationException exception)
        {
            return Result<AuditFindingResponse>.ValidationFailure(
            [
                new ValidationError("Status", exception.Message)
            ]);
        }

        await _auditFindingRepository.SaveChangesAsync(cancellationToken);

        return Result<AuditFindingResponse>.Success(AuditFindingResponseMapper.ToResponse(auditFinding));
    }
}
