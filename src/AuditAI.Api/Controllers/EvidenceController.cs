using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Evidence.Contracts;
using AuditAI.Application.Evidence.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditAI.Api.Controllers;

[Authorize]
[Route("api/evidence")]
[Tags("Evidence")]
public sealed class EvidenceController : ApiControllerBase
{
    private readonly CreateEvidenceService _createEvidenceService;
    private readonly GetEvidenceByIdService _getEvidenceByIdService;
    private readonly ListEvidenceService _listEvidenceService;
    private readonly AcceptEvidenceService _acceptEvidenceService;
    private readonly RejectEvidenceService _rejectEvidenceService;

    public EvidenceController(
        CreateEvidenceService createEvidenceService,
        GetEvidenceByIdService getEvidenceByIdService,
        ListEvidenceService listEvidenceService,
        AcceptEvidenceService acceptEvidenceService,
        RejectEvidenceService rejectEvidenceService)
    {
        _createEvidenceService = createEvidenceService;
        _getEvidenceByIdService = getEvidenceByIdService;
        _listEvidenceService = listEvidenceService;
        _acceptEvidenceService = acceptEvidenceService;
        _rejectEvidenceService = rejectEvidenceService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(EvidenceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EvidenceResponse>> Create(
        [FromBody] CreateEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createEvidenceService.ExecuteAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EvidenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EvidenceResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getEvidenceByIdService.ExecuteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EvidenceListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<EvidenceListItemResponse>>> List(
        [FromQuery] EvidenceQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var result = await _listEvidenceService.ExecuteAsync(queryParameters, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }

    [HttpPatch("{id:guid}/accept")]
    [ProducesResponseType(typeof(EvidenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EvidenceResponse>> Accept(
        Guid id,
        [FromBody] ReviewEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _acceptEvidenceService.ExecuteAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }

    [HttpPatch("{id:guid}/reject")]
    [ProducesResponseType(typeof(EvidenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EvidenceResponse>> Reject(
        Guid id,
        [FromBody] ReviewEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _rejectEvidenceService.ExecuteAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }
}
