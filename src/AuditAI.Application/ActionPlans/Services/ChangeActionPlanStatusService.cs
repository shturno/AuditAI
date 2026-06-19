using AuditAI.Application.ActionPlans.Contracts;
using AuditAI.Application.ActionPlans.Interfaces;
using AuditAI.Application.ActionPlans.Mappers;
using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.AuditLogs.Services;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using AuditAI.Domain.Enums;
using AuditAI.Domain.Exceptions;
using FluentValidation;

namespace AuditAI.Application.ActionPlans.Services;

public sealed class ChangeActionPlanStatusService
{
    private readonly IActionPlanRepository _actionPlanRepository;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<ChangeActionPlanStatusRequest> _validator;

    public ChangeActionPlanStatusService(
        IActionPlanRepository actionPlanRepository,
        IAuditLogWriter auditLogWriter,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IValidator<ChangeActionPlanStatusRequest> validator)
    {
        _actionPlanRepository = actionPlanRepository;
        _auditLogWriter = auditLogWriter;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<Result<ActionPlanResponse>> ExecuteAsync(
        Guid actionPlanId,
        ChangeActionPlanStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ActionPlansCurrentUserContext.TryGetActor(_currentUser, out var userId, out var organizationId))
        {
            return Result<ActionPlanResponse>.Unauthorized(ActionPlansCurrentUserContext.UnauthorizedMessage);
        }

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<ActionPlanResponse>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var actionPlan = await _actionPlanRepository.GetByIdForUpdateAsync(actionPlanId, organizationId, cancellationToken);
        if (actionPlan is null)
        {
            return Result<ActionPlanResponse>.NotFound("Action plan was not found.");
        }

        try
        {
            switch (request.Status)
            {
                case ActionPlanStatus.InProgress:
                    actionPlan.MarkInProgress(_dateTimeProvider.UtcNow);
                    break;
                case ActionPlanStatus.Completed:
                    actionPlan.Complete(_dateTimeProvider.UtcNow);
                    break;
                case ActionPlanStatus.Overdue:
                    actionPlan.MarkOverdue(_dateTimeProvider.UtcNow);
                    break;
                case ActionPlanStatus.Cancelled:
                    actionPlan.Cancel(_dateTimeProvider.UtcNow);
                    break;
                default:
                    return Result<ActionPlanResponse>.ValidationFailure(
                    [
                        new ValidationError("Status", "Unsupported status transition.")
                    ]);
            }
        }
        catch (DomainRuleViolationException exception)
        {
            return Result<ActionPlanResponse>.ValidationFailure(
            [
                new ValidationError("Status", exception.Message)
            ]);
        }

        await _auditLogWriter.WriteAsync(
            new AuditLogWriteEntry(
                organizationId,
                userId,
                AuditLogAction.ActionPlanStatusChanged,
                nameof(AuditAI.Domain.Entities.ActionPlan),
                actionPlan.Id,
                AuditLogMetadata.Build(
                    ("status", actionPlan.Status.ToString()),
                    ("dueDate", actionPlan.DueDate))),
            cancellationToken);
        await _actionPlanRepository.SaveChangesAsync(cancellationToken);

        return Result<ActionPlanResponse>.Success(ActionPlanResponseMapper.ToResponse(actionPlan));
    }
}
