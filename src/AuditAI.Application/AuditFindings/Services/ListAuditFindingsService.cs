using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.AuditFindings.Interfaces;
using AuditAI.Application.AuditFindings.Mappers;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using FluentValidation;

namespace AuditAI.Application.AuditFindings.Services;

public sealed class ListAuditFindingsService
{
    private readonly IAuditFindingRepository _auditFindingRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<AuditFindingQueryParameters> _validator;

    public ListAuditFindingsService(
        IAuditFindingRepository auditFindingRepository,
        ICurrentUser currentUser,
        IValidator<AuditFindingQueryParameters> validator)
    {
        _auditFindingRepository = auditFindingRepository;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<PagedResult<AuditFindingListItemResponse>>> ExecuteAsync(
        AuditFindingQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        if (!AuditFindingsCurrentUserContext.TryGetOrganization(_currentUser, out var organizationId))
        {
            return Result<PagedResult<AuditFindingListItemResponse>>.Unauthorized(AuditFindingsCurrentUserContext.UnauthorizedMessage);
        }

        var validationResult = await _validator.ValidateAsync(queryParameters, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<PagedResult<AuditFindingListItemResponse>>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var page = await _auditFindingRepository.ListAsync(organizationId, queryParameters, cancellationToken);
        var items = page.Items.Select(AuditFindingResponseMapper.ToListItem).ToArray();

        return Result<PagedResult<AuditFindingListItemResponse>>.Success(
            new PagedResult<AuditFindingListItemResponse>(items, page.TotalCount, page.PageNumber, page.PageSize));
    }
}
