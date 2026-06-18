using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using AuditAI.Application.Evidence.Contracts;
using AuditAI.Application.Evidence.Interfaces;
using AuditAI.Application.Evidence.Mappers;
using FluentValidation;

namespace AuditAI.Application.Evidence.Services;

public sealed class ListEvidenceService
{
    private readonly IEvidenceRepository _evidenceRepository;
    private readonly IValidator<EvidenceQueryParameters> _validator;

    public ListEvidenceService(
        IEvidenceRepository evidenceRepository,
        IValidator<EvidenceQueryParameters> validator)
    {
        _evidenceRepository = evidenceRepository;
        _validator = validator;
    }

    public async Task<Result<PagedResult<EvidenceListItemResponse>>> ExecuteAsync(
        EvidenceQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(queryParameters, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<PagedResult<EvidenceListItemResponse>>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var page = await _evidenceRepository.ListAsync(queryParameters, cancellationToken);
        var items = page.Items.Select(EvidenceResponseMapper.ToListItem).ToArray();

        return Result<PagedResult<EvidenceListItemResponse>>.Success(
            new PagedResult<EvidenceListItemResponse>(items, page.TotalCount, page.PageNumber, page.PageSize));
    }
}
