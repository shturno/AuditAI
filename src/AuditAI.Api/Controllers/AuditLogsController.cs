using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Services;
using AuditAI.Application.Common.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditAI.Api.Controllers;

[Authorize]
[Route("api/audit-logs")]
[Tags("Audit Logs")]
public sealed class AuditLogsController : ApiControllerBase
{
    private readonly GetAuditLogByIdService _getAuditLogByIdService;
    private readonly ListAuditLogsService _listAuditLogsService;

    public AuditLogsController(
        GetAuditLogByIdService getAuditLogByIdService,
        ListAuditLogsService listAuditLogsService)
    {
        _getAuditLogByIdService = getAuditLogByIdService;
        _listAuditLogsService = listAuditLogsService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AuditLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditLogResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getAuditLogByIdService.ExecuteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AuditLogListItemResponse>>> List(
        [FromQuery] AuditLogQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var result = await _listAuditLogsService.ExecuteAsync(queryParameters, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }
}
