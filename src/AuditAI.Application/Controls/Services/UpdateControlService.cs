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
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<UpdateControlRequest> _validator;

    public UpdateControlService(
        IControlRepository controlRepository,
        IDateTimeProvider dateTimeProvider,
        IValidator<UpdateControlRequest> validator)
    {
        _controlRepository = controlRepository;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<Result<ControlResponse>> ExecuteAsync(
        Guid controlId,
        UpdateControlRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<ControlResponse>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var control = await _controlRepository.GetByIdForUpdateAsync(controlId, cancellationToken);
        if (control is null)
        {
            return Result<ControlResponse>.NotFound("Control was not found.");
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

        await _controlRepository.SaveChangesAsync(cancellationToken);

        return Result<ControlResponse>.Success(ControlResponseMapper.ToResponse(control));
    }
}
