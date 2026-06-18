using AuditAI.Application.ActionPlans.Contracts;
using AuditAI.Application.ActionPlans.Services;
using AuditAI.Application.Common.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace AuditAI.Api.Controllers;

[Route("api/action-plans")]
[Tags("Action Plans")]
public sealed class ActionPlansController : ApiControllerBase
{
    private readonly CreateActionPlanService _createActionPlanService;
    private readonly GetActionPlanByIdService _getActionPlanByIdService;
    private readonly ListActionPlansService _listActionPlansService;
    private readonly UpdateActionPlanService _updateActionPlanService;
    private readonly ChangeActionPlanStatusService _changeActionPlanStatusService;

    public ActionPlansController(
        CreateActionPlanService createActionPlanService,
        GetActionPlanByIdService getActionPlanByIdService,
        ListActionPlansService listActionPlansService,
        UpdateActionPlanService updateActionPlanService,
        ChangeActionPlanStatusService changeActionPlanStatusService)
    {
        _createActionPlanService = createActionPlanService;
        _getActionPlanByIdService = getActionPlanByIdService;
        _listActionPlansService = listActionPlansService;
        _updateActionPlanService = updateActionPlanService;
        _changeActionPlanStatusService = changeActionPlanStatusService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ActionPlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ActionPlanResponse>> Create(
        [FromBody] CreateActionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createActionPlanService.ExecuteAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ActionPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActionPlanResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getActionPlanByIdService.ExecuteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ActionPlanListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ActionPlanListItemResponse>>> List(
        [FromQuery] ActionPlanQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var result = await _listActionPlansService.ExecuteAsync(queryParameters, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ActionPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActionPlanResponse>> Update(
        Guid id,
        [FromBody] UpdateActionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _updateActionPlanService.ExecuteAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ActionPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActionPlanResponse>> ChangeStatus(
        Guid id,
        [FromBody] ChangeActionPlanStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _changeActionPlanStatusService.ExecuteAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }
}
