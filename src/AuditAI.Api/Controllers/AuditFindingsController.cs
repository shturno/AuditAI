using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.AuditFindings.Services;
using AuditAI.Application.Common.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditAI.Api.Controllers;

[Authorize]
[Route("api/audit-findings")]
[Tags("Audit Findings")]
public sealed class AuditFindingsController : ApiControllerBase
{
    private readonly CreateAuditFindingService _createAuditFindingService;
    private readonly GetAuditFindingByIdService _getAuditFindingByIdService;
    private readonly ListAuditFindingsService _listAuditFindingsService;
    private readonly UpdateAuditFindingService _updateAuditFindingService;
    private readonly ChangeAuditFindingStatusService _changeAuditFindingStatusService;

    public AuditFindingsController(
        CreateAuditFindingService createAuditFindingService,
        GetAuditFindingByIdService getAuditFindingByIdService,
        ListAuditFindingsService listAuditFindingsService,
        UpdateAuditFindingService updateAuditFindingService,
        ChangeAuditFindingStatusService changeAuditFindingStatusService)
    {
        _createAuditFindingService = createAuditFindingService;
        _getAuditFindingByIdService = getAuditFindingByIdService;
        _listAuditFindingsService = listAuditFindingsService;
        _updateAuditFindingService = updateAuditFindingService;
        _changeAuditFindingStatusService = changeAuditFindingStatusService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AuditFindingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuditFindingResponse>> Create(
        [FromBody] CreateAuditFindingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createAuditFindingService.ExecuteAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AuditFindingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditFindingResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getAuditFindingByIdService.ExecuteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditFindingListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AuditFindingListItemResponse>>> List(
        [FromQuery] AuditFindingQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var result = await _listAuditFindingsService.ExecuteAsync(queryParameters, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AuditFindingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditFindingResponse>> Update(
        Guid id,
        [FromBody] UpdateAuditFindingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _updateAuditFindingService.ExecuteAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(AuditFindingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditFindingResponse>> ChangeStatus(
        Guid id,
        [FromBody] ChangeAuditFindingStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _changeAuditFindingStatusService.ExecuteAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }
}
