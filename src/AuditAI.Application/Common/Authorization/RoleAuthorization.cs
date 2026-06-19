using AuditAI.Application.Common.Abstractions;
using AuditAI.Domain.Enums;

namespace AuditAI.Application.Common.Authorization;

internal static class RoleAuthorization
{
    public const string ControlsReadForbiddenMessage = "The current user is not allowed to read controls.";
    public const string ControlsManageForbiddenMessage = "The current user is not allowed to manage controls.";
    public const string EvidenceReadForbiddenMessage = "The current user is not allowed to read evidence.";
    public const string EvidenceSubmitForbiddenMessage = "The current user is not allowed to submit evidence.";
    public const string EvidenceReviewForbiddenMessage = "The current user is not allowed to review evidence.";
    public const string AuditFindingsReadForbiddenMessage = "The current user is not allowed to read audit findings.";
    public const string AuditFindingsManageForbiddenMessage = "The current user is not allowed to manage audit findings.";
    public const string ActionPlansReadForbiddenMessage = "The current user is not allowed to read action plans.";
    public const string ActionPlansManageForbiddenMessage = "The current user is not allowed to manage action plans.";
    public const string AuditLogsReadForbiddenMessage = "The current user is not allowed to read audit logs.";

    public static bool CanReadControls(ICurrentUser currentUser) => HasAnyRole(currentUser, UserRole.Admin, UserRole.Auditor, UserRole.Reviewer);

    public static bool CanManageControls(ICurrentUser currentUser) => HasAnyRole(currentUser, UserRole.Admin, UserRole.Auditor);

    public static bool CanReadEvidence(ICurrentUser currentUser) => HasAnyRole(currentUser, UserRole.Admin, UserRole.Auditor, UserRole.Reviewer);

    public static bool CanSubmitEvidence(ICurrentUser currentUser) => HasAnyRole(currentUser, UserRole.Admin, UserRole.Auditor);

    public static bool CanReviewEvidence(ICurrentUser currentUser) => HasAnyRole(currentUser, UserRole.Admin, UserRole.Reviewer);

    public static bool CanReadAuditFindings(ICurrentUser currentUser) => HasAnyRole(currentUser, UserRole.Admin, UserRole.Auditor, UserRole.Reviewer);

    public static bool CanManageAuditFindings(ICurrentUser currentUser) => HasAnyRole(currentUser, UserRole.Admin, UserRole.Auditor);

    public static bool CanReadActionPlans(ICurrentUser currentUser) => HasAnyRole(currentUser, UserRole.Admin, UserRole.Auditor, UserRole.Reviewer);

    public static bool CanManageActionPlans(ICurrentUser currentUser) => HasAnyRole(currentUser, UserRole.Admin, UserRole.Auditor);

    public static bool CanReadAuditLogs(ICurrentUser currentUser) => HasAnyRole(currentUser, UserRole.Admin, UserRole.Auditor);

    private static bool HasAnyRole(ICurrentUser currentUser, params UserRole[] allowedRoles)
    {
        if (!currentUser.Role.HasValue)
        {
            return false;
        }

        return allowedRoles.Contains(currentUser.Role.Value);
    }
}
