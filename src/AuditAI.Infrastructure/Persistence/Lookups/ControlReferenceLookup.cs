using AuditAI.Application.Controls.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuditAI.Infrastructure.Persistence.Lookups;

internal sealed class ControlReferenceLookup : IOrganizationLookup, IDepartmentLookup
{
    private readonly AuditAIDbContext _dbContext;

    public ControlReferenceLookup(AuditAIDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return await _dbContext.Organizations
            .AsNoTracking()
            .AnyAsync(organization => organization.Id == organizationId, cancellationToken);
    }

    public async Task<bool> DepartmentExistsAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        return await _dbContext.Departments
            .AsNoTracking()
            .AnyAsync(department => department.Id == departmentId, cancellationToken);
    }

    public async Task<bool> DepartmentBelongsToOrganizationAsync(
        Guid departmentId,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Departments
            .AsNoTracking()
            .AnyAsync(
                department => department.Id == departmentId && department.OrganizationId == organizationId,
                cancellationToken);
    }
}
