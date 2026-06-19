using AuditAI.Application.ActionPlans.Contracts;
using AuditAI.Application.ActionPlans.Interfaces;
using AuditAI.Application.ActionPlans.Mappers;
using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.AuditLogs.Services;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Authorization;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using AuditAI.Domain.Exceptions;
using FluentValidation;

namespace AuditAI.Application.ActionPlans.Services;

public sealed class UpdateActionPlanService
{
    private readonly IActionPlanRepository _actionPlanRepository;
    private readonly IUserLookup _userLookup;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<UpdateActionPlanRequest> _validator;

    public UpdateActionPlanService(
        IActionPlanRepository actionPlanRepository,
        IUserLookup userLookup,
        IAuditLogWriter auditLogWriter,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IValidator<UpdateActionPlanRequest> validator)
    {
        _actionPlanRepository = actionPlanRepository;
        _userLookup = userLookup;
        _auditLogWriter = auditLogWriter;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<Result<ActionPlanResponse>> ExecuteAsync(
        Guid actionPlanId,
        UpdateActionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ActionPlansCurrentUserContext.TryGetActor(_currentUser, out var userId, out var organizationId))
        {
            return Result<ActionPlanResponse>.Unauthorized(ActionPlansCurrentUserContext.UnauthorizedMessage);
        }

        if (!RoleAuthorization.CanManageActionPlans(_currentUser))
        {
            return Result<ActionPlanResponse>.Forbidden(RoleAuthorization.ActionPlansManageForbiddenMessage);
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

        var assignedUserOrganizationId = await _userLookup.GetUserOrganizationIdAsync(request.AssignedToUserId, cancellationToken);
        if (!assignedUserOrganizationId.HasValue)
        {
            return Result<ActionPlanResponse>.ValidationFailure(
            [
                new ValidationError("AssignedToUserId", "Assigned user does not exist.")
            ]);
        }

        if (assignedUserOrganizationId.Value != organizationId)
        {
            return Result<ActionPlanResponse>.ValidationFailure(
            [
                new ValidationError("AssignedToUserId", "Assigned user must belong to the same organization as the action plan.")
            ]);
        }

        try
        {
            actionPlan.UpdateDetails(
                request.AssignedToUserId,
                request.Title,
                request.Description,
                request.DueDate,
                _dateTimeProvider.UtcNow);
        }
        catch (DomainRuleViolationException exception)
        {
            return Result<ActionPlanResponse>.ValidationFailure(
            [
                new ValidationError("DueDate", exception.Message)
            ]);
        }

        await _auditLogWriter.WriteAsync(
            new AuditLogWriteEntry(
                organizationId,
                userId,
                AuditAI.Domain.Enums.AuditLogAction.ActionPlanUpdated,
                nameof(AuditAI.Domain.Entities.ActionPlan),
                actionPlan.Id,
                AuditLogMetadata.Build(
                    ("status", actionPlan.Status.ToString()),
                    ("assignedToUserId", actionPlan.AssignedToUserId),
                    ("dueDate", actionPlan.DueDate))),
            cancellationToken);
        await _actionPlanRepository.SaveChangesAsync(cancellationToken);

        return Result<ActionPlanResponse>.Success(ActionPlanResponseMapper.ToResponse(actionPlan));
    }
}
