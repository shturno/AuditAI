using System.IdentityModel.Tokens.Jwt;
using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;
using AuditAI.Infrastructure.Auth.Jwt;
using Microsoft.Extensions.Options;

namespace AuditAI.UnitTests.Infrastructure.Auth;

public sealed class JwtTokenGeneratorTests
{
    [Fact]
    public void Should_CreateToken_WithExpectedClaims_AndNoSensitiveClaims()
    {
        var user = User.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Audit User",
            "user@auditai.test",
            "hashed-password",
            UserRole.Reviewer,
            new DateTimeOffset(2026, 06, 18, 12, 0, 0, TimeSpan.Zero));
        var generator = new JwtTokenGenerator(
            Options.Create(new JwtOptions
            {
                Issuer = "AuditAI.Tests",
                Audience = "AuditAI.Tests.Users",
                Secret = "unit-tests-secret-key-with-32chars",
                ExpirationMinutes = 60
            }));

        var token = generator.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken);

        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.Email && claim.Value == user.Email);
        Assert.Contains(jwt.Claims, claim => claim.Type == System.Security.Claims.ClaimTypes.Role && claim.Value == user.Role.ToString());
        Assert.Contains(jwt.Claims, claim => claim.Type == "org_id" && claim.Value == user.OrganizationId.ToString());
        Assert.Contains(jwt.Claims, claim => claim.Type == "department_id" && claim.Value == user.DepartmentId!.Value.ToString());
        Assert.DoesNotContain(jwt.Claims, claim => claim.Value == user.PasswordHash);
    }
}
