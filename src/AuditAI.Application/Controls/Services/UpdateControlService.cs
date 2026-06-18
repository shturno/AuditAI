using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.AuditLogs.Services;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using AuditAI.Application.Controls.Contracts;
using AuditAI.Application.Controls.Interfaces;
using AuditAI.Application.Controls.Mappers;
using FluentValidation;

namespace AuditAI.Application.Controls.Services;

public sealed class UpdateControlService
{
    private readonly IControlRepository _controlRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IOrganizationLookup _organizationLookup;
    private readonly IDepartmentLookup _departmentLookup;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<UpdateControlRequest> _validator;

    public UpdateControlService(
        IControlRepository controlRepository,
        ICurrentUser currentUser,
        IOrganizationLookup organizationLookup,
        IDepartmentLookup departmentLookup,
        IAuditLogWriter auditLogWriter,
        IDateTimeProvider dateTimeProvider,
        IValidator<UpdateControlRequest> validator)
    {
        _controlRepository = controlRepository;
        _currentUser = currentUser;
        _organizationLookup = organizationLookup;
        _departmentLookup = departmentLookup;
        _auditLogWriter = auditLogWriter;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<Result<ControlResponse>> ExecuteAsync(
        Guid controlId,
        UpdateControlRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ControlsCurrentUserContext.TryGetActor(_currentUser, out var userId, out var organizationId))
        {
            return Result<ControlResponse>.Unauthorized(ControlsCurrentUserContext.UnauthorizedMessage);
        }

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<ControlResponse>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var control = await _controlRepository.GetByIdForUpdateAsync(controlId, cancellationToken);
        if (control is null || control.OrganizationId != organizationId)
        {
            return Result<ControlResponse>.NotFound("Control was not found.");
        }

        var referenceErrors = await ControlOrganizationValidation.ValidateAsync(
            control.OrganizationId,
            request.DepartmentId,
            _organizationLookup,
            _departmentLookup,
            cancellationToken);

        if (referenceErrors.Count > 0)
        {
            return Result<ControlResponse>.ValidationFailure(referenceErrors);
        }

        if (await _controlRepository.ExistsWithCodeAsync(
                control.OrganizationId,
                request.Code.Trim(),
                controlId,
                cancellationToken))
        {
            return Result<ControlResponse>.Failure(
                "controls.code_already_exists",
                "A control with the same code already exists in this organization.");
        }

        control.UpdateDetails(
            request.Code,
            request.Category,
            request.Title,
            request.Description,
            request.Frequency,
            _dateTimeProvider.UtcNow);

        control.AssignDepartment(request.DepartmentId, _dateTimeProvider.UtcNow);

        await _auditLogWriter.WriteAsync(
            new AuditLogWriteEntry(
                control.OrganizationId,
                userId,
                AuditAI.Domain.Enums.AuditLogAction.ControlUpdated,
                nameof(AuditAI.Domain.Entities.Control),
                control.Id,
                AuditLogMetadata.Build(
                    ("code", control.Code),
                    ("category", control.Category),
                    ("status", control.Status.ToString()))),
            cancellationToken);
        await _controlRepository.SaveChangesAsync(cancellationToken);

        return Result<ControlResponse>.Success(ControlResponseMapper.ToResponse(control));
    }
}
