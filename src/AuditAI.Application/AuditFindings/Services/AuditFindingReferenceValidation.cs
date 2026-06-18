using AuditAI.Application.Common.Results;
using AuditAI.Application.Evidence.Interfaces;

namespace AuditAI.Application.AuditFindings.Services;

internal static class AuditFindingReferenceValidation
{
    public static async Task<IReadOnlyList<ValidationError>> ValidateCreateAsync(
        Guid controlId,
        Guid createdByUserId,
        IControlLookup controlLookup,
        IUserLookup userLookup,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        var controlOrganizationId = await controlLookup.GetControlOrganizationIdAsync(controlId, cancellationToken);
        if (!controlOrganizationId.HasValue)
        {
            errors.Add(new ValidationError("ControlId", "Control does not exist."));
            return errors;
        }

        var creatorOrganizationId = await userLookup.GetUserOrganizationIdAsync(createdByUserId, cancellationToken);
        if (!creatorOrganizationId.HasValue)
        {
            errors.Add(new ValidationError("CreatedByUserId", "Creator user does not exist."));
            return errors;
        }

        if (creatorOrganizationId.Value != controlOrganizationId.Value)
        {
            errors.Add(new ValidationError(
                "CreatedByUserId",
                "Creator user must belong to the same organization as the control."));
        }

        return errors;
    }
}
