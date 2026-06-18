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
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<CreateControlRequest> _validator;

    public CreateControlService(
        IControlRepository controlRepository,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateControlRequest> validator)
    {
        _controlRepository = controlRepository;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<Result<ControlResponse>> ExecuteAsync(
        CreateControlRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<ControlResponse>.ValidationFailure(validationResult.ToValidationErrors());
        }

        if (await _controlRepository.ExistsWithCodeAsync(
                request.OrganizationId,
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
            request.OrganizationId,
            request.DepartmentId,
            request.Code,
            request.Category,
            request.Title,
            request.Description,
            request.Frequency,
            _dateTimeProvider.UtcNow);

        await _controlRepository.AddAsync(control, cancellationToken);
        await _controlRepository.SaveChangesAsync(cancellationToken);

        return Result<ControlResponse>.Success(ControlResponseMapper.ToResponse(control));
    }
}
