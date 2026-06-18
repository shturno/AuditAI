using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Controls.Contracts;
using AuditAI.Application.Controls.Interfaces;
using AuditAI.Application.Controls.Mappers;

namespace AuditAI.Application.Controls.Services;

public sealed class GetControlByIdService
{
    private readonly IControlRepository _controlRepository;
    private readonly ICurrentUser _currentUser;

    public GetControlByIdService(IControlRepository controlRepository, ICurrentUser currentUser)
    {
        _controlRepository = controlRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<ControlResponse>> ExecuteAsync(
        Guid controlId,
        CancellationToken cancellationToken = default)
    {
        if (!ControlsCurrentUserContext.TryGetOrganization(_currentUser, out var organizationId))
        {
            return Result<ControlResponse>.Unauthorized(ControlsCurrentUserContext.UnauthorizedMessage);
        }

        var control = await _controlRepository.GetByIdAsync(controlId, cancellationToken);
        if (control is null || control.OrganizationId != organizationId)
        {
            return Result<ControlResponse>.NotFound("Control was not found.");
        }

        return Result<ControlResponse>.Success(ControlResponseMapper.ToResponse(control));
    }
}
