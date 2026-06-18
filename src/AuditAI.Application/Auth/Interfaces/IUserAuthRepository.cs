using AuditAI.Domain.Entities;

namespace AuditAI.Application.Auth.Interfaces;

public interface IUserAuthRepository
{
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
}
