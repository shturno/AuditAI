using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Controls.Contracts;
using AuditAI.Domain.Entities;

namespace AuditAI.Application.Controls.Interfaces;

public interface IControlRepository
{
    Task AddAsync(Control control, CancellationToken cancellationToken);

    Task<bool> ExistsWithCodeAsync(
        Guid organizationId,
        string code,
        Guid? excludedControlId,
        CancellationToken cancellationToken);

    Task<Control?> GetByIdAsync(Guid controlId, CancellationToken cancellationToken);

    Task<Control?> GetByIdForUpdateAsync(Guid controlId, CancellationToken cancellationToken);

    Task<PagedResult<Control>> ListAsync(
        ControlQueryParameters queryParameters,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
