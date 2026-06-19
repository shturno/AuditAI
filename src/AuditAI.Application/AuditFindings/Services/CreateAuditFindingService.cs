using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.AuditFindings.Interfaces;
using AuditAI.Application.AuditFindings.Mappers;
using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.AuditLogs.Services;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Authorization;
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
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<CreateAuditFindingRequest> _validator;

    public CreateAuditFindingService(
        IAuditFindingRepository auditFindingRepository,
        IControlLookup controlLookup,
        IAuditLogWriter auditLogWriter,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateAuditFindingRequest> validator)
    {
        _auditFindingRepository = auditFindingRepository;
        _controlLookup = controlLookup;
        _auditLogWriter = auditLogWriter;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<Result<AuditFindingResponse>> ExecuteAsync(
        CreateAuditFindingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!AuditFindingsCurrentUserContext.TryGetActor(_currentUser, out var userId, out var organizationId))
        {
            return Result<AuditFindingResponse>.Unauthorized(AuditFindingsCurrentUserContext.UnauthorizedMessage);
        }

        if (!RoleAuthorization.CanManageAuditFindings(_currentUser))
        {
            return Result<AuditFindingResponse>.Forbidden(RoleAuthorization.AuditFindingsManageForbiddenMessage);
        }

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<AuditFindingResponse>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var controlOrganizationId = await _controlLookup.GetControlOrganizationIdAsync(request.ControlId, cancellationToken);
        if (!controlOrganizationId.HasValue || controlOrganizationId.Value != organizationId)
        {
            return Result<AuditFindingResponse>.NotFound("Control was not found.");
        }

        var auditFinding = AuditFindingEntity.Create(
            Guid.NewGuid(),
            request.ControlId,
            userId,
            request.Title,
            request.Description,
            request.Severity,
            _dateTimeProvider.UtcNow);

        await _auditFindingRepository.AddAsync(auditFinding, cancellationToken);
        await _auditLogWriter.WriteAsync(
            new AuditLogWriteEntry(
                organizationId,
                userId,
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
