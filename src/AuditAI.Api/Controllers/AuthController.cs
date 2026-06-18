using AuditAI.Application.Auth.Contracts;
using AuditAI.Application.Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuditAI.Api.Controllers;

[Route("api/auth")]
[Tags("Authentication")]
public sealed class AuthController : ApiControllerBase
{
    private readonly LoginService _loginService;

    public AuthController(LoginService loginService)
    {
        _loginService = loginService;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _loginService.ExecuteAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromFailure(result);
        }

        return Ok(result.Value);
    }
}
