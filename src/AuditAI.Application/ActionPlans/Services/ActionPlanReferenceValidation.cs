using AuditAI.Application.ActionPlans.Interfaces;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Evidence.Interfaces;

namespace AuditAI.Application.ActionPlans.Services;

internal static class ActionPlanReferenceValidation
{
    public static async Task<IReadOnlyList<ValidationError>> ValidateAsync(
        Guid auditFindingId,
        Guid assignedToUserId,
        IAuditFindingLookup auditFindingLookup,
        IUserLookup userLookup,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        var findingOrganizationId = await auditFindingLookup.GetFindingOrganizationIdAsync(auditFindingId, cancellationToken);
        if (!findingOrganizationId.HasValue)
        {
            errors.Add(new ValidationError("AuditFindingId", "Audit finding does not exist."));
            return errors;
        }

        var assignedUserOrganizationId = await userLookup.GetUserOrganizationIdAsync(assignedToUserId, cancellationToken);
        if (!assignedUserOrganizationId.HasValue)
        {
            errors.Add(new ValidationError("AssignedToUserId", "Assigned user does not exist."));
            return errors;
        }

        if (assignedUserOrganizationId.Value != findingOrganizationId.Value)
        {
            errors.Add(new ValidationError(
                "AssignedToUserId",
                "Assigned user must belong to the same organization as the audit finding."));
        }

        return errors;
    }
}
