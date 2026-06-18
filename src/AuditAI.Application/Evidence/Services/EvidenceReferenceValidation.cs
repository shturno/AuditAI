using AuditAI.Application.Common.Results;
using AuditAI.Application.Evidence.Interfaces;

namespace AuditAI.Application.Evidence.Services;

internal static class EvidenceReferenceValidation
{
    public static async Task<IReadOnlyList<ValidationError>> ValidateCreateAsync(
        Guid controlId,
        Guid submittedByUserId,
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

        var submitterOrganizationId = await userLookup.GetUserOrganizationIdAsync(submittedByUserId, cancellationToken);
        if (!submitterOrganizationId.HasValue)
        {
            errors.Add(new ValidationError("SubmittedByUserId", "Submitter user does not exist."));
            return errors;
        }

        if (submitterOrganizationId.Value != controlOrganizationId.Value)
        {
            errors.Add(new ValidationError(
                "SubmittedByUserId",
                "Submitter user must belong to the same organization as the control."));
        }

        return errors;
    }

    public static async Task<IReadOnlyList<ValidationError>> ValidateReviewerAsync(
        Guid controlId,
        Guid reviewerUserId,
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

        var reviewerOrganizationId = await userLookup.GetUserOrganizationIdAsync(reviewerUserId, cancellationToken);
        if (!reviewerOrganizationId.HasValue)
        {
            errors.Add(new ValidationError("ReviewerUserId", "Reviewer user does not exist."));
            return errors;
        }

        if (reviewerOrganizationId.Value != controlOrganizationId.Value)
        {
            errors.Add(new ValidationError(
                "ReviewerUserId",
                "Reviewer user must belong to the same organization as the control."));
        }

        return errors;
    }
}
