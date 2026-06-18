using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.AuditLogs.Mappers;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using FluentValidation;

namespace AuditAI.Application.AuditLogs.Services;

public sealed class ListAuditLogsService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IValidator<AuditLogQueryParameters> _validator;

    public ListAuditLogsService(
        IAuditLogRepository auditLogRepository,
        IValidator<AuditLogQueryParameters> validator)
    {
        _auditLogRepository = auditLogRepository;
        _validator = validator;
    }

    public async Task<Result<PagedResult<AuditLogListItemResponse>>> ExecuteAsync(
        AuditLogQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(queryParameters, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<PagedResult<AuditLogListItemResponse>>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var page = await _auditLogRepository.ListAsync(queryParameters, cancellationToken);
        var items = page.Items.Select(AuditLogResponseMapper.ToListItem).ToArray();

        return Result<PagedResult<AuditLogListItemResponse>>.Success(
            new PagedResult<AuditLogListItemResponse>(items, page.TotalCount, page.PageNumber, page.PageSize));
    }
}
