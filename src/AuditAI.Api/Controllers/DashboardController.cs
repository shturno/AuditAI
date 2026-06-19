using AuditAI.Application.Common.Results;
using AuditAI.Application.Dashboard.Contracts;
using AuditAI.Application.Dashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditAI.Api.Controllers;

[Authorize]
[Route("api/dashboard")]
[Tags("Dashboard")]
public sealed class DashboardController : ApiControllerBase
{
    private readonly GetDashboardSummaryService _service;

    public DashboardController(GetDashboardSummaryService service)
    {
        _service = service;
    }

    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<DashboardSummaryResponse>>> GetSummary(
        [FromQuery] DashboardQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.ExecuteAsync(queryParameters, cancellationToken);

        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result);
    }
}
