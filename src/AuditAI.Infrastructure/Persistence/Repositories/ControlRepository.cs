using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Controls.Contracts;
using AuditAI.Application.Controls.Interfaces;
using AuditAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using AuditControl = AuditAI.Domain.Entities.Control;

namespace AuditAI.Infrastructure.Persistence.Repositories;

internal sealed class ControlRepository : IControlRepository
{
    private readonly AuditAIDbContext _dbContext;

    public ControlRepository(AuditAIDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AuditControl control, CancellationToken cancellationToken)
    {
        await _dbContext.Controls.AddAsync(control, cancellationToken);
    }

    public async Task<bool> ExistsWithCodeAsync(
        Guid organizationId,
        string code,
        Guid? excludedControlId,
        CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim();

        return await _dbContext.Controls
            .AsNoTracking()
            .Where(control => control.OrganizationId == organizationId && control.Code == normalizedCode)
            .Where(control => !excludedControlId.HasValue || control.Id != excludedControlId.Value)
            .AnyAsync(cancellationToken);
    }

    public async Task<AuditControl?> GetByIdAsync(Guid controlId, CancellationToken cancellationToken)
    {
        return await _dbContext.Controls
            .AsNoTracking()
            .SingleOrDefaultAsync(control => control.Id == controlId, cancellationToken);
    }

    public async Task<AuditControl?> GetByIdForUpdateAsync(Guid controlId, CancellationToken cancellationToken)
    {
        return await _dbContext.Controls
            .SingleOrDefaultAsync(control => control.Id == controlId, cancellationToken);
    }

    public async Task<PagedResult<AuditControl>> ListAsync(
        ControlQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Controls
            .AsNoTracking()
            .AsQueryable();

        if (queryParameters.OrganizationId.HasValue)
        {
            query = query.Where(control => control.OrganizationId == queryParameters.OrganizationId.Value);
        }

        if (queryParameters.DepartmentId.HasValue)
        {
            query = query.Where(control => control.DepartmentId == queryParameters.DepartmentId.Value);
        }

        if (queryParameters.Status.HasValue)
        {
            query = query.Where(control => control.Status == queryParameters.Status.Value);
        }

        if (queryParameters.Frequency.HasValue)
        {
            query = query.Where(control => control.Frequency == queryParameters.Frequency.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.SearchTerm))
        {
            var searchTerm = queryParameters.SearchTerm.Trim().ToLower();
            query = query.Where(control =>
                control.Code.ToLower().Contains(searchTerm) ||
                control.Category.ToLower().Contains(searchTerm) ||
                control.Title.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(control => control.CreatedAt)
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<AuditControl>(
            items,
            totalCount,
            queryParameters.PageNumber,
            queryParameters.PageSize);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
