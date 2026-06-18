namespace AuditAI.Application.Controls.Interfaces;

public interface IDepartmentLookup
{
    Task<bool> DepartmentExistsAsync(Guid departmentId, CancellationToken cancellationToken);

    Task<bool> DepartmentBelongsToOrganizationAsync(
        Guid departmentId,
        Guid organizationId,
        CancellationToken cancellationToken);
}
