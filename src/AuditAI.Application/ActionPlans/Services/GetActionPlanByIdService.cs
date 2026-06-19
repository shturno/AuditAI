using AuditAI.Application.ActionPlans.Contracts;
using AuditAI.Application.ActionPlans.Interfaces;
using AuditAI.Application.ActionPlans.Mappers;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Results;

namespace AuditAI.Application.ActionPlans.Services;

public sealed class GetActionPlanByIdService
{
    private readonly IActionPlanRepository _actionPlanRepository;
    private readonly ICurrentUser _currentUser;

    public GetActionPlanByIdService(
        IActionPlanRepository actionPlanRepository,
        ICurrentUser currentUser)
    {
        _actionPlanRepository = actionPlanRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<ActionPlanResponse>> ExecuteAsync(
        Guid actionPlanId,
        CancellationToken cancellationToken = default)
    {
        if (!ActionPlansCurrentUserContext.TryGetOrganization(_currentUser, out var organizationId))
        {
            return Result<ActionPlanResponse>.Unauthorized(ActionPlansCurrentUserContext.UnauthorizedMessage);
        }

        var actionPlan = await _actionPlanRepository.GetByIdAsync(actionPlanId, organizationId, cancellationToken);
        if (actionPlan is null)
        {
            return Result<ActionPlanResponse>.NotFound("Action plan was not found.");
        }

        return Result<ActionPlanResponse>.Success(ActionPlanResponseMapper.ToResponse(actionPlan));
    }
}
