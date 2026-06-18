using AuditAI.Infrastructure.Auth.PasswordHashing;

namespace AuditAI.UnitTests.Infrastructure.Auth;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Should_VerifyValidPassword()
    {
        var passwordHasher = new AspNetPasswordHasher();
        var hash = passwordHasher.HashPassword("P@ssword123!");

        var result = passwordHasher.VerifyPassword("P@ssword123!", hash);

        Assert.True(result);
    }

    [Fact]
    public void Should_RejectInvalidPassword()
    {
        var passwordHasher = new AspNetPasswordHasher();
        var hash = passwordHasher.HashPassword("P@ssword123!");

        var result = passwordHasher.VerifyPassword("WrongPassword123!", hash);

        Assert.False(result);
    }
}
