using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Authorization;
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
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<EvidenceQueryParameters> _validator;

    public ListEvidenceService(
        IEvidenceRepository evidenceRepository,
        ICurrentUser currentUser,
        IValidator<EvidenceQueryParameters> validator)
    {
        _evidenceRepository = evidenceRepository;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<PagedResult<EvidenceListItemResponse>>> ExecuteAsync(
        EvidenceQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        if (!EvidenceCurrentUserContext.TryGetOrganization(_currentUser, out var organizationId))
        {
            return Result<PagedResult<EvidenceListItemResponse>>.Unauthorized(EvidenceCurrentUserContext.UnauthorizedMessage);
        }

        if (!RoleAuthorization.CanReadEvidence(_currentUser))
        {
            return Result<PagedResult<EvidenceListItemResponse>>.Forbidden(RoleAuthorization.EvidenceReadForbiddenMessage);
        }

        var validationResult = await _validator.ValidateAsync(queryParameters, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<PagedResult<EvidenceListItemResponse>>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var page = await _evidenceRepository.ListAsync(organizationId, queryParameters, cancellationToken);
        var items = page.Items.Select(EvidenceResponseMapper.ToListItem).ToArray();

        return Result<PagedResult<EvidenceListItemResponse>>.Success(
            new PagedResult<EvidenceListItemResponse>(items, page.TotalCount, page.PageNumber, page.PageSize));
    }
}
