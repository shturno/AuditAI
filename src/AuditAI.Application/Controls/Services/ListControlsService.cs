using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Authorization;
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
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<ControlQueryParameters> _validator;

    public ListControlsService(
        IControlRepository controlRepository,
        ICurrentUser currentUser,
        IValidator<ControlQueryParameters> validator)
    {
        _controlRepository = controlRepository;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<PagedResult<ControlListItemResponse>>> ExecuteAsync(
        ControlQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        if (!ControlsCurrentUserContext.TryGetOrganization(_currentUser, out var organizationId))
        {
            return Result<PagedResult<ControlListItemResponse>>.Unauthorized(ControlsCurrentUserContext.UnauthorizedMessage);
        }

        if (!RoleAuthorization.CanReadControls(_currentUser))
        {
            return Result<PagedResult<ControlListItemResponse>>.Forbidden(RoleAuthorization.ControlsReadForbiddenMessage);
        }

        var validationResult = await _validator.ValidateAsync(queryParameters, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<PagedResult<ControlListItemResponse>>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var scopedQueryParameters = new ControlQueryParameters
        {
            PageNumber = queryParameters.PageNumber,
            PageSize = queryParameters.PageSize,
            OrganizationId = organizationId,
            DepartmentId = queryParameters.DepartmentId,
            Status = queryParameters.Status,
            Frequency = queryParameters.Frequency,
            SearchTerm = queryParameters.SearchTerm
        };

        var controls = await _controlRepository.ListAsync(scopedQueryParameters, cancellationToken);
        var items = controls.Items
            .Select(ControlResponseMapper.ToListItemResponse)
            .ToArray();

        return Result<PagedResult<ControlListItemResponse>>.Success(
            new PagedResult<ControlListItemResponse>(items, controls.TotalCount, controls.PageNumber, controls.PageSize));
    }
}
