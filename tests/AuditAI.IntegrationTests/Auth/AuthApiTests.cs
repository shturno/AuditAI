using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using AuditAI.Application.Auth.Contracts;
using AuditAI.Application.Controls.Contracts;
using AuditAI.Domain.Enums;
using AuditAI.IntegrationTests.Infrastructure;

namespace AuditAI.IntegrationTests.Auth;

[Collection(IntegrationTestCollection.Name)]
public sealed class AuthApiTests
{
    private readonly PostgreSqlContainerFixture _fixture;

    public AuthApiTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_LoginEmailFormatIsInvalid()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "not-an-email",
            Password = "P@ssword123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_UserIsUnknown()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "missing@auditai.test",
            Password = "P@ssword123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_PasswordIsWrong()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = TestData.UserEmail,
            Password = "WrongPassword123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnOk_When_CredentialsAreValid()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = TestData.UserEmail,
            Password = TestData.UserPassword
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResponse);
        Assert.False(string.IsNullOrWhiteSpace(loginResponse.AccessToken));
        Assert.True(loginResponse.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.Equal(TestData.UserId, loginResponse.User.Id);

        var rawJson = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", rawJson, StringComparison.OrdinalIgnoreCase);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(loginResponse.AccessToken);
        Assert.Equal(TestData.UserId.ToString(), token.Subject);
        Assert.Contains(token.Claims, claim => claim.Type == JwtRegisteredClaimNames.Email && claim.Value == TestData.UserEmail);
        Assert.Contains(token.Claims, claim => claim.Type == System.Security.Claims.ClaimTypes.Role && claim.Value == UserRole.Auditor.ToString());
        Assert.Contains(token.Claims, claim => claim.Type == "org_id" && claim.Value == TestData.OrganizationId.ToString());
        Assert.Contains(token.Claims, claim => claim.Type == "department_id" && claim.Value == TestData.DepartmentId.ToString());
    }

    [Fact]
    public async Task Should_KeepExistingBusinessEndpoint_Anonymous_ForNow()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.GetAsync("/api/audit-logs?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
