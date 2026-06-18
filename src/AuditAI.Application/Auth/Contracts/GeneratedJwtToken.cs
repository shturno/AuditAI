namespace AuditAI.Application.Auth.Contracts;

public sealed record GeneratedJwtToken(
    string AccessToken,
    DateTimeOffset ExpiresAt);
