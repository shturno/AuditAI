using AuditAI.Application.ActionPlans.Contracts;
using AuditAI.Application.ActionPlans.Interfaces;
using AuditAI.Application.ActionPlans.Mappers;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Authorization;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using FluentValidation;

namespace AuditAI.Application.ActionPlans.Services;

public sealed class ListActionPlansService
{
    private readonly IActionPlanRepository _actionPlanRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<ActionPlanQueryParameters> _validator;

    public ListActionPlansService(
        IActionPlanRepository actionPlanRepository,
        ICurrentUser currentUser,
        IValidator<ActionPlanQueryParameters> validator)
    {
        _actionPlanRepository = actionPlanRepository;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<PagedResult<ActionPlanListItemResponse>>> ExecuteAsync(
        ActionPlanQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        if (!ActionPlansCurrentUserContext.TryGetOrganization(_currentUser, out var organizationId))
        {
            return Result<PagedResult<ActionPlanListItemResponse>>.Unauthorized(ActionPlansCurrentUserContext.UnauthorizedMessage);
        }

        if (!RoleAuthorization.CanReadActionPlans(_currentUser))
        {
            return Result<PagedResult<ActionPlanListItemResponse>>.Forbidden(RoleAuthorization.ActionPlansReadForbiddenMessage);
        }

        var validationResult = await _validator.ValidateAsync(queryParameters, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<PagedResult<ActionPlanListItemResponse>>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var page = await _actionPlanRepository.ListAsync(organizationId, queryParameters, cancellationToken);
        var items = page.Items.Select(ActionPlanResponseMapper.ToListItem).ToArray();

        return Result<PagedResult<ActionPlanListItemResponse>>.Success(
            new PagedResult<ActionPlanListItemResponse>(items, page.TotalCount, page.PageNumber, page.PageSize));
    }
}
