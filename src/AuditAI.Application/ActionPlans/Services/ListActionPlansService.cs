using AuditAI.Application.ActionPlans.Contracts;
using AuditAI.Application.ActionPlans.Interfaces;
using AuditAI.Application.ActionPlans.Mappers;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using FluentValidation;

namespace AuditAI.Application.ActionPlans.Services;

public sealed class ListActionPlansService
{
    private readonly IActionPlanRepository _actionPlanRepository;
    private readonly IValidator<ActionPlanQueryParameters> _validator;

    public ListActionPlansService(
        IActionPlanRepository actionPlanRepository,
        IValidator<ActionPlanQueryParameters> validator)
    {
        _actionPlanRepository = actionPlanRepository;
        _validator = validator;
    }

    public async Task<Result<PagedResult<ActionPlanListItemResponse>>> ExecuteAsync(
        ActionPlanQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(queryParameters, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<PagedResult<ActionPlanListItemResponse>>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var page = await _actionPlanRepository.ListAsync(queryParameters, cancellationToken);
        var items = page.Items.Select(ActionPlanResponseMapper.ToListItem).ToArray();

        return Result<PagedResult<ActionPlanListItemResponse>>.Success(
            new PagedResult<ActionPlanListItemResponse>(items, page.TotalCount, page.PageNumber, page.PageSize));
    }
}
