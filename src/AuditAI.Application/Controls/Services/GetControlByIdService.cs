using AuditAI.Application.Common.Results;
using AuditAI.Application.Controls.Contracts;
using AuditAI.Application.Controls.Interfaces;
using AuditAI.Application.Controls.Mappers;

namespace AuditAI.Application.Controls.Services;

public sealed class GetControlByIdService
{
    private readonly IControlRepository _controlRepository;

    public GetControlByIdService(IControlRepository controlRepository)
    {
        _controlRepository = controlRepository;
    }

    public async Task<Result<ControlResponse>> ExecuteAsync(
        Guid controlId,
        CancellationToken cancellationToken = default)
    {
        var control = await _controlRepository.GetByIdAsync(controlId, cancellationToken);
        if (control is null)
        {
            return Result<ControlResponse>.NotFound("Control was not found.");
        }

        return Result<ControlResponse>.Success(ControlResponseMapper.ToResponse(control));
    }
}
