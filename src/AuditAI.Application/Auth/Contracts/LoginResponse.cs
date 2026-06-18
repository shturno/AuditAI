namespace AuditAI.Application.Auth.Contracts;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    AuthenticatedUserResponse User);
