using AuditAI.Application.Auth.Interfaces;
using AuditAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuditAI.Infrastructure.Persistence.Repositories;

internal sealed class UserAuthRepository : IUserAuthRepository
{
    private readonly AuditAIDbContext _dbContext;

    public UserAuthRepository(AuditAIDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.Email == normalizedEmail,
                cancellationToken);
    }
}
