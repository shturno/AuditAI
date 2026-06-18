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
using AuditControl = AuditAI.Domain.Entities.Control;

namespace AuditAI.Application.Controls.Services;

public sealed class CreateControlService
{
    private readonly IControlRepository _controlRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IOrganizationLookup _organizationLookup;
    private readonly IDepartmentLookup _departmentLookup;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<CreateControlRequest> _validator;

    public CreateControlService(
        IControlRepository controlRepository,
        ICurrentUser currentUser,
        IOrganizationLookup organizationLookup,
        IDepartmentLookup departmentLookup,
        IAuditLogWriter auditLogWriter,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateControlRequest> validator)
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
        CreateControlRequest request,
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

        if (!await _organizationLookup.OrganizationExistsAsync(organizationId, cancellationToken))
        {
            return Result<ControlResponse>.Unauthorized(ControlsCurrentUserContext.UnauthorizedMessage);
        }

        var referenceErrors = await ControlOrganizationValidation.ValidateAsync(
            organizationId,
            request.DepartmentId,
            _organizationLookup,
            _departmentLookup,
            cancellationToken);

        if (referenceErrors.Count > 0)
        {
            return Result<ControlResponse>.ValidationFailure(referenceErrors);
        }

        if (await _controlRepository.ExistsWithCodeAsync(
                organizationId,
                request.Code.Trim(),
                null,
                cancellationToken))
        {
            return Result<ControlResponse>.Failure(
                "controls.code_already_exists",
                "A control with the same code already exists in this organization.");
        }

        var control = AuditControl.Create(
            Guid.NewGuid(),
            organizationId,
            request.DepartmentId,
            request.Code,
            request.Category,
            request.Title,
            request.Description,
            request.Frequency,
            _dateTimeProvider.UtcNow);

        await _controlRepository.AddAsync(control, cancellationToken);
        await _auditLogWriter.WriteAsync(
            new AuditLogWriteEntry(
                organizationId,
                userId,
                AuditAI.Domain.Enums.AuditLogAction.ControlCreated,
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
