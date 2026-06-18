using AuditAI.Application.ActionPlans.Contracts;
using AuditAI.Application.ActionPlans.Interfaces;
using AuditAI.Application.ActionPlans.Mappers;
using AuditAI.Application.Common.Results;

namespace AuditAI.Application.ActionPlans.Services;

public sealed class GetActionPlanByIdService
{
    private readonly IActionPlanRepository _actionPlanRepository;

    public GetActionPlanByIdService(IActionPlanRepository actionPlanRepository)
    {
        _actionPlanRepository = actionPlanRepository;
    }

    public async Task<Result<ActionPlanResponse>> ExecuteAsync(
        Guid actionPlanId,
        CancellationToken cancellationToken = default)
    {
        var actionPlan = await _actionPlanRepository.GetByIdAsync(actionPlanId, cancellationToken);
        if (actionPlan is null)
        {
            return Result<ActionPlanResponse>.NotFound("Action plan was not found.");
        }

        return Result<ActionPlanResponse>.Success(ActionPlanResponseMapper.ToResponse(actionPlan));
    }
}
