using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Controls.Contracts;
using AuditAI.Application.Controls.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditAI.Api.Controllers;

[Authorize]
[Route("api/controls")]
[Tags("Controls")]
public sealed class ControlsController : ApiControllerBase
{
    private readonly CreateControlService _createControlService;
    private readonly GetControlByIdService _getControlByIdService;
    private readonly ListControlsService _listControlsService;
    private readonly UpdateControlService _updateControlService;
    private readonly DeactivateControlService _deactivateControlService;

    public ControlsController(
        CreateControlService createControlService,
        GetControlByIdService getControlByIdService,
        ListControlsService listControlsService,
        UpdateControlService updateControlService,
        DeactivateControlService deactivateControlService)
    {
        _createControlService = createControlService;
        _getControlByIdService = getControlByIdService;
        _listControlsService = listControlsService;
        _updateControlService = updateControlService;
        _deactivateControlService = deactivateControlService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ControlResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ControlResponse>> Create(
        [FromBody] CreateControlRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createControlService.ExecuteAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ControlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ControlResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getControlByIdService.ExecuteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ControlListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ControlListItemResponse>>> List(
        [FromQuery] ControlQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var result = await _listControlsService.ExecuteAsync(queryParameters, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ControlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ControlResponse>> Update(
        Guid id,
        [FromBody] UpdateControlRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _updateControlService.ExecuteAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(ControlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ControlResponse>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _deactivateControlService.ExecuteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }
}
