using AuditAI.Application.Common.Results;
using AuditAI.Application.Controls.Interfaces;

namespace AuditAI.Application.Controls.Services;

internal static class ControlOrganizationValidation
{
    public static async Task<IReadOnlyList<ValidationError>> ValidateAsync(
        Guid organizationId,
        Guid? departmentId,
        IOrganizationLookup organizationLookup,
        IDepartmentLookup departmentLookup,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (!await organizationLookup.OrganizationExistsAsync(organizationId, cancellationToken))
        {
            errors.Add(new ValidationError("OrganizationId", "Organization does not exist."));
        }

        if (!departmentId.HasValue)
        {
            return errors;
        }

        if (!await departmentLookup.DepartmentExistsAsync(departmentId.Value, cancellationToken))
        {
            errors.Add(new ValidationError("DepartmentId", "Department does not exist."));
            return errors;
        }

        if (!await departmentLookup.DepartmentBelongsToOrganizationAsync(
                departmentId.Value,
                organizationId,
                cancellationToken))
        {
            errors.Add(new ValidationError(
                "DepartmentId",
                "Department must belong to the same organization as the control."));
        }

        return errors;
    }
}
