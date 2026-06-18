using AuditAI.Application.Auth.Contracts;
using AuditAI.Application.Auth.Interfaces;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using FluentValidation;

namespace AuditAI.Application.Auth.Services;

public sealed class LoginService
{
    private const string InvalidCredentialsMessage = "Invalid credentials.";

    private readonly IUserAuthRepository _userAuthRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IValidator<LoginRequest> _validator;

    public LoginService(
        IUserAuthRepository userAuthRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IValidator<LoginRequest> validator)
    {
        _userAuthRepository = userAuthRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _validator = validator;
    }

    public async Task<Result<LoginResponse>> ExecuteAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<LoginResponse>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userAuthRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
        {
            return Result<LoginResponse>.Unauthorized(InvalidCredentialsMessage);
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result<LoginResponse>.Unauthorized(InvalidCredentialsMessage);
        }

        var generatedToken = _jwtTokenGenerator.GenerateToken(user);

        return Result<LoginResponse>.Success(
            new LoginResponse(
                generatedToken.AccessToken,
                generatedToken.ExpiresAt,
                new AuthenticatedUserResponse(
                    user.Id,
                    user.OrganizationId,
                    user.DepartmentId,
                    user.FullName,
                    user.Email,
                    user.Role)));
    }
}
