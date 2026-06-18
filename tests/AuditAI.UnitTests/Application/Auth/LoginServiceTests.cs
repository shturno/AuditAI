using AuditAI.Application.Auth.Contracts;
using AuditAI.Application.Auth.Interfaces;
using AuditAI.Application.Auth.Services;
using AuditAI.Application.Auth.Validators;
using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;

namespace AuditAI.UnitTests.Application.Auth;

public sealed class LoginServiceTests
{
    [Fact]
    public async Task Should_FailLogin_When_EmailFormatIsInvalid()
    {
        var service = CreateService(user: null);

        var result = await service.ExecuteAsync(new LoginRequest
        {
            Email = "not-an-email",
            Password = "P@ssword123!"
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsValidationFailure);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "Email");
    }

    [Fact]
    public async Task Should_FailLogin_When_UserDoesNotExist()
    {
        var service = CreateService(user: null);

        var result = await service.ExecuteAsync(new LoginRequest
        {
            Email = "missing@auditai.test",
            Password = "P@ssword123!"
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsUnauthorized);
    }

    [Fact]
    public async Task Should_FailLogin_When_PasswordIsWrong()
    {
        var passwordHasher = new FakePasswordHasher();
        var user = CreateUser(passwordHasher.HashPassword("CorrectPassword123!"));
        var service = CreateService(user, passwordHasher);

        var result = await service.ExecuteAsync(new LoginRequest
        {
            Email = "submitter@auditai.test",
            Password = "WrongPassword123!"
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsUnauthorized);
    }

    [Fact]
    public async Task Should_SucceedLogin_When_CredentialsAreValid()
    {
        var passwordHasher = new FakePasswordHasher();
        var user = CreateUser(passwordHasher.HashPassword("CorrectPassword123!"));
        var service = CreateService(user, passwordHasher);

        var result = await service.ExecuteAsync(new LoginRequest
        {
            Email = "SUBMITTER@AUDITAI.TEST",
            Password = "CorrectPassword123!"
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("generated-token", result.Value!.AccessToken);
        Assert.Equal(user.Email, result.Value.User.Email);
    }

    [Fact]
    public void Should_NotExposePasswordHash_In_LoginResponse()
    {
        var response = new LoginResponse(
            "generated-token",
            new DateTimeOffset(2026, 06, 18, 12, 0, 0, TimeSpan.Zero),
            new AuthenticatedUserResponse(
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                "Audit User",
                "user@auditai.test",
                UserRole.Auditor));

        var json = System.Text.Json.JsonSerializer.Serialize(
            response,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
    }

    private static LoginService CreateService(
        User? user,
        IPasswordHasher? passwordHasher = null,
        IJwtTokenGenerator? jwtTokenGenerator = null)
    {
        return new LoginService(
            new FakeUserAuthRepository(user),
            passwordHasher ?? new FakePasswordHasher(),
            jwtTokenGenerator ?? new FakeJwtTokenGenerator(),
            new LoginRequestValidator());
    }

    private static User CreateUser(string passwordHash)
    {
        return User.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Evidence Submitter",
            "submitter@auditai.test",
            passwordHash,
            UserRole.Auditor,
            new DateTimeOffset(2026, 06, 18, 12, 0, 0, TimeSpan.Zero));
    }

    private sealed class FakeUserAuthRepository : IUserAuthRepository
    {
        private readonly User? _user;

        public FakeUserAuthRepository(User? user)
        {
            _user = user;
        }

        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            if (_user is not null && _user.Email == normalizedEmail)
            {
                return Task.FromResult<User?>(_user);
            }

            return Task.FromResult<User?>(null);
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            return $"HASH::{password}";
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            return passwordHash == HashPassword(password);
        }
    }

    private sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
    {
        public AuditAI.Application.Auth.Contracts.GeneratedJwtToken GenerateToken(User user)
        {
            return new("generated-token", new DateTimeOffset(2026, 06, 18, 13, 0, 0, TimeSpan.Zero));
        }
    }
}
