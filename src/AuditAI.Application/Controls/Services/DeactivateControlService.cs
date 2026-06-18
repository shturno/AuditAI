using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Controls.Contracts;
using AuditAI.Application.Controls.Interfaces;
using AuditAI.Application.Controls.Mappers;

namespace AuditAI.Application.Controls.Services;

public sealed class DeactivateControlService
{
    private readonly IControlRepository _controlRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeactivateControlService(
        IControlRepository controlRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _controlRepository = controlRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<ControlResponse>> ExecuteAsync(
        Guid controlId,
        CancellationToken cancellationToken = default)
    {
        var control = await _controlRepository.GetByIdForUpdateAsync(controlId, cancellationToken);
        if (control is null)
        {
            return Result<ControlResponse>.NotFound("Control was not found.");
        }

        control.Deactivate(_dateTimeProvider.UtcNow);
        await _controlRepository.SaveChangesAsync(cancellationToken);

        return Result<ControlResponse>.Success(ControlResponseMapper.ToResponse(control));
    }
}
