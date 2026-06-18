using AuditAI.Application.Auth.Contracts;
using AuditAI.Domain.Entities;

namespace AuditAI.Application.Auth.Interfaces;

public interface IJwtTokenGenerator
{
    GeneratedJwtToken GenerateToken(User user);
}
