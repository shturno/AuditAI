using System.Security.Claims;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Domain.Enums;

namespace AuditAI.Api.Auth;

public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identities.Any(identity => identity.IsAuthenticated) == true;

    public Guid? UserId => TryGetGuidClaim(ClaimTypes.NameIdentifier, "sub");

    public string? Email => FindClaimValue(ClaimTypes.Email, "email");

    public UserRole? Role
    {
        get
        {
            var value = FindClaimValue(ClaimTypes.Role, "role");
            return Enum.TryParse<UserRole>(value, ignoreCase: false, out var role) ? role : null;
        }
    }

    public Guid? OrganizationId => TryGetGuidClaim("org_id");

    public Guid? DepartmentId => TryGetGuidClaim("department_id");

    private Guid? TryGetGuidClaim(params string[] claimTypes)
    {
        var value = FindClaimValue(claimTypes);
        return Guid.TryParse(value, out var result) ? result : null;
    }

    private string? FindClaimValue(params string[] claimTypes)
    {
        if (Principal is null)
        {
            return null;
        }

        foreach (var claimType in claimTypes)
        {
            var exactMatch = Principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(exactMatch))
            {
                return exactMatch;
            }

            var suffixMatch = Principal.Claims
                .FirstOrDefault(claim => claim.Type.EndsWith(claimType, StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (!string.IsNullOrWhiteSpace(suffixMatch))
            {
                return suffixMatch;
            }
        }

        return null;
    }
}
