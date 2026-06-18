using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using AuditAI.Application.Controls.Contracts;
using AuditAI.Application.Controls.Interfaces;
using AuditAI.Application.Controls.Mappers;
using FluentValidation;

namespace AuditAI.Application.Controls.Services;

public sealed class ListControlsService
{
    private readonly IControlRepository _controlRepository;
    private readonly IValidator<ControlQueryParameters> _validator;

    public ListControlsService(
        IControlRepository controlRepository,
        IValidator<ControlQueryParameters> validator)
    {
        _controlRepository = controlRepository;
        _validator = validator;
    }

    public async Task<Result<PagedResult<ControlListItemResponse>>> ExecuteAsync(
        ControlQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(queryParameters, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<PagedResult<ControlListItemResponse>>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var controls = await _controlRepository.ListAsync(queryParameters, cancellationToken);
        var items = controls.Items
            .Select(ControlResponseMapper.ToListItemResponse)
            .ToArray();

        return Result<PagedResult<ControlListItemResponse>>.Success(
            new PagedResult<ControlListItemResponse>(items, controls.TotalCount, controls.PageNumber, controls.PageSize));
    }
}
