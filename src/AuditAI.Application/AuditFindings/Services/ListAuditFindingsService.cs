using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.AuditFindings.Interfaces;
using AuditAI.Application.AuditFindings.Mappers;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using FluentValidation;

namespace AuditAI.Application.AuditFindings.Services;

public sealed class ListAuditFindingsService
{
    private readonly IAuditFindingRepository _auditFindingRepository;
    private readonly IValidator<AuditFindingQueryParameters> _validator;

    public ListAuditFindingsService(
        IAuditFindingRepository auditFindingRepository,
        IValidator<AuditFindingQueryParameters> validator)
    {
        _auditFindingRepository = auditFindingRepository;
        _validator = validator;
    }

    public async Task<Result<PagedResult<AuditFindingListItemResponse>>> ExecuteAsync(
        AuditFindingQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(queryParameters, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<PagedResult<AuditFindingListItemResponse>>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var page = await _auditFindingRepository.ListAsync(queryParameters, cancellationToken);
        var items = page.Items.Select(AuditFindingResponseMapper.ToListItem).ToArray();

        return Result<PagedResult<AuditFindingListItemResponse>>.Success(
            new PagedResult<AuditFindingListItemResponse>(items, page.TotalCount, page.PageNumber, page.PageSize));
    }
}
