using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.AuditLogs.Mappers;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Authorization;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using FluentValidation;

namespace AuditAI.Application.AuditLogs.Services;

public sealed class ListAuditLogsService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<AuditLogQueryParameters> _validator;

    public ListAuditLogsService(
        IAuditLogRepository auditLogRepository,
        ICurrentUser currentUser,
        IValidator<AuditLogQueryParameters> validator)
    {
        _auditLogRepository = auditLogRepository;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<PagedResult<AuditLogListItemResponse>>> ExecuteAsync(
        AuditLogQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.OrganizationId.HasValue)
        {
            return Result<PagedResult<AuditLogListItemResponse>>.Unauthorized("An authenticated user context is required.");
        }

        if (!RoleAuthorization.CanReadAuditLogs(_currentUser))
        {
            return Result<PagedResult<AuditLogListItemResponse>>.Forbidden(RoleAuthorization.AuditLogsReadForbiddenMessage);
        }

        var validationResult = await _validator.ValidateAsync(queryParameters, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<PagedResult<AuditLogListItemResponse>>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var scopedQueryParameters = new AuditLogQueryParameters
        {
            PageNumber = queryParameters.PageNumber,
            PageSize = queryParameters.PageSize,
            OrganizationId = _currentUser.OrganizationId.Value,
            UserId = queryParameters.UserId,
            EntityName = queryParameters.EntityName,
            EntityId = queryParameters.EntityId,
            Action = queryParameters.Action,
            From = queryParameters.From,
            To = queryParameters.To
        };

        var page = await _auditLogRepository.ListAsync(_currentUser.OrganizationId.Value, scopedQueryParameters, cancellationToken);
        var items = page.Items.Select(AuditLogResponseMapper.ToListItem).ToArray();

        return Result<PagedResult<AuditLogListItemResponse>>.Success(
            new PagedResult<AuditLogListItemResponse>(items, page.TotalCount, page.PageNumber, page.PageSize));
    }
}
