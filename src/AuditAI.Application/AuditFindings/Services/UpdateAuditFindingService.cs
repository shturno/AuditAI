using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.AuditFindings.Interfaces;
using AuditAI.Application.AuditFindings.Mappers;
using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.AuditLogs.Services;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using FluentValidation;

namespace AuditAI.Application.AuditFindings.Services;

public sealed class UpdateAuditFindingService
{
    private readonly IAuditFindingRepository _auditFindingRepository;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<UpdateAuditFindingRequest> _validator;

    public UpdateAuditFindingService(
        IAuditFindingRepository auditFindingRepository,
        IAuditLogWriter auditLogWriter,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IValidator<UpdateAuditFindingRequest> validator)
    {
        _auditFindingRepository = auditFindingRepository;
        _auditLogWriter = auditLogWriter;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<Result<AuditFindingResponse>> ExecuteAsync(
        Guid auditFindingId,
        UpdateAuditFindingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!AuditFindingsCurrentUserContext.TryGetActor(_currentUser, out _, out var organizationId))
        {
            return Result<AuditFindingResponse>.Unauthorized(AuditFindingsCurrentUserContext.UnauthorizedMessage);
        }

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<AuditFindingResponse>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var auditFinding = await _auditFindingRepository.GetByIdForUpdateAsync(auditFindingId, organizationId, cancellationToken);
        if (auditFinding is null)
        {
            return Result<AuditFindingResponse>.NotFound("Audit finding was not found.");
        }

        auditFinding.UpdateDetails(
            request.Title,
            request.Description,
            request.Severity,
            _dateTimeProvider.UtcNow);

        await _auditLogWriter.WriteAsync(
            new AuditLogWriteEntry(
                organizationId,
                _currentUser.UserId,
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
