using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.AuditLogs.Services;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Authorization;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Controls.Contracts;
using AuditAI.Application.Controls.Interfaces;
using AuditAI.Application.Controls.Mappers;

namespace AuditAI.Application.Controls.Services;

public sealed class DeactivateControlService
{
    private readonly IControlRepository _controlRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeactivateControlService(
        IControlRepository controlRepository,
        ICurrentUser currentUser,
        IAuditLogWriter auditLogWriter,
        IDateTimeProvider dateTimeProvider)
    {
        _controlRepository = controlRepository;
        _currentUser = currentUser;
        _auditLogWriter = auditLogWriter;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<ControlResponse>> ExecuteAsync(
        Guid controlId,
        CancellationToken cancellationToken = default)
    {
        if (!ControlsCurrentUserContext.TryGetActor(_currentUser, out var userId, out var organizationId))
        {
            return Result<ControlResponse>.Unauthorized(ControlsCurrentUserContext.UnauthorizedMessage);
        }

        if (!RoleAuthorization.CanManageControls(_currentUser))
        {
            return Result<ControlResponse>.Forbidden(RoleAuthorization.ControlsManageForbiddenMessage);
        }

        var control = await _controlRepository.GetByIdForUpdateAsync(controlId, cancellationToken);
        if (control is null || control.OrganizationId != organizationId)
        {
            return Result<ControlResponse>.NotFound("Control was not found.");
        }

        control.Deactivate(_dateTimeProvider.UtcNow);
        await _auditLogWriter.WriteAsync(
            new AuditLogWriteEntry(
                control.OrganizationId,
                userId,
                AuditAI.Domain.Enums.AuditLogAction.ControlDeactivated,
                nameof(AuditAI.Domain.Entities.Control),
                control.Id,
                AuditLogMetadata.Build(
                    ("code", control.Code),
                    ("status", control.Status.ToString()))),
            cancellationToken);
        await _controlRepository.SaveChangesAsync(cancellationToken);

        return Result<ControlResponse>.Success(ControlResponseMapper.ToResponse(control));
    }
}
