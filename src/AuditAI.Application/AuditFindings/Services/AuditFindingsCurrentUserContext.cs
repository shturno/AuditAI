using AuditAI.Application.Common.Abstractions;

namespace AuditAI.Application.AuditFindings.Services;

internal static class AuditFindingsCurrentUserContext
{
    public const string UnauthorizedMessage = "An authenticated user context is required.";

    public static bool TryGetOrganization(ICurrentUser currentUser, out Guid organizationId)
    {
        if (currentUser.OrganizationId.HasValue)
        {
            organizationId = currentUser.OrganizationId.Value;
            return true;
        }

        organizationId = Guid.Empty;
        return false;
    }

    public static bool TryGetActor(ICurrentUser currentUser, out Guid userId, out Guid organizationId)
    {
        if (currentUser.UserId.HasValue &&
            currentUser.OrganizationId.HasValue)
        {
            userId = currentUser.UserId.Value;
            organizationId = currentUser.OrganizationId.Value;
            return true;
        }

        userId = Guid.Empty;
        organizationId = Guid.Empty;
        return false;
    }
}
